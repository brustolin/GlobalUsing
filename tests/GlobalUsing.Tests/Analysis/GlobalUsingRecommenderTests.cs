
using GlobalUsing.Analysis;
using GlobalUsing.Core.Enums;
using GlobalUsing.Core.Models;

namespace GlobalUsing.Tests.Analysis;

public sealed class GlobalUsingRecommenderTests
{
    [Fact]
    public void Recommend_marks_candidates_and_existing_globals_correctly()
    {
        var recommender = new GlobalUsingRecommender();
        var project = new DiscoveredProject(
            RootPath: "C:\\repo\\src\\Project",
            ProjectPath: "C:\\repo\\src\\Project\\Project.csproj",
            ImplicitUsingsEnabled: false,
            ImplicitNamespaces: System.Collections.Frozen.FrozenSet.ToFrozenSet<string>([]),
            CSharpFiles: ["a.cs", "b.cs", "c.cs", "d.cs"]);
        var snapshot = new UsageAnalysisSnapshot(
            SourceFiles: [],
            LocalUsages:
            [
                new NamespaceUsageMetric(new UsingSignature("System.Linq", UsingKind.Normal), 4, 4, 100),
                new NamespaceUsageMetric(new UsingSignature("System.Text.Json", UsingKind.Normal), 2, 4, 50),
            ],
            ExistingGlobalUsings:
            [
                new UsingSignature("System", UsingKind.Normal),
            ],
            TotalAnalyzedFiles: 4,
            TotalExplicitUsingDirectives: 6);
        var options = AnalysisOptions.Default() with { ThresholdPercentage = 75, MinFiles = 2 };

        var result = recommender.Recommend(project, snapshot, options);

        result.PromotionCandidates.Should().ContainSingle(candidate => candidate.Signature.Name == "System.Linq");
        result.NamespaceUsages.Should().Contain(usage => usage.Signature.Name == "System" && usage.Status == RecommendationStatus.AlreadyGlobal);
        result.NamespaceUsages.Should().Contain(usage => usage.Signature.Name == "System.Text.Json" && usage.Status == RecommendationStatus.KeepLocal);
        result.Summary.CandidatesAboveThreshold.Should().Be(1);
        result.Summary.EstimatedReductionOfDuplicatedUsings.Should().Be(4);
    }
}
