namespace GitHub.VsTest.Logger;

internal class TestRunSummaryWriter
{
    private static readonly Random _rand = new();
    private readonly string _outputFilePath;

    public TestRunSummaryWriter(string file) => _outputFilePath = file;

    public async Task WriteAsync(string summary)
    {
        var bytes = Encoding.UTF8.GetBytes(summary);
        await FileOperationWithRetryAsync(async () => {
            var file = new FileStream(
                _outputFilePath,
                FileMode.Append,
                FileAccess.Write,
                FileShare.None,
                4096,
                useAsync: true
            );
            await using var _ = file.ConfigureAwait(false);
            await file.WriteAsync(bytes, CancellationToken.None).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    internal static async Task FileOperationWithRetryAsync(Func<Task> operation, int retries = 10, int minMsDelay = 100, int maxMsDelay = 1000)
    {
        while (retries-- > 0)
        {
            try
            {
                await operation().ConfigureAwait(false);
                break;
            }
            catch (IOException ex) when ((ex.HResult & 0x0000FFFF) == 32 && retries > 0)
            {
                // ERROR_SHARING_VIOLATION
                await Task.Delay(_rand.Next(minMsDelay, maxMsDelay)).ConfigureAwait(false);
            }
        }
    }
}