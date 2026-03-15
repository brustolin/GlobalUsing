namespace GlobalUsing.Core.Models;

public sealed record AnalysisSummary(
    int TotalCSharpFilesAnalyzed,
    int TotalExplicitUsingDirectives,
    int UniqueNamespacesDiscovered,
    int CandidatesAboveThreshold,
    int EstimatedReductionOfDuplicatedUsings);
