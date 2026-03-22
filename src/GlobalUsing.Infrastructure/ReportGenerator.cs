using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using GlobalUsing.Core.Enums;
using GlobalUsing.Core.Interfaces;
using GlobalUsing.Core.Models;

namespace GlobalUsing.Infrastructure;

public sealed class ReportGenerator : IReportGenerator
{
    public string Generate(AnalysisResult result, ReportFormat format, bool summaryOnly = false, IReadOnlyList<string>? targetNamespaces = null)
    {
        var normalizedTargetNamespaces = NormalizeTargetNamespaces(targetNamespaces);

        if (normalizedTargetNamespaces.Length > 0)
        {
            return GenerateNamespaceReport(result, format, normalizedTargetNamespaces);
        }

        return format switch
        {
            ReportFormat.Json => GenerateJson(result, summaryOnly),
            ReportFormat.Markdown => GenerateMarkdown(result, summaryOnly),
            _ => GenerateConsole(result, summaryOnly),
        };
    }

    private static string GenerateNamespaceReport(AnalysisResult result, ReportFormat format, IReadOnlyList<string> targetNamespaces)
    {
        var summaries = BuildNamespaceSummaries(result, targetNamespaces);

        return format switch
        {
            ReportFormat.Json => GenerateNamespaceJson(summaries),
            ReportFormat.Markdown => GenerateNamespaceMarkdown(summaries),
            _ => GenerateNamespaceConsole(summaries),
        };
    }

    private static ImmutableArray<NamespaceReportSummary> BuildNamespaceSummaries(AnalysisResult result, IReadOnlyList<string> targetNamespaces) =>
        targetNamespaces
            .Select(targetNamespace => BuildNamespaceSummary(result, targetNamespace))
            .ToImmutableArray();

    private static NamespaceReportSummary BuildNamespaceSummary(AnalysisResult result, string targetNamespace)
    {
        var matchingUsages = result.Projects
            .Select(project => project.NamespaceUsages.FirstOrDefault(usage =>
                usage.Signature.Kind == UsingKind.Normal
                && string.Equals(usage.Signature.Name, targetNamespace, StringComparison.Ordinal)))
            .ToImmutableArray();
        var filesUsingNamespace = matchingUsages.Sum(usage => usage?.FileCount ?? 0);
        var totalFiles = result.Summary.TotalCSharpFilesAnalyzed;

        return new NamespaceReportSummary(
            targetNamespace,
            filesUsingNamespace,
            totalFiles,
            totalFiles == 0 ? 0d : Math.Round(filesUsingNamespace * 100d / totalFiles, 2, MidpointRounding.AwayFromZero),
            matchingUsages.Count(usage => usage is { FileCount: > 0 }),
            matchingUsages.Count(usage => usage?.Status == RecommendationStatus.AlreadyGlobal));
    }

    private static string GenerateNamespaceJson(ImmutableArray<NamespaceReportSummary> summaries) =>
        summaries.Length == 1
            ? JsonSerializer.Serialize(summaries[0], InfrastructureJsonSerializerContext.Default.NamespaceReportSummary)
            : JsonSerializer.Serialize(summaries.ToArray(), InfrastructureJsonSerializerContext.Default.NamespaceReportSummaryArray);

    private static string GenerateNamespaceConsole(ImmutableArray<NamespaceReportSummary> summaries)
    {
        var builder = new StringBuilder();

        for (var index = 0; index < summaries.Length; index++)
        {
            if (index > 0)
            {
                builder.AppendLine();
            }

            AppendNamespaceSummary(builder, summaries[index]);
        }

        return builder.ToString().TrimEnd();
    }

    private static string GenerateNamespaceMarkdown(ImmutableArray<NamespaceReportSummary> summaries)
    {
        var builder = new StringBuilder();
        builder.AppendLine(summaries.Length == 1 ? "# Namespace Report" : "# Namespace Reports");

        foreach (var summary in summaries)
        {
            builder.AppendLine();
            builder.AppendLine($"## `{summary.Namespace}`");
            builder.AppendLine();
            builder.AppendLine($"- Files using namespace: {summary.FilesUsingNamespace}");
            builder.AppendLine($"- Total C# files analyzed: {summary.TotalCSharpFilesAnalyzed}");
            builder.AppendLine($"- Usage percentage: {summary.UsagePercentage:F2}%");
            builder.AppendLine($"- Projects with namespace usage: {summary.ProjectsWithNamespaceUsage}");
            builder.AppendLine($"- Projects where already global: {summary.ProjectsAlreadyGlobal}");
        }

        return builder.ToString().TrimEnd();
    }

