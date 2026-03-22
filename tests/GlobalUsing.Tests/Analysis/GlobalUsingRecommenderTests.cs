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

        Assert.Single(result.PromotionCandidates.Where(candidate => candidate.Signature.Name == "System.Linq"));
        Assert.Contains(result.NamespaceUsages, usage => usage.Signature.Name == "System" && usage.Status == RecommendationStatus.AlreadyGlobal);
        Assert.Contains(result.NamespaceUsages, usage => usage.Signature.Name == "System.Text.Json" && usage.Status == RecommendationStatus.KeepLocal);
        Assert.Equal(1, result.Summary.CandidatesAboveThreshold);
        Assert.Equal(4, result.Summary.EstimatedReductionOfDuplicatedUsings);
    }

    [Fact]
    public void Recommend_promotes_requested_namespace_even_below_threshold()
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
                new NamespaceUsageMetric(new UsingSignature("System.Text.Json", UsingKind.Normal), 1, 4, 25),
            ],
            ExistingGlobalUsings: [],
            TotalAnalyzedFiles: 4,
            TotalExplicitUsingDirectives: 1);
        var options = AnalysisOptions.Default() with
        {
            ThresholdPercentage = 80,
            MinFiles = 2,
            TargetNamespaces = ["System.Text.Json"],
        };

        var result = recommender.Recommend(project, snapshot, options);

        Assert.Contains(result.PromotionCandidates, candidate => candidate.Signature.Name == "System.Text.Json");
        Assert.Contains(result.NamespaceUsages, usage => usage.Signature.Name == "System.Text.Json" && usage.Status == RecommendationStatus.CandidateForGlobal);
    }

    [Fact]
    public void Recommend_promotes_multiple_requested_namespaces_even_below_threshold()
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
                new NamespaceUsageMetric(new UsingSignature("System.Text.Json", UsingKind.Normal), 1, 4, 25),
                new NamespaceUsageMetric(new UsingSignature("System.Net.Http", UsingKind.Normal), 1, 4, 25),
            ],
            ExistingGlobalUsings: [],
            TotalAnalyzedFiles: 4,
            TotalExplicitUsingDirectives: 2);
        var options = AnalysisOptions.Default() with
        {
            ThresholdPercentage = 80,
            MinFiles = 2,
            TargetNamespaces = ["System.Text.Json", "System.Net.Http"],
        };

        var result = recommender.Recommend(project, snapshot, options);

        Assert.Contains(result.PromotionCandidates, candidate => candidate.Signature.Name == "System.Text.Json");
        Assert.Contains(result.PromotionCandidates, candidate => candidate.Signature.Name == "System.Net.Http");
    }

    [Fact]
    public void Recommend_marks_move_namespace_as_candidate_while_preserving_normal_behavior()
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
                new NamespaceUsageMetric(new UsingSignature("System.Text.Json", UsingKind.Normal), 1, 4, 25),
                new NamespaceUsageMetric(new UsingSignature("System.Net.Http", UsingKind.Normal), 1, 4, 25),
            ],
            ExistingGlobalUsings: [],
            TotalAnalyzedFiles: 4,
            TotalExplicitUsingDirectives: 6);
        var options = AnalysisOptions.Default() with
        {
            ThresholdPercentage = 75,
            MinFiles = 2,
            MoveNamespaces = ["System.Text.Json"],
        };

        var result = recommender.Recommend(project, snapshot, options);

        Assert.Contains(result.PromotionCandidates, candidate => candidate.Signature.Name == "System.Linq");
        Assert.Contains(result.PromotionCandidates, candidate => candidate.Signature.Name == "System.Text.Json");
        Assert.DoesNotContain(result.PromotionCandidates, candidate => candidate.Signature.Name == "System.Net.Http");
        Assert.Contains(result.NamespaceUsages, usage => usage.Signature.Name == "System.Text.Json" && usage.Status == RecommendationStatus.CandidateForGlobal);
    }

    [Fact]
    public void Recommend_keeps_ignored_namespace_local_even_when_moved_or_above_threshold()
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
                new NamespaceUsageMetric(new UsingSignature("System.Text.Json", UsingKind.Normal), 1, 4, 25),
            ],
            ExistingGlobalUsings: [],
            TotalAnalyzedFiles: 4,
            TotalExplicitUsingDirectives: 5);
        var options = AnalysisOptions.Default() with
        {
            ThresholdPercentage = 75,
            MinFiles = 2,
            MoveNamespaces = ["System.Text.Json"],
            IgnoreNamespaces = ["System.Linq", "System.Text.Json"],
        };

        var result = recommender.Recommend(project, snapshot, options);

        Assert.DoesNotContain(result.PromotionCandidates, candidate => candidate.Signature.Name == "System.Linq");
        Assert.DoesNotContain(result.PromotionCandidates, candidate => candidate.Signature.Name == "System.Text.Json");
        Assert.Contains(result.NamespaceUsages, usage => usage.Signature.Name == "System.Linq" && usage.Status == RecommendationStatus.KeepLocal);
        Assert.Contains(result.NamespaceUsages, usage => usage.Signature.Name == "System.Text.Json" && usage.Status == RecommendationStatus.KeepLocal);
    }
}
