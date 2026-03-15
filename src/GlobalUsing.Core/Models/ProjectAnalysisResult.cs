using System.Collections.Immutable;

namespace GlobalUsing.Core.Models;

public sealed record ProjectAnalysisResult(
    string Name,
    string RootPath,
    string? ProjectPath,
    ImmutableArray<NamespaceUsage> NamespaceUsages,
    ImmutableArray<PromotionCandidate> PromotionCandidates,
    ImmutableArray<UsingSignature> ExistingGlobalUsings,
    AnalysisSummary Summary);
