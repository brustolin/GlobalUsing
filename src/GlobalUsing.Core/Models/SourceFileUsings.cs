using System.Collections.Immutable;

namespace GlobalUsing.Core.Models;

public sealed record SourceFileUsings(
    string FilePath,
    ImmutableArray<CollectedUsingDirective> UsingDirectives);
