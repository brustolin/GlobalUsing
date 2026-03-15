using System.Collections.Frozen;
using Microsoft.Extensions.Logging;

namespace GlobalUsing.Analysis;

public sealed class AnalysisWorkflow(
    IFileDiscoveryService fileDiscoveryService,
    IUsingCollector usingCollector,
    INamespaceUsageAnalyzer namespaceUsageAnalyzer,
    IGlobalUsingRecommender globalUsingRecommender,
    IGlobalUsingsWriter globalUsingsWriter,
    ISourceFileRewriter sourceFileRewriter,
    IFileSystem fileSystem,
    ILogger<AnalysisWorkflow> logger) : IAnalysisWorkflow
{
    public async Task<AnalysisResult> ReportAsync(AnalysisOptions options, CancellationToken cancellationToken)
    {
        var context = await BuildContextAsync(options, cancellationToken);
        return context.Result;
    }

    public async Task<ApplyResult> ApplyAsync(AnalysisOptions options, CancellationToken cancellationToken)
    {
        var context = await BuildContextAsync(options, cancellationToken);
        var fileChanges = ImmutableArray.CreateBuilder<FileChange>();
        var totalAdded = 0;
        var totalRemoved = 0;

        foreach (var project in context.Discovery.Projects)
        {
            var projectResult = context.ProjectResultsByRoot[project.RootPath];
            var promotedCandidates = projectResult.PromotionCandidates
                .Select(candidate => candidate.Signature)
                .ToImmutableSortedSet(UsingSignatureComparer.Instance);

            if (promotedCandidates.Count == 0 && projectResult.ExistingGlobalUsings.Length == 0)
            {
                continue;
            }

            var globalUsingsPath = Path.Combine(project.RootPath, options.GlobalUsingsFileName);
            var globalUpdate = await globalUsingsWriter.BuildUpdateAsync(globalUsingsPath, promotedCandidates, cancellationToken);

            if (globalUpdate.HasChanges)
            {
                fileChanges.Add(new FileChange(globalUpdate.FilePath, true, globalUpdate.AddedCount, 0));
                totalAdded += globalUpdate.AddedCount;

                if (!options.DryRun)
                {
                    var directory = Path.GetDirectoryName(globalUpdate.FilePath);
                    if (!string.IsNullOrWhiteSpace(directory))
                    {
                        fileSystem.CreateDirectory(directory);
                    }

                    await fileSystem.WriteAllTextAsync(globalUpdate.FilePath, globalUpdate.Content, cancellationToken);
                }
            }

            var removableUsings = projectResult.ExistingGlobalUsings
                .Concat(promotedCandidates)
                .ToImmutableHashSet();

            if (removableUsings.Count == 0)
            {
                continue;
            }

            var rewriteTargets = context.SourceFilesByPath.Values
                .Where(source => project.CSharpFiles.Contains(source.FilePath, StringComparer.OrdinalIgnoreCase))
                .Where(source => source.UsingDirectives.Any(usingDirective => !usingDirective.IsGlobal && removableUsings.Contains(usingDirective.Signature)))
                .ToImmutableArray();

            var rewriteTasks = rewriteTargets
                .Select(source => sourceFileRewriter.RemoveRedundantUsingsAsync(source.FilePath, removableUsings, options, cancellationToken));

            foreach (var rewriteResult in await Task.WhenAll(rewriteTasks))
            {
                if (!rewriteResult.HasChanges)
                {
                    continue;
                }

                fileChanges.Add(new FileChange(rewriteResult.FilePath, true, 0, rewriteResult.RemovedUsings));
                totalRemoved += rewriteResult.RemovedUsings;

                if (!options.DryRun)
                {
                    await fileSystem.WriteAllTextAsync(rewriteResult.FilePath, rewriteResult.Content, cancellationToken);
                }
            }
        }

        return new ApplyResult(
            context.Result,
            options.DryRun,
            totalAdded,
            fileChanges.Count,
            totalRemoved,
            fileChanges.ToImmutable());
    }

    private async Task<WorkflowContext> BuildContextAsync(AnalysisOptions options, CancellationToken cancellationToken)
    {
        var discovery = await fileDiscoveryService.DiscoverAsync(options, cancellationToken);
        var sourceFiles = await usingCollector.CollectAsync(discovery.AllFiles, cancellationToken);
        var sourceFilesByPath = sourceFiles
            .ToFrozenDictionary(sourceFile => sourceFile.FilePath, sourceFile => sourceFile, StringComparer.OrdinalIgnoreCase);
        var projectResults = ImmutableArray.CreateBuilder<ProjectAnalysisResult>(discovery.Projects.Length);
        var projectResultsByRoot = new Dictionary<string, ProjectAnalysisResult>(StringComparer.OrdinalIgnoreCase);

        foreach (var project in discovery.Projects)
        {
            logger.LogDebug("Analyzing project root {ProjectRoot} with {FileCount} files.", project.RootPath, project.CSharpFiles.Length);

            var projectSourceFiles = project.CSharpFiles
                .Select(path => sourceFilesByPath[path])
                .ToImmutableArray();

            var snapshot = namespaceUsageAnalyzer.Analyze(project, projectSourceFiles, options);
            var projectResult = globalUsingRecommender.Recommend(project, snapshot, options);
            projectResults.Add(projectResult);
            projectResultsByRoot[project.RootPath] = projectResult;
        }

        var analysisResult = new AnalysisResult(projectResults.ToImmutable(), BuildSummary(projectResults));
        return new WorkflowContext(discovery, sourceFilesByPath, projectResultsByRoot.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase), analysisResult);
    }

    private static AnalysisSummary BuildSummary(IEnumerable<ProjectAnalysisResult> projects)
    {
        var projectArray = projects.ToImmutableArray();
        var uniqueNamespaces = projectArray
            .SelectMany(project => project.NamespaceUsages.Select(usage => usage.Signature))
            .Distinct()
            .Count();

        return new AnalysisSummary(
            projectArray.Sum(project => project.Summary.TotalCSharpFilesAnalyzed),
            projectArray.Sum(project => project.Summary.TotalExplicitUsingDirectives),
            uniqueNamespaces,
            projectArray.Sum(project => project.Summary.CandidatesAboveThreshold),
            projectArray.Sum(project => project.Summary.EstimatedReductionOfDuplicatedUsings));
    }

    private sealed record WorkflowContext(
        FileDiscoveryResult Discovery,
        FrozenDictionary<string, SourceFileUsings> SourceFilesByPath,
        FrozenDictionary<string, ProjectAnalysisResult> ProjectResultsByRoot,
        AnalysisResult Result);
}
