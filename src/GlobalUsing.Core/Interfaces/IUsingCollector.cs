using GlobalUsing.Core.Models;

namespace GlobalUsing.Core.Interfaces;

public interface IUsingCollector
{
    Task<IReadOnlyList<SourceFileUsings>> CollectAsync(
        IReadOnlyCollection<string> filePaths,
        CancellationToken cancellationToken);
}
