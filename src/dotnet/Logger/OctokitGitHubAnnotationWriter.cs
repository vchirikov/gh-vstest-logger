using Octokit;

namespace GitHub.VsTest.Logger;

/// <inheritdoc cref="IGitHubAnnotationWriter" />
internal sealed class OctokitGitHubAnnotationWriter : IGitHubAnnotationWriter
{
    private CheckRun _checkRun;
    private readonly IGitHubClient _api;
    private readonly string _owner;
    private readonly string _repository;
    private bool _hasError;

    public OctokitGitHubAnnotationWriter(CheckRun checkRun, IGitHubClient api, string owner, string repository)
    {
        _checkRun = checkRun;
        _api = api;
        _owner = owner;
        _repository = repository;
    }

    private async Task CreateAnnotationAsync(CheckAnnotationLevel level, string message, string? title = null, string? file = null, int? line = null, int? endLine = null, int? col = null, int? endColumn = null)
    {
        var annotation = new NewCheckRunAnnotation(file ?? "", line ?? 0, endLine ?? 0, level, message) { Title = title, };
        var update = new CheckRunUpdate() {
            Output = new NewCheckRunOutput(_checkRun.Name, "Running..." ) {
                Annotations = new List<NewCheckRunAnnotation>() { annotation },
            },
            Status = CheckStatus.InProgress,
        };
        _checkRun = await _api.Check.Run.Update(_owner, _repository, _checkRun.Id, update).ConfigureAwait(false);
    }

    public Task ErrorAsync(string message, string? title = null, string? file = null, int? line = null, int? endLine = null, int? col = null, int? endColumn = null)
    {
        _hasError = true;
        return CreateAnnotationAsync(CheckAnnotationLevel.Failure, message, title, file, line, endLine, col, endColumn);
    }

    public Task NoticeAsync(string message, string? title = null, string? file = null, int? line = null, int? endLine = null, int? col = null, int? endColumn = null)
        => CreateAnnotationAsync(CheckAnnotationLevel.Notice, message, title, file, line, endLine, col, endColumn);

    public Task WarningAsync(string message, string? title = null, string? file = null, int? line = null, int? endLine = null, int? col = null, int? endColumn = null)
        => CreateAnnotationAsync(CheckAnnotationLevel.Warning, message, title, file, line, endLine, col, endColumn);

    public async ValueTask DisposeAsync()
    {
        var update = new CheckRunUpdate() {
            Output = new NewCheckRunOutput(_checkRun.Name, "Completed"),
            Status = CheckStatus.Completed,
            Conclusion = _hasError ? CheckConclusion.Failure : CheckConclusion.Success,
        };
        _checkRun = await _api.Check.Run.Update(_owner, _repository, _checkRun.Id, update).ConfigureAwait(false);
    }
}
