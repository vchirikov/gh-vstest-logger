using System.Text;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Octokit;
using TestPlatform.Extensions.GitHubLogger;

using static TestPlatform.Extension.GitHubLogger.GitHubApi;

namespace TestPlatform.Extension.GitHubLogger;

[FriendlyName(FriendlyName)]
[ExtensionUri(ExtensionUri)]
public class Logger : ITestLoggerWithParameters
{
    /// <summary> Uri used to uniquely identify the logger </summary>
    public const string ExtensionUri = $"logger://vchirikov/gh-vstest-logger/{ThisAssembly.ApiVersion}";

    /// <summary> Friendly name which uniquely identifies the logger </summary>
    public const string FriendlyName = "github";

    private LoggerParameters _params = null!;
    private GitHubApi _api = null!;
    private TestRunStatus _status = TestRunStatus.NotRunning;
    private Task _initializationTask = Task.FromException(new ("Lifetime error"));
    private CheckRun _checkRun = null!;
    private readonly SemaphoreSlim _locker = new(1,1);
    private readonly TimeSpan _timeout = TimeSpan.FromSeconds(120);
    private readonly ConcurrentDictionary<TestResult, Task> _testResults = new();

    public void Initialize(TestLoggerEvents events, Dictionary<string, string> parameters)
    {
        _params = LoggerParameters.Create(parameters);
        _api = new GitHubApi(_params);
        if (!_api.IsGitHubActions())
        {
            Warning("This isn't a GitHub Actions environment. Logger won't do anything. You can force it with 'CI=1;GITHUB_ACTIONS=1' parameters or env variables.");
            return;
        }
        if (string.IsNullOrEmpty(_params.GITHUB_TOKEN))
        {
            Warning("Can't see GITHUB_TOKEN env variable or parameter. Logger won't do anything.");
            return;
        }

        if (_params.GITHUB_TOKEN.Length != 40)
        {
            Warning("GITHUB_TOKEN is an unsupported token (length != 40). Logger won't do anything.");
            return;
        }

        events.TestRunStart += (_, args) => OnTestRunStart(args.TestRunCriteria);
        events.TestResult += (_, args) => OnTestResult(args.Result);
        events.TestRunComplete += (_, args) => OnTestRunComplete(args);
    }


    /// <summary> Raised when a test run starts. </summary>
    private void OnTestRunStart(TestRunCriteria _)
    {
        if (_status == TestRunStatus.Started)
        {
            const string errMsg = "Something went wrong, TestRunStart was already called.";
            Error(errMsg);
            throw new(errMsg);
        }
        _initializationTask = Task.Run(async () => {
            try
            {
                await Init().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Error($"Exception: {ex.Message}");
                using var _ = Block("Exception info");
                Error(ex.ToString());
            }
        });

        async Task Init()
        {
            if (!await _locker.WaitAsync(_timeout).ConfigureAwait(false))
                throw new TimeoutException($"{nameof(OnTestRunStart)}: Waiting for the lock is too long");
            try
            {
                _checkRun = await _api.CreateCheckRunAsync().ConfigureAwait(false);
                _status = TestRunStatus.Started;
            }
            finally
            {
                _locker.Release();
            }
        }
    }

