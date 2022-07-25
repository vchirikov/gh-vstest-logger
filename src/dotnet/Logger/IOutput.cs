namespace GitHub.VsTest.Logger;

/// <summary>
/// Minimal abstraction above Console, because we don't need full console capabilities nether <see cref="TextWriter" />.
/// </summary>
internal interface IOutput
{
    /// <inheritdoc cref="Console.Write(string)"/>
    void Write(string value);

    /// <inheritdoc cref="Console.WriteLine(string)"/>
    void WriteLine(string value) => Write(value + Environment.NewLine);

    /// <inheritdoc cref="Console.WriteLine(string, object[])"/>
    void WriteLine(string format, params object[] arg)
        => Write(string.Format(CultureInfo.InvariantCulture, format + Environment.NewLine, arg));

    /// <inheritdoc cref="Console.Write(string, object[])"/>
    void Write(string format, params object[] arg)
        => Write(string.Format(CultureInfo.InvariantCulture, format, arg));
}

internal class ConsoleOutput : IOutput
{
    /// <inheritdoc cref="Console.Write(string)"/>
    public void Write(string value) => Console.Write(value);
}

internal static class OutputExtensions
{
    /// <summary>
    /// Writes to output command in the format <br />
    /// <c>::workflow-command parameter1={data},parameter2={data}::{command value}</c>
    /// </summary>
    public static void WriteCommand(this IOutput self, GitHubWorkflowCommand command)
        => self.WriteLine(Invariant($"::{Escape(command.Name)}{(command.Parameters?.Any(kv => !string.IsNullOrEmpty(kv.Value)) == true ? " " + string.Join(',', command.Parameters.Where(kv => !string.IsNullOrEmpty(kv.Value)).Select(kv => Invariant($"{Escape(kv.Key, isParameter: true)}={Escape(kv.Value, isParameter: true)}"))) : "")}::{Escape(command.Value)}"));

    public static string? Escape(string? value, bool isParameter = false)
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

    /// <summary>
    /// <seealso href="https://github.com/actions/toolkit/blob/c5278cdd088a8ed6a87dbd5c80d7c1ae03beb6e5/packages/core/src/utils.ts#L34-L39" />
    /// </summary>
    private static void WriteAnnotation(this IOutput self,
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
        self.WriteCommand(new(type, value, new(StringComparer.Ordinal) {
            ["title"] = title,
            ["file"] = file,
            ["line"] = line?.ToString(CultureInfo.InvariantCulture),
            ["endLine"] = endLine?.ToString(CultureInfo.InvariantCulture),
            ["col"] = col?.ToString(CultureInfo.InvariantCulture),
            ["endColumn"] = endColumn?.ToString(CultureInfo.InvariantCulture),
        }));
    }

    public static void Warning(this IOutput self,
        string message,
        string? title = null,
        string? file = null,
        int? line = null,
        int? endLine = null,
        int? col = null,
        int? endColumn = null)
    => self.WriteAnnotation("warning", message, title, file, line, endLine, col, endColumn);

    public static void Error(this IOutput self,
            string message,
            string? title = null,
            string? file = null,
            int? line = null,
            int? endLine = null,
            int? col = null,
            int? endColumn = null)
        => self.WriteAnnotation("error", message, title, file, line, endLine, col, endColumn);

    public static void Debug(this IOutput self,
            string message,
            string? title = null,
            string? file = null,
            int? line = null,
            int? endLine = null,
            int? col = null,
            int? endColumn = null)
        => self.WriteAnnotation("debug", message, title, file, line, endLine, col, endColumn);

    public static void Notice(this IOutput self,
        string message,
        string? title = null,
        string? file = null,
        int? line = null,
        int? endLine = null,
        int? col = null,
        int? endColumn = null)
    => self.WriteAnnotation("notice", message, title, file, line, endLine, col, endColumn);

    public static void Echo(this IOutput self, bool isOn)
    => self.WriteCommand(new("echo", isOn ? "on" : "off"));

    private static void Group(this IOutput self, string name)
        => self.WriteCommand(new("group", name));

    private static void EndGroup(this IOutput self)
        => self.WriteCommand(new("endgroup"));

    public static IDisposable Block(this IOutput self, string name) => new DisposableBlock(name, self);

    private sealed class DisposableBlock : IDisposable
    {
        private readonly IOutput _out;
        public DisposableBlock(string name, IOutput output)
        {
            _out = output;
            _out.Group(name);
        }
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            _out.EndGroup();
        }
    }
}