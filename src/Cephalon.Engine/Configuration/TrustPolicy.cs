using Cephalon.Abstractions.Capabilities;
using Microsoft.Extensions.Configuration;

namespace Cephalon.Engine.Configuration;

/// <summary>
/// Defines package-trust and capability-governance rules for a Cephalon runtime.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="TrustPolicy" /> is the engine's host-owned trust contract. It decides whether
/// independently shipped packages must be explicitly trusted, how capability access is resolved,
/// and which publishers, signer fingerprints, public keys, signing certificates, or assembly
/// checksums are accepted.
/// </para>
/// <para>
/// Package-loading decisions use this policy together with package metadata from
/// <c>cephalon.package.json</c>, cryptographic signature verification results, and the active
/// package policy. Capability access decisions then flow into runtime introspection and optional
/// HTTP request-time enforcement through the ASP.NET Core host adapters.
/// </para>
/// </remarks>
public sealed class TrustPolicy
{
    /// <summary>
    /// Gets the default trust policy.
    /// </summary>
    public static TrustPolicy Default { get; } = new();

    /// <summary>
    /// Creates a trust policy.
    /// </summary>
    /// <param name="requireTrustedPackages">
    /// <see langword="true" /> to require independently loaded packages to match at least one trust rule;
    /// otherwise package loads may proceed without an explicit trust match.
    /// </param>
    /// <param name="defaultCapabilityAccess">
    /// The default access applied when a capability key does not appear in <paramref name="capabilities" />.
    /// </param>
    /// <param name="trustedPackages">
    /// Package identifiers that should be treated as trusted when package-level allow-listing is in use.
    /// </param>
    /// <param name="trustedAssemblies">
    /// Assembly names that should be treated as trusted when assembly-level allow-listing is in use.
    /// </param>
    /// <param name="trustedPublishers">
    /// Stable publisher identifiers that should be treated as trusted for independently shipped packages.
    /// </param>
    /// <param name="trustedSignerFingerprints">
    /// Signer fingerprints that should be treated as trusted for detached-signature provenance checks.
    /// </param>
    /// <param name="trustedSignaturePublicKeys">
    /// Public keys keyed by signing identity or signer fingerprint, used for cryptographic signature verification.
    /// </param>
    /// <param name="trustedSignatureCertificates">
    /// Signing certificates keyed by signing identity or signer fingerprint, used for certificate-backed
    /// cryptographic signature verification.
    /// </param>
    /// <param name="trustedSignatureCertificateAuthorities">
    /// Root or intermediate certificate authorities used to validate configured signing certificates when
    /// certificate-chain verification is enabled.
    /// </param>
    /// <param name="capabilities">
    /// Explicit per-capability access overrides keyed by capability key.
    /// </param>
    /// <param name="allowedPackageChecksums">
    /// Explicit package checksum allow-lists keyed by package identifier.
    /// </param>
    public TrustPolicy(
        bool requireTrustedPackages = false,
        CapabilityAccess defaultCapabilityAccess = CapabilityAccess.Allowed,
        IReadOnlyList<string>? trustedPackages = null,
        IReadOnlyList<string>? trustedAssemblies = null,
        IReadOnlyList<string>? trustedPublishers = null,
        IReadOnlyList<string>? trustedSignerFingerprints = null,
        IReadOnlyDictionary<string, string>? trustedSignaturePublicKeys = null,
        IReadOnlyDictionary<string, string>? trustedSignatureCertificates = null,
        IReadOnlyList<string>? trustedSignatureCertificateAuthorities = null,
        IReadOnlyDictionary<string, CapabilityAccess>? capabilities = null,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? allowedPackageChecksums = null)
    {
        RequireTrustedPackages = requireTrustedPackages;
        DefaultCapabilityAccess = defaultCapabilityAccess;
        TrustedPackages = NormalizeList(trustedPackages);
        TrustedAssemblies = NormalizeList(trustedAssemblies);
        TrustedPublishers = NormalizeList(trustedPublishers);
        TrustedSignerFingerprints = NormalizeChecksums(trustedSignerFingerprints);
        TrustedSignaturePublicKeys = NormalizeStringDictionary(trustedSignaturePublicKeys);
        TrustedSignatureCertificates = NormalizeStringDictionary(trustedSignatureCertificates);
        TrustedSignatureCertificateAuthorities = NormalizeList(trustedSignatureCertificateAuthorities);
        Capabilities = NormalizeRules(capabilities);
        AllowedPackageChecksums = NormalizeChecksumRules(allowedPackageChecksums);
    }

