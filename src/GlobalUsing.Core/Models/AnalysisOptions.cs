using GlobalUsing.Core.Enums;

namespace GlobalUsing.Core.Models;

public sealed record AnalysisOptions(
    string Path,
    int ThresholdPercentage,
    int MinFiles,
    string GlobalUsingsFileName,
    ReportFormat Format,
    IReadOnlyList<string> ExcludePatterns,
    bool IncludeStatic,
    bool IncludeAlias,
    bool DryRun,
    bool Verbose)
{
    public static AnalysisOptions Default(string? path = null) =>
        new(
            Path: string.IsNullOrWhiteSpace(path) ? Environment.CurrentDirectory : path,
            ThresholdPercentage: 80,
            MinFiles: 1,
            GlobalUsingsFileName: "GlobalUsings.cs",
            Format: ReportFormat.Console,
            ExcludePatterns: [],
            IncludeStatic: false,
            IncludeAlias: false,
            DryRun: false,
            Verbose: false);
}
