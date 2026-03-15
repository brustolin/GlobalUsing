using System.Collections.Immutable;
using GlobalUsing.Core.Enums;
using GlobalUsing.Core.Interfaces;
using GlobalUsing.Core.Models;
using GlobalUsing.Core.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GlobalUsing.Infrastructure;

public sealed class GlobalUsingsWriter(IFileSystem fileSystem) : IGlobalUsingsWriter
{
    public async Task<GlobalUsingsFileUpdate> BuildUpdateAsync(
        string filePath,
        IReadOnlyCollection<UsingSignature> promotedUsings,
        CancellationToken cancellationToken)
    {
        var exists = fileSystem.FileExists(filePath);
        var originalContent = exists
            ? await fileSystem.ReadAllTextAsync(filePath, cancellationToken)
            : string.Empty;
        var root = exists
            ? (CompilationUnitSyntax)CSharpSyntaxTree.ParseText(originalContent, cancellationToken: cancellationToken).GetRoot(cancellationToken)
            : SyntaxFactory.CompilationUnit();
        var existingGlobalNodes = root.Usings
            .Where(static usingDirective => usingDirective.GlobalKeyword.IsKind(SyntaxKind.GlobalKeyword))
            .Select(usingDirective => new
            {
                Node = usingDirective,
                Signature = TryCreateSignature(usingDirective),
            })
            .Where(item => item.Signature is not null)
            .ToDictionary(item => item.Signature!, item => item.Node);
        var mergedSignatures = existingGlobalNodes.Keys
            .Concat(promotedUsings)
            .Distinct()
            .OrderBy(signature => signature, UsingSignatureComparer.Instance)
            .ToImmutableArray();
        var globalUsingNodes = mergedSignatures
            .Select(signature => existingGlobalNodes.TryGetValue(signature, out var existingNode) ? EnsureLineEnding(existingNode) : CreateGlobalUsing(signature))
            .ToImmutableArray();
        var nonGlobalUsings = root.Usings.Where(static usingDirective => !usingDirective.GlobalKeyword.IsKind(SyntaxKind.GlobalKeyword));
        var updatedRoot = root.WithUsings(SyntaxFactory.List(globalUsingNodes.Concat(nonGlobalUsings)));
        var content = exists
            ? updatedRoot.ToFullString()
            : CreateNewFileContent(globalUsingNodes);
        var addedCount = mergedSignatures.Length - existingGlobalNodes.Count;

        return new GlobalUsingsFileUpdate(filePath, content, exists, !string.Equals(originalContent, content, StringComparison.Ordinal), addedCount, mergedSignatures);
    }

    private static string CreateNewFileContent(IEnumerable<UsingDirectiveSyntax> globalUsingNodes)
    {
        var root = SyntaxFactory.CompilationUnit().WithUsings(SyntaxFactory.List(globalUsingNodes));
        var content = root.NormalizeWhitespace(eol: Environment.NewLine).ToFullString();
        return content.EndsWith(Environment.NewLine, StringComparison.Ordinal) ? content : $"{content}{Environment.NewLine}";
    }

    private static UsingDirectiveSyntax EnsureLineEnding(UsingDirectiveSyntax usingDirective) =>
        usingDirective.GetTrailingTrivia().Any(trivia => trivia.IsKind(SyntaxKind.EndOfLineTrivia))
            ? usingDirective
            : usingDirective.WithTrailingTrivia(usingDirective.GetTrailingTrivia().Add(SyntaxFactory.ElasticCarriageReturnLineFeed));

    private static UsingDirectiveSyntax CreateGlobalUsing(UsingSignature signature)
    {
        var usingDirective = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(signature.Name))
            .WithUsingKeyword(SyntaxFactory.Token(SyntaxKind.UsingKeyword))
            .WithGlobalKeyword(SyntaxFactory.Token(SyntaxKind.GlobalKeyword).WithTrailingTrivia(SyntaxFactory.Space))
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
            .WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed);

        return signature.Kind switch
        {
            UsingKind.Static => usingDirective.WithStaticKeyword(SyntaxFactory.Token(SyntaxKind.StaticKeyword).WithTrailingTrivia(SyntaxFactory.Space)),
            UsingKind.Alias when !string.IsNullOrWhiteSpace(signature.Alias) => usingDirective.WithAlias(
                SyntaxFactory.NameEquals(SyntaxFactory.IdentifierName(signature.Alias!)).WithEqualsToken(SyntaxFactory.Token(SyntaxKind.EqualsToken).WithTrailingTrivia(SyntaxFactory.Space))),
            _ => usingDirective,
        };
    }

    private static UsingSignature? TryCreateSignature(UsingDirectiveSyntax usingDirective)
    {
        if (usingDirective.Name is null)
        {
            return null;
        }

        if (usingDirective.Alias is not null)
        {
            return new UsingSignature(usingDirective.Name.ToString(), UsingKind.Alias, usingDirective.Alias.Name.Identifier.ValueText);
        }

        return usingDirective.StaticKeyword.IsKind(SyntaxKind.StaticKeyword)
            ? new UsingSignature(usingDirective.Name.ToString(), UsingKind.Static)
            : new UsingSignature(usingDirective.Name.ToString(), UsingKind.Normal);
    }
}
