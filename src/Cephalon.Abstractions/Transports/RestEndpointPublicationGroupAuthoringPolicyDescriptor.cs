namespace Cephalon.Abstractions.Transports;

/// <summary>
/// Describes the effective authoring-policy intent for one behavior-level REST publication group.
/// </summary>
/// <remarks>
/// This descriptor captures authoring-policy intent, not the already-resolved publication outcome.
/// The grouped publication answer remains authoritative for which candidates actually published or
/// were suppressed at runtime.
/// </remarks>
public sealed class RestEndpointPublicationGroupAuthoringPolicyDescriptor
{
    /// <summary>
    /// Creates a behavior-level REST publication-group authoring policy descriptor.
    /// </summary>
    /// <param name="behaviorId">The stable behavior identifier that this authoring policy applies to.</param>
    /// <param name="isConfigured">
    /// <see langword="true" /> when the policy was supplied through host configuration;
    /// otherwise <see langword="false" /> when the runtime is exposing the implicit default policy.
    /// </param>
    /// <param name="allowMultiplePublishedCandidates">
    /// <see langword="true" /> when the policy explicitly allows more than one projection candidate
    /// to remain published for the same behavior boundary after authoring-policy enforcement.
    /// </param>
    /// <param name="preferredAuthoringStyle">
    /// The normalized preferred authoring style when the policy declares one.
    /// </param>
    /// <param name="allowedAuthoringStyles">
    /// The normalized authoring styles that the policy explicitly allows for this behavior
    /// boundary when one or more are declared.
    /// </param>
    /// <param name="disallowedAuthoringStyles">
    /// The normalized authoring styles that the policy explicitly disallows for this behavior
    /// boundary when one or more are declared.
    /// </param>
    public RestEndpointPublicationGroupAuthoringPolicyDescriptor(
        string behaviorId,
        bool isConfigured = false,
        bool allowMultiplePublishedCandidates = false,
        string? preferredAuthoringStyle = null,
        IReadOnlyList<string>? allowedAuthoringStyles = null,
        IReadOnlyList<string>? disallowedAuthoringStyles = null)
    {
        if (string.IsNullOrWhiteSpace(behaviorId))
        {
            throw new ArgumentException("A non-empty behavior id is required.", nameof(behaviorId));
        }

        var normalizedPreferredAuthoringStyle = NormalizeOptional(preferredAuthoringStyle);
        var normalizedAllowedAuthoringStyles = NormalizeList(allowedAuthoringStyles);
        var normalizedDisallowedAuthoringStyles = NormalizeList(disallowedAuthoringStyles);

        if (normalizedAllowedAuthoringStyles.Intersect(
                normalizedDisallowedAuthoringStyles,
                StringComparer.OrdinalIgnoreCase).Any())
        {
            throw new ArgumentException(
                "An authoring policy cannot both allow and disallow the same authoring style.",
                nameof(allowedAuthoringStyles));
        }

        if (normalizedPreferredAuthoringStyle is not null &&
            normalizedDisallowedAuthoringStyles.Contains(normalizedPreferredAuthoringStyle, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "A preferred authoring style cannot also be disallowed.",
                nameof(preferredAuthoringStyle));
        }

        if (normalizedPreferredAuthoringStyle is not null &&
            normalizedAllowedAuthoringStyles.Length > 0 &&
            !normalizedAllowedAuthoringStyles.Contains(normalizedPreferredAuthoringStyle, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "A preferred authoring style must appear in the allowed authoring-style set when that set is declared.",
                nameof(preferredAuthoringStyle));
        }

        BehaviorId = behaviorId.Trim();
        IsConfigured = isConfigured;
        AllowMultiplePublishedCandidates = allowMultiplePublishedCandidates;
        PreferredAuthoringStyle = normalizedPreferredAuthoringStyle;
        AllowedAuthoringStyles = normalizedAllowedAuthoringStyles;
        DisallowedAuthoringStyles = normalizedDisallowedAuthoringStyles;
    }

    /// <summary>
    /// Gets the stable behavior identifier that this authoring policy applies to.
    /// </summary>
    public string BehaviorId { get; }

    /// <summary>
    /// Gets a value indicating whether this authoring policy came from explicit host configuration.
    /// </summary>
    public bool IsConfigured { get; }

    /// <summary>
    /// Gets a value indicating whether the policy explicitly allows multiple published candidates
    /// for the same behavior boundary after authoring-policy enforcement.
    /// </summary>
    public bool AllowMultiplePublishedCandidates { get; }

    /// <summary>
    /// Gets the normalized preferred authoring style when the policy declares one.
    /// </summary>
    public string? PreferredAuthoringStyle { get; }

    /// <summary>
    /// Gets the normalized authoring styles that the policy explicitly allows.
    /// </summary>
    public IReadOnlyList<string> AllowedAuthoringStyles { get; }

    /// <summary>
    /// Gets the normalized authoring styles that the policy explicitly disallows.
    /// </summary>
    public IReadOnlyList<string> DisallowedAuthoringStyles { get; }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static string[] NormalizeList(IReadOnlyList<string>? values)
    {
        return values?
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }
}
