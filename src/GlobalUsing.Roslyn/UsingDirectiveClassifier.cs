using GlobalUsing.Core.Enums;
using GlobalUsing.Core.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;

namespace GlobalUsing.Roslyn;

internal static class UsingDirectiveClassifier
{
    public static CollectedUsingDirective? TryCreate(UsingDirectiveSyntax usingDirective, string filePath)
    {
        var signature = TryCreateSignature(usingDirective);
        return signature is null
            ? null
            : new CollectedUsingDirective(signature, usingDirective.ToFullString().Trim(), usingDirective.GlobalKeyword.IsKind(SyntaxKind.GlobalKeyword), filePath);
    }

    public static UsingSignature? TryCreateSignature(UsingDirectiveSyntax usingDirective)
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
