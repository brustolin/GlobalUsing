using System.Collections.Concurrent;
using System.Collections.Immutable;
using GlobalUsing.Core.Interfaces;
using GlobalUsing.Core.Models;
using Microsoft.Extensions.Logging;

namespace GlobalUsing.Roslyn;

public sealed class CachingUsingCollector(
    SourceDocumentCache sourceDocumentCache,
    ILogger<CachingUsingCollector> logger) : IUsingCollector
{
    public async Task<IReadOnlyList<SourceFileUsings>> CollectAsync(
        IReadOnlyCollection<string> filePaths,
        CancellationToken cancellationToken)
    {
        var results = new ConcurrentBag<SourceFileUsings>();

        await Parallel.ForEachAsync(filePaths, cancellationToken, async (filePath, token) =>
        {
            var cachedSourceFile = await sourceDocumentCache.GetOrLoadAsync(filePath, token);
            var usingDirectives = cachedSourceFile.Root.Usings
                .Select(usingDirective => UsingDirectiveClassifier.TryCreate(usingDirective, filePath))
                .Where(model => model is not null)
                .Cast<CollectedUsingDirective>()
                .ToImmutableArray();

            results.Add(new SourceFileUsings(filePath, usingDirectives));
            logger.LogDebug("Collected {UsingCount} using directives from {FilePath}.", usingDirectives.Length, filePath);
        });

        return results
            .OrderBy(sourceFile => sourceFile.FilePath, StringComparer.OrdinalIgnoreCase)
            .ToImmutableArray();
    }
}
