namespace Cephalon.Engine.Composition.Packages;

internal sealed record PackageDistributionLoadRequest(
    string? Channel,
    string? ManifestUri,
    string? PackageUri);
