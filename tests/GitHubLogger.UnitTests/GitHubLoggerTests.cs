namespace TestPlatform.Extension.GitHubLogger.UnitTests;

public class GitHubLoggerTests
{
    [Fact]
    public void LoggerParameters_Create_Should_Have_Parameter_Priority()
    {
        var parameters = LoggerParameters.Create(new() {{ "GITHUB_TOKEN", "foo" } }, (str) => str switch {
            "GITHUB_TOKEN" => "bar",
            _ => null
        });
        Assert.Equal("foo", parameters.GITHUB_TOKEN);
    }
}
public class GitHubApiTests
{
    [Fact]
    public void IsGitHubActions_Success()
    {
        var parameters = LoggerParameters.Create(new() {{ "GITHUB_TOKEN", "test" } }, (str) => str switch {
            "GITHUB_ACTIONS" => "true",
            "CI" => "true",
            _ => null
        });

        var api = new GitHubApi(parameters, null);
        var result = api.IsGitHubActions();

        Assert.True(result);
    }

    [Fact]
    public void IsGitHubActions_Should_Return_False_If_Env_Not_Presented()
    {
        var parameters = LoggerParameters.Create(new() {{ "GITHUB_TOKEN", "test" } }, (str) => str switch {
            _ => null
        });

        var api = new GitHubApi(parameters, null);
        var result = api.IsGitHubActions();

        Assert.False(result);
    }
}
