using Microsoft.Extensions.Configuration;

namespace Cephalon.Engine.Configuration;

/// <summary>
/// Defines governance requirements for independently shipped module packages.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="PackagePolicy" /> lets a host decide whether package loading should remain permissive
/// or require stronger metadata such as declared versions, compatibility ranges, publisher provenance,
/// or integrity hashes.
/// </para>
/// <para>
/// The policy applies to package assembly-path loads, manifest-file loads, and package-directory discovery.
/// When stricter requirements are enabled, packages that do not declare the required metadata fail fast
/// during engine build instead of loading ambiguously at runtime.
/// </para>
/// </remarks>
public sealed class PackagePolicy
{
    /// <summary>
    /// Gets the default package policy.
    /// </summary>
    public static PackagePolicy Default { get; } = new();

    /// <summary>
    /// Creates a package policy.
    /// </summary>
    /// <param name="allowAssemblyPathPackages">
    /// <see langword="true" /> to allow raw assembly-path packages; otherwise package loads must come
    /// through a manifest-driven flow.
    /// </param>
    /// <param name="requireVersion">
    /// <see langword="true" /> to require a declared package version in <c>cephalon.package.json</c>.
    /// </param>
    /// <param name="requireMinimumEngineVersion">
    /// <see langword="true" /> to require <c>compatibility.minimumEngineVersion</c>.
    /// </param>
    /// <param name="requireMaximumEngineVersion">
    /// <see langword="true" /> to require <c>compatibility.maximumEngineVersion</c>.
    /// </param>
    /// <param name="requireSupportedTargetFrameworks">
    /// <see langword="true" /> to require <c>compatibility.supportedTargetFrameworks</c>.
    /// </param>
    /// <param name="requirePublisherId">
    /// <see langword="true" /> to require <c>publisher.id</c>.
    /// </param>
    /// <param name="requireSignatureFingerprint">
    /// <see langword="true" /> to require at least one declared signature entry to provide a signer fingerprint.
    /// </param>
    /// <param name="requireSignatureKeyId">
    /// <see langword="true" /> to require at least one declared signature entry to provide a signature key identifier.
    /// </param>
    /// <param name="requireSignatureValue">
    /// <see langword="true" /> to require at least one declared signature entry to provide a detached signature value.
    /// </param>
    /// <param name="requireSignatureVerification">
    /// <see langword="true" /> to require a successful cryptographic signature verification against
    /// a trusted public key.
    /// </param>
    /// <param name="requireIntegritySha256">
    /// <see langword="true" /> to require <c>integrity.sha256</c>.
    /// </param>
    public PackagePolicy(
        bool allowAssemblyPathPackages = true,
        bool requireVersion = false,
        bool requireMinimumEngineVersion = false,
        bool requireMaximumEngineVersion = false,
        bool requireSupportedTargetFrameworks = false,
        bool requirePublisherId = false,
        bool requireSignatureFingerprint = false,
        bool requireSignatureKeyId = false,
        bool requireSignatureValue = false,
        bool requireSignatureVerification = false,
        bool requireIntegritySha256 = false)
    {
        AllowAssemblyPathPackages = allowAssemblyPathPackages;
        RequireVersion = requireVersion;
        RequireMinimumEngineVersion = requireMinimumEngineVersion;
        RequireMaximumEngineVersion = requireMaximumEngineVersion;
        RequireSupportedTargetFrameworks = requireSupportedTargetFrameworks;
        RequirePublisherId = requirePublisherId;
        RequireSignatureFingerprint = requireSignatureFingerprint;
        RequireSignatureKeyId = requireSignatureKeyId;
        RequireSignatureValue = requireSignatureValue;
        RequireSignatureVerification = requireSignatureVerification;
        RequireIntegritySha256 = requireIntegritySha256;
    }

    /// <summary>
    /// Gets a value indicating whether raw assembly-path packages are allowed.
    /// </summary>
    public bool AllowAssemblyPathPackages { get; }

    /// <summary>
    /// Gets a value indicating whether package manifests must declare a version.
    /// </summary>
    public bool RequireVersion { get; }

    /// <summary>
    /// Gets a value indicating whether package manifests must declare a minimum supported engine version.
    /// </summary>
    public bool RequireMinimumEngineVersion { get; }

    /// <summary>
    /// Gets a value indicating whether package manifests must declare a maximum supported engine version.
    /// </summary>
    public bool RequireMaximumEngineVersion { get; }

    /// <summary>
    /// Gets a value indicating whether package manifests must declare supported target frameworks.
    /// </summary>
    public bool RequireSupportedTargetFrameworks { get; }

    /// <summary>
    /// Gets a value indicating whether package manifests must declare a stable publisher identifier.
    /// </summary>
    public bool RequirePublisherId { get; }

