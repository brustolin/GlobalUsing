# Changelog

## [Unreleased]
- Updated `--namespace` to accept repeated values so multiple namespaces can be forced into global usings in a single run.
- Added a repeatable `--move` option to force selected namespaces into global usings while keeping normal report/apply behavior for everything else.
- Added `globalusing.json` config file support with automatic discovery, `--config` override, CLI-over-config precedence, and warnings when `namespace` mode causes `move` values to be ignored.
- Added repeatable `--ignore` support in both CLI and config files to keep selected namespaces local and prevent them from being promoted.

## [0.2.0] - 2026-03-17

- Added a `--summary-only` option to the `report` command to print only the summary section instead of every project's namespace usage details.
- Added a `--namespace` option to focus report output on one namespace and make `apply` operate only on that namespace, including promotion below the normal threshold.

## [0.1.1] - 2026-03-15

- Fix `Usage` message to reflect to correct binary name.

## [0.1.0] - 2026-03-15

- Added the initial `globalusing` .NET tool for analyzing repeated C# `using` directives.
- Added `report` and `apply` commands for recommendation and automatic global usings updates.
- Added Roslyn-based parsing and safe source rewriting for removing redundant local usings.
- Added support for analyzing `.csproj`, `.sln`, `.slnx`, and directory inputs with configurable thresholds and exclusions.
