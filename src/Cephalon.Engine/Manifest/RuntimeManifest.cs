using Cephalon.Abstractions.AppModel;
namespace Cephalon.Engine.Manifest;

/// <summary>
/// Represents the immutable manifest produced when a Cephalon runtime is built.
/// </summary>
/// <remarks>
/// The runtime manifest is the main contract for describing the built engine shape. It captures
/// the selected application profile, the effective module set, the published capabilities, and any
/// package-loading metadata that contributed modules to the runtime.
/// </remarks>
public sealed class RuntimeManifest
{
    /// <summary>
    /// Gets the current manifest schema version emitted by the engine.
    /// </summary>
    public const string CurrentVersion = "2.0";

    /// <summary>
    /// Creates a new runtime manifest.
    /// </summary>
    /// <param name="manifestVersion">The manifest schema version.</param>
    /// <param name="engineVersion">The version of the engine that produced the manifest.</param>
    /// <param name="generatedAtUtc">The UTC timestamp when the manifest was created.</param>
    /// <param name="appProfile">The resolved application profile.</param>
    /// <param name="modules">The effective ordered module set.</param>
    /// <param name="capabilities">The effective capability set after policy has been applied.</param>
    /// <param name="packages">The package-loading metadata associated with the runtime.</param>
    public RuntimeManifest(
        string manifestVersion,
        string engineVersion,
        DateTimeOffset generatedAtUtc,
        AppProfile appProfile,
        IReadOnlyList<ModuleManifest> modules,
        IReadOnlyList<CapabilityManifest> capabilities,
        IReadOnlyList<PackageManifest>? packages = null)
    {
        ManifestVersion = manifestVersion ?? throw new ArgumentNullException(nameof(manifestVersion));
        EngineVersion = engineVersion ?? throw new ArgumentNullException(nameof(engineVersion));
        GeneratedAtUtc = generatedAtUtc;
        AppProfile = appProfile ?? throw new ArgumentNullException(nameof(appProfile));
        Modules = modules ?? throw new ArgumentNullException(nameof(modules));
        Capabilities = capabilities ?? throw new ArgumentNullException(nameof(capabilities));
        Packages = packages ?? [];
    }

    /// <summary>
    /// Gets the manifest schema version.
    /// </summary>
    public string ManifestVersion { get; }

    /// <summary>
    /// Gets the engine version that produced the manifest.
    /// </summary>
    public string EngineVersion { get; }

    /// <summary>
    /// Gets the UTC timestamp when the manifest was generated.
    /// </summary>
    public DateTimeOffset GeneratedAtUtc { get; }

    /// <summary>
    /// Gets the resolved application profile, including blueprint, patterns, transports, technologies,
    /// and any scaffold guidance.
    /// </summary>
    public AppProfile AppProfile { get; }

    /// <summary>
    /// Gets the effective module set after discovery, policy filtering, and dependency ordering.
    /// </summary>
    public IReadOnlyList<ModuleManifest> Modules { get; }

    /// <summary>
    /// Gets the effective capability set after capability and trust policy filtering.
    /// </summary>
    public IReadOnlyList<CapabilityManifest> Capabilities { get; }

    /// <summary>
    /// Gets the packages that contributed modules to the runtime, if any were loaded from packages.
    /// </summary>
    public IReadOnlyList<PackageManifest> Packages { get; }
}
