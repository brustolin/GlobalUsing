namespace GlobalUsing.Core.Models;

public sealed record PromotionCandidate(
    UsingSignature Signature,
    int FileCount,
    int TotalFiles,
    double Percentage);
