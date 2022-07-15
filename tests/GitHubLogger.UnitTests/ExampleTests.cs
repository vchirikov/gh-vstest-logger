using Microsoft.Extensions.Logging;

namespace GitHubLogger.UnitTests;

public class ExampleTests
{
    private readonly ILogger<ExampleTests> _logger;
    private readonly ITestOutputHelper _helper;

    public ExampleTests(ILogger<ExampleTests> logger, ITestOutputHelper helper)
    {
        _logger = logger;
        _helper = helper;
    }

    [Fact]
    public void AlwaysOk()
    {
        _logger.LogError("AlwaysOk() err msg from the MS Logger");
        _helper.WriteLine("AlwaysOk() msg from xunit MS Logger");
        Assert.True(true);
    }

}
