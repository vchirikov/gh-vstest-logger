using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace GitHub.VsTest.Logger;

internal sealed class TestRunSummaryGenerator
{
    private readonly string _serverUrl;
    private readonly string _repository;
    private readonly string _sha;
    private readonly StackTraceParser _stackTraceParser;

    public TestRunSummaryGenerator(StackTraceParser stackTraceParser, string serverUrl, string repository, string sha)
    {
        _stackTraceParser = stackTraceParser;
        _serverUrl = serverUrl;
        _repository = repository;
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
            .Append(failed > 0 ? "📕" : skipped > 0 ? "📙" : "📗")
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
            var stackTraces = !string.IsNullOrEmpty(testResult.ErrorStackTrace)
                ? _stackTraceParser.ParseAndNormalize(testResult.ErrorStackTrace).ToArray()
                : null;
            var stackTrace = stackTraces?.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.File) && File.Exists(x.File))
                             ?? stackTraces?.FirstOrDefault();

            if (stackTrace != null)
            {
                var url = !string.IsNullOrWhiteSpace(stackTrace.File)
                    ? TryGenerateFilePermalink(stackTrace.File, stackTrace.Line)
                    : "#";

                sb
                    .Append("Fail: ")
                    .Append(string.IsNullOrWhiteSpace(url) ? "**" : "[**")
                    .Append(testResult.TestCase.DisplayName)
                    .AppendLine(string.IsNullOrWhiteSpace(url) ? "**" : $"**]({url})")
                    .AppendLine("```yml")
                    .AppendLine(testResult.ErrorMessage)
                    .AppendLine(testResult.ErrorStackTrace)
                    .AppendLine("```");
            }
        }

        sb.AppendLine("</details>").AppendLine();
        return sb.ToString();

        static string FormatTimeSpan(TimeSpan timeSpan)
            => timeSpan switch {
                { TotalDays: >= 1 } => Invariant($"{timeSpan.TotalDays:0.0000} days"),
                { TotalHours: >= 1 } => Invariant($"{timeSpan.TotalHours:0.0000} hours"),
                { TotalMinutes: >= 1 } => Invariant($"{timeSpan.TotalMinutes:0.0000} minutes"),
                { } => Invariant($"{timeSpan.TotalSeconds:0.0000} seconds"),
            };
    }

    private string? TryGenerateFilePermalink(string filePath, string? line)
    {
        if (string.IsNullOrWhiteSpace(_serverUrl) ||
            string.IsNullOrWhiteSpace(_repository) ||
            string.IsNullOrWhiteSpace(_sha))
        {
            return null;
        }

        line = string.IsNullOrWhiteSpace(line) ? "" : $"#L{line}";

        return $"{_serverUrl}/{_repository}/blob/{_sha}/{filePath}{line}";
    }
}
