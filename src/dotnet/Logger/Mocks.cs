using Octokit;

#nullable disable
#pragma warning disable MA0025
namespace GitHub.VsTest.Logger;
/// <summary>
/// We can't use Moq here, because it dll will be in output and might conflict with Moq of user version,
/// so we create Mock manually.
/// </summary>
internal class MockGitHubClient : IGitHubClient
{
    public IConnection Connection { get; }
    public IAuthorizationsClient Authorization { get; }
    public IActivitiesClient Activity { get; }
    public IGitHubAppsClient GitHubApps { get; }
    public IIssuesClient Issue { get; }
    public IMigrationClient Migration { get; }
    public IMiscellaneousClient Miscellaneous { get; }
    public IOauthClient Oauth { get; }
    public IOrganizationsClient Organization { get; }
    public IPullRequestsClient PullRequest { get; }
    public IRepositoriesClient Repository { get; }
    public IGistsClient Gist { get; }
    public IUsersClient User { get; }
    public IGitDatabaseClient Git { get; }
    public ISearchClient Search { get; }
    public IEnterpriseClient Enterprise { get; }
    public IReactionsClient Reaction { get; }
    public IChecksClient Check { get; } = new MockChecksClient();

    public ApiInfo GetLastApiInfo() => throw new NotImplementedException();
    public void SetRequestTimeout(TimeSpan timeout) => throw new NotImplementedException();
}

internal class MockChecksClient : IChecksClient
{
    public ICheckRunsClient Run { get; } = new MockCheckRunsClient();
    public ICheckSuitesClient Suite { get; }
}

internal class MockCheckRunsClient : ICheckRunsClient
{
    #region unused

    public Task<CheckRun> Create(long repositoryId, NewCheckRun newCheckRun) => throw new NotImplementedException();
    public Task<CheckRun> Get(string owner, string name, long checkRunId) => throw new NotImplementedException();
    public Task<CheckRun> Get(long repositoryId, long checkRunId) => throw new NotImplementedException();
    public Task<IReadOnlyList<CheckRunAnnotation>> GetAllAnnotations(string owner, string name, long checkRunId) => throw new NotImplementedException();
    public Task<IReadOnlyList<CheckRunAnnotation>> GetAllAnnotations(long repositoryId, long checkRunId) => throw new NotImplementedException();
    public Task<IReadOnlyList<CheckRunAnnotation>> GetAllAnnotations(string owner, string name, long checkRunId, ApiOptions options) => throw new NotImplementedException();
    public Task<IReadOnlyList<CheckRunAnnotation>> GetAllAnnotations(long repositoryId, long checkRunId, ApiOptions options) => throw new NotImplementedException();
    public Task<CheckRunsResponse> GetAllForCheckSuite(string owner, string name, long checkSuiteId) => throw new NotImplementedException();
    public Task<CheckRunsResponse> GetAllForCheckSuite(long repositoryId, long checkSuiteId) => throw new NotImplementedException();
    public Task<CheckRunsResponse> GetAllForCheckSuite(string owner, string name, long checkSuiteId, CheckRunRequest checkRunRequest) => throw new NotImplementedException();
    public Task<CheckRunsResponse> GetAllForCheckSuite(long repositoryId, long checkSuiteId, CheckRunRequest checkRunRequest) => throw new NotImplementedException();
    public Task<CheckRunsResponse> GetAllForCheckSuite(string owner, string name, long checkSuiteId, CheckRunRequest checkRunRequest, ApiOptions options) => throw new NotImplementedException();
    public Task<CheckRunsResponse> GetAllForCheckSuite(long repositoryId, long checkSuiteId, CheckRunRequest checkRunRequest, ApiOptions options) => throw new NotImplementedException();
    public Task<CheckRunsResponse> GetAllForReference(string owner, string name, string reference) => throw new NotImplementedException();
    public Task<CheckRunsResponse> GetAllForReference(long repositoryId, string reference) => throw new NotImplementedException();
    public Task<CheckRunsResponse> GetAllForReference(string owner, string name, string reference, CheckRunRequest checkRunRequest) => throw new NotImplementedException();
    public Task<CheckRunsResponse> GetAllForReference(long repositoryId, string reference, CheckRunRequest checkRunRequest) => throw new NotImplementedException();
    public Task<CheckRunsResponse> GetAllForReference(string owner, string name, string reference, CheckRunRequest checkRunRequest, ApiOptions options) => throw new NotImplementedException();
    public Task<CheckRunsResponse> GetAllForReference(long repositoryId, string reference, CheckRunRequest checkRunRequest, ApiOptions options) => throw new NotImplementedException();
    public Task<CheckRun> Update(long repositoryId, long checkRunId, CheckRunUpdate checkRunUpdate) => throw new NotImplementedException();

    #endregion unused

    internal NewCheckRun _newCheckRun;
    internal CheckRun _checkRun;
    public Task<CheckRun> Create(string owner, string name, NewCheckRun newCheckRun)
    {
        _newCheckRun = newCheckRun;
        Console.WriteLine($"[{nameof(MockCheckRunsClient)}] Got {nameof(Create)}(" +
            $"{nameof(owner)}:{owner}," +
            $"{nameof(name)}:{name}," +
            $"{{{nameof(newCheckRun)}.Name:{newCheckRun.Name}}});"
        );
        _checkRun = new CheckRun(
            1337,
            "98a34f8c54d8e6163974ecf79f9bf5fb90e9474a",
            null!,
            null!,
            null!,
            CheckStatus.Queued,
            null!,
            DateTimeOffset.UtcNow,
            new(),
            new(),
            name,
            new(),
            new(),
            null!);
        return Task.FromResult(_checkRun);
    }
    public Task<CheckRun> Update(string owner, string name, long checkRunId, CheckRunUpdate checkRunUpdate)
    {
        Console.WriteLine($"[{nameof(MockCheckRunsClient)}] Got {nameof(Update)}(" +
            $"{nameof(owner)}:{owner}," +
            $"{nameof(name)}:{name}," +
            Invariant($"{nameof(checkRunId)}:{checkRunId},") +
            $"{{{nameof(checkRunUpdate)}.Name:{checkRunUpdate.Name}," +
            $" {nameof(checkRunUpdate)}.Status:{checkRunUpdate.Status}}});"
        );
        _checkRun = new CheckRun(
            1337,
            "a20b524a2d7e30dae0eea66786e14bbc76a77663",
            null!,
            null!,
            null!,
            checkRunUpdate.Status?.Value ?? CheckStatus.Queued,
            checkRunUpdate.Conclusion?.Value,
            DateTimeOffset.UtcNow,
            new(),
            new(),
            name,
            new(),
            new(),
            null!);
        return Task.FromResult(_checkRun);
    }
}