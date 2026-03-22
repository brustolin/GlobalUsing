namespace GlobalUsing.Infrastructure;

internal sealed record DirectoryAnalysisConfigFile(
    int? Threshold,
    int? MinFiles,
    string? GlobalFile,
    string? Format,
    string[]? Exclude,
    string[]? Namespace,
    string[]? Move,
    string[]? Ignore,
    bool? IncludeStatic,
    bool? IncludeAlias);
