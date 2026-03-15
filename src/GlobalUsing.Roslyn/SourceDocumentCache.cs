using System.Collections.Concurrent;
using GlobalUsing.Core.Interfaces;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GlobalUsing.Roslyn;

public sealed class SourceDocumentCache(IFileSystem fileSystem)
{
    private readonly ConcurrentDictionary<string, CachedSourceFile> _cache = new(StringComparer.OrdinalIgnoreCase);

    internal async Task<CachedSourceFile> GetOrLoadAsync(string filePath, CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(filePath, out var cached))
        {
            return cached;
        }

        var content = await fileSystem.ReadAllTextAsync(filePath, cancellationToken);
        var tree = CSharpSyntaxTree.ParseText(content, path: filePath, cancellationToken: cancellationToken);
        var root = (CompilationUnitSyntax)await tree.GetRootAsync(cancellationToken);
        cached = new CachedSourceFile(content, root);
        _cache[filePath] = cached;
        return cached;
    }
}
