using System.Text.Json.Serialization;

namespace Cephalon.Engine.Composition.Packages;

internal sealed class PackageDefinitionFile
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("version")]
    public string? Version { get; init; }

    [JsonPropertyName("assembly")]
    public string? Assembly { get; init; }

    [JsonPropertyName("assemblyPath")]
    public string? AssemblyPath { get; init; }

    [JsonPropertyName("path")]
    public string? Path { get; init; }

    [JsonPropertyName("compatibility")]
    public PackageCompatibilityDefinition? Compatibility { get; init; }

    [JsonPropertyName("integrity")]
    public PackageIntegrityDefinition? Integrity { get; init; }

    [JsonPropertyName("publisher")]
    public PackagePublisherDefinition? Publisher { get; init; }

    [JsonPropertyName("dependencies")]
    public PackageDependencyDefinition[]? Dependencies { get; init; }

    [JsonPropertyName("signature")]
    public PackageSignatureDefinition? Signature { get; init; }

    [JsonPropertyName("signatures")]
    public PackageSignatureDefinition[]? Signatures { get; init; }

    public string? ResolveAssemblyPath()
    {
        if (!string.IsNullOrWhiteSpace(Assembly))
        {
            return Assembly.Trim();
        }

        if (!string.IsNullOrWhiteSpace(AssemblyPath))
        {
            return AssemblyPath.Trim();
        }

        return string.IsNullOrWhiteSpace(Path) ? null : Path.Trim();
    }

    public string? ResolveVersion()
    {
        return string.IsNullOrWhiteSpace(Version) ? null : Version.Trim();
    }

    public string? ResolveMinimumEngineVersion()
    {
        return string.IsNullOrWhiteSpace(Compatibility?.MinimumEngineVersion)
            ? null
            : Compatibility.MinimumEngineVersion.Trim();
    }

    public string? ResolveMaximumEngineVersion()
    {
        return string.IsNullOrWhiteSpace(Compatibility?.MaximumEngineVersion)
            ? null
            : Compatibility.MaximumEngineVersion.Trim();
    }

    public string[] ResolveSupportedTargetFrameworks()
    {
        return Compatibility?.SupportedTargetFrameworks?
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }

    public string? ResolveSha256()
    {
        return string.IsNullOrWhiteSpace(Integrity?.Sha256)
            ? null
            : Integrity.Sha256.Trim();
    }

    public string? ResolvePublisherId()
    {
        return string.IsNullOrWhiteSpace(Publisher?.Id)
            ? null
            : Publisher.Id.Trim();
    }

    public string? ResolvePublisherDisplayName()
    {
        return string.IsNullOrWhiteSpace(Publisher?.DisplayName)
            ? null
            : Publisher.DisplayName.Trim();
    }

    public string? ResolvePublisherWebsite()
    {
        return string.IsNullOrWhiteSpace(Publisher?.Website)
            ? null
            : Publisher.Website.Trim();
    }

    public PackageDependencyLoadRequest[] ResolveDependencies()
    {
        if (Dependencies is not { Length: > 0 })
        {
            return [];
        }

        return Dependencies
            .Select(static dependency => dependency.ToLoadRequest())
            .ToArray();
    }

    public string? ResolveSignatureType()
    {
        var signature = ResolvePrimarySignature();
        return string.IsNullOrWhiteSpace(signature?.Type)
            ? null
            : signature.Type.Trim();
    }

    public string? ResolveSignatureSigner()
    {
        var signature = ResolvePrimarySignature();
        return string.IsNullOrWhiteSpace(signature?.Signer)
            ? null
            : signature.Signer.Trim();
    }

    public string? ResolveSignatureKeyId()
    {
        var signature = ResolvePrimarySignature();
        return string.IsNullOrWhiteSpace(signature?.KeyId)
            ? null
            : signature.KeyId.Trim();
    }

    public string? ResolveSignatureFingerprint()
    {
        var signature = ResolvePrimarySignature();
        return string.IsNullOrWhiteSpace(signature?.Fingerprint)
            ? null
            : signature.Fingerprint.Trim();
    }

    public string? ResolveSignatureAlgorithm()
    {
        var signature = ResolvePrimarySignature();
        return string.IsNullOrWhiteSpace(signature?.Algorithm)
            ? null
            : signature.Algorithm.Trim();
    }

    public string? ResolveSignatureValue()
    {
        var signature = ResolvePrimarySignature();
        return string.IsNullOrWhiteSpace(signature?.Value)
            ? null
            : signature.Value.Trim();
    }

    public PackageSignatureDefinition[] ResolveSignatures()
    {
        var signatures = new List<PackageSignatureDefinition>();
        if (Signature is not null)
        {
            signatures.Add(Signature);
        }

        if (Signatures is { Length: > 0 })
        {
            signatures.AddRange(Signatures);
        }

        return signatures
            .Where(static signature =>
                !string.IsNullOrWhiteSpace(signature.Type) ||
                !string.IsNullOrWhiteSpace(signature.Signer) ||
                !string.IsNullOrWhiteSpace(signature.KeyId) ||
                !string.IsNullOrWhiteSpace(signature.Fingerprint) ||
                !string.IsNullOrWhiteSpace(signature.Algorithm) ||
                !string.IsNullOrWhiteSpace(signature.Value))
            .ToArray();
    }

    private PackageSignatureDefinition? ResolvePrimarySignature()
    {
        return ResolveSignatures().FirstOrDefault();
    }

    internal sealed class PackageCompatibilityDefinition
    {
        [JsonPropertyName("minimumEngineVersion")]
        public string? MinimumEngineVersion { get; init; }

        [JsonPropertyName("maximumEngineVersion")]
        public string? MaximumEngineVersion { get; init; }

        [JsonPropertyName("supportedTargetFrameworks")]
        public string[]? SupportedTargetFrameworks { get; init; }
    }

    internal sealed class PackageIntegrityDefinition
    {
        [JsonPropertyName("sha256")]
        public string? Sha256 { get; init; }
    }

    internal sealed class PackagePublisherDefinition
    {
        [JsonPropertyName("id")]
        public string? Id { get; init; }

        [JsonPropertyName("displayName")]
        public string? DisplayName { get; init; }

        [JsonPropertyName("website")]
        public string? Website { get; init; }
    }

    internal sealed class PackageDependencyDefinition
    {
        [JsonPropertyName("id")]
        public string? Id { get; init; }

        [JsonPropertyName("minimumVersion")]
        public string? MinimumVersion { get; init; }

        [JsonPropertyName("maximumVersion")]
        public string? MaximumVersion { get; init; }

        public PackageDependencyLoadRequest ToLoadRequest()
        {
            if (string.IsNullOrWhiteSpace(Id))
            {
                throw new InvalidOperationException(
                    "Package dependency entries must declare an 'id' value.");
            }

            return new PackageDependencyLoadRequest(
                id: Id,
                minimumVersion: MinimumVersion,
                maximumVersion: MaximumVersion);
        }
    }

    internal sealed class PackageSignatureDefinition
    {
        [JsonPropertyName("type")]
        public string? Type { get; init; }

        [JsonPropertyName("signer")]
        public string? Signer { get; init; }

        [JsonPropertyName("keyId")]
        public string? KeyId { get; init; }

        [JsonPropertyName("fingerprint")]
        public string? Fingerprint { get; init; }

        [JsonPropertyName("algorithm")]
        public string? Algorithm { get; init; }

        [JsonPropertyName("value")]
        public string? Value { get; init; }
    }
}
