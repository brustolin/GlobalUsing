namespace GlobalUsing.Core.Models;

public sealed record NamespaceReportSummary(
    string Namespace,
    int FilesUsingNamespace,
    int TotalCSharpFilesAnalyzed,
    double UsagePercentage,
    int ProjectsWithNamespaceUsage,
    int ProjectsAlreadyGlobal);
