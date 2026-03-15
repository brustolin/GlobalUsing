using System.Collections.Immutable;

namespace GlobalUsing.Core.Models;

public sealed record ApplyResult(
    AnalysisResult AnalysisResult,
    bool DryRun,
    int GlobalUsingsAdded,
    int FilesModified,
    int LocalUsingsRemoved,
    ImmutableArray<FileChange> FileChanges);
