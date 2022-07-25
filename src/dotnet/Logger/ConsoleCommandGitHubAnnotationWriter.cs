namespace GitHub.VsTest.Logger;

/// <inheritdoc cref="IGitHubAnnotationWriter" />
internal sealed class ConsoleCommandGitHubAnnotationWriter : IGitHubAnnotationWriter
{
    private readonly IOutput _out;
    private readonly IDisposable _block;

    public ConsoleCommandGitHubAnnotationWriter(IOutput output, string name)
    {
        _out = output;
        _block = _out.Block(name);
    }

    public Task ErrorAsync(string message, string? title = null, string? file = null, int? line = null, int? endLine = null, int? col = null, int? endColumn = null)
    {
        _out.Error(message, title, file, line, endLine, col, endColumn);
        return Task.CompletedTask;
    }

    public Task NoticeAsync(string message, string? title = null, string? file = null, int? line = null, int? endLine = null, int? col = null, int? endColumn = null)
    {
        _out.Notice(message, title, file, line, endLine, col, endColumn);
        return Task.CompletedTask;
    }

    public Task WarningAsync(string message, string? title = null, string? file = null, int? line = null, int? endLine = null, int? col = null, int? endColumn = null)
    {
        _out.Warning(message, title, file, line, endLine, col, endColumn);
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        _block.Dispose();
        return default;
    }
}
