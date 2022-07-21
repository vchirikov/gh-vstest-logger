# Contributing 

## Conventions

[Conventional commits](https://www.conventionalcommits.org/en/v1.0.0/)

## Versioning

Read [Nerdbank.GitVersioning docs](https://github.com/dotnet/Nerdbank.GitVersioning/blob/master/doc/nbgv-cli.md)  

```bash
dotnet nbgv prepare-release beta
```

Use the `alpha` suffix in the `master` branch, `beta`,`rc-*` in release branches. When a release branch drops the version suffix it becomes a production release.


| Example                   | Formula                                                  | Public branch | Prerelease suffix | Example explanation                                                   |
| ------------------------- | -------------------------------------------------------- | ------------- | ----------------- | --------------------------------------------------------------------- |
| 2.46.2                    | {Major}.{Minor}.{GitHeight}{PreReleaseSuffix}            | true          | null              | public branch `master`, version.json without any `-prerelease` suffix |
| 1.46.2-beta               | {Major}.{Minor}.{GitHeight}{PreReleaseSuffix}            | true          | `-beta`           | public branch `release/v1.4`, version.json with `-beta` suffix        |
| 2.46.2.gba19945638        | {Major}.{Minor}.{GitHeight}.g{GitHash}                   | false         | null              | non-public branch `feat1`, version.json without suffix                |
| 2.46.2-custom.gba19945638 | {Major}.{Minor}.{GitHeight}{PreReleaseSuffix}.g{GitHash} | false         | `-custom`         | non-public branch `feat1`,  version.json with `-custom` suffix        |


## Commit messages

We use [conventional commits](https://www.conventionalcommits.org). For writing a conventional commit message, you can use
your default message editor, CLI tool or IDE plugin.

### CLI tool

1. Install CLI tool
  
  ```bash
  npm install -g commitizen
  ```

1. [Make the repo Commitizen-friendly](https://github.com/commitizen/cz-cli#making-your-repo-commitizen-friendly)

1. Run wizard of cli tool

  ```bash
  git cz
  ```

### IDE Plugins

* [Plugin with GUI](https://marketplace.visualstudio.com/items?itemName=mrluje.vs-commitizen) for Visual Studio
* [Plugin with GUI](https://plugins.jetbrains.com/plugin/9861-git-commit-template) for Rider