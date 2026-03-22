namespace GlobalUsing.Cli;

internal sealed record AnalysisConfigFile(
    int? Threshold,
    int? MinFiles,
    string? GlobalFile,
    string? Format,
    string[]? Exclude,
    string[]? Namespace,
    string[]? Move,
    bool? IncludeStatic,
    bool? IncludeAlias);
