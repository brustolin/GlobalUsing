using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using GlobalUsing.Core.Enums;
using GlobalUsing.Core.Interfaces;
using GlobalUsing.Core.Models;

namespace GlobalUsing.Infrastructure;

public sealed class ReportGenerator : IReportGenerator
{
    public string Generate(AnalysisResult result, ReportFormat format, bool summaryOnly = false, string? targetNamespace = null)
    {
        if (!string.IsNullOrWhiteSpace(targetNamespace))
        {
            return GenerateNamespaceReport(result, format, targetNamespace);
        }

        return format switch
        {
            ReportFormat.Json => GenerateJson(result, summaryOnly),
            ReportFormat.Markdown => GenerateMarkdown(result, summaryOnly),
            _ => GenerateConsole(result, summaryOnly),
        };
    }

    private static string GenerateNamespaceReport(AnalysisResult result, ReportFormat format, string targetNamespace)
    {
        var summary = BuildNamespaceSummary(result, targetNamespace);

        return format switch
        {
            ReportFormat.Json => JsonSerializer.Serialize(summary, InfrastructureJsonSerializerContext.Default.NamespaceReportSummary),
            ReportFormat.Markdown => GenerateNamespaceMarkdown(summary),
            _ => GenerateNamespaceConsole(summary),
        };
    }

    private static NamespaceReportSummary BuildNamespaceSummary(AnalysisResult result, string targetNamespace)
    {
        var normalizedNamespace = targetNamespace.Trim();
        var matchingUsages = result.Projects
            .Select(project => project.NamespaceUsages.FirstOrDefault(usage =>
                usage.Signature.Kind == UsingKind.Normal
                && string.Equals(usage.Signature.Name, normalizedNamespace, StringComparison.Ordinal)))
            .ToImmutableArray();
        var filesUsingNamespace = matchingUsages.Sum(usage => usage?.FileCount ?? 0);
        var totalFiles = result.Summary.TotalCSharpFilesAnalyzed;

        return new NamespaceReportSummary(
            normalizedNamespace,
            filesUsingNamespace,
            totalFiles,
            totalFiles == 0 ? 0d : Math.Round(filesUsingNamespace * 100d / totalFiles, 2, MidpointRounding.AwayFromZero),
            matchingUsages.Count(usage => usage is { FileCount: > 0 }),
            matchingUsages.Count(usage => usage?.Status == RecommendationStatus.AlreadyGlobal));
    }

    private static string GenerateNamespaceConsole(NamespaceReportSummary summary)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Namespace Summary");
        builder.AppendLine($"Namespace: {summary.Namespace}");
        builder.AppendLine($"Files using namespace: {summary.FilesUsingNamespace}");
        builder.AppendLine($"Total C# files analyzed: {summary.TotalCSharpFilesAnalyzed}");
        builder.AppendLine($"Usage percentage: {summary.UsagePercentage:F2}%");
        builder.AppendLine($"Projects with namespace usage: {summary.ProjectsWithNamespaceUsage}");
        builder.AppendLine($"Projects where already global: {summary.ProjectsAlreadyGlobal}");
        return builder.ToString().TrimEnd();
    }

    private static string GenerateNamespaceMarkdown(NamespaceReportSummary summary)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Namespace Report");
        builder.AppendLine();
        builder.AppendLine($"- Namespace: `{summary.Namespace}`");
        builder.AppendLine($"- Files using namespace: {summary.FilesUsingNamespace}");
        builder.AppendLine($"- Total C# files analyzed: {summary.TotalCSharpFilesAnalyzed}");
        builder.AppendLine($"- Usage percentage: {summary.UsagePercentage:F2}%");
        builder.AppendLine($"- Projects with namespace usage: {summary.ProjectsWithNamespaceUsage}");
        builder.AppendLine($"- Projects where already global: {summary.ProjectsAlreadyGlobal}");
        return builder.ToString().TrimEnd();
    }

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
