using GlobalUsing.Core.Enums;

namespace GlobalUsing.Core.Models;

public sealed record NamespaceUsage(
    UsingSignature Signature,
    int FileCount,
    int TotalFiles,
    double Percentage,
    RecommendationStatus Status);
