namespace GlobalUsing.Core.Models;

public sealed record NamespaceUsageMetric(
    UsingSignature Signature,
    int FileCount,
    int TotalFiles,
    double Percentage);
