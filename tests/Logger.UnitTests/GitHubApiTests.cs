#define ALWAYS_FAIL

namespace GitHub.VsTest.Logger.UnitTests;
public class GitHubApiTests
{
    [Fact]
    public void IsGitHubActions_Success()
    {
        var parameters = LoggerParameters.Create(new(), (str) => str switch {
            "GITHUB_ACTIONS" => "true",
            "CI" => "true",
            _ => null
        });

        var api = new GitHubApi(parameters, Mock.Of<IOutput>());
        var result = api.IsGitHubActions;

        Assert.True(result);
    }

    [Fact]
    public void IsGitHubActions_Should_Return_False_If_Env_Not_Presented()
    {
        var parameters = LoggerParameters.Create(new(), (str) => str switch {
            _ => null
        });

        var api = new GitHubApi(parameters, Mock.Of<IOutput>());
        var result = api.IsGitHubActions;

        Assert.False(result);
        #warning add new warning
    }

    [Theory]
    [MemberData(nameof(WriteCommand_Input))]
    public void WriteCommand(GitHubWorkflowCommand cmd, string expected)
    {
        var output = new Mock<IOutput>(){ CallBase = true, };
        string result = "";
        output.Setup(x => x.Write(It.IsAny<string>())).Callback((string s) => result = s);
        var api = new GitHubApi(LoggerParameters.Create(), output.Object);
        api.Output.WriteCommand(cmd);
        Assert.Equal(expected, result);
    }

    public readonly static IEnumerable<object[]> WriteCommand_Input = new[]
    {
        new object[]{
            new GitHubWorkflowCommand("workflow-command", "{command value}", new() {
                {"parameter1", "{data}"},
                {"parameter2", "{data}"},
            }),
            "::workflow-command parameter1={data},parameter2={data}::{command value}" + Environment.NewLine
        },
        new object[]{
            new GitHubWorkflowCommand("echo", "off"),
            "::echo::off" + Environment.NewLine
        },
        new object[]{
            new GitHubWorkflowCommand("set-output", "multi\nline", new(){ ["name"] = "text" }),
            "::set-output name=text::multi%0Aline" + Environment.NewLine
        },
        new object[]{
            new GitHubWorkflowCommand("error", "foo", new(){ ["title"] = null }),
            "::error::foo" + Environment.NewLine
        },
    };
#if ALWAYS_FAIL
    [Fact]
    public void AlwaysFail()
    {
        Assert.True(false);
    }
#endif

}
