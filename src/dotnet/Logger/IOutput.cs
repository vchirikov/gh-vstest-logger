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

public class ConsoleOutput : IOutput
{
    /// <inheritdoc cref="Console.Write(string)"/>
    public void Write(string value) => Console.Write(value);
}