using System.Collections.Immutable;

namespace GlobalUsing.Core.Models;

public sealed record UsageAnalysisSnapshot(
    ImmutableArray<SourceFileUsings> SourceFiles,
    ImmutableArray<NamespaceUsageMetric> LocalUsages,
    ImmutableArray<UsingSignature> ExistingGlobalUsings,
    int TotalAnalyzedFiles,
    int TotalExplicitUsingDirectives);
