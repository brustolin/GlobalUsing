# Contributing To GlobalUsing

This document is for contributors and maintainers. If you want to use the tool, start with [README.md](README.md).

## Solution Layout

```text
GlobalUsing.slnx

src/
  GlobalUsing.Cli
  GlobalUsing.Core
  GlobalUsing.Analysis
  GlobalUsing.Roslyn
  GlobalUsing.Infrastructure

tests/
  GlobalUsing.Tests
```

## Architecture Overview

```text
CLI
  -> Command handlers
  -> AnalysisWorkflow
     -> FileDiscoveryService
     -> CachingUsingCollector
     -> NamespaceUsageAnalyzer
     -> GlobalUsingRecommender
     -> ReportGenerator / GlobalUsingsWriter / SourceFileRewriter
```

Project responsibilities:

- `GlobalUsing.Cli`: `System.CommandLine`, DI, logging, console output, entry point
- `GlobalUsing.Core`: domain models, interfaces, enums, shared result types
- `GlobalUsing.Analysis`: aggregation, thresholds, candidate detection, recommendations, workflow orchestration
- `GlobalUsing.Roslyn`: Roslyn parsing, using classification, syntax-aware rewriting
- `GlobalUsing.Infrastructure`: file discovery, filesystem access, report formatting, global usings generation

## Design Notes

- Code analysis and rewriting must not use regex.
- Roslyn syntax APIs are used for both analysis and source updates.
- Analysis is project-aware so solution and directory processing still write project-scoped global usings files.
- Alias usings and `using static` are excluded by default unless explicitly enabled.
- Common SDK implicit using namespaces are filtered when `ImplicitUsings` is enabled.
- The solution is designed to be trim- and AOT-conscious:
  - avoid reflection-heavy command binding
  - prefer explicit DI registration
  - prefer source-generated JSON serialization
  - avoid dynamic loading and runtime code generation

## Build And Test

Build the solution:

```bash
dotnet build GlobalUsing.slnx
```

Run tests:

```bash
dotnet test GlobalUsing.slnx
```

Run the CLI locally:

```bash
dotnet run --project src/GlobalUsing.Cli -- report --path .
```

Create the NuGet tool package:

```bash
dotnet pack src/GlobalUsing.Cli/GlobalUsing.Cli.csproj -c Release
```

Test the packed tool from a local package source:

```bash
dotnet tool install --tool-path ./.tools --add-source ./src/GlobalUsing.Cli/bin/Release GlobalUsing
```

## GitHub Release Workflow

The repository includes a GitHub Actions workflow at `.github/workflows/publish-tool.yml`.

It will:

- trigger on tags matching `v*`
- restore, build, and test the solution
- pack `GlobalUsing.Cli` as a NuGet tool
- publish the package to NuGet.org

Required repository secret:

- `NUGET_API_KEY`: an API key with permission to push packages to NuGet.org

Release flow:

```bash
git tag v0.1.0
git push origin v0.1.0
```

The workflow strips the leading `v` and publishes package version `0.1.0`.

You can also trigger the workflow manually with `workflow_dispatch` and provide the package version as an input.

## Native AOT

The CLI project includes optional AOT-oriented settings and can be published with:

```bash
dotnet publish src/GlobalUsing.Cli/GlobalUsing.Cli.csproj -c Release -r win-x64 -p:PublishAot=true
```

Current relevant settings in `src/GlobalUsing.Cli/GlobalUsing.Cli.csproj`:

- `PublishAot`
- `PublishTrimmed`
- `TrimMode=full`
- `InvariantGlobalization=true`
- `IsAotCompatible=true`

## Contributor Guidance

- Keep the CLI thin. Business logic belongs outside `GlobalUsing.Cli`.
- Prefer immutable models in `GlobalUsing.Core`.
- Preserve formatting and trivia when rewriting source files.
- Reuse parsed syntax trees where possible to avoid reparsing.
- Add or update tests for aggregation, classification, and rewrite behavior when changing logic.
- Keep package metadata and the user-facing `README.md` aligned with the shipped command name and installation flow.
