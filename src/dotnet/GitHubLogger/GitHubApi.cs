using Octokit;

namespace TestPlatform.Extension.GitHubLogger;

internal sealed class GitHubApi
{
    private readonly IGitHubClient _api;

    public GitHubApi()
    {
        //_api = new GitHubClient()
    }

    internal GitHubApi(IGitHubClient api)
        => _api = api;
}