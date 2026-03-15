# Changelog

## [0.1.0] - 2026-03-15

- Added the initial `globalusing` .NET tool for analyzing repeated C# `using` directives.
- Added `report` and `apply` commands for recommendation and automatic global usings updates.
- Added Roslyn-based parsing and safe source rewriting for removing redundant local usings.
- Added support for analyzing `.csproj`, `.sln`, `.slnx`, and directory inputs with configurable thresholds and exclusions.
