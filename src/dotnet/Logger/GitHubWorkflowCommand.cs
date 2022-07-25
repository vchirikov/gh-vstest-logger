namespace GitHub.VsTest.Logger;

/// <summary>
/// <see href="https://docs.github.com/en/github-ae@latest/actions/using-workflows/workflow-commands-for-github-actions" /> <br />
/// <see href="https://github.com/actions/toolkit/blob/main/docs/commands.md" />
/// </summary>
public record class GitHubWorkflowCommand(string Name, string Value = "", Dictionary<string, string?>? Parameters = null);
