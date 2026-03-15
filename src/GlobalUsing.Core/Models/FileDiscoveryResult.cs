using System.Collections.Immutable;

namespace GlobalUsing.Core.Models;

public sealed record FileDiscoveryResult(
    ImmutableArray<DiscoveredProject> Projects,
    ImmutableArray<string> AllFiles);
