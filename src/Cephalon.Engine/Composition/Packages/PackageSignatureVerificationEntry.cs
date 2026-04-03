namespace Cephalon.Engine.Composition.Packages;

internal sealed record PackageSignatureVerificationEntry(
    string? Type,
    string? Signer,
    string? KeyId,
    string? Fingerprint,
    string? Algorithm,
    bool IsVerified,
    string Reason);
