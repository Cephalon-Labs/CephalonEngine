namespace Cephalon.Abstractions.Transports;

/// <summary>
/// Describes one authoring-style partition inside a rule-centric REST authoring-policy runtime answer.
/// </summary>
/// <remarks>
/// This descriptor keeps one behavior-level authoring-policy answer readable without reopening the
/// broader publication-group surface when operators need to understand how authoring-policy,
/// precedence, and later governance outcomes distribute across authoring styles.
/// </remarks>
public sealed class RestEndpointAuthoringPolicyAuthoringStyleDescriptor
{
    /// <summary>
    /// Creates an authoring-style partition for a rule-centric REST authoring-policy runtime answer.
    /// </summary>
    /// <param name="authoringStyle">The normalized authoring style summarized by this entry.</param>
    /// <param name="candidateIds">
    /// The ordered candidate identifiers contributed by this authoring style before authoring-policy
    /// enforcement is considered.
    /// </param>
    /// <param name="retainedCandidateIds">
    /// The ordered candidate identifiers that survived authoring-policy enforcement for this
    /// authoring style, even if later precedence or host governance suppressed them.
    /// </param>
    /// <param name="publishedCandidateIds">
    /// The ordered candidate identifiers that remain published after all runtime publication steps
    /// for this authoring style.
    /// </param>
    /// <param name="precedenceSuppressedCandidateIds">
    /// The ordered candidate identifiers that survived authoring policy but were later suppressed
    /// by candidate precedence for this authoring style.
    /// </param>
    /// <param name="governanceSuppressedCandidateIds">
    /// The ordered candidate identifiers that survived authoring policy but were later suppressed
    /// by host-level REST governance for this authoring style.
    /// </param>
    /// <param name="suppressedCandidateIds">
    /// The ordered candidate identifiers that were suppressed by behavior-level authoring-policy
    /// enforcement for this authoring style.
    /// </param>
    /// <param name="suppressionKinds">
    /// The grouped authoring-policy suppression kinds that appear in the runtime effect for this
    /// authoring style.
    /// </param>
    /// <param name="suppressionSummaries">
    /// The grouped authoring-policy suppression outcomes summarized by suppression kind for this
    /// authoring style.
    /// </param>
    /// <param name="hostGovernanceEligibleCandidateIds">
    /// The ordered candidate identifiers whose original projections allowed host governance to
    /// participate for this authoring style.
    /// </param>
    /// <param name="hostGovernanceIneligibleCandidateIds">
    /// The ordered candidate identifiers whose original projections kept host governance out of
    /// scope for this authoring style.
    /// </param>
    /// <param name="skippedSuppressionIds">
    /// The ordered suppression-rule identifiers that targeted host-governance-ineligible
    /// candidates for this authoring style.
    /// </param>
    /// <param name="skippedOverrideIds">
    /// The ordered override-rule identifiers that targeted host-governance-ineligible candidates
    /// for this authoring style.
    /// </param>
    public RestEndpointAuthoringPolicyAuthoringStyleDescriptor(
        string authoringStyle,
        IReadOnlyList<string>? candidateIds = null,
        IReadOnlyList<string>? retainedCandidateIds = null,
        IReadOnlyList<string>? publishedCandidateIds = null,
        IReadOnlyList<string>? precedenceSuppressedCandidateIds = null,
        IReadOnlyList<string>? governanceSuppressedCandidateIds = null,
        IReadOnlyList<string>? suppressedCandidateIds = null,
        IReadOnlyList<RestEndpointAuthoringPolicySuppressionKind>? suppressionKinds = null,
        IReadOnlyList<RestEndpointAuthoringPolicySuppressionSummaryDescriptor>? suppressionSummaries = null,
        IReadOnlyList<string>? hostGovernanceEligibleCandidateIds = null,
        IReadOnlyList<string>? hostGovernanceIneligibleCandidateIds = null,
        IReadOnlyList<string>? skippedSuppressionIds = null,
        IReadOnlyList<string>? skippedOverrideIds = null)
    {
        if (string.IsNullOrWhiteSpace(authoringStyle))
        {
            throw new ArgumentException("A non-empty authoring style is required.", nameof(authoringStyle));
        }

        var normalizedCandidateIds = NormalizeOrderedList(candidateIds);
        if (normalizedCandidateIds.Length == 0)
        {
            throw new ArgumentException(
                "At least one candidate id is required for an authoring-style authoring-policy answer.",
                nameof(candidateIds));
        }

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
        var normalizedHostGovernanceEligibleCandidateIds = NormalizeOrderedList(hostGovernanceEligibleCandidateIds);
        var normalizedHostGovernanceIneligibleCandidateIds = NormalizeOrderedList(hostGovernanceIneligibleCandidateIds);
        var normalizedSkippedSuppressionIds = NormalizeOrderedList(skippedSuppressionIds);
        var normalizedSkippedOverrideIds = NormalizeOrderedList(skippedOverrideIds);
        var retainedCandidateIdSet = normalizedRetainedCandidateIds.ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (normalizedSuppressionSummaries.Length > 0)
        {
            var summarizedCandidateIds = RestEndpointAuthoringPolicySuppressionSummaryBuilder.BuildSuppressedCandidateIds(
                normalizedSuppressionSummaries);
            if (!normalizedSuppressedCandidateIds.ToHashSet(StringComparer.OrdinalIgnoreCase)
                    .SetEquals(summarizedCandidateIds))
            {
                throw new ArgumentException(
                    "Authoring-policy suppression summaries must describe the same candidate ids as the grouped authoring-style suppressed candidate bucket.",
                    nameof(suppressedCandidateIds));
            }

            var summarizedKinds = RestEndpointAuthoringPolicySuppressionSummaryBuilder.BuildKinds(
                normalizedSuppressionSummaries);
            if (normalizedSuppressionKinds.Length > 0 &&
                !normalizedSuppressionKinds.SequenceEqual(summarizedKinds))
            {
                throw new ArgumentException(
                    "Authoring-policy suppression summaries must describe the same suppression kinds as the grouped authoring-style suppression-kind bucket.",
                    nameof(suppressionKinds));
            }
        }

        if (!candidateIdSet.IsSupersetOf(normalizedRetainedCandidateIds))
        {
            throw new ArgumentException(
                "Retained candidate ids must refer to candidates in the grouped authoring-style answer.",
                nameof(retainedCandidateIds));
        }

        if (!candidateIdSet.IsSupersetOf(normalizedPublishedCandidateIds))
        {
            throw new ArgumentException(
                "Published candidate ids must refer to candidates in the grouped authoring-style answer.",
                nameof(publishedCandidateIds));
        }

        if (!candidateIdSet.IsSupersetOf(normalizedPrecedenceSuppressedCandidateIds))
        {
            throw new ArgumentException(
                "Precedence-suppressed candidate ids must refer to candidates in the grouped authoring-style answer.",
                nameof(precedenceSuppressedCandidateIds));
        }

        if (!candidateIdSet.IsSupersetOf(normalizedGovernanceSuppressedCandidateIds))
        {
            throw new ArgumentException(
                "Governance-suppressed candidate ids must refer to candidates in the grouped authoring-style answer.",
                nameof(governanceSuppressedCandidateIds));
        }

        if (!candidateIdSet.IsSupersetOf(normalizedSuppressedCandidateIds))
        {
            throw new ArgumentException(
                "Suppressed candidate ids must refer to candidates in the grouped authoring-style answer.",
                nameof(suppressedCandidateIds));
        }

        if (!candidateIdSet.IsSupersetOf(normalizedHostGovernanceEligibleCandidateIds))
        {
            throw new ArgumentException(
                "Host-governance-eligible candidate ids must refer to candidates in the grouped authoring-style answer.",
                nameof(hostGovernanceEligibleCandidateIds));
        }

        if (!candidateIdSet.IsSupersetOf(normalizedHostGovernanceIneligibleCandidateIds))
        {
            throw new ArgumentException(
                "Host-governance-ineligible candidate ids must refer to candidates in the grouped authoring-style answer.",
                nameof(hostGovernanceIneligibleCandidateIds));
        }

        if (normalizedRetainedCandidateIds.Intersect(normalizedSuppressedCandidateIds, StringComparer.OrdinalIgnoreCase).Any())
        {
            throw new ArgumentException(
                "A candidate cannot be both retained and suppressed by authoring policy inside one authoring-style answer.",
                nameof(retainedCandidateIds));
        }

        if (!candidateIdSet.SetEquals(normalizedRetainedCandidateIds.Concat(normalizedSuppressedCandidateIds)))
        {
            throw new ArgumentException(
                "Candidate ids must equal the union of the retained and authoring-policy-suppressed candidate buckets for one authoring-style answer.",
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
                "Published, precedence-suppressed, governance-suppressed, and authoring-policy-suppressed candidate buckets must stay disjoint inside one authoring-style answer.",
                nameof(publishedCandidateIds));
        }

        if (!retainedCandidateIdSet.IsSupersetOf(normalizedPublishedCandidateIds) ||
            !retainedCandidateIdSet.IsSupersetOf(normalizedPrecedenceSuppressedCandidateIds) ||
            !retainedCandidateIdSet.IsSupersetOf(normalizedGovernanceSuppressedCandidateIds))
        {
            throw new ArgumentException(
                "Final published, precedence-suppressed, and governance-suppressed candidate buckets must be a subset of the retained candidate bucket inside one authoring-style answer.",
                nameof(retainedCandidateIds));
        }

        if (normalizedHostGovernanceEligibleCandidateIds.Intersect(normalizedHostGovernanceIneligibleCandidateIds, StringComparer.OrdinalIgnoreCase).Any())
        {
            throw new ArgumentException(
                "A candidate cannot be both host-governance-eligible and host-governance-ineligible inside one authoring-style answer.",
                nameof(hostGovernanceEligibleCandidateIds));
        }

        AuthoringStyle = authoringStyle.Trim();
        CandidateIds = normalizedCandidateIds;
        RetainedCandidateIds = normalizedRetainedCandidateIds;
        PublishedCandidateIds = normalizedPublishedCandidateIds;
        PrecedenceSuppressedCandidateIds = normalizedPrecedenceSuppressedCandidateIds;
        GovernanceSuppressedCandidateIds = normalizedGovernanceSuppressedCandidateIds;
        SuppressedCandidateIds = normalizedSuppressedCandidateIds;
        SuppressionKinds = normalizedSuppressionKinds;
        SuppressionSummaries = normalizedSuppressionSummaries;
        HostGovernanceEligibleCandidateIds = normalizedHostGovernanceEligibleCandidateIds;
        HostGovernanceIneligibleCandidateIds = normalizedHostGovernanceIneligibleCandidateIds;
        SkippedSuppressionIds = normalizedSkippedSuppressionIds;
        SkippedOverrideIds = normalizedSkippedOverrideIds;
    }

