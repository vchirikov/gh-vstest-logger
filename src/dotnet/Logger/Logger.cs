using System.Text;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Octokit;

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
    private TestRunSummaryGenerator _summaryGenerator = null!;
    private TestRunSummaryWriter _summaryWriter = null!;
    private string? _testRunName;
    private string? _testRunFramework;
    private IGitHubAnnotationWriter? _annotationWriter;
    private TestRunStatus _status = TestRunStatus.NotRunning;
    private readonly SemaphoreSlim _locker = new(1,1);
    private readonly TimeSpan _timeout = TimeSpan.FromSeconds(120);
    private readonly ConcurrentDictionary<TestResult, Task> _testResults = new();
    private Task _initializationTask = Task.FromException(new ("Lifetime error"));

    public void Initialize(TestLoggerEvents events, Dictionary<string, string> parameters)
    {
        _params = LoggerParameters.Create(parameters);
        IGitHubClient? apiClient = null;
        if (string.Equals(_params.GITHUB_TOKEN, "ghp_____________________________________", StringComparison.Ordinal))
        {
            if (_params.GH_VSTEST_DBG.asBool())
                Console.WriteLine("[GitHub.VsTest.Logger]: Use the mock of IGitHubClient");
            apiClient = new MockGitHubClient();
        }
        _gh = new GitHubApi(_params, new ConsoleOutput(), apiClient);
        _gh.Output.Echo(_params.echo.asBool());
        if (!_gh.IsGitHubActions)
        {
            _gh.Output.Warning("This isn't a GitHub Actions environment. Logger won't do anything. You can force it with 'CI=1;GITHUB_ACTIONS=1' parameters or env variables.");
            return;
        }
        if (!string.IsNullOrEmpty(_params.GITHUB_TOKEN) && _params.GITHUB_TOKEN.Length != 40)
        {
            _gh.Output.Warning("GITHUB_TOKEN is an unsupported token (length != 40). Logger won't do anything.");
            return;
        }

        _summaryGenerator = new TestRunSummaryGenerator(
            _params.GITHUB_SERVER_URL,
            _params.GITHUB_REPOSITORY,
            _params.GITHUB_WORKSPACE,
            _params.GITHUB_SHA
        );
        var summaryFile = !string.IsNullOrWhiteSpace(_params.GITHUB_STEP_SUMMARY)
            ? _params.GITHUB_STEP_SUMMARY
            : "test-run.md";
        _summaryWriter = new TestRunSummaryWriter(summaryFile);

        events.TestRunStart += (_, args) => OnTestRunStart(args.TestRunCriteria);
        events.TestResult += (_, args) => OnTestResult(args.Result);
        events.TestRunComplete += (_, args) => OnTestRunComplete(args);

        if (_params.GH_VSTEST_DBG.asBool())
            Console.WriteLine("[GitHub.VsTest.Logger]: Initialize() ended");
    }

    /// <summary> Raised when a test run starts. </summary>
    private void OnTestRunStart(TestRunCriteria testRunCriteria)
    {
        if (_status == TestRunStatus.Started)
        {
            _gh.Output.Error($"Something went wrong, {nameof(OnTestRunStart)} was already called.");
            return;
        }

        _initializationTask = Task.Run(async () => {
            try
            {
                await InitAsync().ConfigureAwait(false);
                _status = TestRunStatus.Started;
            }
            catch (Exception ex)
            {
                _gh.Output.Error($"Exception: {ex.Message}");
                using var _ = _gh.Output.Block("Exception info");
                _gh.Output.Error(ex.ToString());
            }
        });

        async Task InitAsync()
        {
            if (!await _locker.WaitAsync(_timeout).ConfigureAwait(false))
                throw new TimeoutException($"{nameof(OnTestRunStart)}: Waiting for the lock is too long");
            try
            {
                string testRunName =$"dotnet test{(string.IsNullOrEmpty(testRunCriteria.TestCaseFilter) ? "" : " --filter:" + testRunCriteria.TestCaseFilter)} / {_params.name}";
                var sources = testRunCriteria?.Sources?.FirstOrDefault();
                _testRunName = !string.IsNullOrWhiteSpace(sources)
                    ? Path.GetFileNameWithoutExtension(sources)
                    : null;

                _testRunFramework = !string.IsNullOrWhiteSpace(testRunCriteria?.TestRunSettings)
                    ? (string?)XElement.Parse(testRunCriteria.TestRunSettings)
                        .Element("RunConfiguration")?
                        .Element("TargetFrameworkVersion")
                    : null;

                _annotationWriter = await _gh.CreateAnnotationWriterAsync(testRunName).ConfigureAwait(false);
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
        if (_params.GH_VSTEST_DBG.asBool())
            Console.WriteLine($"[GitHub.VsTest.Logger]: {nameof(OnTestResult)}(result:{result.DisplayName})");

        // annotate only failed tests
        if (result.Outcome == TestOutcome.Failed || result.Outcome == TestOutcome.NotFound)
        {
            var task = Task.Run(async () => {
                try
                {
                    if (_status != TestRunStatus.Started)
                        await _initializationTask.ConfigureAwait(false);
                    await OnTestResultInternalAsync(result).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _gh.Output.Error($"Exception: {ex.Message}");
                    using var _ = _gh.Output.Block("Exception info");
                    _gh.Output.Error(ex.ToString());
                }
            });

            if (!_testResults.TryAdd(result, task))
                _gh.Output.Error($"Dictionary already contains the testResult for {result.DisplayName}");
        }

        async Task OnTestResultInternalAsync(TestResult result)
        {
            if (_status != TestRunStatus.Started)
                await _initializationTask.ConfigureAwait(false);

            if (_annotationWriter == null)
                throw new($"annotationWriter must not be null, test run status: {_status}");

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

            if (!await _locker.WaitAsync(_timeout).ConfigureAwait(false))
                throw new TimeoutException($"{nameof(OnTestResult)}: Waiting for the lock is too long");
            try
            {
                foreach (var st in stackTraces)
                {
                    if (!int.TryParse(st.Line, NumberStyles.Integer, CultureInfo.InvariantCulture, out int line))
                        _gh.Output.Warning("Can't parse line in stacktrace");

                    await _annotationWriter.ErrorAsync(
                        message: GetDetailsMessage(result, sb),
                        title: $"{result.TestCase.DisplayName}",
                        file: WorkspaceRelativePath(st.File),
                        line: line > 5 ? line - 5 : line,
                        endLine: line + 1
                    ).ConfigureAwait(false);
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
            if (_params.GH_VSTEST_DBG.asBool())
                Console.WriteLine($"[GitHub.VsTest.Logger]: {nameof(OnTestRunComplete)}()");
            if (_status != TestRunStatus.Started)
                throw new($"Unexpected test run status: '{_status}'.");
            OnTestRunCompleteInternalAsync(results).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _gh.Output.Error($"Exception: {ex.Message}");
            using var _ = _gh.Output.Block("Exception info");
            _gh.Output.Error(ex.ToString());
        }

        async Task OnTestRunCompleteInternalAsync(TestRunCompleteEventArgs results)
        {
            try
            {
                await Task.WhenAll(_testResults.Values.ToArray()).ConfigureAwait(false);
                // write summary only if filter will get some tests for assembly
                if (results.TestRunStatistics.ExecutedTests > 0)
                {
                    if (!results.TestRunStatistics.Stats.TryGetValue(TestOutcome.Passed, out var passed))
                        passed = 0;
                    if (!results.TestRunStatistics.Stats.TryGetValue(TestOutcome.Failed, out var failed))
                        failed = 0;
                    if (!results.TestRunStatistics.Stats.TryGetValue(TestOutcome.Skipped, out var skipped))
                        skipped = 0;

                    var summary = _summaryGenerator.Generate(
                        name: _params.name,
                        suite: _testRunName,
                        framework: _testRunFramework,
                        passed,
                        failed,
                        skipped,
                        total: results.TestRunStatistics.ExecutedTests,
                        elapsed: results.ElapsedTimeInRunningTests,
                        testResults: _testResults.Keys
                    );

                    // we might have several msbuild processes (per testRun),
                    // so we can't just print summary into gh action output value (bc we will override the value)
                    // thus we must use a file to store results and work with contention between msbuilds
                    await _summaryWriter.WriteAsync(summary).ConfigureAwait(false);
                }
            }
            finally
            {
                if (_annotationWriter != null)
                {
                    await _annotationWriter.DisposeAsync().ConfigureAwait(false);
                    _annotationWriter = null;
                }
                if (await _locker.WaitAsync(_timeout).ConfigureAwait(false))
                {
                    _status = TestRunStatus.Finished;
                    _locker.Release();
                }
                else
                {
                    _gh.Output.Error("Waiting for the lock is too long");
                }
            }
        }
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
