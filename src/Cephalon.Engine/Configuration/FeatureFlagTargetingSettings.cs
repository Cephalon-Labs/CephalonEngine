using Microsoft.Extensions.Configuration;

namespace Cephalon.Engine.Configuration;

/// <summary>
/// Describes configuration-driven targeting constraints for one feature flag.
/// </summary>
public sealed class FeatureFlagTargetingSettings
{
    /// <summary>
    /// Gets an empty feature-flag targeting settings instance.
    /// </summary>
    public static FeatureFlagTargetingSettings Empty { get; } = new();

    /// <summary>
    /// Creates feature-flag targeting settings.
    /// </summary>
    /// <param name="includedModuleIds">The explicitly included module identifiers.</param>
    /// <param name="excludedModuleIds">The explicitly excluded module identifiers.</param>
    /// <param name="includedBehaviorIds">The explicitly included behavior identifiers.</param>
    /// <param name="excludedBehaviorIds">The explicitly excluded behavior identifiers.</param>
    /// <param name="includedCapabilityKeys">The explicitly included capability keys.</param>
    /// <param name="excludedCapabilityKeys">The explicitly excluded capability keys.</param>
    /// <param name="includedTransportIds">The explicitly included transport identifiers.</param>
    /// <param name="excludedTransportIds">The explicitly excluded transport identifiers.</param>
    /// <param name="includedEnvironmentNames">The explicitly included environment names.</param>
    /// <param name="excludedEnvironmentNames">The explicitly excluded environment names.</param>
    /// <param name="includedTenantIds">The explicitly included tenant identifiers.</param>
    /// <param name="excludedTenantIds">The explicitly excluded tenant identifiers.</param>
    /// <param name="includedSubjectIds">The explicitly included subject identifiers.</param>
    /// <param name="excludedSubjectIds">The explicitly excluded subject identifiers.</param>
    /// <param name="includedTags">The explicitly included descriptive tags.</param>
    /// <param name="excludedTags">The explicitly excluded descriptive tags.</param>
    public FeatureFlagTargetingSettings(
        IReadOnlyList<string>? includedModuleIds = null,
        IReadOnlyList<string>? excludedModuleIds = null,
        IReadOnlyList<string>? includedBehaviorIds = null,
        IReadOnlyList<string>? excludedBehaviorIds = null,
        IReadOnlyList<string>? includedCapabilityKeys = null,
        IReadOnlyList<string>? excludedCapabilityKeys = null,
        IReadOnlyList<string>? includedTransportIds = null,
        IReadOnlyList<string>? excludedTransportIds = null,
        IReadOnlyList<string>? includedEnvironmentNames = null,
        IReadOnlyList<string>? excludedEnvironmentNames = null,
        IReadOnlyList<string>? includedTenantIds = null,
        IReadOnlyList<string>? excludedTenantIds = null,
        IReadOnlyList<string>? includedSubjectIds = null,
        IReadOnlyList<string>? excludedSubjectIds = null,
        IReadOnlyList<string>? includedTags = null,
        IReadOnlyList<string>? excludedTags = null)
    {
        IncludedModuleIds = Normalize(includedModuleIds);
        ExcludedModuleIds = Normalize(excludedModuleIds);
        IncludedBehaviorIds = Normalize(includedBehaviorIds);
        ExcludedBehaviorIds = Normalize(excludedBehaviorIds);
        IncludedCapabilityKeys = Normalize(includedCapabilityKeys);
        ExcludedCapabilityKeys = Normalize(excludedCapabilityKeys);
        IncludedTransportIds = Normalize(includedTransportIds);
        ExcludedTransportIds = Normalize(excludedTransportIds);
        IncludedEnvironmentNames = Normalize(includedEnvironmentNames);
        ExcludedEnvironmentNames = Normalize(excludedEnvironmentNames);
        IncludedTenantIds = Normalize(includedTenantIds);
        ExcludedTenantIds = Normalize(excludedTenantIds);
        IncludedSubjectIds = Normalize(includedSubjectIds);
        ExcludedSubjectIds = Normalize(excludedSubjectIds);
        IncludedTags = Normalize(includedTags);
        ExcludedTags = Normalize(excludedTags);
    }

    /// <summary>
    /// Gets the explicitly included module identifiers.
    /// </summary>
    public IReadOnlyList<string> IncludedModuleIds { get; }

    /// <summary>
    /// Gets the explicitly excluded module identifiers.
    /// </summary>
    public IReadOnlyList<string> ExcludedModuleIds { get; }

    /// <summary>
    /// Gets the explicitly included behavior identifiers.
    /// </summary>
    public IReadOnlyList<string> IncludedBehaviorIds { get; }

