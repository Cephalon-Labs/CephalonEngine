using Microsoft.Extensions.Configuration;

namespace Cephalon.Engine.Configuration;

public sealed class EngineSettings
{
    public const string SectionName = "Engine";

    public EngineSettings(
        string? blueprint = null,
        IReadOnlyList<string>? patterns = null,
        IReadOnlyList<string>? transports = null,
        IReadOnlyList<string>? technologies = null,
        EngineOptions? options = null,
        ModuleDiscoverySettings? discovery = null,
        LocalizationSettings? localization = null,
        FailurePolicy? failurePolicy = null,
        TrustPolicy? trustPolicy = null,
        PackagePolicy? packagePolicy = null)
    {
        Blueprint = string.IsNullOrWhiteSpace(blueprint) ? null : blueprint.Trim();
        Patterns = patterns?
            .Where(pattern => !string.IsNullOrWhiteSpace(pattern))
            .Select(pattern => pattern.Trim())
            .ToArray() ?? [];
        Transports = transports?
            .Where(transport => !string.IsNullOrWhiteSpace(transport))
            .Select(transport => transport.Trim())
            .ToArray() ?? [];
        Technologies = technologies?
            .Where(technology => !string.IsNullOrWhiteSpace(technology))
            .Select(technology => technology.Trim())
            .ToArray() ?? [];
        Options = options ?? EngineOptions.Empty;
        Discovery = discovery ?? ModuleDiscoverySettings.Empty;
        Localization = localization ?? LocalizationSettings.Empty;
        FailurePolicy = failurePolicy ?? FailurePolicy.Default;
        TrustPolicy = trustPolicy ?? TrustPolicy.Default;
        PackagePolicy = packagePolicy ?? PackagePolicy.Default;
    }

    public string? Blueprint { get; }

    public IReadOnlyList<string> Patterns { get; }

    public IReadOnlyList<string> Transports { get; }

    public IReadOnlyList<string> Technologies { get; }

    public EngineOptions Options { get; }

    public ModuleDiscoverySettings Discovery { get; }

    public LocalizationSettings Localization { get; }

    public FailurePolicy FailurePolicy { get; }

    public TrustPolicy TrustPolicy { get; }

    public PackagePolicy PackagePolicy { get; }

    public bool HasValues =>
        Blueprint is not null ||
        Patterns.Count > 0 ||
        Transports.Count > 0 ||
        Technologies.Count > 0 ||
        Options.HasValues ||
        Discovery.HasValues ||
        Localization.HasValues ||
        FailurePolicy.HasValues ||
        TrustPolicy.HasValues ||
        PackagePolicy.HasValues;

    public static EngineSettings FromConfiguration(
        IConfiguration configuration,
        string sectionPath = SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration.GetSection(sectionPath);
        var patterns = section.GetSection("Patterns")
            .GetChildren()
            .Select(child => child.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .ToArray();
        var transports = section.GetSection("Transports")
            .GetChildren()
            .Select(child => child.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .ToArray();
        var technologies = section.GetSection("Technologies")
            .GetChildren()
            .Select(child => child.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .ToArray();

        return new EngineSettings(
            blueprint: section["Blueprint"],
            patterns: patterns,
            transports: transports,
            technologies: technologies,
            options: EngineOptions.FromConfiguration(configuration, sectionPath),
            discovery: ModuleDiscoverySettings.FromConfiguration(configuration, sectionPath),
            localization: LocalizationSettings.FromConfiguration(configuration, sectionPath),
            failurePolicy: FailurePolicy.FromConfiguration(configuration, sectionPath),
            trustPolicy: TrustPolicy.FromConfiguration(configuration, sectionPath),
            packagePolicy: PackagePolicy.FromConfiguration(configuration, sectionPath));
    }
}