    /// <summary> Raised when a test result is received. </summary>
    private void OnTestResult(TestResult result)
    {
        // annotate only failed tests
        if (result.Outcome != TestOutcome.Failed && result.Outcome != TestOutcome.NotFound)
        {
            if (!_testResults.TryAdd(result, Task.CompletedTask))
                Error($"Dictionary already contains the testResult for {result.DisplayName}");
            return;
        }
        var task = Task.Run(async () => {
            try
            {
                await OnTestResultInternalAsync(result).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Error($"Exception: {ex.Message}");
                using var _ = Block("Exception info");
                Error(ex.ToString());
            }
        });

        if (!_testResults.TryAdd(result, task))
            Error($"Dictionary already contains the testResult for {result.DisplayName}");

        async Task OnTestResultInternalAsync(TestResult result)
        {
            if (_status != TestRunStatus.Started)
                await _initializationTask.ConfigureAwait(false);

            if (!await _locker.WaitAsync(_timeout).ConfigureAwait(false))
                throw new TimeoutException($"{nameof(OnTestResult)}: Waiting for the lock is too long");

            try
            {
                if (_status != TestRunStatus.Started)
                    throw new($"Unexpected TestRun status: {_status}");

                var stackTraces = StackTraceParser.Parse(result.ErrorStackTrace, (f, t, m, pl, ps, fn, ln) => new
                {
                    Frame         = f,
                    Type          = t,
                    Method        = m,
                    ParameterList = pl,
                    Parameters    = ps,
                    File          = fn,
                    Line          = ln,
                });
                var sb = new StringBuilder(1024);
                foreach (var st in stackTraces)
                {
                    if (!int.TryParse(st.Line, NumberStyles.Integer, CultureInfo.InvariantCulture, out int line))
                        Warning("Can't parse line in stacktrace");

                    var annotation = new NewCheckRunAnnotation(
                        WorkspaceRelativePath(st.File),
                        line,
                        line + 5,
                        CheckAnnotationLevel.Failure,
                        (string.IsNullOrWhiteSpace(st.Method) ? "" : $"{st.Method}(): ") + result.ErrorMessage
                    ){
                        Title = result.DisplayName,
                        RawDetails = GetDetailsMessage(result, sb),
                    };

                    var update = new CheckRunUpdate() {
                        Output = new NewCheckRunOutput(_params.name, "Running..." ) {
                            Annotations = new List<NewCheckRunAnnotation>() { annotation },
                        },
                        Status = CheckStatus.InProgress,
                    };

                    _checkRun = await _api.UpdateCheckRunAsync(_checkRun, update).ConfigureAwait(false);
                }
            }
            finally
            {
                _locker.Release();
            }
        }
        string WorkspaceRelativePath(string fullPath) => string.IsNullOrEmpty(_params.GITHUB_WORKSPACE)
            ? fullPath
            : fullPath.Replace(_params.GITHUB_WORKSPACE, "", StringComparison.Ordinal).TrimStart('\\', '/');

        static string GetDetailsMessage(TestResult result, StringBuilder sb)
        {
            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                sb.AppendLine("[Error Message]");
                sb.AppendLine(result.ErrorMessage);
            }

            if (!string.IsNullOrEmpty(result.ErrorStackTrace))
            {
                sb.AppendLine("[Stack Trace]");
                sb.AppendLine(result.ErrorStackTrace);
            }

            if (result.Messages.Count > 0)
            {
                sb.AppendLine("[Output]");
                foreach (TestResultMessage message in result.Messages)
                {
                    if (message.Category.Equals(TestResultMessage.StandardErrorCategory, StringComparison.OrdinalIgnoreCase))
                        sb.Append("[stderr]: ");
                    else if (message.Category.Equals(TestResultMessage.DebugTraceCategory, StringComparison.OrdinalIgnoreCase))
                        sb.Append("[dbg]: ");
                    else if (message.Category.Equals(TestResultMessage.AdditionalInfoCategory, StringComparison.OrdinalIgnoreCase))
                        sb.Append("[info]: ");
                    sb.AppendLine(message.Text);
                }
            }
            var str = sb.ToString();
            sb.Clear();
            return str;
        }
    }

    /// <summary> Raised when a test run is complete. </summary>
    private void OnTestRunComplete(TestRunCompleteEventArgs results)
    {
        try
        {
            OnTestRunCompleteInternalAsync(results).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Error($"Exception: {ex.Message}");
            using var _ = Block("Exception info");
            Error(ex.ToString());
        }

        async Task OnTestRunCompleteInternalAsync(TestRunCompleteEventArgs results)
        {
            CheckConclusion conclusion = CheckConclusion.Neutral;
            string summary = "";
            try
            {
                // just in case
                await Task.WhenAll(_testResults.Values.ToArray()).ConfigureAwait(false);
                var sb = new StringBuilder(1024);
                if (results.TestRunStatistics.Stats.Any(stat => stat.Key == TestOutcome.Failed && stat.Value > 0))
                {
                    conclusion = CheckConclusion.Failure;
                    sb.AppendLine(":red_circle: Test run is failed.");
                }
                else
                {
                    conclusion = CheckConclusion.Success;
                    sb.AppendLine(":green_circle: Test run is successful.");
                }

                if (results.IsAborted || results.IsCanceled)
                {
                    conclusion = CheckConclusion.Cancelled;
                    sb.AppendLine(":green_circle: Test run is cancelled.");
                }

                sb.Append("Total tests: ").Append(results.TestRunStatistics.ExecutedTests).AppendLine();

                foreach (var stat in results.TestRunStatistics.Stats)
                {
                    sb.Append(" - ").Append(stat.Key switch {
                        TestOutcome.None => ":question:",
                        TestOutcome.Passed => ":heavy_check_mark:",
                        TestOutcome.Failed => ":heavy_multiplication_x:",
                        TestOutcome.Skipped => ":large_orange_diamond:",
                        TestOutcome.NotFound => ":skull_and_crossbones:",
                        _ => ":skull:"
                    }).Append(' ').Append(stat.Key).Append(": ").Append(stat.Value).AppendLine();
                }
                sb.AppendLine();
                sb.Append("Total time: ").AppendLine(FormatTimeSpan(results.ElapsedTimeInRunningTests));
                summary = sb.ToString();
                var update = new CheckRunUpdate() {
                    Output = new NewCheckRunOutput(_params.name, summary),
                    Status = CheckStatus.Completed,
                    Conclusion = conclusion,
                };
                _checkRun = await _api.UpdateCheckRunAsync(_checkRun, update).ConfigureAwait(false);
            }
            finally
            {
                OutputVariable("conclusion", conclusion.ToString());
                OutputVariable("summary", summary);
                if (await _locker.WaitAsync(_timeout).ConfigureAwait(false))
                {
                    _status = TestRunStatus.Finished;
                    _locker.Release();
                }
                else
                {
                    Error("Waiting for the lock is too long");
                }
            }
        }

        // bad omnisharp auto-formatting (?)
        static string FormatTimeSpan(TimeSpan timeSpan)
            => timeSpan switch { { TotalDays: var days } when days >= 1 => Invariant($"{timeSpan.TotalDays:0.0000} days"), { TotalHours: var hours } when hours >= 1 => Invariant($"{timeSpan.TotalHours:0.0000} hours"), { TotalMinutes: var min } when min >= 1 => Invariant($"{timeSpan.TotalMinutes:0.0000} minutes"), { TotalSeconds: var sec } => Invariant($"{timeSpan.TotalSeconds:0.0000} seconds") };
    }

    public void Initialize(TestLoggerEvents events, string testRunDirectory)
            => Initialize(events, new Dictionary<string, string>(StringComparer.Ordinal) { { DefaultLoggerParameterNames.TestRunDirectory, testRunDirectory } });

    private enum TestRunStatus
    {
        NotRunning = 0,
        Started = 1,
        Finished = 2,
    }
}
