using GlobalUsing.Core.Enums;

namespace GlobalUsing.Core.Models;

public sealed record AnalysisOptions(
    string Path,
    int ThresholdPercentage,
    int MinFiles,
    string GlobalUsingsFileName,
    ReportFormat Format,
    IReadOnlyList<string> ExcludePatterns,
    IReadOnlyList<string> TargetNamespaces,
    IReadOnlyList<string> MoveNamespaces,
    bool IncludeStatic,
    bool IncludeAlias,
    bool SummaryOnly,
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
            TargetNamespaces: [],
            MoveNamespaces: [],
            IncludeStatic: false,
            IncludeAlias: false,
            SummaryOnly: false,
            DryRun: false,
            Verbose: false);
}
