using FluentAssertions;
using GlobalUsing.Core.Enums;
using GlobalUsing.Infrastructure;
using GlobalUsing.Roslyn;
using Microsoft.Extensions.Logging.Abstractions;

namespace GlobalUsing.Tests.Roslyn;

public sealed class CachingUsingCollectorTests
{
    [Theory]
    [InlineData("using System.Linq;", "System.Linq", UsingKind.Normal, false, null)]
    [InlineData("global using System.Linq;", "System.Linq", UsingKind.Normal, true, null)]
    [InlineData("using static System.Math;", "System.Math", UsingKind.Static, false, null)]
    [InlineData("using Json = System.Text.Json;", "System.Text.Json", UsingKind.Alias, false, "Json")]
    public async Task CollectAsync_detects_using_kinds(
        string source,
        string expectedName,
        UsingKind expectedKind,
        bool expectedGlobal,
        string? expectedAlias)
    {
        using var temporaryDirectory = TemporaryDirectory.Create();
        var filePath = Path.Combine(temporaryDirectory.Path, "Sample.cs");
        await File.WriteAllTextAsync(filePath, source);
        var collector = new CachingUsingCollector(new SourceDocumentCache(new PhysicalFileSystem()), NullLogger<CachingUsingCollector>.Instance);

        var result = await collector.CollectAsync([filePath], CancellationToken.None);

        var usingDirective = result.Single().UsingDirectives.Single();
        usingDirective.Signature.Name.Should().Be(expectedName);
        usingDirective.Signature.Kind.Should().Be(expectedKind);
        usingDirective.IsGlobal.Should().Be(expectedGlobal);
        usingDirective.Signature.Alias.Should().Be(expectedAlias);
    }
}
