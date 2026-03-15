using System.Collections.Frozen;
using System.Collections.Immutable;

namespace GlobalUsing.Core.Models;

public sealed record DiscoveredProject(
    string RootPath,
    string? ProjectPath,
    bool ImplicitUsingsEnabled,
    FrozenSet<string> ImplicitNamespaces,
    ImmutableArray<string> CSharpFiles);
