using System.Reflection;

namespace TestPlatform.Extension.GitHubLogger;

/// <summary>
/// Most of them generated from <see href="https://docs.github.com/en/actions/learn-github-actions/environment-variables"/>
/// </summary>
internal record class LoggerParameters
{
    private LoggerParameters() { }

    internal static LoggerParameters Create(Dictionary<string, string>? parameters = null, Func<string, string?>? envReader = null)
    {
        var obj = new LoggerParameters();
        envReader ??= static (string variable) => Environment.GetEnvironmentVariable(variable);
        TypedReference tr = __makeref(obj);
        var fields = typeof(LoggerParameters).GetFields(BindingFlags.Public | BindingFlags.Instance);
        foreach (var fi in typeof(LoggerParameters).GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            if (parameters?.TryGetValue(fi.Name, out string fieldValue) != true)
                fieldValue = envReader(fi.Name) ?? "";
            fi.SetValueDirect(tr, fieldValue);
        }
        return obj;
    }

    /// <summary>
    /// The GH api token, <c>${{ secrets.GITHUB_TOKEN }}</c> or some other token should be here.
    /// If the value is empty the logger must be no-op.
    ///</summary>
    public string GITHUB_TOKEN = "";

    /// <inheritdoc cref="Microsoft.VisualStudio.TestPlatform.ObjectModel.DefaultLoggerParameterNames.TestRunDirectory"/>
    public string TestRunDirectory = "";

    public string name = "dotnet-test-report";

    /// <summary>
    /// Always set to true.
    ///</summary>
    public string CI = "";

    /// <summary>
    /// The name of the action currently running, or the id of a step. For example, for an action, __repo-owner_name-of-action-repo. GitHub removes special characters, and uses the name __run when the current step runs a script without an id. If you use the same script or action more than once in the same job, the name will include a suffix that consists of the sequence number preceded by an underscore. For example, the first script you run will have the name __run, and the second script will be named __run_2. Similarly, the second invocation of actions/checkout will be actionscheckout2.
    ///</summary>
    public string GITHUB_ACTION = "";

    /// <summary>
    /// The path where an action is located. This property is only supported in composite actions. You can use this path to access files located in the same repository as the action. For example, /home/runner/work/_actions/repo-owner/name-of-action-repo/v1.
    ///</summary>
    public string GITHUB_ACTION_PATH = "";

    /// <summary>
    /// For a step executing an action, this is the owner and repository name of the action. For example, actions/checkout.
    ///</summary>
    public string GITHUB_ACTION_REPOSITORY = "";

    /// <summary>
    /// Always set to true when GitHub Actions is running the workflow. You can use this variable to differentiate when tests are being run locally or by GitHub Actions.
    ///</summary>
    public string GITHUB_ACTIONS = "";

    /// <summary>
    /// The name of the person or app that initiated the workflow. For example, octocat.
    ///</summary>
    public string GITHUB_ACTOR = "";

    /// <summary>
    /// Returns the API URL. For example: https://api.github.com.
    ///</summary>
    public string GITHUB_API_URL = "";

    /// <summary>
    /// The name of the base ref or target branch of the pull request in a workflow run. This is only set when the event that triggers a workflow run is either pull_request or pull_request_target. For example, main.
    ///</summary>
    public string GITHUB_BASE_REF = "";

    /// <summary>
    /// The path on the runner to the file that sets environment variables from workflow commands. This file is unique to the current step and changes for each step in a job. For example, /home/runner/work/_temp/_runner_file_commands/set_env_87406d6e-4979-4d42-98e1-3dab1f48b13a. For more information, see "Workflow commands for GitHub Actions."
    ///</summary>
    public string GITHUB_ENV = "";

    /// <summary>
    /// The name of the event that triggered the workflow. For example, workflow_dispatch.
    ///</summary>
    public string GITHUB_EVENT_NAME = "";

    /// <summary>
    /// The path to the file on the runner that contains the full event webhook payload. For example, /github/workflow/event.json.
    ///</summary>
    public string GITHUB_EVENT_PATH = "";

    /// <summary>
    /// Returns the GraphQL API URL. For example: https://api.github.com/graphql.
    ///</summary>
    public string GITHUB_GRAPHQL_URL = "";

    /// <summary>
    /// The head ref or source branch of the pull request in a workflow run. This property is only set when the event that triggers a workflow run is either pull_request or pull_request_target. For example, feature-branch-1.
    ///</summary>
    public string GITHUB_HEAD_REF = "";

    /// <summary>
    /// The job_id of the current job. For example, greeting_job.
    ///</summary>
    public string GITHUB_JOB = "";

    /// <summary>
    /// The path on the runner to the file that sets system PATH variables from workflow commands. This file is unique to the current step and changes for each step in a job. For example, /home/runner/work/_temp/_runner_file_commands/add_path_899b9445-ad4a-400c-aa89-249f18632cf5. For more information, see "Workflow commands for GitHub Actions."
    ///</summary>
    public string GITHUB_PATH = "";

