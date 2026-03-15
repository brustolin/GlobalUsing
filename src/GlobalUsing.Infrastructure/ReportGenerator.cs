using System.Text;
using System.Text.Json;
using GlobalUsing.Core.Enums;
using GlobalUsing.Core.Interfaces;
using GlobalUsing.Core.Models;

namespace GlobalUsing.Infrastructure;

public sealed class ReportGenerator : IReportGenerator
{
    public string Generate(AnalysisResult result, ReportFormat format) =>
        format switch
        {
            ReportFormat.Json => JsonSerializer.Serialize(result, InfrastructureJsonSerializerContext.Default.AnalysisResult),
            ReportFormat.Markdown => GenerateMarkdown(result),
            _ => GenerateConsole(result),
        };

    private static string GenerateConsole(AnalysisResult result)
    {
        var builder = new StringBuilder();
        AppendSummary(builder, result.Summary);

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

    private static string GenerateMarkdown(AnalysisResult result)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# GlobalUsing Report");
        builder.AppendLine();
        AppendSummary(builder, result.Summary, markdown: true);

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
