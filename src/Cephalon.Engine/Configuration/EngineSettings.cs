using Microsoft.Extensions.Configuration;

namespace Cephalon.Engine.Configuration;

/// <summary>
/// Represents the configuration-driven app-model and runtime policy settings for Cephalon.
/// </summary>
public sealed class EngineSettings
{
    /// <summary>
    /// Gets the default root configuration section name for engine settings.
    /// </summary>
    public const string SectionName = "Engine";

    /// <summary>
    /// Initializes a new instance of the <see cref="EngineSettings" /> class.
    /// </summary>
    /// <param name="blueprint">The selected blueprint identifier, if any.</param>
    /// <param name="patterns">The selected pattern identifiers.</param>
    /// <param name="transports">The selected transport identifiers.</param>
    /// <param name="technologies">The selected technology identifiers.</param>
    /// <param name="options">Module and capability option overrides.</param>
    /// <param name="discovery">Module discovery inputs.</param>
    /// <param name="localization">Localization configuration values.</param>
    /// <param name="failurePolicy">Runtime failure policy values.</param>
    /// <param name="trustPolicy">Capability and package trust policy values.</param>
    /// <param name="packagePolicy">Package metadata and integrity policy values.</param>
    /// <param name="data">Configuration-driven data settings.</param>
    /// <param name="databases">Configuration-driven database topology settings.</param>
    /// <param name="identity">Configuration-driven identity and authorization settings.</param>
    /// <param name="tenancy">Configuration-driven multi-tenancy settings.</param>
    /// <param name="audit">Configuration-driven audit settings.</param>
    /// <param name="messaging">Configuration-driven messaging settings.</param>
    /// <param name="resilience">Configuration-driven resilience settings.</param>
    /// <param name="migration">Configuration-driven migration settings.</param>
    /// <param name="backendForFrontend">Configuration-driven backend-for-frontend settings.</param>
    /// <param name="features">Configuration-driven feature-flag settings.</param>
    /// <param name="cells">Configuration-driven cell-based architecture settings.</param>
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
        PackagePolicy? packagePolicy = null,
        DataSettings? data = null,
        DatabaseTopologySettings? databases = null,
        IdentitySettings? identity = null,
        TenancySettings? tenancy = null,
        AuditSettings? audit = null,
        MessagingSettings? messaging = null,
        ResilienceSettings? resilience = null,
        MigrationSettings? migration = null,
        BackendForFrontendSettings? backendForFrontend = null,
        FeatureSettings? features = null,
        CellSettings? cells = null)
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
        Data = data ?? DataSettings.Empty;
        Databases = databases ?? DatabaseTopologySettings.Empty;
        Identity = identity ?? IdentitySettings.Empty;
        Tenancy = tenancy ?? TenancySettings.Empty;
        Audit = audit ?? AuditSettings.Empty;
        Messaging = messaging ?? MessagingSettings.Empty;
        Resilience = resilience ?? ResilienceSettings.Empty;
        Migration = migration ?? MigrationSettings.Empty;
        BackendForFrontend = backendForFrontend ?? BackendForFrontendSettings.Empty;
        Features = features ?? FeatureSettings.Empty;
        Cells = cells ?? CellSettings.Empty;
    }

    /// <summary>
    /// Gets the selected blueprint identifier.
    /// </summary>
    public string? Blueprint { get; }

    /// <summary>
    /// Gets the selected pattern identifiers.
    /// </summary>
    public IReadOnlyList<string> Patterns { get; }

    /// <summary>
    /// Gets the selected transport identifiers.
    /// </summary>
    public IReadOnlyList<string> Transports { get; }

    /// <summary>
    /// Gets the selected technology identifiers.
    /// </summary>
    public IReadOnlyList<string> Technologies { get; }

    /// <summary>
    /// Gets module and capability option overrides.
    /// </summary>
    public EngineOptions Options { get; }

    /// <summary>
    /// Gets module discovery inputs.
    /// </summary>
    public ModuleDiscoverySettings Discovery { get; }

    /// <summary>
    /// Gets localization configuration values.
    /// </summary>
    public LocalizationSettings Localization { get; }

    /// <summary>
    /// Gets runtime failure policy values.
    /// </summary>
    public FailurePolicy FailurePolicy { get; }

    /// <summary>
    /// Gets capability and package trust policy values.
    /// </summary>
    public TrustPolicy TrustPolicy { get; }

    /// <summary>
    /// Gets package metadata and integrity policy values.
    /// </summary>
    public PackagePolicy PackagePolicy { get; }

    /// <summary>
    /// Gets configuration-driven data settings.
    /// </summary>
    public DataSettings Data { get; }

    /// <summary>
    /// Gets configuration-driven database topology settings.
    /// </summary>
    public DatabaseTopologySettings Databases { get; }

    /// <summary>
    /// Gets configuration-driven identity and authorization settings.
    /// </summary>
    public IdentitySettings Identity { get; }

    /// <summary>
    /// Gets configuration-driven multi-tenancy settings.
    /// </summary>
    public TenancySettings Tenancy { get; }

    /// <summary>
    /// Gets configuration-driven audit settings.
    /// </summary>
    public AuditSettings Audit { get; }

    /// <summary>
    /// Gets configuration-driven messaging settings.
    /// </summary>
    public MessagingSettings Messaging { get; }

    /// <summary>
    /// Gets configuration-driven resilience settings.
    /// </summary>
    public ResilienceSettings Resilience { get; }

    /// <summary>
    /// Gets configuration-driven migration settings.
    /// </summary>
    public MigrationSettings Migration { get; }

    /// <summary>
    /// Gets configuration-driven backend-for-frontend settings.
    /// </summary>
    public BackendForFrontendSettings BackendForFrontend { get; }

    /// <summary>
    /// Gets configuration-driven feature-flag settings.
    /// </summary>
    public FeatureSettings Features { get; }

    /// <summary>
    /// Gets configuration-driven cell-based architecture settings.
    /// </summary>
    public CellSettings Cells { get; }

    /// <summary>
    /// Gets a value indicating whether any engine settings were explicitly supplied.
    /// </summary>
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
        PackagePolicy.HasValues ||
        Data.HasValues ||
        Databases.HasValues ||
        Identity.HasValues ||
        Tenancy.HasValues ||
        Audit.HasValues ||
        Messaging.HasValues ||
        Resilience.HasValues ||
        Migration.HasValues ||
        BackendForFrontend.HasValues ||
        Features.HasValues ||
        Cells.HasValues;

    /// <summary>
    /// Reads engine settings from configuration.
    /// </summary>
    /// <param name="configuration">The configuration source that contains the engine section.</param>
    /// <param name="sectionPath">The root configuration section path to read from.</param>
    /// <returns>The parsed engine settings.</returns>
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
            packagePolicy: PackagePolicy.FromConfiguration(configuration, sectionPath),
            data: DataSettings.FromConfiguration(configuration, sectionPath),
            databases: DatabaseTopologySettings.FromConfiguration(configuration, sectionPath),
            identity: IdentitySettings.FromConfiguration(configuration, sectionPath),
            tenancy: TenancySettings.FromConfiguration(configuration, sectionPath),
            audit: AuditSettings.FromConfiguration(configuration, sectionPath),
            messaging: MessagingSettings.FromConfiguration(configuration, sectionPath),
            resilience: ResilienceSettings.FromConfiguration(configuration, sectionPath),
            migration: MigrationSettings.FromConfiguration(configuration, sectionPath),
            backendForFrontend: BackendForFrontendSettings.FromConfiguration(configuration, sectionPath),
            features: FeatureSettings.FromConfiguration(configuration, sectionPath),
            cells: CellSettings.FromConfiguration(configuration, sectionPath));
    }
}
