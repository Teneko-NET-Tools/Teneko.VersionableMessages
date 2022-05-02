![Vernuntii Logo](res/logo.svg)

[:running: **Quick start guide**](#quick-start-guide) &nbsp; | &nbsp; [ :scroll: Chat on gitter](https://gitter.im/vernuntii/vernuntii?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge)

Vernuntii (transl. versionable messages) is a tool for calculating the next semantic version. The tool has the capability to iterate a stream of commit messages and decide upon versioning mode to increment major, minor, patch or height. The pre-release is derived from branch and highly customizable. The version prefix (e.g. v) is either inherited (default), initially set or explicitly removed depending on configuration. Each branch is separatly configurable. The most important fact is that this tool is single branch scoped like MinVer, so simply said it reads all commits you see in git log.

<!-- omit in toc -->
### Key facts

- Next version is [adaptiv](#version-adaptivity)
- Optional [configuration file][configuration-file] (but recommended)
  - Either json or yaml
- In-built [versioning modes](./docs/configuration-file.md#versioning-mode)
  - Possiblity to override increment mode, message convention and more
- Git plugin
  - Searches for latest commit version
  - Uses commit messages as message stream
  - Enables branch-based configuration
- Plugin system (TBD)
  - Write your own plugins
  - Replace or mutate existing plugins
- Inbuilt cache mechanism
- Can be run concurrently

<!-- omit in toc -->
# Quick start guide

#### Use case #1

Using only [MSBuild Integration][msbuild-nuget-package-docs] because

- You are author of one or more projects whose resulting packages need to be versioned by Vernuntii
- You do not need to coordinate the versioning "from above" like Nuke, Cake or any continous integration platform
- After publishing the packages with the produced version from Vernuntii you are willing to create manually the produced version as git tag

#### Use case #2

Using [MSBuild Integration][msbuild-nuget-package-docs] with [GitHub Actions](#github-actions) because

- You would like to automate the git tag creation
- You want to define a GitHub workflow when pushing changes to repository with the intention to publish the packages:
  1. You define a step that uses [GitHub Actions](#github-actions) to have access the produced next version
  2. You define a step that pushes the packages to your package registry (e.g. NuGet) and on success ..
  3. .. you creates a git tag with produced next version and pushes it back to repository

> This repository uses such workflow: [.github/workflows/build-test-pack-publish.yml](.github/workflows/build-test-pack-publish.yml)

#### Use case #3

Using only [.NET CLI package](#net-cli-package) because

- You want to have access to the "vernuntii"-binary
- You need full control and want to realize custom workflows
- You want to take a look at --help :upside_down_face:

#### **Use case #?**

Your use case is not described above? Open an issue and tell me about it.

<!-- omit in toc -->
# Table of Contents

- [Version adaptivity](#version-adaptivity)
- [Vernuntii installers](#vernuntii-installers)
  - [.NET CLI package](#net-cli-package)
- [Vernuntii integrations](#vernuntii-integrations)
  - [MSBuild package](#msbuild-package)
  - [GitHub Actions](#github-actions)
- [Development](#development)
  - [Getting started](#getting-started)
      - [Minimum requirements](#minimum-requirements)
  - [Vernuntii.SemVer.Parser](#vernuntiisemverparser)
  - [Issues I am working on](#issues-i-am-working-on)
- [License](#license)

# Version adaptivity

Vernuntii wants to simplify semantic versioning, so we try to adapt as much as possible and let the user control how the next version should be calculated.

<!-- omit in toc -->
## Version prefix

The version prefix refers to the prefix before the version core e.g. `v0.1.0`. The current behaviour is to adapt if existing the prefix `v` of the latest version.

- If latest is
  - `v0.1.0` -> prefix `v` is adapted
  - `0.1.0` -> no prefix is adapted

> Currently none or `v` prefix is adapted. You cannot set initial or explicit version prefix yet. When you want an initial prefix then it is recommended to set the start version via git tag or configuration file.

<!-- omit in toc -->
## Version core

The version core refers to `<major>.<minor>.<patch>`. When Vernuntii searches for the latest version. It does not care what the latest version core is. If we assume

```
771c4ea (HEAD -> main) second commit
8f24ad3 (tag: 0.1.0) initial commit
```

then the latest version would be `0.1.0`. If we decide to change the version by ourself by tagging HEAD with `0.5.0` for whatever reason

```
771c4ea (HEAD -> main, tag: 0.5.0) second commit
8f24ad3 (tag: 0.1.0) initial commit
```

then the latest version would be `0.5.0`.

<!-- omit in toc -->
## Drawback

One drawback of being adaptive is that the latest or next version is not deterministic when you would remove all tags, but to be fair: I am not aware of one versioning tool that gurantees full deterministic in calculating the next version in case you remove all tags.

# Vernuntii installers

A Vernuntii installer is another term for getting the bare metal binaries to execute Vernuntii directly. For example the .NET CLI package is used in [GitHub Actions](#github-actions) integration.

## .NET CLI package

[![Nuget][globaltool-nuget-package-badge]][globaltool-nuget-package]

```
dotnet tool install --global Vernuntii.Console.GlobalTool --version 0.1.0-alpha.0

# local
dotnet new tool-manifest # if you are setting up this repo
dotnet tool install --local Vernuntii.Console.GlobalTool --version 0.1.0-alpha.0
```

# Vernuntii integrations

A Vernuntii integration is a facility that uses Vernuntii internally and allows cool workflows.

## MSBuild package

[![Nuget][msbuild-nuget-package-badge]][msbuild-nuget-package]

The MSBuild package is called `Vernuntii.Console.MSBuild` and installable over NuGet store or by adding these lines to your project:

```
<PackageReference Include="Vernuntii.Console.GlobalTool" Version="0.1.0-alpha.0">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
```

When installed it sets MSBuild-specific properties:

- `<Version>$(Vernuntii_SemanticVersion)</Version>`
- `<VersionPrefix>$(Vernuntii_Version)</VersionPrefix>`
- `<VersionSuffix>$(Vernuntii_PreRelease)$(Vernuntii_PlusBuild)</VersionSuffix>`
- `<PackageVersion>$(Vernuntii_SemanticVersion)</PackageVersion>`
- `<AssemblyVersion>$(Vernuntii_Version).0</AssemblyVersion>` (if not previously defined)
- `<InformationalVersion>$(Vernuntii_Version)$(Vernuntii_HyphenPreRelease)+$(Vernuntii_BranchName)</InformationalVersion>` (if not previously defined)
- `<FileVersion>$(Vernuntii_Version).0</FileVersion>` (if not previously defined)

The `Vernuntii_*`-properties are provided by an internal MSBuild-task that calls the Vernuntii global tool.

From the following set of **optional properties** you can choose to change the behaviour of the MSBuild package:

- `<DisableVernuntii/>`
  - Disables Vernuntii
- `<VernuntiiAssemblyFile/>`
- `<VernuntiiConsoleExecutableFile/>`
- `<VernuntiiVerbosity/>`
  - Allowed value: `Debug`, `Error`, `Fatal` (implicit default), `Information`, `Verbose`, `Warning`
- `<VernuntiiConfigPath/>`
  - Path to [configuration file][configuration-file]
- `<VernuntiiCacheId/>`
  - The cache id (default is `SHORT_LIVING_CACHE`)
- `<VernuntiiCacheCreationRetentionTime/>`
  - The retention time after what time since creation the cache should be renewed
- `<VernuntiiCacheLastAccessRetentionTime/>`
  - The retention time after what time of last access the cache should be renewed
- `<VernuntiiEmptyCaches/>`
  - `true` empties the cache before applying any rules of retention time
- `<ExecuteVernuntiiTaskDependsOn/>`
  - MSBuild-targets to depend on when calling the `ExecuteVernuntiiTask`-MSBuild-target.
- `<ExecuteVernuntiiTaskBeforeTargets/>`
  - Prepends MSBuild-targets to the `BeforeTargets` of `ExecuteVernuntiiTask`-MSBuild-target.
- `<UpdateVersionPropsFromVernuntiiTask/>`
  - `false` means the MSBuild-specific properties (`Version`, `VersionPrefix`, ...) are not set anymore but  `Vernuntii_*`-properties are still available

## GitHub Actions

The following [GitHub actions][github-actions] are available.

- `vernuntii/actions/install/dotnet-tool@main`
  - Using this GitHub action makes the global command "vernuntii" available
- `vernuntii/actions/install/msbuild-import@main`
  - Enables "Vernuntii"-.targets file in subsequent calls of MSBuild
- `vernuntii/actions/execute@main`
  - Executes the "vernuntii"-binary

[msbuild-nuget-package]: https://www.nuget.org/packages/Vernuntii.Console.MSBuild
[msbuild-nuget-package-badge]: https://img.shields.io/nuget/v/Vernuntii.Console.MSBuild
[msbuild-nuget-package-docs]: #msbuild-package
[globaltool-nuget-package]: https://www.nuget.org/packages/Vernuntii.Console.GlobalTool
[globaltool-nuget-package-badge]: https://img.shields.io/nuget/v/Vernuntii.Console.GlobalTool
[github-actions]: https://github.com/vernuntii/actions
[configuration-file]: ./docs/configuration-file.md
[semver-nuget-package]: https://www.nuget.org/packages/Vernuntii.SemVer
[semver-parser-nuget-package]: https://www.nuget.org/packages/Vernuntii.SemVer.Parser

# Development

## Getting started

The project is out of the box compilable and you don't have to initialize anything before. Only consider the [Minimum requirements](#minimum-requirements).

#### Minimum requirements

- Visual Studio 2022 (optional)
- .NET 6.0 SDK

## Vernuntii.SemVer.Parser

[![Nuget][vernuntii-semver-parser-nuget-badge]][vernuntii-semver-parser-nuget]

Vernuntii uses [Vernuntii.SemVer.Parser][vernuntii-semver-parser-nuget] to parse your version strings. If you want to use it too, then check out the [README.md][vernuntii-semver-parser-readme] for more details.

[vernuntii-semver-parser-readme]: ./src/Vernuntii.SemVer.Parser
[vernuntii-semver-parser-nuget]: https://www.nuget.org/packages/Vernuntii.SemVer.Parser
[vernuntii-semver-parser-nuget-badge]: https://img.shields.io/nuget/v/Vernuntii.SemVer.Parser

## Issues I am working on

This is my work list. :slightly_smiling_face:

- Allow setting initial or explicit version prefix
- Allow additional custom prefixes that differ from `v`
- Write commit-version pre-release matcher regarding height-convention
- Finish plugin system
  - Separate and modularize further
- Write tests, tests and more tests
- Issues I don't know at this moment

# License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.