    /// <summary>
    /// Gets the normalized authoring style summarized by this entry.
    /// </summary>
    public string AuthoringStyle { get; }

    /// <summary>
    /// Gets the ordered candidate identifiers contributed by this authoring style.
    /// </summary>
    public IReadOnlyList<string> CandidateIds { get; }

    /// <summary>
    /// Gets the ordered candidate identifiers that survived authoring-policy enforcement for this authoring style.
    /// </summary>
    public IReadOnlyList<string> RetainedCandidateIds { get; }

    /// <summary>
    /// Gets the ordered candidate identifiers that remain published after all runtime publication steps for this authoring style.
    /// </summary>
    public IReadOnlyList<string> PublishedCandidateIds { get; }

    /// <summary>
    /// Gets the ordered candidate identifiers that survived authoring policy but were later suppressed by precedence for this authoring style.
    /// </summary>
    public IReadOnlyList<string> PrecedenceSuppressedCandidateIds { get; }

    /// <summary>
    /// Gets the ordered candidate identifiers that survived authoring policy but were later suppressed by host governance for this authoring style.
    /// </summary>
    public IReadOnlyList<string> GovernanceSuppressedCandidateIds { get; }

    /// <summary>
    /// Gets the ordered candidate identifiers that were suppressed by behavior-level authoring-policy enforcement for this authoring style.
    /// </summary>
    public IReadOnlyList<string> SuppressedCandidateIds { get; }

