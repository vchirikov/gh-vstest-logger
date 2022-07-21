using System.Text;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;

namespace GitHub.VsTest.Logger;

[FriendlyName(FriendlyName)]
[ExtensionUri(ExtensionUri)]
public class GitHubLogger : ITestLoggerWithParameters
{
    /// <summary> Uri used to uniquely identify the logger </summary>
    public const string ExtensionUri = $"logger://vchirikov/gh-vstest-logger/{ThisAssembly.ApiVersion}";

    /// <summary> Friendly name which uniquely identifies the logger </summary>
    public const string FriendlyName = "github";

    private LoggerParameters _params = null!;
    private GitHubApi _gh = null!;
    private TestRunStatus _status = TestRunStatus.NotRunning;
    private readonly SemaphoreSlim _locker = new(1,1);
    private readonly TimeSpan _timeout = TimeSpan.FromSeconds(120);
    private readonly ConcurrentDictionary<TestResult, Task> _testResults = new();
    private IDisposable? _block;

    public void Initialize(TestLoggerEvents events, Dictionary<string, string> parameters)
    {
        _params = LoggerParameters.Create(parameters);
        _gh = new GitHubApi(_params, new ConsoleOutput());
        _gh.Echo(_params.echo.asBool());
        if (!_gh.IsGitHubActions())
        {
            _gh.Warning("This isn't a GitHub Actions environment. Logger won't do anything. You can force it with 'CI=1;GITHUB_ACTIONS=1' parameters or env variables.");
            return;
        }
        if (!string.IsNullOrEmpty(_params.GITHUB_TOKEN) && _params.GITHUB_TOKEN.Length != 40)
        {
            _gh.Warning("GITHUB_TOKEN is an unsupported token (length != 40). Logger won't do anything.");
            return;
        }
        events.TestRunStart += (_, args) => OnTestRunStart(args.TestRunCriteria);
        events.TestResult += (_, args) => OnTestResult(args.Result);
        events.TestRunComplete += (_, args) => OnTestRunComplete(args);
    }

    /// <summary> Raised when a test run starts. </summary>
    private void OnTestRunStart(TestRunCriteria testRunCriteria)
    {
        try
        {
            if (_status == TestRunStatus.Started)
                throw new($"Something went wrong, {nameof(OnTestRunStart)} was already called.");

            if (!_locker.Wait(_timeout))
                throw new TimeoutException($"{nameof(OnTestRunStart)}: Waiting for the lock is too long");

            try
            {
                _status = TestRunStatus.Started;
                _block = _gh.Block($"dotnet test{(string.IsNullOrEmpty(testRunCriteria.TestCaseFilter) ? "" : " --filter:" + testRunCriteria.TestCaseFilter)} / {_params.name}");
            }
            finally
            {
                _locker.Release();
            }
        }
        catch (Exception ex)
        {
            _gh.Error($"Exception: {ex.Message}");
            using var _ = _gh.Block("Exception info");
            _gh.Error(ex.ToString());
        }
    }

    /// <summary> Raised when a test result is received. </summary>
    private void OnTestResult(TestResult result)
    {
        if (_status != TestRunStatus.Started)
            throw new($"Unexpected test run status: '{_status}'.");

        // annotate only failed tests
        if (result.Outcome != TestOutcome.Failed && result.Outcome != TestOutcome.NotFound)
        {
            if (!_testResults.TryAdd(result, Task.CompletedTask))
                _gh.Error($"Dictionary already contains the testResult for {result.DisplayName}");
            return;
        }
        var task = Task.Run(async () => {
            try
            {
                await OnTestResultInternalAsync(result).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _gh.Error($"Exception: {ex.Message}");
                using var _ = _gh.Block("Exception info");
                _gh.Error(ex.ToString());
            }
        });

        if (!_testResults.TryAdd(result, task))
            _gh.Error($"Dictionary already contains the testResult for {result.DisplayName}");

        // could be sync, but for the future use let it be async (for file io/rest calls)
        async Task OnTestResultInternalAsync(TestResult result)
        {
            #warning WARNING EXAMPLE
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
                just build errror
                var sb = new StringBuilder(1024);
                foreach (var st in stackTraces)
                {
                    if (!int.TryParse(st.Line, NumberStyles.Integer, CultureInfo.InvariantCulture, out int line))
                        _gh.Warning("Can't parse line in stacktrace");

                    _gh.Error(
                        message: GetDetailsMessage(result, sb),
                        title: $"{result.TestCase.DisplayName}",
                        file: WorkspaceRelativePath(st.File),
                        line: line > 5 ? line - 5 : line,
                        endLine: line + 1
                    );
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
            if (_status != TestRunStatus.Started)
                throw new($"Unexpected test run status: '{_status}'.");
            OnTestRunCompleteInternalAsync(results).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _gh.Error($"Exception: {ex.Message}");
            using var _ = _gh.Block("Exception info");
            _gh.Error(ex.ToString());
        }

        // could be sync, but for the future use let it be async (for file io/rest calls)
        async Task OnTestRunCompleteInternalAsync(TestRunCompleteEventArgs results)
        {
            string summary = "";
            try
            {
                await Task.WhenAll(_testResults.Values.ToArray()).ConfigureAwait(false);
                var sb = new StringBuilder(1024);

                sb.Append("**Total tests**: ").Append(results.TestRunStatistics.ExecutedTests).AppendLine("  ");

                foreach (var stat in results.TestRunStatistics.Stats)
                {
                    sb.Append(stat.Key switch {
                        TestOutcome.None => ":question:",
                        TestOutcome.Passed => ":white_check_mark:",
                        TestOutcome.Failed => ":x:",
                        TestOutcome.Skipped => ":brown_circle:",
                        TestOutcome.NotFound => ":skull_and_crossbones:",
                        _ => ":skull:"
                    }).Append(" **").Append(stat.Key).Append("**: ").Append(stat.Value).AppendLine();
                }
                sb.AppendLine();
                sb.Append("⌚ **Total time**: ").AppendLine(FormatTimeSpan(results.ElapsedTimeInRunningTests));
                summary = sb.ToString();
            }
            finally
            {
                _block?.Dispose();
                _gh.OutputVariable("summary", summary);
                if (await _locker.WaitAsync(_timeout).ConfigureAwait(false))
                {
                    _status = TestRunStatus.Finished;
                    _locker.Release();
                }
                else
                {
                    _gh.Error("Waiting for the lock is too long");
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
