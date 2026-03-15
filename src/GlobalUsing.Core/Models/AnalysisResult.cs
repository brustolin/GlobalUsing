using System.Collections.Immutable;

namespace GlobalUsing.Core.Models;

public sealed record AnalysisResult(
    ImmutableArray<ProjectAnalysisResult> Projects,
    AnalysisSummary Summary);
