# GlobalUsing

`GlobalUsing` is a command-line tool for finding repeated C# `using` directives and recommending when they should be promoted to `global using` declarations.

It can analyze:

- a `.csproj`
- a `.sln`
- a `.slnx`
- a directory

It can either:

- generate a report with recommendations
- apply the recommendations by updating a global usings file and removing redundant local usings

## What It Does

GlobalUsing scans explicit `using` directives in your source files and measures how often each namespace appears across a project. Based on the thresholds you choose, it classifies each namespace as:

- `already global`
- `candidate for global`
- `keep local`

By default, it analyzes normal `using` directives only. Alias usings and `using static` directives are ignored unless you opt in.

## Install

Install as a global tool:

```bash
dotnet tool install --global GlobalUsing
```

Update an existing installation:

```bash
dotnet tool update --global GlobalUsing
```

Install into a local tool manifest:

```bash
dotnet new tool-manifest
dotnet tool install --local GlobalUsing
```

After installation, invoke the command as:

```bash
globalusing
```

If you installed it locally, use:

```bash
dotnet tool run globalusing
```

## Quick Start

Run a report for the current directory:

```bash
globalusing report --path .
```

Preview changes without writing files:

```bash
globalusing apply --path . --dry-run
```

Apply changes:

```bash
globalusing apply --path .
```

For a locally installed tool:

```bash
dotnet tool run globalusing report --path .
```

## Commands

### `report`

Analyzes files and prints a report. This command never modifies files.

Example:

```bash
globalusing report --path ./src/MyProject --threshold 80 --format markdown
```

### `apply`

Analyzes files, updates the global usings file, and removes matching redundant local usings when safe.

Example:

```bash
globalusing apply --path ./src/MyProject --global-file GlobalUsings.cs
```

## Options

- `--path <path>`: path to a `.csproj`, `.sln`, `.slnx`, or directory. Default: current directory.
- `--threshold <number>`: minimum percentage of files that must contain a namespace before it becomes a candidate. Default: `80`.
- `--min-files <number>`: minimum number of files that must contain a namespace before promotion.
- `--global-file <name>`: name of the global usings file. Default: `GlobalUsings.cs`.
- `--format <console|json|markdown>`: report output format. Default: `console`.
- `--exclude <pattern>`: glob pattern to exclude files or directories. Repeatable.
- `--include-static`: include `using static` directives in analysis.
- `--include-alias`: include alias usings in analysis.
- `--dry-run`: show planned changes without writing files.
- `--verbose`: enable detailed logging.

## Typical Workflows

Analyze a single project:

```bash
globalusing report --path ./src/MyProject/MyProject.csproj
```

Analyze an entire solution:

```bash
globalusing report --path ./MySolution.sln
```

Analyze a `.slnx` solution:

```bash
globalusing report --path ./MySolution.slnx
```

Exclude generated or special folders:

```bash
globalusing report --path . --exclude "artifacts/**" --exclude "samples/**"
```

Include alias and static usings:

```bash
globalusing report --path . --include-alias --include-static
```

Preview apply results as JSON:

```bash
globalusing apply --path . --dry-run --format json
```

## Example Report Output

```text
Summary
Total C# files analyzed: 12
Total explicit using directives: 48
Unique namespaces discovered: 9
Candidates above threshold: 3
Estimated reduction of duplicated using directives: 21

Project: MyProject
Root: C:\repo\src\MyProject
Namespace | Files | Total | Percent | Kind | Status
--- | ---: | ---: | ---: | --- | ---
System.Linq | 12 | 12 | 100.00% | Normal | candidate for global
System.Text.Json | 11 | 12 | 91.67% | Normal | candidate for global
System | 0 | 12 | 0.00% | Normal | already global
```

## Apply Behavior

When you run `apply`, the tool will:

- create the global usings file if it does not exist
- preserve existing global usings
- avoid duplicates
- keep global usings sorted
- remove matching local usings only when safe
- support idempotent repeated runs

If you want to inspect the planned changes first, use `--dry-run`.

## Exit Codes

- `0`: success with no candidates
- `1`: candidates detected in report mode
- `2`: execution error

## Notes

- `bin` and `obj` folders are ignored automatically.
- If `ImplicitUsings` is enabled in the project, SDK-provided namespaces are not counted as explicit using candidates.
- Contributor and architecture notes live in `CONTRIBUTE.md` in the repository.
