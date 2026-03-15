using GlobalUsing.Core.Models;

namespace GlobalUsing.Core.Interfaces;

public interface IFileDiscoveryService
{
    Task<FileDiscoveryResult> DiscoverAsync(AnalysisOptions options, CancellationToken cancellationToken);
}
