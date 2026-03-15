using GlobalUsing.Core.Enums;

namespace GlobalUsing.Core.Models;

public sealed record UsingSignature(string Name, UsingKind Kind, string? Alias = null)
{
    public string SortKey =>
        Kind switch
        {
            UsingKind.Alias => $"{Alias ?? string.Empty}={Name}",
            UsingKind.Static => $"static:{Name}",
            _ => Name,
        };

    public string DisplayName =>
        Kind switch
        {
            UsingKind.Alias when Alias is { Length: > 0 } alias => $"{alias} = {Name}",
            UsingKind.Static => $"static {Name}",
            _ => Name,
        };
}
