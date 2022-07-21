# GitHub Actions adapter for Microsoft.TestPlatform

GitHub `dotnet test` logger without political shit.  

> **Warning**
> The logger is supposed to **be used from within a GitHub Workflow**,

## TLDR 

```bash
dotnet add package GitHub.VsTest.Logger --version *-*
dotnet test --logger "github"
```

## Why?

Because technologies must be shared without the idiotic crap in their licenses.  

![IT commune](https://raw.githubusercontent.com/vchirikov/gh-vstest-logger/master/docs/img/bunny.png)


## Screenshots

![Example of a summary](https://raw.githubusercontent.com/vchirikov/gh-vstest-logger/master/docs/img/test-ci-workflow.png)  

![Example of PR comment](https://raw.githubusercontent.com/vchirikov/gh-vstest-logger/master/docs/img/pr-comment.png)


## Usage:

```xml
<PackageReference Include="GitHub.VsTest.Logger" Version="*-*" PrivateAssets="All" IncludeAssets="runtime; build; native; contentfiles; analyzers" />
```

```bash
dotnet test --logger "github;name=unit-tests"
```

Parameters can be set with the command line args, or through environment variables.  

We took all default variables from default github actions [environment variables](https://docs.github.com/en/actions/learn-github-actions/environment-variables)
and add `name` & `GITHUB_TOKEN`. All parameters are defined [here](./src/dotnet/Logger/LoggerParameters.cs).  

After test run the logger produce a GitHub Workflow step variable `summary`, which you can use in your workflow.  

You can also view [how we use it in our workflows](./.github/workflows/unit-tests.yml).

### Additional

[Contributing](https://github.com/vchirikov/gh-vstest-logger/blob/master/docs/contributing.md)
