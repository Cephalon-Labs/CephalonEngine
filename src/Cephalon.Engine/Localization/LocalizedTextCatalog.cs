using Cephalon.Abstractions.Localization;
using Cephalon.Engine.Configuration;
using System.Globalization;

namespace Cephalon.Engine.Localization;

/// <summary>
/// Resolves localized resources from built-in and configuration-supplied resource catalogs.
/// </summary>
public sealed class LocalizedTextCatalog : ILocalizedTextCatalog
{
    private readonly Dictionary<string, IReadOnlyDictionary<string, string>> resources;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalizedTextCatalog" /> class.
    /// </summary>
    /// <param name="settings">The localization settings that supply culture and resource overrides.</param>
    public LocalizedTextCatalog(LocalizationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        resources = MergeResources(settings);
        DefaultCulture = ResolveDefaultCulture(settings.DefaultCulture);
        SupportedCultures = resources.Keys
            .Append(DefaultCulture)
            .Concat(settings.SupportedCultures)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(culture => culture, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>
    /// Gets the default culture used when no explicit culture is requested.
    /// </summary>
    public string DefaultCulture { get; }

    /// <summary>
    /// Gets the supported cultures available from the merged resource catalog.
    /// </summary>
    public IReadOnlyList<string> SupportedCultures { get; }

    /// <summary>
    /// Attempts to resolve a localized value for the specified key and culture.
    /// </summary>
    /// <param name="key">The resource key to resolve.</param>
    /// <param name="culture">The preferred culture to resolve from.</param>
    /// <param name="value">The resolved localized value when found.</param>
    /// <returns><see langword="true" /> when a value was resolved; otherwise, <see langword="false" />.</returns>
    public bool TryGet(string key, string? culture, out string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        foreach (var candidate in GetCultureChain(culture))
        {
            if (resources.TryGetValue(candidate, out var cultureResources) &&
                cultureResources.TryGetValue(key.Trim(), out value!))
            {
                return true;
            }
        }

        value = string.Empty;
        return false;
    }

    /// <summary>
    /// Resolves a localized value or returns the provided fallback.
    /// </summary>
    /// <param name="key">The resource key to resolve.</param>
    /// <param name="culture">The preferred culture to resolve from.</param>
    /// <param name="fallback">The fallback value to return when the resource cannot be resolved.</param>
    /// <returns>The resolved localized value or the fallback.</returns>
    public string ResolveText(string key, string? culture = null, string? fallback = null)
    {
        return TryGet(key, culture, out var value)
            ? value
            : fallback ?? key;
    }

    /// <summary>
    /// Gets the merged resources visible for the specified culture.
    /// </summary>
    /// <param name="culture">The preferred culture to resolve from.</param>
    /// <returns>The merged resource dictionary for the resolved culture chain.</returns>
    public IReadOnlyDictionary<string, string> GetResources(string? culture = null)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var candidate in GetCultureChain(culture).Reverse())
        {
            if (!resources.TryGetValue(candidate, out var cultureResources))
            {
                continue;
            }

            foreach (var resource in cultureResources)
            {
                result[resource.Key] = resource.Value;
            }
        }

        return result;
    }

    /// <summary>
    /// Creates a serialization-friendly snapshot of the merged localized resources.
    /// </summary>
    /// <param name="culture">The preferred culture to resolve from.</param>
    /// <returns>A snapshot of the resolved localization view.</returns>
    public LocalizedResourcesSnapshot CreateSnapshot(string? culture = null)
    {
        return new LocalizedResourcesSnapshot(
            defaultCulture: DefaultCulture,
            resolvedCulture: ResolveCulture(culture),
            supportedCultures: SupportedCultures,
            resources: GetResources(culture));
    }

    private string ResolveCulture(string? culture)
    {
        foreach (var candidate in GetCultureChain(culture))
        {
            if (resources.ContainsKey(candidate))
            {
                return candidate;
            }
        }

        return DefaultCulture;
    }

    private IEnumerable<string> GetCultureChain(string? culture)
    {
        var candidates = new List<string>();

        if (!string.IsNullOrWhiteSpace(culture))
        {
            var current = CultureInfo.GetCultureInfo(culture.Trim());
            while (!string.IsNullOrWhiteSpace(current.Name))
            {
                candidates.Add(current.Name);
                current = current.Parent;
            }
        }

        candidates.Add(DefaultCulture);
        candidates.Add(BuiltInLocalizedResources.DefaultCulture);

        return candidates
            .Where(candidate => !string.IsNullOrWhiteSpace(candidate))
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static Dictionary<string, IReadOnlyDictionary<string, string>> MergeResources(LocalizationSettings settings)
    {
        var merged = BuiltInLocalizedResources.All.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyDictionary<string, string>)new Dictionary<string, string>(pair.Value, StringComparer.OrdinalIgnoreCase),
            StringComparer.OrdinalIgnoreCase);

        foreach (var culture in settings.Resources)
        {
            if (!merged.TryGetValue(culture.Key, out var existing))
            {
                merged[culture.Key] = new Dictionary<string, string>(culture.Value, StringComparer.OrdinalIgnoreCase);
                continue;
            }

            var updated = new Dictionary<string, string>(existing, StringComparer.OrdinalIgnoreCase);
            foreach (var resource in culture.Value)
            {
                updated[resource.Key] = resource.Value;
            }

            merged[culture.Key] = updated;
        }

        return merged;
    }

    private static string ResolveDefaultCulture(string? configuredDefaultCulture)
    {
        return string.IsNullOrWhiteSpace(configuredDefaultCulture)
            ? BuiltInLocalizedResources.DefaultCulture
            : CultureInfo.GetCultureInfo(configuredDefaultCulture.Trim()).Name;
    }
}
