namespace GlobalUsing.Core.Models;

public sealed record SourceFileRewriteResult(
    string FilePath,
    string Content,
    bool HasChanges,
    int RemovedUsings);
