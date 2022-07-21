namespace GitHub.VsTest.Logger.UnitTests;

public class LoggerParametersTests
{
    [Fact]
    public void LoggerParameters_Create_Should_Have_Parameter_Priority()
    {
        var parameters = LoggerParameters.Create(new(){{"CI", "false"}}, (str) => str switch {
            "CI" => "true",
            _ => null
        });
        Assert.Equal("false", parameters.CI);
    }
}