    /// <summary>
    /// The branch or tag ref that triggered the workflow run. For workflows triggered by push, this is the branch or tag ref that was pushed. For workflows triggered by pull_request, this is the pull request merge branch. For workflows triggered by release, this is the release tag created. For other triggers, this is the branch or tag ref that triggered the workflow run. This is only set if a branch or tag is available for the event type. The ref given is fully-formed, meaning that for branches the format is refs/heads/<branch_name>, for pull requests it is refs/pull/<pr_number>/merge, and for tags it is refs/tags/<tag_name>. For example, refs/heads/feature-branch-1.
    ///</summary>
    public string GITHUB_REF = "";

    /// <summary>
    /// The branch or tag name that triggered the workflow run. For example, feature-branch-1.
    ///</summary>
    public string GITHUB_REF_NAME = "";

    /// <summary>
    /// true if branch protections are configured for the ref that triggered the workflow run.
    ///</summary>
    public string GITHUB_REF_PROTECTED = "";

    /// <summary>
    /// The type of ref that triggered the workflow run. Valid values are branch or tag.
    ///</summary>
    public string GITHUB_REF_TYPE = "";

    /// <summary>
    /// The owner and repository name. For example, octocat/Hello-World.
    ///</summary>
    public string GITHUB_REPOSITORY = "";

    /// <summary>
    /// The repository owner's name. For example, octocat.
    ///</summary>
    public string GITHUB_REPOSITORY_OWNER = "";

    /// <summary>
    /// The number of days that workflow run logs and artifacts are kept. For example, 90.
    ///</summary>
    public string GITHUB_RETENTION_DAYS = "";

    /// <summary>
    /// A unique number for each attempt of a particular workflow run in a repository. This number begins at 1 for the workflow run's first attempt, and increments with each re-run. For example, 3.
    ///</summary>
    public string GITHUB_RUN_ATTEMPT = "";

    /// <summary>
    /// A unique number for each workflow run within a repository. This number does not change if you re-run the workflow run. For example, 1658821493.
    ///</summary>
    public string GITHUB_RUN_ID = "";

    /// <summary>
    /// A unique number for each run of a particular workflow in a repository. This number begins at 1 for the workflow's first run, and increments with each new run. This number does not change if you re-run the workflow run. For example, 3.
    ///</summary>
    public string GITHUB_RUN_NUMBER = "";

    /// <summary>
    /// The URL of the GitHub server. For example: https://github.com.
    ///</summary>
    public string GITHUB_SERVER_URL = "";

    /// <summary>
    /// The commit SHA that triggered the workflow. The value of this commit SHA depends on the event that triggered the workflow. For more information, see Events that trigger workflows. For example, ffac537e6cbbf934b08745a378932722df287a53.
    ///</summary>
    public string GITHUB_SHA = "";

    /// <summary>
    /// The path on the runner to the file that contains job summaries from workflow commands. This file is unique to the current step and changes for each step in a job. For example, /home/rob/runner/_layout/_work/_temp/_runner_file_commands/step_summary_1cb22d7f-5663-41a8-9ffc-13472605c76c. For more information, see "Workflow commands for GitHub Actions."
    ///</summary>
    public string GITHUB_STEP_SUMMARY = "";

    /// <summary>
    /// The name of the workflow. For example, My test workflow. If the workflow file doesn't specify a name, the value of this variable is the full path of the workflow file in the repository.
    ///</summary>
    public string GITHUB_WORKFLOW = "";

    /// <summary>
    /// The default working directory on the runner for steps, and the default location of your repository when using the checkout action. For example, /home/runner/work/my-repo-name/my-repo-name.
    ///</summary>
    public string GITHUB_WORKSPACE = "";

    /// <summary>
    /// The architecture of the runner executing the job. Possible values are X86, X64, ARM, or ARM64.
    ///</summary>
    public string RUNNER_ARCH = "";

    /// <summary>
    /// The name of the runner executing the job. For example, Hosted Agent
    ///</summary>
    public string RUNNER_NAME = "";

    /// <summary>
    /// The operating system of the runner executing the job. Possible values are Linux, Windows, or macOS. For example, Windows
    ///</summary>
    public string RUNNER_OS = "";

    /// <summary>
    /// The path to a temporary directory on the runner. This directory is emptied at the beginning and end of each job. Note that files will not be removed if the runner's user account does not have permission to delete them. For example, D:\a\_temp
    ///</summary>
    public string RUNNER_TEMP = "";

    /// <summary>
    /// The path to the directory containing preinstalled tools for GitHub-hosted runners. For more information, see "About GitHub-hosted runners". For example, C:\hostedtoolcache\windows
    ///</summary>
    public string RUNNER_TOOL_CACHE = "";

}