using System.Collections.Immutable;

namespace GlobalUsing.Core.Models;

public sealed record GlobalUsingsFileUpdate(
    string FilePath,
    string Content,
    bool Exists,
    bool HasChanges,
    int AddedCount,
    ImmutableArray<UsingSignature> FinalUsings);