    /// <summary>
    /// Gets the explicitly excluded behavior identifiers.
    /// </summary>
    public IReadOnlyList<string> ExcludedBehaviorIds { get; }

    /// <summary>
    /// Gets the explicitly included capability keys.
    /// </summary>
    public IReadOnlyList<string> IncludedCapabilityKeys { get; }

    /// <summary>
    /// Gets the explicitly excluded capability keys.
    /// </summary>
    public IReadOnlyList<string> ExcludedCapabilityKeys { get; }

    /// <summary>
    /// Gets the explicitly included transport identifiers.
    /// </summary>
    public IReadOnlyList<string> IncludedTransportIds { get; }

    /// <summary>
    /// Gets the explicitly excluded transport identifiers.
    /// </summary>
    public IReadOnlyList<string> ExcludedTransportIds { get; }

    /// <summary>
    /// Gets the explicitly included environment names.
    /// </summary>
    public IReadOnlyList<string> IncludedEnvironmentNames { get; }

    /// <summary>
    /// Gets the explicitly excluded environment names.
    /// </summary>
    public IReadOnlyList<string> ExcludedEnvironmentNames { get; }

    /// <summary>
    /// Gets the explicitly included tenant identifiers.
    /// </summary>
    public IReadOnlyList<string> IncludedTenantIds { get; }

    /// <summary>
    /// Gets the explicitly excluded tenant identifiers.
    /// </summary>
    public IReadOnlyList<string> ExcludedTenantIds { get; }

    /// <summary>
    /// Gets the explicitly included subject identifiers.
    /// </summary>
    public IReadOnlyList<string> IncludedSubjectIds { get; }

    /// <summary>
    /// Gets the explicitly excluded subject identifiers.
    /// </summary>
    public IReadOnlyList<string> ExcludedSubjectIds { get; }

    /// <summary>
    /// Gets the explicitly included descriptive tags.
    /// </summary>
    public IReadOnlyList<string> IncludedTags { get; }

    /// <summary>
    /// Gets the explicitly excluded descriptive tags.
    /// </summary>
    public IReadOnlyList<string> ExcludedTags { get; }

    /// <summary>
    /// Gets a value indicating whether any targeting constraint was supplied.
    /// </summary>
    public bool HasValues =>
        IncludedModuleIds.Count > 0 ||
        ExcludedModuleIds.Count > 0 ||
        IncludedBehaviorIds.Count > 0 ||
        ExcludedBehaviorIds.Count > 0 ||
        IncludedCapabilityKeys.Count > 0 ||
        ExcludedCapabilityKeys.Count > 0 ||
        IncludedTransportIds.Count > 0 ||
        ExcludedTransportIds.Count > 0 ||
        IncludedEnvironmentNames.Count > 0 ||
        ExcludedEnvironmentNames.Count > 0 ||
        IncludedTenantIds.Count > 0 ||
        ExcludedTenantIds.Count > 0 ||
        IncludedSubjectIds.Count > 0 ||
        ExcludedSubjectIds.Count > 0 ||
        IncludedTags.Count > 0 ||
        ExcludedTags.Count > 0;

    /// <summary>
    /// Reads feature-flag targeting settings from the supplied configuration section.
    /// </summary>
    /// <param name="section">The configuration section that contains the targeting settings.</param>
    /// <returns>The parsed targeting settings.</returns>
    public static FeatureFlagTargetingSettings FromSection(IConfigurationSection section)
    {
        ArgumentNullException.ThrowIfNull(section);

        if (!section.Exists())
        {
            return Empty;
        }

        return new FeatureFlagTargetingSettings(
            includedModuleIds: ReadArray(section, "IncludedModuleIds"),
            excludedModuleIds: ReadArray(section, "ExcludedModuleIds"),
            includedBehaviorIds: ReadArray(section, "IncludedBehaviorIds"),
            excludedBehaviorIds: ReadArray(section, "ExcludedBehaviorIds"),
            includedCapabilityKeys: ReadArray(section, "IncludedCapabilityKeys"),
            excludedCapabilityKeys: ReadArray(section, "ExcludedCapabilityKeys"),
            includedTransportIds: ReadArray(section, "IncludedTransportIds"),
            excludedTransportIds: ReadArray(section, "ExcludedTransportIds"),
            includedEnvironmentNames: ReadArray(section, "IncludedEnvironmentNames"),
            excludedEnvironmentNames: ReadArray(section, "ExcludedEnvironmentNames"),
            includedTenantIds: ReadArray(section, "IncludedTenantIds"),
            excludedTenantIds: ReadArray(section, "ExcludedTenantIds"),
            includedSubjectIds: ReadArray(section, "IncludedSubjectIds"),
            excludedSubjectIds: ReadArray(section, "ExcludedSubjectIds"),
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
