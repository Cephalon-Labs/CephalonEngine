using Microsoft.Extensions.Configuration;

namespace Cephalon.Engine.Configuration;

/// <summary>
/// Describes configuration-driven behavior-filter hints for one backend-for-frontend binding.
/// </summary>
public sealed class BackendForFrontendBehaviorFilterSettings
{
    /// <summary>
    /// Gets an empty backend-for-frontend behavior-filter settings instance.
    /// </summary>
    public static BackendForFrontendBehaviorFilterSettings Empty { get; } = new();

    /// <summary>
    /// Creates backend-for-frontend behavior-filter settings.
    /// </summary>
    /// <param name="includedBehaviorIds">The explicit behavior identifiers that should stay visible to the client.</param>
    /// <param name="excludedBehaviorIds">The explicit behavior identifiers that should be hidden from the client.</param>
    /// <param name="includedCapabilityKeys">The explicit capability keys that should stay visible to the client.</param>
    /// <param name="excludedCapabilityKeys">The explicit capability keys that should be hidden from the client.</param>
    /// <param name="includedTags">The behavior or endpoint tags that should stay visible to the client.</param>
    /// <param name="excludedTags">The behavior or endpoint tags that should be hidden from the client.</param>
    public BackendForFrontendBehaviorFilterSettings(
        IReadOnlyList<string>? includedBehaviorIds = null,
        IReadOnlyList<string>? excludedBehaviorIds = null,
        IReadOnlyList<string>? includedCapabilityKeys = null,
        IReadOnlyList<string>? excludedCapabilityKeys = null,
        IReadOnlyList<string>? includedTags = null,
        IReadOnlyList<string>? excludedTags = null)
    {
        IncludedBehaviorIds = Normalize(includedBehaviorIds);
        ExcludedBehaviorIds = Normalize(excludedBehaviorIds);
        IncludedCapabilityKeys = Normalize(includedCapabilityKeys);
        ExcludedCapabilityKeys = Normalize(excludedCapabilityKeys);
        IncludedTags = Normalize(includedTags);
        ExcludedTags = Normalize(excludedTags);
    }

    /// <summary>
    /// Gets the explicit behavior identifiers that should stay visible to the client.
    /// </summary>
    public IReadOnlyList<string> IncludedBehaviorIds { get; }

    /// <summary>
    /// Gets the explicit behavior identifiers that should be hidden from the client.
    /// </summary>
    public IReadOnlyList<string> ExcludedBehaviorIds { get; }

    /// <summary>
    /// Gets the explicit capability keys that should stay visible to the client.
    /// </summary>
    public IReadOnlyList<string> IncludedCapabilityKeys { get; }

    /// <summary>
    /// Gets the explicit capability keys that should be hidden from the client.
    /// </summary>
    public IReadOnlyList<string> ExcludedCapabilityKeys { get; }

    /// <summary>
    /// Gets the behavior or endpoint tags that should stay visible to the client.
    /// </summary>
    public IReadOnlyList<string> IncludedTags { get; }

    /// <summary>
    /// Gets the behavior or endpoint tags that should be hidden from the client.
    /// </summary>
    public IReadOnlyList<string> ExcludedTags { get; }

    /// <summary>
    /// Gets a value indicating whether any backend-for-frontend behavior-filter settings were explicitly supplied.
    /// </summary>
    public bool HasValues =>
        IncludedBehaviorIds.Count > 0 ||
        ExcludedBehaviorIds.Count > 0 ||
        IncludedCapabilityKeys.Count > 0 ||
        ExcludedCapabilityKeys.Count > 0 ||
        IncludedTags.Count > 0 ||
        ExcludedTags.Count > 0;

    /// <summary>
    /// Reads backend-for-frontend behavior-filter settings from the supplied configuration section.
    /// </summary>
    /// <param name="section">The configuration section that contains the behavior-filter settings.</param>
    /// <returns>The parsed behavior-filter settings.</returns>
    public static BackendForFrontendBehaviorFilterSettings FromSection(IConfigurationSection section)
    {
        ArgumentNullException.ThrowIfNull(section);

        if (!section.Exists())
        {
            return Empty;
        }

        return new BackendForFrontendBehaviorFilterSettings(
            includedBehaviorIds: ReadArray(section, "IncludedBehaviorIds"),
            excludedBehaviorIds: ReadArray(section, "ExcludedBehaviorIds"),
            includedCapabilityKeys: ReadArray(section, "IncludedCapabilityKeys"),
            excludedCapabilityKeys: ReadArray(section, "ExcludedCapabilityKeys"),
            includedTags: ReadArray(section, "IncludedTags"),
            excludedTags: ReadArray(section, "ExcludedTags"));
    }

    private static string[] ReadArray(IConfiguration section, string childSectionName)
    {
        return Normalize(section.GetSection(childSectionName)
            .GetChildren()
            .Select(static child => child.Value)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value!)
            .ToArray());
    }

    private static string[] Normalize(IReadOnlyList<string>? values)
    {
        return values?
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }
}
