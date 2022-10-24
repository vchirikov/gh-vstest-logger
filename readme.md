# GitHub Actions adapter for Microsoft.TestPlatform

GitHub `dotnet test` logger without political shit.  

> **Warning**
> The logger is supposed to **be used from within a GitHub Workflow**.

## TLDR 

```bash
dotnet add package GitHub.VsTest.Logger --version *-*
dotnet test --logger "github"
```

## Why?

Because technologies must be shared without the idiotic crap in their licenses.  

![IT commune](https://raw.githubusercontent.com/vchirikov/gh-vstest-logger/master/docs/img/bunny.png)


## Screenshots 

#### Example of an annotation

![Example of PR annotation](https://raw.githubusercontent.com/vchirikov/gh-vstest-logger/master/docs/img/pr-annotation.png)  

#### Example of a Github Workflow job summary

![Example of Job summary](https://raw.githubusercontent.com/vchirikov/gh-vstest-logger/master/docs/img/job-summary-example.png)

#### Test run PR comment on success or failure

![Example of PR comment success](https://raw.githubusercontent.com/vchirikov/gh-vstest-logger/master/docs/img/pr-comment-success.png)

![Example of PR comment failure](https://raw.githubusercontent.com/vchirikov/gh-vstest-logger/master/docs/img/pr-comment-failre.png)

## Usage:

```xml
<PackageReference Include="GitHub.VsTest.Logger" Version="*-*" PrivateAssets="All" IncludeAssets="runtime; build; native; contentfiles; analyzers" />
```

```bash
# with creation annotations via output commands
dotnet test --logger "github;name=unit-tests"
# or via Octokit (it's necessary if a workflow is triggered on "pull_request" or issue_comment, we should provide the real `sha` to the logger)
dotnet test --logger "github;name=unit-tests;GITHUB_TOKEN=${{ secrets.GITHUB_TOKEN }};GITHUB_SHA=$sha"
```

Parameters can be set with the command line args, or through environment variables.  

We took all default variables from default github actions [environment variables](https://docs.github.com/en/actions/learn-github-actions/environment-variables)
and add `name` & `GITHUB_TOKEN`. All parameters are defined [here](./src/dotnet/Logger/LoggerParameters.cs).  

After test run the logger produce a [GitHub Workflow job summary](https://docs.github.com/en/actions/using-workflows/workflow-commands-for-github-actions#adding-a-job-summary).

You also can use the tests summary as content of a PR comment. As a reference you can view [how we use it in our workflows](./.github/workflows/unit-tests.yml).

### Additional

[Contributing](https://github.com/vchirikov/gh-vstest-logger/blob/master/docs/contributing.md)
