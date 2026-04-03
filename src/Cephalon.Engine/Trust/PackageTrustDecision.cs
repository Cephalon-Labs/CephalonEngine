namespace Cephalon.Engine.Trust;

/// <summary>
/// Describes the trust outcome for a package after package metadata, signature verification, and host trust rules have been evaluated.
/// </summary>
/// <param name="PackageId">The stable package identifier.</param>
/// <param name="AssemblyName">The resolved assembly name for the package.</param>
/// <param name="Path">The resolved assembly path used for the package load.</param>
/// <param name="PublisherId">The declared publisher identifier, when available.</param>
/// <param name="SignatureKeyId">The primary signature key identifier, when available.</param>
/// <param name="SignatureFingerprint">The primary signature fingerprint, when available.</param>
/// <param name="SignatureCertificateThumbprint">
/// The primary signing certificate thumbprint used during verification, when certificate-backed trust was used.
/// </param>
/// <param name="Signatures">The per-signer trust and verification details declared by the package.</param>
/// <param name="IsSignatureVerified">Whether at least one declared signature verified successfully.</param>
/// <param name="SignatureVerificationReason">The aggregate signature verification outcome summary.</param>
/// <param name="IsTrusted">Whether the package is trusted by the active runtime trust policy.</param>
/// <param name="Reason">The reason the package was trusted or rejected.</param>
public sealed record PackageTrustDecision(
    string PackageId,
    string AssemblyName,
    string Path,
    string? PublisherId,
    string? SignatureKeyId,
    string? SignatureFingerprint,
    string? SignatureCertificateThumbprint,
    IReadOnlyList<PackageSignatureTrustDecision> Signatures,
    bool IsSignatureVerified,
    string SignatureVerificationReason,
    bool IsTrusted,
    string Reason);
