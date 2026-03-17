# Changelog

## [Unreleased]

- Added a `--summary-only` option to the `report` command to print only the summary section instead of every project's namespace usage details.

## [0.1.1] - 2026-03-15

- Fix `Usage` message to reflect to correct binary name.

## [0.1.0] - 2026-03-15

- Added the initial `globalusing` .NET tool for analyzing repeated C# `using` directives.
- Added `report` and `apply` commands for recommendation and automatic global usings updates.
- Added Roslyn-based parsing and safe source rewriting for removing redundant local usings.
- Added support for analyzing `.csproj`, `.sln`, `.slnx`, and directory inputs with configurable thresholds and exclusions.
