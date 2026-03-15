using System.Collections.Immutable;
using GlobalUsing.Core.Enums;
using GlobalUsing.Core.Interfaces;
using GlobalUsing.Core.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace GlobalUsing.Roslyn;

public sealed class SourceFileRewriter(SourceDocumentCache sourceDocumentCache) : ISourceFileRewriter
{
    public async Task<SourceFileRewriteResult> RemoveRedundantUsingsAsync(
        string filePath,
        IReadOnlySet<UsingSignature> promotedUsings,
        AnalysisOptions options,
        CancellationToken cancellationToken)
    {
        var cachedSourceFile = await sourceDocumentCache.GetOrLoadAsync(filePath, cancellationToken);
        var removableNodes = cachedSourceFile.Root.Usings
            .Where(static usingDirective => !usingDirective.GlobalKeyword.IsKind(SyntaxKind.GlobalKeyword))
            .Where(usingDirective => UsingDirectiveClassifier.TryCreateSignature(usingDirective) is { } signature
                && promotedUsings.Contains(signature)
                && IsRemovalAllowed(signature.Kind, options))
            .ToImmutableArray();

        if (removableNodes.Length == 0)
        {
            return new SourceFileRewriteResult(filePath, cachedSourceFile.Content, false, 0);
        }

        var updatedRoot = cachedSourceFile.Root.RemoveNodes(removableNodes, SyntaxRemoveOptions.KeepExteriorTrivia);
        var updatedContent = updatedRoot?.ToFullString() ?? cachedSourceFile.Content;
        return new SourceFileRewriteResult(filePath, updatedContent, !string.Equals(cachedSourceFile.Content, updatedContent, StringComparison.Ordinal), removableNodes.Length);
    }

    private static bool IsRemovalAllowed(UsingKind kind, AnalysisOptions options) =>
        kind switch
        {
            UsingKind.Static => options.IncludeStatic,
            UsingKind.Alias => options.IncludeAlias,
            _ => true,
        };
}
