using Octokit;

namespace GitHub.VsTest.Logger;

internal interface IGitHubApi
{
    bool IsGitHubActions { get; }
    IOutput Output { get; }
    void OutputVariable(string name, string value);
    Task<IGitHubAnnotationWriter> CreateAnnotationWriterAsync(string name);
}

internal sealed class GitHubApi : IGitHubApi
{
    public bool IsGitHubActions { get; }
    public IOutput Output { get; }

    private readonly LoggerParameters _params;
    private readonly bool _isOctokitEnabled;
    private readonly Lazy<IGitHubClient>? _api;

    public GitHubApi(LoggerParameters parameters, IOutput output, IGitHubClient? api = null)
    {
        _params = parameters;
        Output = output;
        IsGitHubActions = _params.CI.asBool() && _params.GITHUB_ACTIONS.asBool();
        _isOctokitEnabled = IsGitHubActions && _params.GITHUB_TOKEN?.Length == 40 && _params.GITHUB_SHA?.Length > 0;
        _api = _isOctokitEnabled
            ? new(() => api ?? new GitHubClient(
                new ProductHeaderValue(parameters.GITHUB_REPOSITORY_OWNER ?? "github", ThisAssembly.AssemblyInformationalVersion),
                new Uri(parameters.GITHUB_API_URL ?? "https://api.github.com/")
                ) { Credentials = new(token: parameters.GITHUB_TOKEN) })
            : null;
    }

    public void OutputVariable(string name, string value)
        => Output.WriteCommand(new("set-output", value, new(StringComparer.Ordinal) { ["name"] = name }));

    public async Task<IGitHubAnnotationWriter> CreateAnnotationWriterAsync(string name)
    {
        return _isOctokitEnabled
            ? await CreateOctokitGitHubAnnotationWriterAsync(name).ConfigureAwait(false)
            : new ConsoleCommandGitHubAnnotationWriter(Output, name);

        async Task<IGitHubAnnotationWriter> CreateOctokitGitHubAnnotationWriterAsync(string name)
        {
            if (_api == null)
                throw new("GitHub api client should be initialized");

            var owner = _params.GITHUB_REPOSITORY_OWNER ?? "github";
            var repository = _params.GITHUB_REPOSITORY.Split('/').LastOrDefault() ?? "unknown";

            var newCheckRun = new NewCheckRun(name, _params.GITHUB_SHA) {
                Output = new NewCheckRunOutput(name, "Starting...") {
                    Annotations = new List<NewCheckRunAnnotation>(),
                },
                Status = CheckStatus.Queued,
            };
            var api = _api.Value;
            var checkRun =  await api.Check.Run.Create(owner, repository, newCheckRun).ConfigureAwait(false);
            return new OctokitGitHubAnnotationWriter(checkRun, api, owner, repository);
        }
    }
}