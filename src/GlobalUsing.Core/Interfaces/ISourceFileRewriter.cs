using GlobalUsing.Core.Models;

namespace GlobalUsing.Core.Interfaces;

public interface ISourceFileRewriter
{
    Task<SourceFileRewriteResult> RemoveRedundantUsingsAsync(
        string filePath,
        IReadOnlySet<UsingSignature> promotedUsings,
        AnalysisOptions options,
        CancellationToken cancellationToken);
}
