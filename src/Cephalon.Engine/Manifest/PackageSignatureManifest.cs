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
    /// <param name="isVerified">Whether this signature was cryptographically verified.</param>
    /// <param name="verificationReason">The verification outcome summary for this signature.</param>
    public PackageSignatureManifest(
        string? type,
        string? signer,
        string? keyId,
        string? fingerprint,
        string? algorithm,
        bool isVerified,
        string verificationReason)
    {
        Type = type;
        Signer = signer;
        KeyId = keyId;
        Fingerprint = fingerprint;
        Algorithm = algorithm;
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
    /// Gets a value indicating whether this signature was cryptographically verified.
    /// </summary>
    public bool IsVerified { get; }

    /// <summary>
    /// Gets the verification outcome summary for this signature.
    /// </summary>
    public string VerificationReason { get; }
}
