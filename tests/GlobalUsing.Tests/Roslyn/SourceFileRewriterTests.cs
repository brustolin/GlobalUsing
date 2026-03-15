
using GlobalUsing.Core.Enums;
using GlobalUsing.Core.Models;
using GlobalUsing.Infrastructure;
using GlobalUsing.Roslyn;

namespace GlobalUsing.Tests.Roslyn;

public sealed class SourceFileRewriterTests
{
    [Fact]
    public async Task RemoveRedundantUsingsAsync_removes_matching_usings_and_preserves_comments()
    {
        using var temporaryDirectory = TemporaryDirectory.Create();
        var filePath = Path.Combine(temporaryDirectory.Path, "Sample.cs");
        var source = """
            using System;
            // keep this comment
            using System.Linq;
            using System.Text.Json;

            namespace Demo;
            """;
        await File.WriteAllTextAsync(filePath, source);

        var cache = new SourceDocumentCache(new PhysicalFileSystem());
        var rewriter = new SourceFileRewriter(cache);

        var result = await rewriter.RemoveRedundantUsingsAsync(
            filePath,
            new HashSet<UsingSignature> { new("System.Linq", UsingKind.Normal) },
            AnalysisOptions.Default(),
            CancellationToken.None);

        result.HasChanges.Should().BeTrue();
        result.RemovedUsings.Should().Be(1);
        result.Content.Should().Contain("// keep this comment");
        result.Content.Should().NotContain("using System.Linq;");
        result.Content.Should().Contain("using System.Text.Json;");
    }

    [Fact]
    public async Task RemoveRedundantUsingsAsync_is_idempotent()
    {
        using var temporaryDirectory = TemporaryDirectory.Create();
        var filePath = Path.Combine(temporaryDirectory.Path, "Sample.cs");
        var source = """
            using System;
            using System.Linq;

            namespace Demo;
            """;
        await File.WriteAllTextAsync(filePath, source);

        var fileSystem = new PhysicalFileSystem();
        var cache = new SourceDocumentCache(fileSystem);
        var rewriter = new SourceFileRewriter(cache);
        var promotedUsings = new HashSet<UsingSignature> { new("System.Linq", UsingKind.Normal) };
        var firstPass = await rewriter.RemoveRedundantUsingsAsync(filePath, promotedUsings, AnalysisOptions.Default(), CancellationToken.None);
        await File.WriteAllTextAsync(filePath, firstPass.Content);

        var secondCache = new SourceDocumentCache(fileSystem);
        var secondPass = await new SourceFileRewriter(secondCache).RemoveRedundantUsingsAsync(filePath, promotedUsings, AnalysisOptions.Default(), CancellationToken.None);

        secondPass.HasChanges.Should().BeFalse();
        secondPass.Content.Should().Be(firstPass.Content);
    }
}
