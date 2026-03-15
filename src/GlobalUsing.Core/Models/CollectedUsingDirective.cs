namespace GlobalUsing.Core.Models;

public sealed record CollectedUsingDirective(
    UsingSignature Signature,
    string OriginalText,
    bool IsGlobal,
    string SourceFilePath);
