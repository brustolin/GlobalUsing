using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GlobalUsing.Roslyn;

internal sealed record CachedSourceFile(
    string Content,
    CompilationUnitSyntax Root);
