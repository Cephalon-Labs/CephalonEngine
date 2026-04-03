namespace Cephalon.Engine.Manifest;

/// <summary>
/// Describes a package-to-package dependency declared by a loaded Cephalon package.
/// </summary>
public sealed class PackageDependencyManifest
{
    /// <summary>
    /// Creates a new package dependency manifest entry.
    /// </summary>
    /// <param name="id">The stable identifier of the required package.</param>
    /// <param name="minimumVersion">The minimum acceptable version of the required package, when declared.</param>
    /// <param name="maximumVersion">The maximum acceptable version of the required package, when declared.</param>
    public PackageDependencyManifest(
        string id,
        string? minimumVersion = null,
        string? maximumVersion = null)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        MinimumVersion = minimumVersion;
        MaximumVersion = maximumVersion;
    }

    /// <summary>
    /// Gets the stable identifier of the required package.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the minimum acceptable version of the required package, when declared.
    /// </summary>
    public string? MinimumVersion { get; }

    /// <summary>
    /// Gets the maximum acceptable version of the required package, when declared.
    /// </summary>
    public string? MaximumVersion { get; }
}