    private static void AppendNamespaceSummary(StringBuilder builder, NamespaceReportSummary summary)
    {
        builder.AppendLine("Namespace Summary");
        builder.AppendLine($"Namespace: {summary.Namespace}");
        builder.AppendLine($"Files using namespace: {summary.FilesUsingNamespace}");
        builder.AppendLine($"Total C# files analyzed: {summary.TotalCSharpFilesAnalyzed}");
        builder.AppendLine($"Usage percentage: {summary.UsagePercentage:F2}%");
        builder.AppendLine($"Projects with namespace usage: {summary.ProjectsWithNamespaceUsage}");
        builder.AppendLine($"Projects where already global: {summary.ProjectsAlreadyGlobal}");
    }

    private static ImmutableArray<string> NormalizeTargetNamespaces(IReadOnlyList<string>? targetNamespaces) =>
        targetNamespaces is null
            ? []
            : targetNamespaces
                .Where(targetNamespace => !string.IsNullOrWhiteSpace(targetNamespace))
                .Select(targetNamespace => targetNamespace.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToImmutableArray();

    private static string GenerateJson(AnalysisResult result, bool summaryOnly) =>
        summaryOnly
            ? JsonSerializer.Serialize(result.Summary, InfrastructureJsonSerializerContext.Default.AnalysisSummary)
            : JsonSerializer.Serialize(result, InfrastructureJsonSerializerContext.Default.AnalysisResult);

    private static string GenerateConsole(AnalysisResult result, bool summaryOnly)
    {
        var builder = new StringBuilder();
        AppendSummary(builder, result.Summary);

        if (summaryOnly)
        {
            return builder.ToString().TrimEnd();
        }

        foreach (var project in result.Projects)
        {
            builder.AppendLine();
            builder.AppendLine($"Project: {project.Name}");
            builder.AppendLine($"Root: {project.RootPath}");
            builder.AppendLine("Namespace | Files | Total | Percent | Kind | Status");
            builder.AppendLine("--- | ---: | ---: | ---: | --- | ---");

            foreach (var usage in project.NamespaceUsages)
            {
                builder.AppendLine($"{usage.Signature.DisplayName} | {usage.FileCount} | {usage.TotalFiles} | {usage.Percentage:F2}% | {usage.Signature.Kind} | {FormatStatus(usage.Status)}");
            }
        }

        return builder.ToString().TrimEnd();
    }

    private static string GenerateMarkdown(AnalysisResult result, bool summaryOnly)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# GlobalUsing Report");
        builder.AppendLine();
        AppendSummary(builder, result.Summary, markdown: true);

        if (summaryOnly)
        {
            return builder.ToString().TrimEnd();
        }

        foreach (var project in result.Projects)
        {
            builder.AppendLine();
            builder.AppendLine($"## {project.Name}");
            builder.AppendLine();
            builder.AppendLine($"Path: `{project.RootPath}`");
            builder.AppendLine();
            builder.AppendLine("| Namespace | Files | Total | Percent | Kind | Status |");
            builder.AppendLine("| --- | ---: | ---: | ---: | --- | --- |");

            foreach (var usage in project.NamespaceUsages)
            {
                builder.AppendLine($"| `{usage.Signature.DisplayName}` | {usage.FileCount} | {usage.TotalFiles} | {usage.Percentage:F2}% | {usage.Signature.Kind} | {FormatStatus(usage.Status)} |");
            }
        }

        return builder.ToString().TrimEnd();
    }

    private static void AppendSummary(StringBuilder builder, AnalysisSummary summary, bool markdown = false)
    {
        if (markdown)
        {
            builder.AppendLine("## Summary");
            builder.AppendLine();
            builder.AppendLine($"- Total C# files analyzed: {summary.TotalCSharpFilesAnalyzed}");
            builder.AppendLine($"- Total explicit using directives: {summary.TotalExplicitUsingDirectives}");
            builder.AppendLine($"- Unique namespaces discovered: {summary.UniqueNamespacesDiscovered}");
            builder.AppendLine($"- Candidates above threshold: {summary.CandidatesAboveThreshold}");
            builder.AppendLine($"- Estimated reduction of duplicated using directives: {summary.EstimatedReductionOfDuplicatedUsings}");
            return;
        }

        builder.AppendLine("Summary");
        builder.AppendLine($"Total C# files analyzed: {summary.TotalCSharpFilesAnalyzed}");
        builder.AppendLine($"Total explicit using directives: {summary.TotalExplicitUsingDirectives}");
        builder.AppendLine($"Unique namespaces discovered: {summary.UniqueNamespacesDiscovered}");
        builder.AppendLine($"Candidates above threshold: {summary.CandidatesAboveThreshold}");
        builder.AppendLine($"Estimated reduction of duplicated using directives: {summary.EstimatedReductionOfDuplicatedUsings}");
    }

    private static string FormatStatus(GlobalUsing.Core.Enums.RecommendationStatus status) =>
        status switch
        {
            GlobalUsing.Core.Enums.RecommendationStatus.AlreadyGlobal => "already global",
            GlobalUsing.Core.Enums.RecommendationStatus.CandidateForGlobal => "candidate for global",
            _ => "keep local",
        };
}
