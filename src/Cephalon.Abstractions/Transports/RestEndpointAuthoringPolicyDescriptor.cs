namespace Cephalon.Abstractions.Transports;

/// <summary>
/// Describes one behavior-level REST authoring policy together with its runtime effect.
/// </summary>
/// <remarks>
/// This descriptor keeps authoring-policy intent readable as a first-class runtime answer while
/// preserving separate buckets for authoring-policy suppression, later precedence suppression, and
/// later governance suppression.
/// </remarks>
public sealed class RestEndpointAuthoringPolicyDescriptor
{
    /// <summary>
    /// Creates a REST authoring-policy runtime descriptor.
    /// </summary>
    /// <param name="behaviorId">The stable behavior identifier that this policy applies to.</param>
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
    /// <param name="candidateIds">
    /// The ordered candidate identifiers visible for this behavior boundary before authoring-policy
    /// enforcement is considered.
    /// </param>
    /// <param name="retainedCandidateIds">
    /// The ordered candidate identifiers that survived authoring-policy enforcement, even if a
    /// later precedence or host-governance step suppressed them.
    /// </param>
    /// <param name="publishedCandidateIds">
    /// The ordered candidate identifiers that remain published after all runtime publication steps.
    /// </param>
    /// <param name="precedenceSuppressedCandidateIds">
    /// The ordered candidate identifiers that survived authoring policy but were later suppressed
    /// by candidate precedence.
    /// </param>
    /// <param name="governanceSuppressedCandidateIds">
    /// The ordered candidate identifiers that survived authoring policy but were later suppressed
    /// by host-level REST governance.
    /// </param>
    /// <param name="suppressedCandidateIds">
    /// The ordered candidate identifiers that were suppressed by behavior-level authoring-policy enforcement.
    /// </param>
    /// <param name="suppressionKinds">
    /// The grouped authoring-policy suppression kinds that appear in the runtime effect.
    /// </param>
    /// <param name="suppressionSummaries">
    /// The grouped authoring-policy suppression outcomes summarized by suppression kind.
    /// </param>
    public RestEndpointAuthoringPolicyDescriptor(
        string behaviorId,
        bool isConfigured = false,
        bool allowMultiplePublishedCandidates = false,
        string? preferredAuthoringStyle = null,
        IReadOnlyList<string>? allowedAuthoringStyles = null,
        IReadOnlyList<string>? disallowedAuthoringStyles = null,
        IReadOnlyList<string>? candidateIds = null,
        IReadOnlyList<string>? retainedCandidateIds = null,
        IReadOnlyList<string>? publishedCandidateIds = null,
        IReadOnlyList<string>? precedenceSuppressedCandidateIds = null,
        IReadOnlyList<string>? governanceSuppressedCandidateIds = null,
        IReadOnlyList<string>? suppressedCandidateIds = null,
        IReadOnlyList<RestEndpointAuthoringPolicySuppressionKind>? suppressionKinds = null,
        IReadOnlyList<RestEndpointAuthoringPolicySuppressionSummaryDescriptor>? suppressionSummaries = null)
    {
        var normalizedPolicy = new RestEndpointPublicationGroupAuthoringPolicyDescriptor(
            behaviorId,
            isConfigured,
            allowMultiplePublishedCandidates,
            preferredAuthoringStyle,
            allowedAuthoringStyles,
            disallowedAuthoringStyles);

        var normalizedCandidateIds = NormalizeOrderedList(candidateIds);
        var candidateIdSet = normalizedCandidateIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var normalizedSuppressionSummaries = suppressionSummaries is null
            ? []
            : RestEndpointAuthoringPolicySuppressionSummaryBuilder.Normalize(
                suppressionSummaries,
                candidateIdSet,
                nameof(suppressionSummaries));
        var normalizedSuppressedCandidateIds = NormalizeOrderedList(suppressedCandidateIds);
        if (normalizedSuppressedCandidateIds.Length == 0 &&
            normalizedSuppressionSummaries.Length > 0)
        {
            normalizedSuppressedCandidateIds =
                RestEndpointAuthoringPolicySuppressionSummaryBuilder.BuildSuppressedCandidateIds(
                    normalizedSuppressionSummaries);
        }

        var normalizedSuppressionKinds = NormalizeKinds(suppressionKinds);
        if (normalizedSuppressionKinds.Length == 0 &&
            normalizedSuppressionSummaries.Length > 0)
        {
            normalizedSuppressionKinds = RestEndpointAuthoringPolicySuppressionSummaryBuilder.BuildKinds(
                normalizedSuppressionSummaries);
        }

        var normalizedRetainedCandidateIds = retainedCandidateIds is null
            ? normalizedCandidateIds
                .Where(candidateId => !normalizedSuppressedCandidateIds.Contains(candidateId, StringComparer.OrdinalIgnoreCase))
                .ToArray()
            : NormalizeOrderedList(retainedCandidateIds);
        var normalizedPublishedCandidateIds = NormalizeOrderedList(publishedCandidateIds);
        var normalizedPrecedenceSuppressedCandidateIds = NormalizeOrderedList(precedenceSuppressedCandidateIds);
        var normalizedGovernanceSuppressedCandidateIds = NormalizeOrderedList(governanceSuppressedCandidateIds);
        var retainedCandidateIdSet = normalizedRetainedCandidateIds.ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (normalizedSuppressionSummaries.Length > 0)
        {
            var summarizedCandidateIds = RestEndpointAuthoringPolicySuppressionSummaryBuilder.BuildSuppressedCandidateIds(
                normalizedSuppressionSummaries);
            if (!normalizedSuppressedCandidateIds.ToHashSet(StringComparer.OrdinalIgnoreCase)
                    .SetEquals(summarizedCandidateIds))
            {
                throw new ArgumentException(
                    "Authoring-policy suppression summaries must describe the same candidate ids as the suppressed candidate bucket.",
                    nameof(suppressedCandidateIds));
            }

            var summarizedKinds = RestEndpointAuthoringPolicySuppressionSummaryBuilder.BuildKinds(
                normalizedSuppressionSummaries);
            if (normalizedSuppressionKinds.Length > 0 &&
                !normalizedSuppressionKinds.SequenceEqual(summarizedKinds))
            {
                throw new ArgumentException(
                    "Authoring-policy suppression summaries must describe the same suppression kinds as the flattened suppression-kind bucket.",
                    nameof(suppressionKinds));
            }
        }

        if (!candidateIdSet.IsSupersetOf(normalizedRetainedCandidateIds))
        {
            throw new ArgumentException(
                "Retained candidate ids must refer to candidates in the authoring-policy runtime answer.",
                nameof(retainedCandidateIds));
        }

        if (!candidateIdSet.IsSupersetOf(normalizedPublishedCandidateIds))
        {
            throw new ArgumentException(
                "Published candidate ids must refer to candidates in the authoring-policy runtime answer.",
                nameof(publishedCandidateIds));
        }

        if (!candidateIdSet.IsSupersetOf(normalizedPrecedenceSuppressedCandidateIds))
        {
            throw new ArgumentException(
                "Precedence-suppressed candidate ids must refer to candidates in the authoring-policy runtime answer.",
                nameof(precedenceSuppressedCandidateIds));
        }

        if (!candidateIdSet.IsSupersetOf(normalizedGovernanceSuppressedCandidateIds))
        {
            throw new ArgumentException(
                "Governance-suppressed candidate ids must refer to candidates in the authoring-policy runtime answer.",
                nameof(governanceSuppressedCandidateIds));
        }

        if (!candidateIdSet.IsSupersetOf(normalizedSuppressedCandidateIds))
        {
            throw new ArgumentException(
                "Suppressed candidate ids must refer to candidates in the authoring-policy runtime answer.",
                nameof(suppressedCandidateIds));
        }

        if (normalizedRetainedCandidateIds.Intersect(normalizedSuppressedCandidateIds, StringComparer.OrdinalIgnoreCase).Any())
        {
            throw new ArgumentException(
                "A candidate cannot be both retained and suppressed by authoring policy.",
                nameof(retainedCandidateIds));
        }

        if (!normalizedCandidateIds.ToHashSet(StringComparer.OrdinalIgnoreCase)
                .SetEquals(normalizedRetainedCandidateIds.Concat(normalizedSuppressedCandidateIds)))
        {
            throw new ArgumentException(
                "Candidate ids must equal the union of the retained and authoring-policy-suppressed candidate buckets.",
                nameof(candidateIds));
        }

        if (normalizedPublishedCandidateIds.Intersect(normalizedPrecedenceSuppressedCandidateIds, StringComparer.OrdinalIgnoreCase).Any() ||
            normalizedPublishedCandidateIds.Intersect(normalizedGovernanceSuppressedCandidateIds, StringComparer.OrdinalIgnoreCase).Any() ||
            normalizedPublishedCandidateIds.Intersect(normalizedSuppressedCandidateIds, StringComparer.OrdinalIgnoreCase).Any() ||
            normalizedPrecedenceSuppressedCandidateIds.Intersect(normalizedGovernanceSuppressedCandidateIds, StringComparer.OrdinalIgnoreCase).Any() ||
            normalizedPrecedenceSuppressedCandidateIds.Intersect(normalizedSuppressedCandidateIds, StringComparer.OrdinalIgnoreCase).Any() ||
            normalizedGovernanceSuppressedCandidateIds.Intersect(normalizedSuppressedCandidateIds, StringComparer.OrdinalIgnoreCase).Any())
        {
            throw new ArgumentException(
                "Published, precedence-suppressed, governance-suppressed, and authoring-policy-suppressed candidate buckets must stay disjoint.",
                nameof(publishedCandidateIds));
        }

        if (!retainedCandidateIdSet.IsSupersetOf(normalizedPublishedCandidateIds) ||
            !retainedCandidateIdSet.IsSupersetOf(normalizedPrecedenceSuppressedCandidateIds) ||
            !retainedCandidateIdSet.IsSupersetOf(normalizedGovernanceSuppressedCandidateIds))
        {
            throw new ArgumentException(
                "Final published, precedence-suppressed, and governance-suppressed candidate buckets must be a subset of the retained candidate bucket.",
                nameof(retainedCandidateIds));
        }

        BehaviorId = normalizedPolicy.BehaviorId;
        IsConfigured = normalizedPolicy.IsConfigured;
        AllowMultiplePublishedCandidates = normalizedPolicy.AllowMultiplePublishedCandidates;
        PreferredAuthoringStyle = normalizedPolicy.PreferredAuthoringStyle;
        AllowedAuthoringStyles = normalizedPolicy.AllowedAuthoringStyles;
        DisallowedAuthoringStyles = normalizedPolicy.DisallowedAuthoringStyles;
        CandidateIds = normalizedCandidateIds;
        RetainedCandidateIds = normalizedRetainedCandidateIds;
        PublishedCandidateIds = normalizedPublishedCandidateIds;
        PrecedenceSuppressedCandidateIds = normalizedPrecedenceSuppressedCandidateIds;
        GovernanceSuppressedCandidateIds = normalizedGovernanceSuppressedCandidateIds;
        SuppressedCandidateIds = normalizedSuppressedCandidateIds;
        SuppressionKinds = normalizedSuppressionKinds;
        SuppressionSummaries = normalizedSuppressionSummaries;
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

    /// <summary>
    /// Gets the ordered candidate identifiers visible for this behavior boundary.
    /// </summary>
    public IReadOnlyList<string> CandidateIds { get; }

    /// <summary>
    /// Gets the ordered candidate identifiers that survived authoring-policy enforcement.
    /// </summary>
    public IReadOnlyList<string> RetainedCandidateIds { get; }

    /// <summary>
    /// Gets the ordered candidate identifiers that remain published after all runtime publication steps.
    /// </summary>
    public IReadOnlyList<string> PublishedCandidateIds { get; }

    /// <summary>
    /// Gets the ordered candidate identifiers that survived authoring policy but were later suppressed by precedence.
    /// </summary>
    public IReadOnlyList<string> PrecedenceSuppressedCandidateIds { get; }

    /// <summary>
    /// Gets the ordered candidate identifiers that survived authoring policy but were later suppressed by host governance.
    /// </summary>
    public IReadOnlyList<string> GovernanceSuppressedCandidateIds { get; }

    /// <summary>
    /// Gets the ordered candidate identifiers that were suppressed by behavior-level authoring-policy enforcement.
    /// </summary>
    public IReadOnlyList<string> SuppressedCandidateIds { get; }

    /// <summary>
    /// Gets the grouped authoring-policy suppression kinds visible in the runtime effect.
    /// </summary>
    public IReadOnlyList<RestEndpointAuthoringPolicySuppressionKind> SuppressionKinds { get; }

    /// <summary>
    /// Gets the grouped authoring-policy suppression outcomes summarized by suppression kind.
    /// </summary>
    public IReadOnlyList<RestEndpointAuthoringPolicySuppressionSummaryDescriptor> SuppressionSummaries { get; }

    private static string[] NormalizeOrderedList(IReadOnlyList<string>? values)
    {
        if (values is null || values.Count == 0)
        {
            return [];
        }

        var normalized = new List<string>(values.Count);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            var trimmed = value.Trim();
            if (seen.Add(trimmed))
            {
                normalized.Add(trimmed);
            }
        }

        return normalized.ToArray();
    }

    private static RestEndpointAuthoringPolicySuppressionKind[] NormalizeKinds(
        IReadOnlyList<RestEndpointAuthoringPolicySuppressionKind>? values)
    {
        if (values is null || values.Count == 0)
        {
            return [];
        }

        var normalized = values
            .Distinct()
            .OrderBy(static value => value.GetWireName(), StringComparer.Ordinal)
            .ToArray();
        if (normalized.Any(static value =>
                !Enum.IsDefined(value) ||
                value == RestEndpointAuthoringPolicySuppressionKind.Unspecified))
        {
            throw new ArgumentException(
                "A supported authoring-policy suppression kind is required when one is declared.",
                nameof(values));
        }

        return normalized;
    }
}
