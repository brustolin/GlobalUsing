using GlobalUsing.Core.Models;

namespace GlobalUsing.Core.Utilities;

public sealed class UsingSignatureComparer : IComparer<UsingSignature>, IEqualityComparer<UsingSignature>
{
    public static UsingSignatureComparer Instance { get; } = new();

    public int Compare(UsingSignature? x, UsingSignature? y)
    {
        if (ReferenceEquals(x, y))
        {
            return 0;
        }

        if (x is null)
        {
            return -1;
        }

        if (y is null)
        {
            return 1;
        }

        return StringComparer.Ordinal.Compare($"{x.Kind}:{x.SortKey}", $"{y.Kind}:{y.SortKey}");
    }

    public bool Equals(UsingSignature? x, UsingSignature? y) => x == y;

    public int GetHashCode(UsingSignature obj) => HashCode.Combine(obj.Name, obj.Kind, obj.Alias);
}
