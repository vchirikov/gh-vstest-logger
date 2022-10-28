
namespace GitHub.VsTest.Logger;

internal record class StackTraceInfo
{
    public string Frame { get; set; } = "";
    public string Type { get; set; } = "";
    public string Method { get; set; } = "";
    public string ParameterList { get; set; } = "";
    public IEnumerable<KeyValuePair<string, string>> Parameters { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal);
    public string File { get; set; } = "";
    public string Line { get; set; } = "";
}

internal partial class StackTraceParser
{
    private readonly string _workspacePath;

    /// <summary>ctor</summary>
    /// <param name="workspacePath">Full path to the current dir / workspace folder</param>
    public StackTraceParser(string workspacePath) => _workspacePath = workspacePath;

    public static IEnumerable<StackTraceInfo> Parse(string stackTrace)
        => Parse(stackTrace, (f, t, m, pl, ps, fn, ln) => new StackTraceInfo {
            Frame = f,
            Type = t,
            Method = m,
            ParameterList = pl,
            Parameters = ps,
            File = fn,
            Line = ln,
        });

    private static readonly StringComparison _pathStringComparison =
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;


    /// <summary> Returns first parsed stack traces with relative file path </summary>
    public IEnumerable<StackTraceInfo> ParseAndNormalize(string stackTrace)
    {
        var directorySeparators = new char[]{'/', '\\'};
        foreach (var st in Parse(stackTrace))
        {
            // won't work on old msbuilds (requires netstandard2.1, but we are already there, so)
            if (!string.IsNullOrWhiteSpace(st.File))
            {
                // file in stacktraces must use full paths, so we can check it before make them relative
                // to exclude files out from our workspace
                if (st.File.StartsWith(_workspacePath, _pathStringComparison))
                    st.File = Path.GetRelativePath(_workspacePath, st.File).Trim(directorySeparators);
                // normalize paths to use '/' as directory separators
                st.File = st.File.Replace('\\', '/');
            }
            yield return st;
        }
    }
}
