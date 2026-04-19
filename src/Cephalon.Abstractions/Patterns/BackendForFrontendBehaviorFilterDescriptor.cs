namespace Cephalon.Abstractions.Patterns;

/// <summary>
/// Describes behavior, capability, and tag-selection hints for one backend-for-frontend binding.
/// </summary>
public sealed class BackendForFrontendBehaviorFilterDescriptor
{
    /// <summary>
    /// Gets an empty backend-for-frontend behavior filter.
    /// </summary>
    public static BackendForFrontendBehaviorFilterDescriptor Empty { get; } = new();

    /// <summary>
    /// Creates a backend-for-frontend behavior filter descriptor.
    /// </summary>
    /// <param name="includedBehaviorIds">The explicit behavior identifiers that should stay visible to the client.</param>
    /// <param name="excludedBehaviorIds">The explicit behavior identifiers that should be hidden from the client.</param>
    /// <param name="includedCapabilityKeys">The explicit capability keys that should stay visible to the client.</param>
    /// <param name="excludedCapabilityKeys">The explicit capability keys that should be hidden from the client.</param>
    /// <param name="includedTags">The behavior or endpoint tags that should stay visible to the client.</param>
    /// <param name="excludedTags">The behavior or endpoint tags that should be hidden from the client.</param>
    public BackendForFrontendBehaviorFilterDescriptor(
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
    /// Gets a value indicating whether any behavior-filter hints were explicitly supplied.
    /// </summary>
    public bool HasValues =>
        IncludedBehaviorIds.Count > 0 ||
        ExcludedBehaviorIds.Count > 0 ||
        IncludedCapabilityKeys.Count > 0 ||
        ExcludedCapabilityKeys.Count > 0 ||
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
