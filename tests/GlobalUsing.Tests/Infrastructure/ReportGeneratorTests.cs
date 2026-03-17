using System.Collections.Immutable;
using System.Text.Json;
using GlobalUsing.Core.Enums;
using GlobalUsing.Core.Models;
using GlobalUsing.Infrastructure;

namespace GlobalUsing.Tests.Infrastructure;

public sealed class ReportGeneratorTests
{
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
