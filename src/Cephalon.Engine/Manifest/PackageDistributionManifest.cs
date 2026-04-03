namespace Cephalon.Engine.Manifest;

/// <summary>
/// Describes where an independently shipped package is distributed from.
/// </summary>
public sealed class PackageDistributionManifest
{
    /// <summary>
    /// Creates a package distribution manifest entry.
    /// </summary>
    /// <param name="channel">The release channel declared by the package manifest, when available.</param>
    /// <param name="manifestUri">The externally reachable package-manifest URI declared by the package manifest, when available.</param>
    /// <param name="packageUri">The externally reachable package archive or feed URI declared by the package manifest, when available.</param>
    public PackageDistributionManifest(
        string? channel,
        string? manifestUri,
        string? packageUri)
    {
        Channel = channel;
        ManifestUri = manifestUri;
        PackageUri = packageUri;
    }

    /// <summary>
    /// Gets the declared release channel, when available.
    /// </summary>
    public string? Channel { get; }

    /// <summary>
    /// Gets the declared externally reachable package-manifest URI, when available.
    /// </summary>
    public string? ManifestUri { get; }

    /// <summary>
    /// Gets the declared externally reachable package archive or feed URI, when available.
    /// </summary>
    public string? PackageUri { get; }
}