    /// <summary>
    /// Gets a value indicating whether explicitly discovered packages must satisfy a trust rule.
    /// </summary>
    public bool RequireTrustedPackages { get; }

    /// <summary>
    /// Gets the default access applied to capability keys without an explicit override.
    /// </summary>
    public CapabilityAccess DefaultCapabilityAccess { get; }

    /// <summary>
    /// Gets the trusted package identifier allow-list.
    /// </summary>
    public IReadOnlyList<string> TrustedPackages { get; }

    /// <summary>
    /// Gets the trusted assembly-name allow-list.
    /// </summary>
    public IReadOnlyList<string> TrustedAssemblies { get; }

    /// <summary>
    /// Gets the trusted publisher identifier allow-list.
    /// </summary>
    public IReadOnlyList<string> TrustedPublishers { get; }

    /// <summary>
    /// Gets the trusted signer fingerprint allow-list.
    /// </summary>
    public IReadOnlyList<string> TrustedSignerFingerprints { get; }

    /// <summary>
    /// Gets the configured trusted public keys used for detached-signature verification.
    /// </summary>
    public IReadOnlyDictionary<string, string> TrustedSignaturePublicKeys { get; }

    /// <summary>
    /// Gets the configured trusted signing certificates used for certificate-backed detached-signature verification.
    /// </summary>
    public IReadOnlyDictionary<string, string> TrustedSignatureCertificates { get; }

    /// <summary>
    /// Gets the configured certificate authorities used to validate trusted signing certificate chains.
    /// </summary>
    public IReadOnlyList<string> TrustedSignatureCertificateAuthorities { get; }

    /// <summary>
    /// Gets the explicit per-capability access rules.
    /// </summary>
    public IReadOnlyDictionary<string, CapabilityAccess> Capabilities { get; }

    /// <summary>
    /// Gets the package checksum allow-lists keyed by package identifier.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> AllowedPackageChecksums { get; }

    /// <summary>
    /// Gets a value indicating whether the policy differs from the default baseline.
    /// </summary>
    public bool HasValues =>
        RequireTrustedPackages ||
        DefaultCapabilityAccess != CapabilityAccess.Allowed ||
        TrustedPackages.Count > 0 ||
        TrustedAssemblies.Count > 0 ||
        TrustedPublishers.Count > 0 ||
        TrustedSignerFingerprints.Count > 0 ||
        TrustedSignaturePublicKeys.Count > 0 ||
        TrustedSignatureCertificates.Count > 0 ||
        TrustedSignatureCertificateAuthorities.Count > 0 ||
        Capabilities.Count > 0 ||
        AllowedPackageChecksums.Count > 0;

    /// <summary>
    /// Resolves the effective access for a capability key.
    /// </summary>
    /// <param name="capabilityKey">The capability key to evaluate.</param>
    /// <returns>
    /// The explicit access configured for <paramref name="capabilityKey" />, or
    /// <see cref="DefaultCapabilityAccess" /> when no override exists.
    /// </returns>
    public CapabilityAccess ResolveCapabilityAccess(string capabilityKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(capabilityKey);

        return Capabilities.TryGetValue(capabilityKey.Trim(), out var access)
            ? access
            : DefaultCapabilityAccess;
    }

