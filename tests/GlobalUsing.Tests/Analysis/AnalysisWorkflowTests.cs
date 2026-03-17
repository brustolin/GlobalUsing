using GlobalUsing.Analysis;
using GlobalUsing.Core.Models;
using GlobalUsing.Infrastructure;
using GlobalUsing.Roslyn;
using Microsoft.Extensions.Logging.Abstractions;

namespace GlobalUsing.Tests.Analysis;

public sealed class AnalysisWorkflowTests
{
    [Fact]
    public async Task ApplyAsync_with_target_namespace_promotes_only_requested_namespace()
    {
        using var temporaryDirectory = TemporaryDirectory.Create();
        var root = temporaryDirectory.Path;
        await File.WriteAllTextAsync(
            Path.Combine(root, "App.csproj"),
            "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><ImplicitUsings>disable</ImplicitUsings></PropertyGroup></Project>");
        await File.WriteAllTextAsync(
            Path.Combine(root, "Program.cs"),
            """
            using System.Linq;
            using System.Text.Json;

            namespace Demo;
            """);
        await File.WriteAllTextAsync(
            Path.Combine(root, "Other.cs"),
            """
            using System.Text.Json;

            namespace Demo;
            """);

        var fileSystem = new PhysicalFileSystem();
        var workflow = new AnalysisWorkflow(
            new FileDiscoveryService(fileSystem, NullLogger<FileDiscoveryService>.Instance),
            new CachingUsingCollector(new SourceDocumentCache(fileSystem), NullLogger<CachingUsingCollector>.Instance),
            new NamespaceUsageAnalyzer(),
            new GlobalUsingRecommender(),
            new GlobalUsingsWriter(fileSystem),
            new SourceFileRewriter(new SourceDocumentCache(fileSystem)),
            fileSystem,
            NullLogger<AnalysisWorkflow>.Instance);
        var options = AnalysisOptions.Default(root) with
        {
            ThresholdPercentage = 100,
            MinFiles = 2,
            TargetNamespace = "System.Linq",
        };

        var result = await workflow.ApplyAsync(options, CancellationToken.None);

        var globalUsingsPath = Path.Combine(root, "GlobalUsings.cs");
        Assert.True(File.Exists(globalUsingsPath));
        await AssertFileContainsAsync(globalUsingsPath, "global using System.Linq;");
        await AssertFileDoesNotContainAsync(globalUsingsPath, "global using System.Text.Json;");
        await AssertFileDoesNotContainAsync(Path.Combine(root, "Program.cs"), "using System.Linq;");
        await AssertFileContainsAsync(Path.Combine(root, "Program.cs"), "using System.Text.Json;");
        await AssertFileContainsAsync(Path.Combine(root, "Other.cs"), "using System.Text.Json;");
        Assert.Contains(result.AnalysisResult.Projects.SelectMany(project => project.PromotionCandidates), candidate => candidate.Signature.Name == "System.Linq");
    }

    private static async Task AssertFileContainsAsync(string path, string expected)
    {
        var content = await File.ReadAllTextAsync(path);
        Assert.Contains(expected, content, StringComparison.Ordinal);
    }

    private static async Task AssertFileDoesNotContainAsync(string path, string expected)
    {
        var content = await File.ReadAllTextAsync(path);
        Assert.DoesNotContain(expected, content, StringComparison.Ordinal);
    }
}
