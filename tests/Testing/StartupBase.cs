using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit.DependencyInjection;
using Xunit.DependencyInjection.Logging;

namespace GitHub.VsTest.Testing;

public class StartupBase
{
    public virtual void ConfigureHost(IHostBuilder hostBuilder) => hostBuilder
            .ConfigureHostConfiguration(cfg => {
                var dir = GetBaseDirectory();
                cfg.Sources.Clear();
                cfg.SetBasePath(dir);
                cfg.AddJsonFile("testsettings.json", optional: false, reloadOnChange: false);
                if (IsRunningInContainer())
                {
                    cfg.AddJsonFile("testsettings.docker.json", optional: false, reloadOnChange: false);
                }
                cfg.AddJsonFile("testsettings.local.json", optional: true, reloadOnChange: false);
            });

    public void Configure(ILoggerFactory loggerFactory, ITestOutputHelperAccessor accessor) =>
        loggerFactory.AddProvider(new XunitTestOutputLoggerProvider(accessor));

    public virtual void ConfigureServices(IServiceCollection services, HostBuilderContext ctx)
    {
        var settings = new TestSettings();

        ctx.Configuration.Bind(settings);
        InitializeSettingsCore(settings);
        InitializeSettings(settings);

        services.TryAddSingleton(settings);
        services.TryAddSingleton<ILogger>(sp => sp.GetRequiredService<ILoggerFactory>().CreateLogger(categoryName: ""));
    }

    private void InitializeSettingsCore(TestSettings settings)
    {
        if (string.IsNullOrEmpty(settings.TempDirectory))
        {
            settings.TempDirectory = Path.Combine(GetBaseDirectory(), "tmp");
        }

        settings.IsRunningInContainer = IsRunningInContainer();
    }

    private static string GetBaseDirectory() => Path.GetDirectoryName(typeof(StartupBase).Assembly.Location ?? Environment.CurrentDirectory!)!;

    protected virtual bool IsRunningInContainer() =>
        Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") != null;

    protected virtual void InitializeSettings(TestSettings settings) { }
}

public record class TestSettings
{
    public string TempDirectory { get; set; } = "";
    public bool IsRunningInContainer { get; set; } = false;
}