    /// <summary>
    /// Gets the grouped authoring-policy suppression kinds visible in the runtime effect for this authoring style.
    /// </summary>
    public IReadOnlyList<RestEndpointAuthoringPolicySuppressionKind> SuppressionKinds { get; }

    /// <summary>
    /// Gets the grouped authoring-policy suppression outcomes summarized by suppression kind for this authoring style.
    /// </summary>
    public IReadOnlyList<RestEndpointAuthoringPolicySuppressionSummaryDescriptor> SuppressionSummaries { get; }

    /// <summary>
    /// Gets the ordered candidate identifiers whose original projections allowed host governance to participate for this authoring style.
    /// </summary>
    public IReadOnlyList<string> HostGovernanceEligibleCandidateIds { get; }

    /// <summary>
    /// Gets the ordered candidate identifiers whose original projections kept host governance out of scope for this authoring style.
    /// </summary>
    public IReadOnlyList<string> HostGovernanceIneligibleCandidateIds { get; }

    /// <summary>
    /// Gets the ordered suppression-rule identifiers that targeted host-governance-ineligible candidates for this authoring style.
    /// </summary>
    public IReadOnlyList<string> SkippedSuppressionIds { get; }

    /// <summary>
    /// Gets the ordered override-rule identifiers that targeted host-governance-ineligible candidates for this authoring style.
    /// </summary>
    public IReadOnlyList<string> SkippedOverrideIds { get; }

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

internal static class RestEndpointAuthoringPolicyAuthoringStyleDescriptorBuilder
{
    private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;

