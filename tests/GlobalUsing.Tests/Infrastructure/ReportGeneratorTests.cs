using System.Collections.Immutable;
using System.Text.Json;
using GlobalUsing.Core.Enums;
using GlobalUsing.Core.Models;
using GlobalUsing.Infrastructure;

namespace GlobalUsing.Tests.Infrastructure;

public sealed class ReportGeneratorTests
{
    [Fact]
    public void Generate_console_with_target_namespace_outputs_namespace_summary()
    {
        var generator = new ReportGenerator();

        var report = generator.Generate(CreateAnalysisResult(), ReportFormat.Console, targetNamespaces: ["System.Linq"]);

        Assert.Contains("Namespace Summary", report);
        Assert.Contains("Namespace: System.Linq", report);
        Assert.Contains("Files using namespace: 3", report);
        Assert.DoesNotContain("Project:", report);
    }

    [Fact]
    public void Generate_console_summary_only_omits_project_details()
    {
        var generator = new ReportGenerator();

        var report = generator.Generate(CreateAnalysisResult(), ReportFormat.Console, summaryOnly: true);

        Assert.Contains("Summary", report);
        Assert.DoesNotContain("Project:", report);
        Assert.DoesNotContain("Namespace | Files", report);
    }

    [Fact]
    public void Generate_markdown_summary_only_omits_project_sections()
    {
        var generator = new ReportGenerator();

        var report = generator.Generate(CreateAnalysisResult(), ReportFormat.Markdown, summaryOnly: true);

        Assert.Contains("# GlobalUsing Report", report);
        Assert.Contains("## Summary", report);
        Assert.DoesNotContain("## MyProject", report);
        Assert.DoesNotContain("| Namespace | Files |", report);
    }

    [Fact]
    public void Generate_json_summary_only_serializes_summary_object()
    {
        var generator = new ReportGenerator();

        var report = generator.Generate(CreateAnalysisResult(), ReportFormat.Json, summaryOnly: true);
        var summary = JsonSerializer.Deserialize<AnalysisSummary>(report);

        Assert.NotNull(summary);
        Assert.Equal(3, summary.TotalCSharpFilesAnalyzed);
        Assert.Equal(2, summary.CandidatesAboveThreshold);
    }

    [Fact]
    public void Generate_json_with_target_namespace_serializes_namespace_summary()
    {
        var generator = new ReportGenerator();

        var report = generator.Generate(CreateAnalysisResult(), ReportFormat.Json, targetNamespaces: ["System.Linq"]);
        var summary = JsonSerializer.Deserialize<NamespaceReportSummary>(report);

        Assert.NotNull(summary);
        Assert.Equal("System.Linq", summary.Namespace);
        Assert.Equal(3, summary.FilesUsingNamespace);
        Assert.Equal(100, summary.UsagePercentage);
    }

    [Fact]
    public void Generate_json_with_multiple_target_namespaces_serializes_namespace_summaries()
    {
        var generator = new ReportGenerator();

        var report = generator.Generate(CreateAnalysisResult(), ReportFormat.Json, targetNamespaces: ["System.Linq", "System.Text.Json"]);
        var summaries = JsonSerializer.Deserialize<NamespaceReportSummary[]>(report);

        Assert.NotNull(summaries);
        Assert.Equal(2, summaries.Length);
        Assert.Contains(summaries, summary => summary.Namespace == "System.Linq" && summary.FilesUsingNamespace == 3);
        Assert.Contains(summaries, summary => summary.Namespace == "System.Text.Json" && summary.FilesUsingNamespace == 1);
    }

    private static AnalysisResult CreateAnalysisResult()
    {
        var projectSummary = new AnalysisSummary(
            TotalCSharpFilesAnalyzed: 3,
            TotalExplicitUsingDirectives: 5,
            UniqueNamespacesDiscovered: 2,
            CandidatesAboveThreshold: 2,
            EstimatedReductionOfDuplicatedUsings: 4);

        var project = new ProjectAnalysisResult(
            Name: "MyProject",
            RootPath: "/repo/src/MyProject",
            ProjectPath: "/repo/src/MyProject/MyProject.csproj",
            NamespaceUsages:
            [
                new NamespaceUsage(
                    new UsingSignature("System.Linq", UsingKind.Normal),
                    FileCount: 3,
                    TotalFiles: 3,
                    Percentage: 100,
                    Status: RecommendationStatus.CandidateForGlobal),
                new NamespaceUsage(
                    new UsingSignature("System.Text.Json", UsingKind.Normal),
                    FileCount: 1,
                    TotalFiles: 3,
                    Percentage: 33.33,
                    Status: RecommendationStatus.KeepLocal),
            ],
            PromotionCandidates:
            [
                new PromotionCandidate(
                    new UsingSignature("System.Linq", UsingKind.Normal),
                    FileCount: 3,
                    TotalFiles: 3,
                    Percentage: 100),
            ],
            ExistingGlobalUsings: [],
            Summary: projectSummary);

        return new AnalysisResult([project], projectSummary);
    }
}
