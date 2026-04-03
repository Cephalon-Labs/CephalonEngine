namespace Cephalon.Engine.Composition.Packages;

internal sealed class PackageLoadRequest
{
    public PackageLoadRequest(
        string id,
        string kind,
        string resolvedSourcePath,
        string resolvedAssemblyPath,
        string? version = null,
        string? minimumEngineVersion = null,
        string? maximumEngineVersion = null,
        IReadOnlyList<string>? supportedTargetFrameworks = null,
        string? publisherId = null,
        string? publisherDisplayName = null,
        string? publisherWebsite = null,
        PackageDistributionLoadRequest? distribution = null,
        PackageProvenanceLoadRequest? provenance = null,
        string? signatureType = null,
        string? signatureSigner = null,
        string? signatureKeyId = null,
        string? signatureFingerprint = null,
        string? signatureAlgorithm = null,
        string? signatureValue = null,
        IReadOnlyList<PackageSignatureLoadRequest>? signatures = null,
        IReadOnlyList<PackageDependencyLoadRequest>? dependencies = null,
        string? expectedSha256 = null)
    {
        Id = id;
        Kind = kind;
        ResolvedSourcePath = resolvedSourcePath;
        ResolvedAssemblyPath = resolvedAssemblyPath;
        Version = version;
        MinimumEngineVersion = minimumEngineVersion;
        MaximumEngineVersion = maximumEngineVersion;
        SupportedTargetFrameworks = supportedTargetFrameworks ?? [];
        PublisherId = publisherId;
        PublisherDisplayName = publisherDisplayName;
        PublisherWebsite = publisherWebsite;
        Distribution = distribution;
        Provenance = provenance;
        Dependencies = dependencies ?? [];
        Signatures = NormalizeSignatures(
            signatures,
            signatureType,
            signatureSigner,
            signatureKeyId,
            signatureFingerprint,
            signatureAlgorithm,
            signatureValue);

        var primarySignature = Signatures.Count > 0 ? Signatures[0] : null;
        SignatureType = primarySignature?.Type;
        SignatureSigner = primarySignature?.Signer;
        SignatureKeyId = primarySignature?.KeyId;
        SignatureFingerprint = primarySignature?.Fingerprint;
        SignatureAlgorithm = primarySignature?.Algorithm;
        SignatureValue = primarySignature?.Value;
        ExpectedSha256 = expectedSha256;
    }

    public string Id { get; }

    public string Kind { get; }

    public string ResolvedSourcePath { get; }

    public string ResolvedAssemblyPath { get; }

    public string? Version { get; }

    public string? MinimumEngineVersion { get; }

    public string? MaximumEngineVersion { get; }

    public IReadOnlyList<string> SupportedTargetFrameworks { get; }

    public string? PublisherId { get; }

    public string? PublisherDisplayName { get; }

    public string? PublisherWebsite { get; }

    public PackageDistributionLoadRequest? Distribution { get; }

    public PackageProvenanceLoadRequest? Provenance { get; }

    public IReadOnlyList<PackageDependencyLoadRequest> Dependencies { get; }

    public IReadOnlyList<PackageSignatureLoadRequest> Signatures { get; }

    public string? SignatureType { get; }

    public string? SignatureSigner { get; }

    public string? SignatureKeyId { get; }

    public string? SignatureFingerprint { get; }

    public string? SignatureAlgorithm { get; }

    public string? SignatureValue { get; }

    public string? ExpectedSha256 { get; }

    public bool HasSignatureFingerprint =>
        Signatures.Any(static signature => !string.IsNullOrWhiteSpace(signature.Fingerprint));

    public bool HasSignatureKeyId =>
        Signatures.Any(static signature => !string.IsNullOrWhiteSpace(signature.KeyId));

    public bool HasSignatureValue =>
        Signatures.Any(static signature => !string.IsNullOrWhiteSpace(signature.Value));

    private static IReadOnlyList<PackageSignatureLoadRequest> NormalizeSignatures(
        IReadOnlyList<PackageSignatureLoadRequest>? signatures,
        string? signatureType,
        string? signatureSigner,
        string? signatureKeyId,
        string? signatureFingerprint,
        string? signatureAlgorithm,
        string? signatureValue)
    {
        if (signatures is { Count: > 0 })
        {
            return signatures;
        }

        if (string.IsNullOrWhiteSpace(signatureType) &&
            string.IsNullOrWhiteSpace(signatureSigner) &&
            string.IsNullOrWhiteSpace(signatureKeyId) &&
            string.IsNullOrWhiteSpace(signatureFingerprint) &&
            string.IsNullOrWhiteSpace(signatureAlgorithm) &&
            string.IsNullOrWhiteSpace(signatureValue))
        {
            return [];
        }

        return
        [
            new PackageSignatureLoadRequest(
                Type: signatureType,
                Signer: signatureSigner,
                KeyId: signatureKeyId,
                Fingerprint: signatureFingerprint,
                Algorithm: signatureAlgorithm,
                Value: signatureValue)
        ];
    }
}
