namespace GitHub.VsTest.Logger;

/// <summary>
/// <see href="https://docs.github.com/en/github-ae@latest/actions/using-workflows/workflow-commands-for-github-actions" /> <br />
/// <see href="https://github.com/actions/toolkit/blob/main/docs/commands.md" />
/// </summary>
public record class GitHubWorkflowCommand(string Name, string Value = "", Dictionary<string, string?>? Parameters = null);

internal sealed class GitHubApi
{
    private readonly LoggerParameters _params;
    private readonly IOutput _out;

    public GitHubApi(LoggerParameters parameters, IOutput output)
    {
        _params = parameters;
        _out = output;
    }

    public bool IsGitHubActions() => _params.CI.asBool() && _params.GITHUB_ACTIONS.asBool();

    /// <summary>
    /// Writes to output command in the format <br />
    /// <c>::workflow-command parameter1={data},parameter2={data}::{command value}</c>
    /// </summary>
    public void WriteCommand(GitHubWorkflowCommand command)
        => _out.WriteLine(Invariant($"::{Escape(command.Name)}{(command.Parameters?.Any(kv => !string.IsNullOrEmpty(kv.Value)) == true ? " " + string.Join(',', command.Parameters.Where(kv => !string.IsNullOrEmpty(kv.Value)).Select(kv => Invariant($"{Escape(kv.Key, isParameter: true)}={Escape(kv.Value, isParameter: true)}"))) : "")}::{Escape(command.Value)}"));

    /// <summary>
    /// <seealso href="https://github.com/actions/toolkit/blob/c5278cdd088a8ed6a87dbd5c80d7c1ae03beb6e5/packages/core/src/utils.ts#L34-L39" />
    /// </summary>
    private void WriteAnnotation(
            string type,
            string value = "",
            string? title = null,
            string? file = null,
            int? line = null,
            int? endLine = null,
            int? col = null,
            int? endColumn = null
            )
    {
        WriteCommand(new(type, value, new(StringComparer.Ordinal) {
            ["title"] = title,
            ["file"] = file,
            ["line"] = line?.ToString(CultureInfo.InvariantCulture),
            ["endLine"] = endLine?.ToString(CultureInfo.InvariantCulture),
            ["col"] = col?.ToString(CultureInfo.InvariantCulture),
            ["endColumn"] = endColumn?.ToString(CultureInfo.InvariantCulture),
        }));
    }

    public void Warning(
            string message,
            string? title = null,
            string? file = null,
            int? line = null,
            int? endLine = null,
            int? col = null,
            int? endColumn = null)
        => WriteAnnotation("warning", message, title, file, line, endLine, col, endColumn);

    public void Error(
            string message,
            string? title = null,
            string? file = null,
            int? line = null,
            int? endLine = null,
            int? col = null,
            int? endColumn = null)
        => WriteAnnotation("error", message, title, file, line, endLine, col, endColumn);

    public void Debug(
            string message,
            string? title = null,
            string? file = null,
            int? line = null,
            int? endLine = null,
            int? col = null,
            int? endColumn = null)
        => WriteAnnotation("debug", message, title, file, line, endLine, col, endColumn);

    public void Notice(
        string message,
        string? title = null,
        string? file = null,
        int? line = null,
        int? endLine = null,
        int? col = null,
        int? endColumn = null)
    => WriteAnnotation("notice", message, title, file, line, endLine, col, endColumn);

    public void OutputVariable(string name, string value)
        => WriteCommand(new("set-output", value, new(StringComparer.Ordinal) { ["name"] = name }));

    private static string? Escape(string? value, bool isParameter = false)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        var result = value
            .Replace("%", "%25", StringComparison.Ordinal)
            .Replace("\n", "%0A", StringComparison.Ordinal)
            .Replace("\r", "%0D", StringComparison.Ordinal);

        if (isParameter)
        {
            result = result
                .Replace(":", "%3A", StringComparison.Ordinal)
                .Replace(",", "%2C", StringComparison.Ordinal);
        }
        return result;
    }

    public void Echo(bool isOn)
        => WriteCommand(new("echo", isOn ? "on" : "off"));

    private void Group(string name)
        => WriteCommand(new("block", name));

    private void EndGroup()
        => WriteCommand(new("endgroup"));

    public IDisposable Block(string name) => new DisposableBlock(name, this);

    private class DisposableBlock : IDisposable
    {
        private readonly GitHubApi _api;
        public DisposableBlock(string name, GitHubApi api)
        {
            _api = api;
            _api.Group(name);
        }
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            _api.EndGroup();
        }
    }
}