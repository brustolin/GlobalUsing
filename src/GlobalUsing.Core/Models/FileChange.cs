namespace GlobalUsing.Core.Models;

public sealed record FileChange(
    string FilePath,
    bool WillChange,
    int AddedGlobalUsings,
    int RemovedLocalUsings);
