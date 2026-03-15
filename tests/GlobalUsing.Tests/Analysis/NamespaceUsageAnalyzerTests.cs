using GlobalUsing.Analysis;
using GlobalUsing.Core.Enums;
using GlobalUsing.Core.Models;

namespace GlobalUsing.Tests.Analysis;

public sealed class NamespaceUsageAnalyzerTests
{
    [Fact]
    public void Analyze_counts_local_usings_once_per_file_and_filters_implicit_usings()
    {
        var analyzer = new NamespaceUsageAnalyzer();
        var project = new DiscoveredProject(
            RootPath: "C:\\repo\\Project",
            ProjectPath: "C:\\repo\\Project\\Project.csproj",
            ImplicitUsingsEnabled: true,
            ImplicitNamespaces: System.Collections.Frozen.FrozenSet.ToFrozenSet(["System"], StringComparer.Ordinal),
            CSharpFiles: ["a.cs", "b.cs"]);
        var sourceFiles = new[]
        {
            new SourceFileUsings(
                "a.cs",
                [
                    new CollectedUsingDirective(new UsingSignature("System", UsingKind.Normal), "using System;", false, "a.cs"),
                    new CollectedUsingDirective(new UsingSignature("System.Linq", UsingKind.Normal), "using System.Linq;", false, "a.cs"),
                    new CollectedUsingDirective(new UsingSignature("System.Linq", UsingKind.Normal), "using System.Linq;", false, "a.cs"),
                ]),
            new SourceFileUsings(
                "b.cs",
                [
                    new CollectedUsingDirective(new UsingSignature("System.Linq", UsingKind.Normal), "using System.Linq;", false, "b.cs"),
                    new CollectedUsingDirective(new UsingSignature("System.Collections", UsingKind.Normal), "global using System.Collections;", true, "b.cs"),
                ]),
        };

        var snapshot = analyzer.Analyze(project, sourceFiles, AnalysisOptions.Default());

        Assert.Single(snapshot.LocalUsages.Where(metric => metric.Signature.Name == "System.Linq" && metric.FileCount == 2));
        Assert.DoesNotContain(snapshot.LocalUsages, metric => metric.Signature.Name == "System");
        Assert.Single(snapshot.ExistingGlobalUsings.Where(signature => signature.Name == "System.Collections"));
        Assert.Equal(4, snapshot.TotalExplicitUsingDirectives);
    }
}
