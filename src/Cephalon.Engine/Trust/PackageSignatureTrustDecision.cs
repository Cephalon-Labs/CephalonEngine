namespace Cephalon.Engine.Trust;

/// <summary>
/// Describes the trust and verification outcome for a single package signature.
/// </summary>
/// <param name="Signer">The declared signer identity, when available.</param>
/// <param name="KeyId">The declared signing-key identifier, when available.</param>
/// <param name="Fingerprint">The declared signer fingerprint, when available.</param>
/// <param name="IsVerified">Whether this signature verified successfully against a trusted public key.</param>
/// <param name="Reason">The verification outcome or failure reason for this signature.</param>
public sealed record PackageSignatureTrustDecision(
    string? Signer,
    string? KeyId,
    string? Fingerprint,
    bool IsVerified,
    string Reason);
