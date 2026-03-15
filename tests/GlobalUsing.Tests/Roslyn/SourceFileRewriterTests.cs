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

        Assert.True(result.HasChanges);
        Assert.Equal(1, result.RemovedUsings);
        Assert.Contains("// keep this comment", result.Content, StringComparison.Ordinal);
        Assert.DoesNotContain("using System.Linq;", result.Content, StringComparison.Ordinal);
        Assert.Contains("using System.Text.Json;", result.Content, StringComparison.Ordinal);
        Assert.Contains("""
            using System;
            // keep this comment
            using System.Text.Json;
            """, NormalizeLineEndings(result.Content), StringComparison.Ordinal);
        Assert.DoesNotContain("using System;\n\n// keep this comment", NormalizeLineEndings(result.Content), StringComparison.Ordinal);
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

        Assert.False(secondPass.HasChanges);
        Assert.Equal(firstPass.Content, secondPass.Content);
    }

    [Fact]
    public async Task RemoveRedundantUsingsAsync_keeps_remaining_usings_packed_together()
    {
        using var temporaryDirectory = TemporaryDirectory.Create();
        var filePath = Path.Combine(temporaryDirectory.Path, "Sample.cs");
        var source = """
            using System;
            using System.Linq;
            using System.Text.Json;
            using Demo.Helpers;

            namespace Demo;
            """;
        await File.WriteAllTextAsync(filePath, source);

        var rewriter = new SourceFileRewriter(new SourceDocumentCache(new PhysicalFileSystem()));

        var result = await rewriter.RemoveRedundantUsingsAsync(
            filePath,
            new HashSet<UsingSignature>
            {
                new("System.Linq", UsingKind.Normal),
                new("System.Text.Json", UsingKind.Normal),
            },
            AnalysisOptions.Default(),
            CancellationToken.None);

        Assert.Equal("""
            using System;
            using Demo.Helpers;

            namespace Demo;
            """, NormalizeLineEndings(result.Content));
    }

    private static string NormalizeLineEndings(string value) => value.Replace("\r\n", "\n", StringComparison.Ordinal);
}
