namespace GitHub.VsTest.Logger;

/// <summary>
/// We can create annotations via console commands and they will use as a parent <c>github.context.sha</c>.
/// But in some cases (for example if workflow is triggered
/// on <c>pull_request</c> we should use <c>github.context.payload.pull_request.head.sha</c> In these cases
/// <see cref="ConsoleCommandGitHubAnnotationWriter" /> won't show annotations in review tab in PR.
/// Use another, specify the <see cref="LoggerParameters.GITHUB_SHA" /> parameter.
/// <br /> <seealso href="https://github.com/actions/toolkit/issues/133" />
/// <br /> <seealso href="https://github.community/t/annotations-not-working-on-pull-request/137491" />
/// <br /> <seealso href="https://github.com/actions/toolkit/issues/1136" />
/// </summary>
internal interface IGitHubAnnotationWriter : IAsyncDisposable
{
    Task NoticeAsync(string message, string? title = null, string? file = null, int? line = null, int? endLine = null, int? col = null, int? endColumn = null);
    Task WarningAsync(string message, string? title = null, string? file = null, int? line = null, int? endLine = null, int? col = null, int? endColumn = null);
    Task ErrorAsync(string message, string? title = null, string? file = null, int? line = null, int? endLine = null, int? col = null, int? endColumn = null);
}
