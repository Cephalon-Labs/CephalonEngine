using Microsoft.Extensions.Configuration;

namespace Cephalon.Engine.Configuration;

/// <summary>
/// Describes localization configuration for the runtime and module resources.
/// </summary>
public sealed class LocalizationSettings
{
    /// <summary>
    /// Gets an empty localization configuration instance.
    /// </summary>
    public static LocalizationSettings Empty { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalizationSettings" /> class.
    /// </summary>
    /// <param name="defaultCulture">The default culture to use when no explicit culture is requested.</param>
    /// <param name="supportedCultures">The supported culture identifiers.</param>
    /// <param name="resources">Localized resource entries keyed by culture and resource key.</param>
    public LocalizationSettings(
        string? defaultCulture = null,
        IReadOnlyList<string>? supportedCultures = null,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>? resources = null)
    {
        DefaultCulture = string.IsNullOrWhiteSpace(defaultCulture) ? null : defaultCulture.Trim();
        SupportedCultures = NormalizeCultures(supportedCultures);
        Resources = NormalizeResources(resources);
    }

    /// <summary>
    /// Gets the default culture to use when no explicit culture is requested.
    /// </summary>
    public string? DefaultCulture { get; }

    /// <summary>
    /// Gets the supported culture identifiers.
    /// </summary>
    public IReadOnlyList<string> SupportedCultures { get; }

    /// <summary>
    /// Gets localized resource entries keyed by culture and resource key.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Resources { get; }

    /// <summary>
    /// Gets a value indicating whether any localization settings were explicitly supplied.
    /// </summary>
    public bool HasValues =>
        DefaultCulture is not null ||
        SupportedCultures.Count > 0 ||
        Resources.Count > 0;

    /// <summary>
    /// Merges another localization settings instance into the current instance.
    /// </summary>
    /// <param name="other">The localization settings to overlay on top of the current values.</param>
    /// <returns>A merged localization settings instance.</returns>
    public LocalizationSettings Merge(LocalizationSettings? other)
    {
        if (other is null || !other.HasValues)
        {
            return this;
        }

        if (!HasValues)
        {
            return other;
        }

        var mergedResources = Resources.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyDictionary<string, string>)new Dictionary<string, string>(pair.Value, StringComparer.OrdinalIgnoreCase),
            StringComparer.OrdinalIgnoreCase);

        foreach (var culture in other.Resources)
        {
            if (!mergedResources.TryGetValue(culture.Key, out var existing))
            {
                mergedResources[culture.Key] = new Dictionary<string, string>(culture.Value, StringComparer.OrdinalIgnoreCase);
                continue;
            }

            var mergedCulture = new Dictionary<string, string>(existing, StringComparer.OrdinalIgnoreCase);
            foreach (var resource in culture.Value)
            {
                mergedCulture[resource.Key] = resource.Value;
            }

            mergedResources[culture.Key] = mergedCulture;
        }

        return new LocalizationSettings(
            defaultCulture: other.DefaultCulture ?? DefaultCulture,
            supportedCultures: SupportedCultures
                .Concat(other.SupportedCultures)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            resources: mergedResources);
    }

    /// <summary>
    /// Reads localization settings from configuration.
    /// </summary>
    /// <param name="configuration">The configuration source that contains the engine section.</param>
    /// <param name="sectionPath">The root configuration section path to read from.</param>
    /// <returns>The parsed localization settings.</returns>
    public static LocalizationSettings FromConfiguration(
        IConfiguration configuration,
        string sectionPath = EngineSettings.SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var localizationSection = configuration
            .GetSection(sectionPath)
            .GetSection("Localization");

        var supportedCultures = localizationSection
            .GetSection("SupportedCultures")
            .GetChildren()
            .Select(child => child.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .ToArray();

        var resources = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var cultureSection in localizationSection.GetSection("Resources").GetChildren())
        {
            var cultureResources = cultureSection
                .GetChildren()
                .Where(child => !string.IsNullOrWhiteSpace(child.Value))
                .ToDictionary(
                    child => child.Key,
                    child => child.Value!.Trim(),
                    StringComparer.OrdinalIgnoreCase);

            if (cultureResources.Count == 0)
            {
                continue;
            }

            resources[cultureSection.Key] = cultureResources;
        }

        return new LocalizationSettings(
            defaultCulture: localizationSection["DefaultCulture"],
            supportedCultures: supportedCultures,
            resources: resources);
    }

    private static string[] NormalizeCultures(IReadOnlyList<string>? cultures)
    {
        return cultures?
            .Where(culture => !string.IsNullOrWhiteSpace(culture))
            .Select(culture => culture.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }

    private static Dictionary<string, IReadOnlyDictionary<string, string>> NormalizeResources(
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>? resources)
    {
        if (resources is null)
        {
            return new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        }

        var result = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var culture in resources)
        {
            if (string.IsNullOrWhiteSpace(culture.Key))
            {
                continue;
            }

            var entries = culture.Value?
                .Where(pair => !string.IsNullOrWhiteSpace(pair.Key) && !string.IsNullOrWhiteSpace(pair.Value))
                .ToDictionary(
                    pair => pair.Key.Trim(),
                    pair => pair.Value.Trim(),
                    StringComparer.OrdinalIgnoreCase);

            if (entries is null || entries.Count == 0)
            {
                continue;
            }

            result[culture.Key.Trim()] = entries;
        }

        return result;
    }
}
