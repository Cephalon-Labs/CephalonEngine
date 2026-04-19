namespace Cephalon.Abstractions.Features;

/// <summary>
/// Describes the optional targeting constraints that govern when a feature flag is considered
/// active for a given runtime evaluation context.
/// </summary>
public sealed class FeatureFlagTargetingDescriptor
{
    /// <summary>
    /// Gets an empty targeting descriptor with no constraints.
    /// </summary>
    public static FeatureFlagTargetingDescriptor Empty { get; } = new();

    /// <summary>
    /// Creates feature-flag targeting constraints.
    /// </summary>
    /// <param name="includedModuleIds">
    /// The module identifiers that are explicitly included in the targeted audience.
    /// </param>
    /// <param name="excludedModuleIds">
    /// The module identifiers that are explicitly excluded from the targeted audience.
    /// </param>
    /// <param name="includedBehaviorIds">
    /// The behavior identifiers that are explicitly included in the targeted audience.
    /// </param>
    /// <param name="excludedBehaviorIds">
    /// The behavior identifiers that are explicitly excluded from the targeted audience.
    /// </param>
    /// <param name="includedCapabilityKeys">
    /// The capability keys that are explicitly included in the targeted audience.
    /// </param>
    /// <param name="excludedCapabilityKeys">
    /// The capability keys that are explicitly excluded from the targeted audience.
    /// </param>
    /// <param name="includedTransportIds">
    /// The transport identifiers that are explicitly included in the targeted audience.
    /// </param>
    /// <param name="excludedTransportIds">
    /// The transport identifiers that are explicitly excluded from the targeted audience.
    /// </param>
    /// <param name="includedEnvironmentNames">
    /// The environment names that are explicitly included in the targeted audience.
    /// </param>
    /// <param name="excludedEnvironmentNames">
    /// The environment names that are explicitly excluded from the targeted audience.
    /// </param>
    /// <param name="includedTenantIds">
    /// The tenant identifiers that are explicitly included in the targeted audience.
    /// </param>
    /// <param name="excludedTenantIds">
    /// The tenant identifiers that are explicitly excluded from the targeted audience.
    /// </param>
    /// <param name="includedSubjectIds">
    /// The subject identifiers that are explicitly included in the targeted audience.
    /// </param>
    /// <param name="excludedSubjectIds">
    /// The subject identifiers that are explicitly excluded from the targeted audience.
    /// </param>
    /// <param name="includedTags">
    /// The descriptive tags that are explicitly included in the targeted audience.
    /// </param>
    /// <param name="excludedTags">
    /// The descriptive tags that are explicitly excluded from the targeted audience.
    /// </param>
    public FeatureFlagTargetingDescriptor(
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
