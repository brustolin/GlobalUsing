namespace GlobalUsing.Core.Models;

public sealed record AnalysisOptionOverrides(
    bool ThresholdPercentage,
    bool MinFiles,
    bool GlobalUsingsFileName,
    bool Format,
    bool ExcludePatterns,
    bool TargetNamespaces,
    bool MoveNamespaces,
    bool IgnoreNamespaces,
    bool IncludeStatic,
    bool IncludeAlias);
