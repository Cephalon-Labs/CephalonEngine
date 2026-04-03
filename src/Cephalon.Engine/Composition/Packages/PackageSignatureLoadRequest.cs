namespace Cephalon.Engine.Composition.Packages;

internal sealed record PackageSignatureLoadRequest(
    string? Type,
    string? Signer,
    string? KeyId,
    string? Fingerprint,
    string? Algorithm,
    string? Value);