    /// <summary>
    /// Gets a value indicating whether package manifests must declare a signer fingerprint on at least one signature entry.
    /// </summary>
    public bool RequireSignatureFingerprint { get; }

    /// <summary>
    /// Gets a value indicating whether package manifests must declare a signature key identifier on at least one signature entry.
    /// </summary>
    public bool RequireSignatureKeyId { get; }

    /// <summary>
    /// Gets a value indicating whether package manifests must declare a detached signature value on at least one signature entry.
    /// </summary>
    public bool RequireSignatureValue { get; }

    /// <summary>
    /// Gets a value indicating whether package signatures must verify against a trusted public key.
    /// </summary>
    public bool RequireSignatureVerification { get; }

    /// <summary>
    /// Gets a value indicating whether package manifests must declare an integrity SHA-256 value.
    /// </summary>
    public bool RequireIntegritySha256 { get; }

    /// <summary>
    /// Gets a value indicating whether the policy differs from the default baseline.
    /// </summary>
    public bool HasValues =>
        AllowAssemblyPathPackages != Default.AllowAssemblyPathPackages ||
        RequireVersion != Default.RequireVersion ||
        RequireMinimumEngineVersion != Default.RequireMinimumEngineVersion ||
        RequireMaximumEngineVersion != Default.RequireMaximumEngineVersion ||
        RequireSupportedTargetFrameworks != Default.RequireSupportedTargetFrameworks ||
        RequirePublisherId != Default.RequirePublisherId ||
        RequireSignatureFingerprint != Default.RequireSignatureFingerprint ||
        RequireSignatureKeyId != Default.RequireSignatureKeyId ||
        RequireSignatureValue != Default.RequireSignatureValue ||
        RequireSignatureVerification != Default.RequireSignatureVerification ||
        RequireIntegritySha256 != Default.RequireIntegritySha256;

    internal bool RequiresManifestMetadata =>
        RequireVersion ||
        RequireMinimumEngineVersion ||
        RequireMaximumEngineVersion ||
        RequireSupportedTargetFrameworks ||
        RequirePublisherId ||
        RequireSignatureFingerprint ||
        RequireSignatureKeyId ||
        RequireSignatureValue ||
        RequireSignatureVerification ||
        RequireIntegritySha256;

    /// <summary>
    /// Reads package policy from configuration.
    /// </summary>
    /// <param name="configuration">The root configuration that contains the engine section.</param>
    /// <param name="sectionPath">
    /// The configuration path that should be interpreted as the engine settings section.
    /// The default value is <see cref="EngineSettings.SectionName" />.
    /// </param>
    /// <returns>The configured package policy, or <see cref="Default" /> when no values are supplied.</returns>
    public static PackagePolicy FromConfiguration(
        IConfiguration configuration,
        string sectionPath = EngineSettings.SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration
            .GetSection(sectionPath)
            .GetSection("PackagePolicy");

        return new PackagePolicy(
            allowAssemblyPathPackages: TryParseBoolean(section["AllowAssemblyPathPackages"], out var allowAssemblyPathPackages)
                ? allowAssemblyPathPackages
                : Default.AllowAssemblyPathPackages,
            requireVersion: TryParseBoolean(section["RequireVersion"], out var requireVersion) && requireVersion,
            requireMinimumEngineVersion: TryParseBoolean(section["RequireMinimumEngineVersion"], out var requireMinimumEngineVersion) && requireMinimumEngineVersion,
            requireMaximumEngineVersion: TryParseBoolean(section["RequireMaximumEngineVersion"], out var requireMaximumEngineVersion) && requireMaximumEngineVersion,
            requireSupportedTargetFrameworks: TryParseBoolean(section["RequireSupportedTargetFrameworks"], out var requireSupportedTargetFrameworks) && requireSupportedTargetFrameworks,
            requirePublisherId: TryParseBoolean(section["RequirePublisherId"], out var requirePublisherId) && requirePublisherId,
            requireSignatureFingerprint: TryParseBoolean(section["RequireSignatureFingerprint"], out var requireSignatureFingerprint) && requireSignatureFingerprint,
            requireSignatureKeyId: TryParseBoolean(section["RequireSignatureKeyId"], out var requireSignatureKeyId) && requireSignatureKeyId,
            requireSignatureValue: TryParseBoolean(section["RequireSignatureValue"], out var requireSignatureValue) && requireSignatureValue,
            requireSignatureVerification: TryParseBoolean(section["RequireSignatureVerification"], out var requireSignatureVerification) && requireSignatureVerification,
            requireIntegritySha256: TryParseBoolean(section["RequireIntegritySha256"], out var requireIntegritySha256) && requireIntegritySha256);
    }

    private static bool TryParseBoolean(string? value, out bool enabled)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            enabled = default;
            return false;
        }

        return bool.TryParse(value.Trim(), out enabled);
    }
}
