namespace Cephalon.Engine.Composition.Packages;

internal sealed class PackageDependencyLoadRequest
{
    public PackageDependencyLoadRequest(
        string id,
        string? minimumVersion = null,
        string? maximumVersion = null)
    {
        Id = string.IsNullOrWhiteSpace(id)
            ? throw new ArgumentException("Package dependency id is required.", nameof(id))
            : id.Trim();
        MinimumVersion = string.IsNullOrWhiteSpace(minimumVersion) ? null : minimumVersion.Trim();
        MaximumVersion = string.IsNullOrWhiteSpace(maximumVersion) ? null : maximumVersion.Trim();
    }

    public string Id { get; }

    public string? MinimumVersion { get; }

    public string? MaximumVersion { get; }

    public bool HasVersionRange =>
        !string.IsNullOrWhiteSpace(MinimumVersion) ||
        !string.IsNullOrWhiteSpace(MaximumVersion);
}
