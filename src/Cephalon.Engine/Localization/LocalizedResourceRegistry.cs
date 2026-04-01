using Cephalon.Abstractions.Localization;
using Cephalon.Engine.Configuration;
using System.Globalization;

namespace Cephalon.Engine.Localization;

internal sealed class LocalizedResourceRegistry : ILocalizedResourceRegistry
{
    private readonly Dictionary<string, Dictionary<string, string>> resources =
        new(StringComparer.OrdinalIgnoreCase);

    public void Add(string culture, string key, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(culture);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var normalizedCulture = CultureInfo.GetCultureInfo(culture.Trim()).Name;
        if (!resources.TryGetValue(normalizedCulture, out var entries))
        {
            entries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            resources[normalizedCulture] = entries;
        }

        entries[key.Trim()] = value.Trim();
    }

    public void Add(string culture, IReadOnlyDictionary<string, string> resources)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(culture);
        ArgumentNullException.ThrowIfNull(resources);

        foreach (var resource in resources)
        {
            if (string.IsNullOrWhiteSpace(resource.Key) || string.IsNullOrWhiteSpace(resource.Value))
            {
                continue;
            }

            Add(culture, resource.Key, resource.Value);
        }
    }

    public LocalizationSettings ToSettings()
    {
        var normalizedResources = resources.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyDictionary<string, string>)pair.Value,
            StringComparer.OrdinalIgnoreCase);

        return new LocalizationSettings(
            supportedCultures: resources.Keys.ToArray(),
            resources: normalizedResources);
    }
}
