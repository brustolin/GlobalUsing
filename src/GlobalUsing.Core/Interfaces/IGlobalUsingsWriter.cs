using GlobalUsing.Core.Models;

namespace GlobalUsing.Core.Interfaces;

public interface IGlobalUsingsWriter
{
    Task<GlobalUsingsFileUpdate> BuildUpdateAsync(
        string filePath,
        IReadOnlyCollection<UsingSignature> promotedUsings,
        CancellationToken cancellationToken);
}
