
using GlobalUsing.Core.Enums;

namespace GlobalUsing.Analysis;

public sealed class GlobalUsingRecommender : IGlobalUsingRecommender
{
    public ProjectAnalysisResult Recommend(DiscoveredProject project, UsageAnalysisSnapshot snapshot, AnalysisOptions options)
    {
        var alreadyGlobal = snapshot.ExistingGlobalUsings.ToImmutableHashSet();
        var usageLookup = snapshot.LocalUsages.ToDictionary(metric => metric.Signature);
        var forcedSignatures = CreateForcedSignatures(options.TargetNamespaces, options.MoveNamespaces);
        var signatures = snapshot.LocalUsages
            .Select(metric => metric.Signature)
            .Concat(snapshot.ExistingGlobalUsings)
            .Concat(forcedSignatures)
            .Distinct()
            .OrderBy(signature => signature, UsingSignatureComparer.Instance)
            .ToImmutableArray();

        var namespaceUsages = signatures
            .Select(signature =>
            {
                usageLookup.TryGetValue(signature, out var metric);
                var fileCount = metric?.FileCount ?? 0;
                var percentage = metric?.Percentage ?? 0d;
                var status = alreadyGlobal.Contains(signature)
                    ? RecommendationStatus.AlreadyGlobal
                    : IsCandidate(metric, options) || IsForcedCandidate(signature, metric, forcedSignatures)
                        ? RecommendationStatus.CandidateForGlobal
                        : RecommendationStatus.KeepLocal;

                return new NamespaceUsage(signature, fileCount, snapshot.TotalAnalyzedFiles, percentage, status);
            })
            .ToImmutableArray();

        var candidates = snapshot.LocalUsages
            .Where(metric => !alreadyGlobal.Contains(metric.Signature))
            .Where(metric => IsCandidate(metric, options) || IsForcedCandidate(metric.Signature, metric, forcedSignatures))
            .OrderBy(metric => metric.Signature, UsingSignatureComparer.Instance)
            .Select(metric => new PromotionCandidate(metric.Signature, metric.FileCount, metric.TotalFiles, metric.Percentage))
            .ToImmutableArray();

        var summary = new AnalysisSummary(
            snapshot.TotalAnalyzedFiles,
            snapshot.TotalExplicitUsingDirectives,
            namespaceUsages.Length,
            candidates.Length,
            candidates.Sum(candidate => candidate.FileCount));

        return new ProjectAnalysisResult(
            project.ProjectPath is null ? Path.GetFileName(project.RootPath) : Path.GetFileNameWithoutExtension(project.ProjectPath),
            project.RootPath,
            project.ProjectPath,
            namespaceUsages,
            candidates,
            snapshot.ExistingGlobalUsings.OrderBy(signature => signature, UsingSignatureComparer.Instance).ToImmutableArray(),
            summary);
    }

    private static bool IsCandidate(NamespaceUsageMetric? metric, AnalysisOptions options) =>
        metric is not null
        && metric.FileCount >= options.MinFiles
        && metric.Percentage >= options.ThresholdPercentage;

    private static bool IsForcedCandidate(UsingSignature signature, NamespaceUsageMetric? metric, IReadOnlySet<UsingSignature> forcedSignatures) =>
        metric is not null
        && forcedSignatures.Contains(signature);

    private static ImmutableHashSet<UsingSignature> CreateForcedSignatures(
        IReadOnlyList<string> targetNamespaces,
        IReadOnlyList<string> moveNamespaces) =>
        targetNamespaces
            .Concat(moveNamespaces)
            .Select(targetNamespace => new UsingSignature(targetNamespace, UsingKind.Normal))
            .ToImmutableHashSet(UsingSignatureComparer.Instance);
}