    /// <summary>
    /// Merges another trust policy into the current policy.
    /// </summary>
    /// <param name="other">The policy to merge on top of the current instance.</param>
    /// <returns>
    /// A merged trust policy where allow-lists are unioned, keyed rules are overwritten by
    /// <paramref name="other" />, and stricter package-trust requirements remain enabled.
    /// </returns>
    public TrustPolicy Merge(TrustPolicy? other)
    {
        if (other is null || !other.HasValues)
        {
            return this;
        }

        if (!HasValues)
        {
            return other;
        }

        var trustedPackages = new HashSet<string>(TrustedPackages, StringComparer.OrdinalIgnoreCase);
        foreach (var trustedPackage in other.TrustedPackages)
        {
            trustedPackages.Add(trustedPackage);
        }

        var trustedAssemblies = new HashSet<string>(TrustedAssemblies, StringComparer.OrdinalIgnoreCase);
        foreach (var trustedAssembly in other.TrustedAssemblies)
        {
            trustedAssemblies.Add(trustedAssembly);
        }

        var trustedPublishers = new HashSet<string>(TrustedPublishers, StringComparer.OrdinalIgnoreCase);
        foreach (var trustedPublisher in other.TrustedPublishers)
        {
            trustedPublishers.Add(trustedPublisher);
        }

        var trustedSignerFingerprints = new HashSet<string>(TrustedSignerFingerprints, StringComparer.OrdinalIgnoreCase);
        foreach (var trustedSignerFingerprint in other.TrustedSignerFingerprints)
        {
            trustedSignerFingerprints.Add(NormalizeChecksum(trustedSignerFingerprint));
        }

        var trustedSignaturePublicKeys = new Dictionary<string, string>(TrustedSignaturePublicKeys, StringComparer.OrdinalIgnoreCase);
        foreach (var pair in other.TrustedSignaturePublicKeys)
        {
            trustedSignaturePublicKeys[pair.Key] = pair.Value;
        }

        var trustedSignatureCertificates = new Dictionary<string, string>(TrustedSignatureCertificates, StringComparer.OrdinalIgnoreCase);
        foreach (var pair in other.TrustedSignatureCertificates)
        {
            trustedSignatureCertificates[pair.Key] = pair.Value;
        }

        var trustedSignatureCertificateAuthorities = new HashSet<string>(TrustedSignatureCertificateAuthorities, StringComparer.OrdinalIgnoreCase);
        foreach (var authority in other.TrustedSignatureCertificateAuthorities)
        {
            trustedSignatureCertificateAuthorities.Add(authority);
        }

        var capabilities = new Dictionary<string, CapabilityAccess>(Capabilities, StringComparer.OrdinalIgnoreCase);
        foreach (var pair in other.Capabilities)
        {
            capabilities[pair.Key] = pair.Value;
        }

        var allowedPackageChecksums = AllowedPackageChecksums.ToDictionary(
            static pair => pair.Key,
            static pair => (IReadOnlyList<string>)pair.Value,
            StringComparer.OrdinalIgnoreCase);
        foreach (var pair in other.AllowedPackageChecksums)
        {
            var mergedChecksums = allowedPackageChecksums.TryGetValue(pair.Key, out var existingChecksums)
                ? existingChecksums.Concat(pair.Value)
                : pair.Value;

            allowedPackageChecksums[pair.Key] = mergedChecksums
                .Where(static value => !string.IsNullOrWhiteSpace(value))
                .Select(static value => NormalizeChecksum(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        return new TrustPolicy(
            requireTrustedPackages: RequireTrustedPackages || other.RequireTrustedPackages,
            defaultCapabilityAccess: other.DefaultCapabilityAccess,
            trustedPackages: trustedPackages.ToArray(),
            trustedAssemblies: trustedAssemblies.ToArray(),
            trustedPublishers: trustedPublishers.ToArray(),
            trustedSignerFingerprints: trustedSignerFingerprints.ToArray(),
            trustedSignaturePublicKeys: trustedSignaturePublicKeys,
            trustedSignatureCertificates: trustedSignatureCertificates,
            trustedSignatureCertificateAuthorities: trustedSignatureCertificateAuthorities.ToArray(),
            capabilities: capabilities,
            allowedPackageChecksums: allowedPackageChecksums);
    }

    /// <summary>
    /// Reads a trust policy from configuration.
    /// </summary>
    /// <param name="configuration">The root configuration that contains the engine section.</param>
    /// <param name="sectionPath">
    /// The configuration path that should be interpreted as the engine settings section.
    /// The default value is <see cref="EngineSettings.SectionName" />.
    /// </param>
    /// <returns>The configured trust policy, or <see cref="Default" /> when no values are supplied.</returns>
    public static TrustPolicy FromConfiguration(
        IConfiguration configuration,
        string sectionPath = EngineSettings.SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var trustSection = configuration
            .GetSection(sectionPath)
            .GetSection("Trust");

        var trustedPackages = trustSection
            .GetSection("TrustedPackages")
            .GetChildren()
            .Select(static child => child.Value)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value!)
            .ToArray();
        var trustedAssemblies = trustSection
            .GetSection("TrustedAssemblies")
            .GetChildren()
            .Select(static child => child.Value)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value!)
            .ToArray();
        var trustedPublishers = trustSection
            .GetSection("TrustedPublishers")
            .GetChildren()
            .Select(static child => child.Value)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value!)
            .ToArray();
        var trustedSignerFingerprints = trustSection
            .GetSection("TrustedSignerFingerprints")
            .GetChildren()
            .Select(static child => child.Value)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value!)
            .ToArray();
        var trustedSignaturePublicKeys = trustSection
            .GetSection("TrustedSignaturePublicKeys")
            .GetChildren()
            .Where(static child => !string.IsNullOrWhiteSpace(child.Key) && !string.IsNullOrWhiteSpace(child.Value))
            .ToDictionary(
                static child => child.Key.Trim(),
                static child => child.Value!.Trim(),
                StringComparer.OrdinalIgnoreCase);
        var trustedSignatureCertificates = trustSection
            .GetSection("TrustedSignatureCertificates")
            .GetChildren()
            .Where(static child => !string.IsNullOrWhiteSpace(child.Key) && !string.IsNullOrWhiteSpace(child.Value))
            .ToDictionary(
                static child => child.Key.Trim(),
                static child => child.Value!.Trim(),
                StringComparer.OrdinalIgnoreCase);
        var trustedSignatureCertificateAuthorities = trustSection
            .GetSection("TrustedSignatureCertificateAuthorities")
            .GetChildren()
            .Select(static child => child.Value)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value!)
            .ToArray();

        var capabilityRules = new Dictionary<string, CapabilityAccess>(StringComparer.OrdinalIgnoreCase);
        foreach (var capabilitySection in trustSection.GetSection("Capabilities").GetChildren())
        {
            if (!TryParseCapabilityAccess(capabilitySection.Value, out var access))
            {
                continue;
            }

            capabilityRules[capabilitySection.Key] = access;
        }

        var checksumRules = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var packageSection in trustSection.GetSection("AllowedPackageChecksums").GetChildren())
        {
            var values = packageSection.Value is { Length: > 0 }
                ? [packageSection.Value]
                : packageSection
                    .GetChildren()
                    .Select(static child => child.Value)
                    .Where(static value => !string.IsNullOrWhiteSpace(value))
                    .Select(static value => value!)
                    .ToArray();

            if (values.Length == 0)
            {
                continue;
            }

            checksumRules[packageSection.Key] = values;
        }

        return new TrustPolicy(
            requireTrustedPackages: bool.TryParse(trustSection["RequireTrustedPackages"], out var requireTrustedPackages) && requireTrustedPackages,
            defaultCapabilityAccess: TryParseCapabilityAccess(trustSection["DefaultCapabilityAccess"], out var defaultAccess)
                ? defaultAccess
                : CapabilityAccess.Allowed,
            trustedPackages: trustedPackages,
            trustedAssemblies: trustedAssemblies,
            trustedPublishers: trustedPublishers,
            trustedSignerFingerprints: trustedSignerFingerprints,
            trustedSignaturePublicKeys: trustedSignaturePublicKeys,
            trustedSignatureCertificates: trustedSignatureCertificates,
            trustedSignatureCertificateAuthorities: trustedSignatureCertificateAuthorities,
            capabilities: capabilityRules,
            allowedPackageChecksums: checksumRules);
    }

    internal bool IsChecksumAllowed(string packageId, string checksumSha256)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(checksumSha256);

        return AllowedPackageChecksums.TryGetValue(packageId.Trim(), out var allowedChecksums) &&
            allowedChecksums.Contains(NormalizeChecksum(checksumSha256), StringComparer.OrdinalIgnoreCase);
    }

    internal bool IsPublisherTrusted(string? publisherId)
    {
        return !string.IsNullOrWhiteSpace(publisherId) &&
            TrustedPublishers.Contains(publisherId.Trim(), StringComparer.OrdinalIgnoreCase);
    }

    internal bool IsSignerFingerprintTrusted(string? signerFingerprint)
    {
        return !string.IsNullOrWhiteSpace(signerFingerprint) &&
            TrustedSignerFingerprints.Contains(NormalizeChecksum(signerFingerprint), StringComparer.OrdinalIgnoreCase);
    }

    internal bool TryResolveTrustedSignaturePublicKey(
        string? keyId,
        string? signerFingerprint,
        out string publicKey)
    {
        if (!string.IsNullOrWhiteSpace(keyId) &&
            TrustedSignaturePublicKeys.TryGetValue(keyId.Trim(), out var resolvedPublicKey) &&
            !string.IsNullOrWhiteSpace(resolvedPublicKey))
        {
            publicKey = resolvedPublicKey;
            return true;
        }

        if (!string.IsNullOrWhiteSpace(signerFingerprint))
        {
            var normalizedFingerprint = NormalizeChecksum(signerFingerprint);
            foreach (var pair in TrustedSignaturePublicKeys)
            {
                if (string.Equals(NormalizeChecksum(pair.Key), normalizedFingerprint, StringComparison.OrdinalIgnoreCase))
                {
                    publicKey = pair.Value;
                    return true;
                }
            }
        }

        publicKey = string.Empty;
        return false;
    }

    internal bool TryResolveTrustedSignatureCertificate(
        string? keyId,
        string? signerFingerprint,
        out string certificate)
    {
        if (!string.IsNullOrWhiteSpace(keyId) &&
            TrustedSignatureCertificates.TryGetValue(keyId.Trim(), out var resolvedCertificate) &&
            !string.IsNullOrWhiteSpace(resolvedCertificate))
        {
            certificate = resolvedCertificate;
            return true;
        }

        if (!string.IsNullOrWhiteSpace(signerFingerprint))
        {
            var normalizedFingerprint = NormalizeChecksum(signerFingerprint);
            foreach (var pair in TrustedSignatureCertificates)
            {
                if (string.Equals(NormalizeChecksum(pair.Key), normalizedFingerprint, StringComparison.OrdinalIgnoreCase))
                {
                    certificate = pair.Value;
                    return true;
                }
            }
        }

        certificate = string.Empty;
        return false;
    }

    private static string[] NormalizeList(IReadOnlyList<string>? values)
    {
        return values?
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }

    private static Dictionary<string, CapabilityAccess> NormalizeRules(IReadOnlyDictionary<string, CapabilityAccess>? rules)
    {
        var normalized = new Dictionary<string, CapabilityAccess>(StringComparer.OrdinalIgnoreCase);
        if (rules is null)
        {
            return normalized;
        }

        foreach (var pair in rules)
        {
            if (string.IsNullOrWhiteSpace(pair.Key))
            {
                continue;
            }

            normalized[pair.Key.Trim()] = pair.Value;
        }

        return normalized;
    }

    private static Dictionary<string, string> NormalizeStringDictionary(IReadOnlyDictionary<string, string>? values)
    {
        var normalized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (values is null)
        {
            return normalized;
        }

        foreach (var pair in values)
        {
            if (string.IsNullOrWhiteSpace(pair.Key) || string.IsNullOrWhiteSpace(pair.Value))
            {
                continue;
            }

            normalized[pair.Key.Trim()] = pair.Value.Trim();
        }

        return normalized;
    }

    private static Dictionary<string, IReadOnlyList<string>> NormalizeChecksumRules(
        IReadOnlyDictionary<string, IReadOnlyList<string>>? rules)
    {
        var normalized = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        if (rules is null)
        {
            return normalized;
        }

        foreach (var pair in rules)
        {
            if (string.IsNullOrWhiteSpace(pair.Key))
            {
                continue;
            }

            var values = pair.Value?
                .Where(static value => !string.IsNullOrWhiteSpace(value))
                .Select(static value => NormalizeChecksum(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
                .ToArray() ?? [];

            if (values.Length == 0)
            {
                continue;
            }

            normalized[pair.Key.Trim()] = values;
        }

        return normalized;
    }

    private static string[] NormalizeChecksums(IReadOnlyList<string>? values)
    {
        return values?
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => NormalizeChecksum(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }

    private static string NormalizeChecksum(string value)
    {
        var normalized = value.Trim();
        if (normalized.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized["sha256:".Length..];
        }

        return normalized.Replace(" ", string.Empty, StringComparison.Ordinal).ToLowerInvariant();
    }

    private static bool TryParseCapabilityAccess(string? value, out CapabilityAccess access)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            access = default;
            return false;
        }

        return Enum.TryParse(value.Trim(), ignoreCase: true, out access);
    }
}
