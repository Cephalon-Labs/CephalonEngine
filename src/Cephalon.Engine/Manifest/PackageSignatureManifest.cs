namespace Cephalon.Engine.Manifest;

/// <summary>
/// Describes a single package signature declared by a package manifest and the outcome of verifying it.
/// </summary>
public sealed class PackageSignatureManifest
{
    /// <summary>
    /// Creates a package signature manifest entry.
    /// </summary>
    /// <param name="type">The declared signature type.</param>
    /// <param name="signer">The declared signer identity.</param>
    /// <param name="keyId">The declared signature key identifier.</param>
    /// <param name="fingerprint">The declared signer fingerprint.</param>
    /// <param name="algorithm">The declared signature algorithm.</param>
    /// <param name="verificationSource">The trust source that verified the signature, when available.</param>
    /// <param name="certificateThumbprint">
    /// The thumbprint of the signing certificate used during verification, when certificate-backed trust was used.
    /// </param>
    /// <param name="isVerified">
    /// Whether this signature was cryptographically verified against a trusted public key or signing certificate.
    /// </param>
    /// <param name="verificationReason">The verification outcome summary for this signature.</param>
    public PackageSignatureManifest(
        string? type,
        string? signer,
        string? keyId,
        string? fingerprint,
        string? algorithm,
        string? verificationSource,
        string? certificateThumbprint,
        bool isVerified,
        string verificationReason)
    {
        Type = type;
        Signer = signer;
        KeyId = keyId;
        Fingerprint = fingerprint;
        Algorithm = algorithm;
        VerificationSource = verificationSource;
        CertificateThumbprint = certificateThumbprint;
        IsVerified = isVerified;
        VerificationReason = verificationReason ?? throw new ArgumentNullException(nameof(verificationReason));
    }

    /// <summary>
    /// Gets the declared signature type.
    /// </summary>
    public string? Type { get; }

    /// <summary>
    /// Gets the declared signer identity.
    /// </summary>
    public string? Signer { get; }

    /// <summary>
    /// Gets the declared signature key identifier.
    /// </summary>
    public string? KeyId { get; }

    /// <summary>
    /// Gets the declared signer fingerprint.
    /// </summary>
    public string? Fingerprint { get; }

    /// <summary>
    /// Gets the declared signature algorithm.
    /// </summary>
    public string? Algorithm { get; }

    /// <summary>
    /// Gets the trust source that verified the signature, when available.
    /// </summary>
    public string? VerificationSource { get; }

    /// <summary>
    /// Gets the signing certificate thumbprint used during verification, when certificate-backed trust was used.
    /// </summary>
    public string? CertificateThumbprint { get; }

    /// <summary>
    /// Gets a value indicating whether this signature was cryptographically verified against a trusted signing identity.
    /// </summary>
    public bool IsVerified { get; }

    /// <summary>
    /// Gets the verification outcome summary for this signature.
    /// </summary>
    public string VerificationReason { get; }
}
