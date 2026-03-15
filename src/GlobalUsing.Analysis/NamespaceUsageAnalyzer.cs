
using GlobalUsing.Core.Enums;




namespace GlobalUsing.Analysis;

public sealed class NamespaceUsageAnalyzer : INamespaceUsageAnalyzer
{
    public UsageAnalysisSnapshot Analyze(
        DiscoveredProject project,
        IReadOnlyCollection<SourceFileUsings> sourceFiles,
        AnalysisOptions options)
    {
        var localUsageCounts = new Dictionary<UsingSignature, int>();
        var existingGlobals = new HashSet<UsingSignature>();
        var totalExplicitUsings = 0;

        foreach (var sourceFile in sourceFiles)
        {
            var perFileSeen = new HashSet<UsingSignature>();

            foreach (var usingDirective in sourceFile.UsingDirectives)
            {
                if (!ShouldInclude(usingDirective, project, options))
                {
                    continue;
                }

                totalExplicitUsings++;

                if (usingDirective.IsGlobal)
                {
                    existingGlobals.Add(usingDirective.Signature);
                    continue;
                }

                if (!perFileSeen.Add(usingDirective.Signature))
                {
                    continue;
                }

                localUsageCounts.TryGetValue(usingDirective.Signature, out var current);
                localUsageCounts[usingDirective.Signature] = current + 1;
            }
        }

        var totalFiles = sourceFiles.Count;
        var metrics = localUsageCounts
            .OrderBy(entry => entry.Key, UsingSignatureComparer.Instance)
            .Select(entry => new NamespaceUsageMetric(
                entry.Key,
                entry.Value,
                totalFiles,
                totalFiles == 0 ? 0d : Math.Round(entry.Value * 100d / totalFiles, 2, MidpointRounding.AwayFromZero)))
            .ToImmutableArray();

        return new UsageAnalysisSnapshot(
            sourceFiles.ToImmutableArray(),
            metrics,
            existingGlobals.OrderBy(signature => signature, UsingSignatureComparer.Instance).ToImmutableArray(),
            totalFiles,
            totalExplicitUsings);
    }

    private static bool ShouldInclude(CollectedUsingDirective usingDirective, DiscoveredProject project, AnalysisOptions options)
    {
        if (usingDirective.Signature.Kind == UsingKind.Static && !options.IncludeStatic)
        {
            return false;
        }

        if (usingDirective.Signature.Kind == UsingKind.Alias && !options.IncludeAlias)
        {
            return false;
        }

        if (!usingDirective.IsGlobal
            && usingDirective.Signature.Kind == UsingKind.Normal
            && project.ImplicitUsingsEnabled
            && project.ImplicitNamespaces.Contains(usingDirective.Signature.Name))
        {
            return false;
        }

        return true;
    }
}
