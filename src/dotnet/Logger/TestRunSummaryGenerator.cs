using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace GitHub.VsTest.Logger;

internal sealed class TestRunSummaryGenerator
{
    private readonly string _serverUrl;
    private readonly string _repository;
    private readonly string _workspace;
    private readonly string _sha;

    public TestRunSummaryGenerator(string serverUrl, string repository, string workspace, string sha)
    {
        _serverUrl = serverUrl;
        _repository = repository;
        _workspace = workspace.Replace('\\', '/').TrimEnd('/');
        _sha = sha;
    }

    public string Generate(
        string name,
        string? suite,
        string? framework,
        long passed,
        long failed,
        long skipped,
        long total,
        TimeSpan elapsed,
        ICollection<TestResult> testResults)
    {
        var sb = new StringBuilder(1024);

        sb.Append("<details>")
            .Append("<summary>")
            .Append(failed >= 0 ? "📕" : skipped >= 0 ? "📙" : "📗")
            .Append(" ")
            .Append("<b>")
            .Append(name);

        if (!string.IsNullOrWhiteSpace(suite))
            sb.Append(" / ").Append(suite);

        sb.Append("</b>");

        if (!string.IsNullOrWhiteSpace(framework))
            sb.Append(" (").Append(framework).Append(')');

        sb.Append("</summary>")
            .Append("<br/>")
            .Append("<table>")
            .Append("<th width=\"99999\">")
                .Append("🟢&nbsp;&nbsp;Passed")
            .Append("</th>")
            .Append("<th width=\"99999\">")
                .Append("🔴&nbsp;&nbsp;Failed")
            .Append("</th>")
            .Append("<th width=\"99999\">")
                .Append("🟡&nbsp;&nbsp;Skipped")
            .Append("</th>")
            .Append("<th width=\"99999\">")
                .Append("∑&nbsp;&nbsp;Total")
            .Append("</th>")
            .Append("<th width=\"99999\">")
                .Append("🕙&nbsp;&nbsp;Elapsed")
            .Append("</th>")
            // Table body
            .Append("<tr>")
            .Append("<td align=\"center\">")
                .Append(passed > 0 ? passed.ToString(CultureInfo.InvariantCulture) : "—")
            .Append("</td>")
            .Append("<td align=\"center\">")
                .Append(failed > 0 ? failed.ToString(CultureInfo.InvariantCulture) : "—")
            .Append("</td>")
            .Append("<td align=\"center\">")
                .Append(skipped > 0 ? skipped.ToString(CultureInfo.InvariantCulture) : "—")
            .Append("</td>")
            .Append("<td align=\"center\">")
                .Append(total)
            .Append("</td>")
            .Append("<td align=\"center\">")
                .Append(FormatTimeSpan(elapsed))
            .Append("</td>")
            .Append("</tr>")
            .AppendLine("</table>")
            .AppendLine();

        foreach (var testResult in testResults.Where(r => r.Outcome == TestOutcome.Failed))
        {
            var stackTraces = StackTraceParser.Parse(testResult.ErrorStackTrace, (f, t, m, pl, ps, fn, ln) => new
            {
                Frame         = f,
                Type          = t,
                Method        = m,
                ParameterList = pl,
                Parameters    = ps,
                File          = fn,
                Line          = ln,
            });

            foreach (var stackTrace in stackTraces)
            {
                var url = !string.IsNullOrWhiteSpace(stackTrace.File)
                ? TryGenerateFilePermalink(stackTrace.File, stackTrace.Line)
                : "#";

                sb
                    .Append("Fail: ")
                    .Append(string.IsNullOrWhiteSpace(url) ? "**" : "[**")
                    .Append(testResult.TestCase.DisplayName)
                    .AppendLine(string.IsNullOrWhiteSpace(url) ? "**" : $"]**({url})")
                    .AppendLine("```yml")
                    .AppendLine(testResult.ErrorMessage)
                    .AppendLine(testResult.ErrorStackTrace)
                    .AppendLine("```");
            }
        }

        sb.AppendLine("</details>").AppendLine();
        return sb.ToString();

        static string FormatTimeSpan(TimeSpan timeSpan)
            => timeSpan switch { { TotalDays: var days } when days >= 1 => Invariant($"{timeSpan.TotalDays:0.0000} days"), { TotalHours: var hours } when hours >= 1 => Invariant($"{timeSpan.TotalHours:0.0000} hours"), { TotalMinutes: var min } when min >= 1 => Invariant($"{timeSpan.TotalMinutes:0.0000} minutes"), { TotalSeconds: var sec } => Invariant($"{timeSpan.TotalSeconds:0.0000} seconds") };
    }

    private string? TryGenerateFilePermalink(string filePath, string? line)
    {
        if (string.IsNullOrWhiteSpace(_serverUrl) ||
            string.IsNullOrWhiteSpace(_repository) ||
            string.IsNullOrWhiteSpace(_workspace) ||
            string.IsNullOrWhiteSpace(_sha))
        {
            return null;
        }

        var filePathNormalized = filePath
            .Replace('\\', '/')
            .TrimEnd('/')
            .Replace(_workspace, "", StringComparison.Ordinal)
            .Trim('/');

        line = string.IsNullOrWhiteSpace(line) ? "" : $"#L{line}";

        return $"{_serverUrl}/{_repository}/blob/{_sha}/{filePathNormalized}{line}";
    }
}
