using System.Collections.Immutable;
using GlobalUsing.Core.Enums;
using GlobalUsing.Core.Interfaces;
using GlobalUsing.Core.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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

        var removableSet = removableNodes.ToImmutableHashSet();
        var updatedRoot = RewriteUsings(cachedSourceFile.Root, removableSet);
        var updatedContent = updatedRoot.ToFullString();
        return new SourceFileRewriteResult(filePath, updatedContent, !string.Equals(cachedSourceFile.Content, updatedContent, StringComparison.Ordinal), removableNodes.Length);
    }

    private static bool IsRemovalAllowed(UsingKind kind, AnalysisOptions options) =>
        kind switch
        {
            UsingKind.Static => options.IncludeStatic,
            UsingKind.Alias => options.IncludeAlias,
            _ => true,
        };

    private static CompilationUnitSyntax RewriteUsings(
        CompilationUnitSyntax root,
        IReadOnlySet<UsingDirectiveSyntax> removableUsings)
    {
        var rewrittenUsings = new List<UsingDirectiveSyntax>(root.Usings.Count);
        var pendingTrivia = new List<SyntaxTrivia>();

        foreach (var usingDirective in root.Usings)
        {
            if (removableUsings.Contains(usingDirective))
            {
                pendingTrivia.AddRange(ExtractPreservedTrivia(usingDirective.GetLeadingTrivia()));
                pendingTrivia.AddRange(ExtractPreservedTrivia(usingDirective.GetTrailingTrivia()));
                continue;
            }

            var updatedUsingDirective = usingDirective;
            if (pendingTrivia.Count > 0)
            {
                updatedUsingDirective = updatedUsingDirective.WithLeadingTrivia(MergeLeadingTrivia(pendingTrivia, updatedUsingDirective.GetLeadingTrivia()));
                pendingTrivia.Clear();
            }

            rewrittenUsings.Add(updatedUsingDirective);
        }

        var updatedRoot = root.WithUsings(SyntaxFactory.List(rewrittenUsings));
        if (pendingTrivia.Count == 0)
        {
            return updatedRoot;
        }

        var preservedTrivia = NormalizePreservedTrivia(pendingTrivia);
        if (preservedTrivia.Count == 0)
        {
            return updatedRoot;
        }

        var firstToken = updatedRoot.GetFirstToken(includeZeroWidth: true);
        var rewrittenToken = firstToken.WithLeadingTrivia(preservedTrivia.AddRange(firstToken.LeadingTrivia));
        return updatedRoot.ReplaceToken(firstToken, rewrittenToken);
    }

    private static SyntaxTriviaList MergeLeadingTrivia(IEnumerable<SyntaxTrivia> pendingTrivia, SyntaxTriviaList existingTrivia) =>
        NormalizePreservedTrivia(pendingTrivia).AddRange(existingTrivia);

    private static IEnumerable<SyntaxTrivia> ExtractPreservedTrivia(SyntaxTriviaList triviaList)
    {
        foreach (var trivia in triviaList)
        {
            if (trivia.IsKind(SyntaxKind.WhitespaceTrivia))
            {
                continue;
            }

            yield return trivia;
        }
    }

    private static SyntaxTriviaList NormalizePreservedTrivia(IEnumerable<SyntaxTrivia> trivia)
    {
        var result = new List<SyntaxTrivia>();
        var hasMeaningfulTrivia = false;
        var lastWasEndOfLine = false;

        foreach (var current in trivia)
        {
            if (current.IsKind(SyntaxKind.WhitespaceTrivia))
            {
                continue;
            }

            if (current.IsKind(SyntaxKind.EndOfLineTrivia))
            {
                if (hasMeaningfulTrivia && !lastWasEndOfLine)
                {
                    result.Add(SyntaxFactory.ElasticCarriageReturnLineFeed);
                    lastWasEndOfLine = true;
                }

                continue;
            }

            result.Add(current);
            hasMeaningfulTrivia = true;
            lastWasEndOfLine = false;
        }

        if (hasMeaningfulTrivia && !lastWasEndOfLine)
        {
            result.Add(SyntaxFactory.ElasticCarriageReturnLineFeed);
        }

        return SyntaxFactory.TriviaList(result);
    }
}
