namespace Cephalon.Engine.Manifest;

/// <summary>
/// Describes a package that contributed one or more modules to the built runtime.
/// </summary>
public sealed class PackageManifest
{
    /// <summary>
    /// Creates a new package manifest entry.
    /// </summary>
    /// <param name="id">The stable package identifier.</param>
    /// <param name="kind">The discovery kind used to resolve the package.</param>
    /// <param name="assemblyName">The loaded package assembly name.</param>
    /// <param name="path">The resolved assembly path that was loaded.</param>
    /// <param name="sourcePath">The original source path that led to the package load.</param>
    /// <param name="loadContext">The assembly load context name used for the package.</param>
    /// <param name="modules">The identifiers of modules contributed by the package.</param>
    /// <param name="version">The package version declared by the package manifest, when available.</param>
    /// <param name="minimumEngineVersion">The minimum supported engine version declared by the package manifest, when available.</param>
    /// <param name="maximumEngineVersion">The maximum supported engine version declared by the package manifest, when available.</param>
    /// <param name="supportedTargetFrameworks">The supported target frameworks declared by the package manifest.</param>
    /// <param name="publisherId">The stable publisher identifier declared by the package manifest, when available.</param>
    /// <param name="publisherDisplayName">The publisher display name declared by the package manifest, when available.</param>
    /// <param name="publisherWebsite">The publisher website declared by the package manifest, when available.</param>
    /// <param name="signatureType">The signature metadata type declared by the package manifest, when available.</param>
    /// <param name="signatureSigner">The signer identity declared by the package manifest, when available.</param>
    /// <param name="signatureKeyId">The trusted-key identifier declared by the package manifest, when available.</param>
    /// <param name="signatureFingerprint">The signer fingerprint declared by the package manifest, when available.</param>
    /// <param name="signatureCertificateThumbprint">
    /// The primary signing certificate thumbprint used during verification, when certificate-backed trust was used.
    /// </param>
    /// <param name="signatureAlgorithm">The signature algorithm declared by the package manifest, when available.</param>
    /// <param name="signatures">The declared package signatures and their individual verification outcomes.</param>
    /// <param name="isSignatureVerified">
    /// Whether the package signature was cryptographically verified against a trusted public key or signing certificate.
    /// </param>
    /// <param name="signatureVerificationReason">The verification outcome summary for the package signature.</param>
    /// <param name="checksumSha256">The computed SHA-256 checksum of the resolved package assembly.</param>
    /// <param name="isTrusted">Whether the package is trusted by the current trust policy.</param>
    /// <param name="trustReason">The reason the package is trusted or not trusted.</param>
    public PackageManifest(
        string id,
        string kind,
        string assemblyName,
        string path,
        string sourcePath,
        string loadContext,
        IReadOnlyList<string> modules,
        string? version,
        string? minimumEngineVersion,
        string? maximumEngineVersion,
        IReadOnlyList<string>? supportedTargetFrameworks,
        string? publisherId,
        string? publisherDisplayName,
        string? publisherWebsite,
        string? signatureType,
        string? signatureSigner,
        string? signatureKeyId,
        string? signatureFingerprint,
        string? signatureCertificateThumbprint,
        string? signatureAlgorithm,
        IReadOnlyList<PackageSignatureManifest>? signatures,
        bool isSignatureVerified,
        string signatureVerificationReason,
        string checksumSha256,
        bool isTrusted,
        string trustReason)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Kind = kind ?? throw new ArgumentNullException(nameof(kind));
        AssemblyName = assemblyName ?? throw new ArgumentNullException(nameof(assemblyName));
        Path = path ?? throw new ArgumentNullException(nameof(path));
        SourcePath = sourcePath ?? throw new ArgumentNullException(nameof(sourcePath));
        LoadContext = loadContext ?? throw new ArgumentNullException(nameof(loadContext));
        Modules = modules ?? throw new ArgumentNullException(nameof(modules));
        Version = version;
        MinimumEngineVersion = minimumEngineVersion;
        MaximumEngineVersion = maximumEngineVersion;
        SupportedTargetFrameworks = supportedTargetFrameworks ?? [];
        PublisherId = publisherId;
        PublisherDisplayName = publisherDisplayName;
        PublisherWebsite = publisherWebsite;
        SignatureType = signatureType;
        SignatureSigner = signatureSigner;
        SignatureKeyId = signatureKeyId;
        SignatureFingerprint = signatureFingerprint;
        SignatureCertificateThumbprint = signatureCertificateThumbprint;
        SignatureAlgorithm = signatureAlgorithm;
        Signatures = signatures ?? [];
        IsSignatureVerified = isSignatureVerified;
        SignatureVerificationReason = signatureVerificationReason ?? throw new ArgumentNullException(nameof(signatureVerificationReason));
        ChecksumSha256 = checksumSha256 ?? throw new ArgumentNullException(nameof(checksumSha256));
        IsTrusted = isTrusted;
        TrustReason = trustReason ?? throw new ArgumentNullException(nameof(trustReason));
    }

    /// <summary>
    /// Gets the stable package identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the package discovery kind, such as assembly path or manifest-file loading.
    /// </summary>
    public string Kind { get; }

    /// <summary>
    /// Gets the assembly name that was loaded for the package.
    /// </summary>
    public string AssemblyName { get; }

    /// <summary>
    /// Gets the resolved assembly path that the engine loaded.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Gets the original source path used to discover the package.
    /// </summary>
    public string SourcePath { get; }

    /// <summary>
    /// Gets the assembly load context name used for the package.
    /// </summary>
    public string LoadContext { get; }

    /// <summary>
    /// Gets the identifiers of modules contributed by the package.
    /// </summary>
    public IReadOnlyList<string> Modules { get; }

    /// <summary>
    /// Gets the package version declared by the package manifest, when available.
    /// </summary>
    public string? Version { get; }

    /// <summary>
    /// Gets the minimum engine version required by the package manifest, when available.
    /// </summary>
    public string? MinimumEngineVersion { get; }

    /// <summary>
    /// Gets the maximum engine version supported by the package manifest, when available.
    /// </summary>
    public string? MaximumEngineVersion { get; }

    /// <summary>
    /// Gets the target frameworks declared as compatible by the package manifest.
    /// </summary>
    public IReadOnlyList<string> SupportedTargetFrameworks { get; }

    /// <summary>
    /// Gets the stable publisher identifier declared by the package manifest, when available.
    /// </summary>
    public string? PublisherId { get; }

    /// <summary>
    /// Gets the publisher display name declared by the package manifest, when available.
    /// </summary>
    public string? PublisherDisplayName { get; }

    /// <summary>
    /// Gets the publisher website declared by the package manifest, when available.
    /// </summary>
    public string? PublisherWebsite { get; }

    /// <summary>
    /// Gets the signature metadata type declared by the package manifest, when available.
    /// </summary>
    public string? SignatureType { get; }

    /// <summary>
    /// Gets the signer identity declared by the package manifest, when available.
    /// </summary>
    public string? SignatureSigner { get; }

    /// <summary>
    /// Gets the trusted-key identifier declared by the package manifest, when available.
    /// </summary>
    public string? SignatureKeyId { get; }

    /// <summary>
    /// Gets the signer fingerprint declared by the package manifest, when available.
    /// </summary>
    public string? SignatureFingerprint { get; }

    /// <summary>
    /// Gets the primary signing certificate thumbprint used during verification, when certificate-backed trust was used.
    /// </summary>
    public string? SignatureCertificateThumbprint { get; }

    /// <summary>
    /// Gets the signature algorithm declared by the package manifest, when available.
    /// </summary>
    public string? SignatureAlgorithm { get; }

    /// <summary>
    /// Gets the declared package signatures and their individual verification outcomes.
    /// </summary>
    public IReadOnlyList<PackageSignatureManifest> Signatures { get; }

    /// <summary>
    /// Gets a value indicating whether the package signature was cryptographically verified against a trusted signing identity.
    /// </summary>
    public bool IsSignatureVerified { get; }

    /// <summary>
    /// Gets the verification outcome summary for the package signature.
    /// </summary>
    public string SignatureVerificationReason { get; }

    /// <summary>
    /// Gets the computed SHA-256 checksum of the resolved package assembly.
    /// </summary>
    public string ChecksumSha256 { get; }

    /// <summary>
    /// Gets a value indicating whether the package is trusted by the current trust policy.
    /// </summary>
    public bool IsTrusted { get; }

    /// <summary>
    /// Gets the reason the package is trusted or not trusted by the current trust policy.
    /// </summary>
    public string TrustReason { get; }

    /// <summary>
    /// Gets the package-to-package dependencies declared by the package manifest.
    /// </summary>
    public IReadOnlyList<PackageDependencyManifest> Dependencies { get; init; } = [];
}
