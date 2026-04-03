namespace Cephalon.Engine.Composition.Packages;

internal sealed record PackageSignatureVerificationResult(
    bool IsVerified,
    string Reason,
    IReadOnlyList<PackageSignatureVerificationEntry> Signatures);
