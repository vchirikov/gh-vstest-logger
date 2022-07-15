using System.Text;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;

namespace TestPlatform.Extension.GitHubLogger;

[FriendlyName(FriendlyName)]
[ExtensionUri(ExtensionUri)]
public class Logger : ITestLoggerWithParameters
{
    /// <summary> Uri used to uniquely identify the logger </summary>
    public const string ExtensionUri = $"logger://vchirikov/gh-vstest-logger/{ThisAssembly.ApiVersion}";

    /// <summary> Friendly name which uniquely identifies the logger </summary>
    public const string FriendlyName = "github";

    public void Initialize(TestLoggerEvents events, Dictionary<string, string> parameters)
    {

    }

    public void Initialize(TestLoggerEvents events, string testRunDirectory)
        => Initialize(events, new Dictionary<string, string>(StringComparer.Ordinal) { { DefaultLoggerParameterNames.TestRunDirectory, testRunDirectory } });

}