    internal static RestEndpointAuthoringPolicyAuthoringStyleDescriptor[] BuildFromCandidates(
        IReadOnlyList<RestEndpointCandidateRuntimeDescriptor> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        return candidates
            .GroupBy(static candidate => candidate.AuthoringStyle, Comparer)
            .OrderBy(static group => group.Key, Comparer)
            .Select(static group =>
            {
                if (string.IsNullOrWhiteSpace(group.Key))
                {
                    throw new InvalidOperationException(
                        "REST endpoint authoring-policy style summaries require candidates to expose an authoring style.");
                }

                var orderedCandidates = group
                    .OrderBy(static candidate => candidate.PrecedenceRank)
                    .ThenBy(static candidate => candidate.Status == RestEndpointCandidateStatus.Published ? 0 : 1)
                    .ThenBy(static candidate => candidate.ProjectedEndpoint.RoutePattern, Comparer)
                    .ThenBy(static candidate => candidate.ProjectedEndpoint.Method, Comparer)
                    .ThenBy(static candidate => candidate.Id, Comparer)
                    .ToArray();
                var suppressionSummaries = RestEndpointAuthoringPolicySuppressionSummaryBuilder.BuildFromCandidates(
                    orderedCandidates);
                var suppressionKinds = suppressionSummaries.Length == 0
                    ? []
                    : RestEndpointAuthoringPolicySuppressionSummaryBuilder.BuildKinds(suppressionSummaries);

                return new RestEndpointAuthoringPolicyAuthoringStyleDescriptor(
                    group.Key,
                    candidateIds: orderedCandidates
                        .Select(static candidate => candidate.Id)
                        .ToArray(),
                    retainedCandidateIds: orderedCandidates
                        .Where(static candidate => candidate.SuppressedByAuthoringPolicyKind is null)
                        .Select(static candidate => candidate.Id)
                        .ToArray(),
                    publishedCandidateIds: orderedCandidates
                        .Where(static candidate => candidate.Status == RestEndpointCandidateStatus.Published)
                        .Select(static candidate => candidate.Id)
                        .ToArray(),
                    precedenceSuppressedCandidateIds: orderedCandidates
                        .Where(static candidate =>
                            candidate.Status == RestEndpointCandidateStatus.Suppressed &&
                            !string.IsNullOrWhiteSpace(candidate.SuppressedByCandidateId))
                        .Select(static candidate => candidate.Id)
                        .ToArray(),
                    governanceSuppressedCandidateIds: orderedCandidates
                        .Where(static candidate =>
                            candidate.Status == RestEndpointCandidateStatus.Suppressed &&
                            !string.IsNullOrWhiteSpace(candidate.SuppressedBySuppressionId))
                        .Select(static candidate => candidate.Id)
                        .ToArray(),
                    suppressedCandidateIds: orderedCandidates
                        .Where(static candidate =>
                            candidate.Status == RestEndpointCandidateStatus.Suppressed &&
                            candidate.SuppressedByAuthoringPolicyKind.HasValue)
                        .Select(static candidate => candidate.Id)
                        .ToArray(),
                    suppressionKinds: suppressionKinds,
                    suppressionSummaries: suppressionSummaries);
            })
            .ToArray();
    }
}
