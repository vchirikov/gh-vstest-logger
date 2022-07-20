using Octokit;

namespace GitHub.VsTest.Logger;

/// <summary> Leaky abstraction ( ˘︹˘ ), but good enough for now. </summary>
internal sealed class GitHubApi
{
    private readonly Lazy<IGitHubClient> _api;
    private readonly LoggerParameters _params;

    public GitHubApi(LoggerParameters parameters, IGitHubClient? api = null)
    {
        _api = new(() => api ?? new GitHubClient(
            new ProductHeaderValue(parameters.GITHUB_REPOSITORY_OWNER ?? "github", ThisAssembly.AssemblyInformationalVersion),
            new Uri(parameters.GITHUB_API_URL ?? "https://api.github.com/")
            ) { Credentials = new(token: parameters.GITHUB_TOKEN) });
        _params = parameters;
    }

    public bool IsGitHubActions()
    {
        bool isCi = asBool(_params.CI);
        bool isGhActions = asBool(_params.GITHUB_ACTIONS);

        return isCi && isGhActions;

        static bool asBool(string envValue)
        {
            bool result = false;
            if (!string.IsNullOrWhiteSpace(envValue) && !bool.TryParse(envValue, out result))
                result = string.Equals(envValue, "1", StringComparison.Ordinal);
            return result;
        }
    }

    public async Task<CheckRun> CreateCheckRunAsync()
    {
        var checkRun = new NewCheckRun(_params.name, _params.GITHUB_SHA){
            Output = new NewCheckRunOutput(_params.name, "Starting tests..."){
                Annotations = new List<NewCheckRunAnnotation>(),
            },
            Status = CheckStatus.Queued,
        };

        return await _api.Value.Check.Run.Create(
            _params.GITHUB_REPOSITORY_OWNER,
            _params.GITHUB_REPOSITORY.Split('/').LastOrDefault() ?? "unknown",
            checkRun
            ).ConfigureAwait(false);
    }

    public async Task<CheckRun> UpdateCheckRunAsync(CheckRun current, CheckRunUpdate update)
    {

        return await _api.Value.Check.Run.Update(
            _params.GITHUB_REPOSITORY_OWNER,
            _params.GITHUB_REPOSITORY.Split('/').LastOrDefault() ?? "unknown",
            current?.Id ?? -1,
            update).ConfigureAwait(false);
    }

    public static void Warning(string msg, [CallerMemberName] string? caller = null)
        => Console.WriteLine($"::warning::{(string.IsNullOrWhiteSpace(caller) ? "" : $"[{caller}()] ")}{Escape(msg)}");

    public static void Error(string msg, [CallerMemberName] string? caller = null)
        => Console.WriteLine($"::error::{(string.IsNullOrWhiteSpace(caller) ? "" : $"[{caller}()] ")}{Escape(msg)}");

    public static void Debug(string msg, [CallerMemberName] string? caller = null)
        => Console.WriteLine($"::debug::{(string.IsNullOrWhiteSpace(caller) ? "" : $"[{caller}()] ")}{Escape(msg)}");

    public static void OutputVariable(string name, string value)
        => Console.WriteLine($"::set-output name={Escape(name)}::{Escape(value)}");


    public static string Escape(string value) => value
        .Replace("%", "%25", StringComparison.Ordinal)
        .Replace("\n", "%0A", StringComparison.Ordinal)
        .Replace("\r", "%0D", StringComparison.Ordinal);

    public static IDisposable Block(string name) => new ScopeHelper(
        openTemplate: "::group::{0}\n",
        closeTemplate: "::endgroup::\n",
        args: name
    );

    private class ScopeHelper : IDisposable
    {
        private readonly string _closeMessage;
        public ScopeHelper(string openTemplate, string closeTemplate, params string[] args)
        {
            Console.Write(string.Format(CultureInfo.InvariantCulture, openTemplate, args));
            _closeMessage = string.Format(CultureInfo.InvariantCulture, closeTemplate, args);
        }
        public void Dispose() => Console.Write(_closeMessage);
    }
}