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
    /// <param name="hostGovernanceEligibleCandidateIds">
    /// The ordered candidate identifiers whose original projections allowed host governance to
    /// participate for this behavior boundary.
    /// </param>
    /// <param name="hostGovernanceIneligibleCandidateIds">
    /// The ordered candidate identifiers whose original projections kept host governance out of
    /// scope for this behavior boundary.
    /// </param>
    /// <param name="skippedSuppressionIds">
    /// The ordered suppression-rule identifiers that targeted host-governance-ineligible
    /// candidates in this behavior boundary.
    /// </param>
    /// <param name="skippedOverrideIds">
    /// The ordered override-rule identifiers that targeted host-governance-ineligible candidates
    /// in this behavior boundary.
    /// </param>
    /// <param name="authoringStyleSummaries">
    /// The per-authoring-style runtime buckets that explain how this policy's candidate,
    /// retained, published, precedence-suppressed, governance-suppressed, and
    /// authoring-policy-suppressed outcomes distribute across authoring styles.
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
        IReadOnlyList<RestEndpointAuthoringPolicySuppressionSummaryDescriptor>? suppressionSummaries = null,
        IReadOnlyList<string>? hostGovernanceEligibleCandidateIds = null,
        IReadOnlyList<string>? hostGovernanceIneligibleCandidateIds = null,
        IReadOnlyList<string>? skippedSuppressionIds = null,
        IReadOnlyList<string>? skippedOverrideIds = null,
        IReadOnlyList<RestEndpointAuthoringPolicyAuthoringStyleDescriptor>? authoringStyleSummaries = null)
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
        var normalizedHostGovernanceEligibleCandidateIds = NormalizeOrderedList(hostGovernanceEligibleCandidateIds);
        var normalizedHostGovernanceIneligibleCandidateIds = NormalizeOrderedList(hostGovernanceIneligibleCandidateIds);
        var normalizedSkippedSuppressionIds = NormalizeOrderedList(skippedSuppressionIds);
        var normalizedSkippedOverrideIds = NormalizeOrderedList(skippedOverrideIds);
        var retainedCandidateIdSet = normalizedRetainedCandidateIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var normalizedAuthoringStyleSummaries = NormalizeAuthoringStyleSummaries(
            authoringStyleSummaries,
            nameof(authoringStyleSummaries));

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

        if (!candidateIdSet.IsSupersetOf(normalizedHostGovernanceEligibleCandidateIds))
        {
            throw new ArgumentException(
                "Host-governance-eligible candidate ids must refer to candidates in the authoring-policy runtime answer.",
                nameof(hostGovernanceEligibleCandidateIds));
        }

        if (!candidateIdSet.IsSupersetOf(normalizedHostGovernanceIneligibleCandidateIds))
        {
            throw new ArgumentException(
                "Host-governance-ineligible candidate ids must refer to candidates in the authoring-policy runtime answer.",
                nameof(hostGovernanceIneligibleCandidateIds));
        }

        if (normalizedRetainedCandidateIds.Intersect(normalizedSuppressedCandidateIds, StringComparer.OrdinalIgnoreCase).Any())
        {
            throw new ArgumentException(
                "A candidate cannot be both retained and suppressed by authoring policy.",
                nameof(retainedCandidateIds));
        }

        if (!candidateIdSet.SetEquals(normalizedRetainedCandidateIds.Concat(normalizedSuppressedCandidateIds)))
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

        if (normalizedHostGovernanceEligibleCandidateIds.Intersect(normalizedHostGovernanceIneligibleCandidateIds, StringComparer.OrdinalIgnoreCase).Any())
        {
            throw new ArgumentException(
                "A candidate cannot be both host-governance-eligible and host-governance-ineligible in the authoring-policy runtime answer.",
                nameof(hostGovernanceEligibleCandidateIds));
        }

        if (normalizedAuthoringStyleSummaries.Length > 0)
        {
            if (!candidateIdSet.SetEquals(
                    normalizedAuthoringStyleSummaries.SelectMany(static summary => summary.CandidateIds)))
            {
                throw new ArgumentException(
                    "Authoring-style summaries must describe the same candidate ids as the authoring-policy runtime answer.",
                    nameof(authoringStyleSummaries));
            }

            if (!retainedCandidateIdSet.SetEquals(
                    normalizedAuthoringStyleSummaries.SelectMany(static summary => summary.RetainedCandidateIds)))
            {
                throw new ArgumentException(
                    "Authoring-style summaries must describe the same retained candidate ids as the authoring-policy runtime answer.",
                    nameof(authoringStyleSummaries));
            }

            if (!normalizedPublishedCandidateIds.ToHashSet(StringComparer.OrdinalIgnoreCase).SetEquals(
                    normalizedAuthoringStyleSummaries.SelectMany(static summary => summary.PublishedCandidateIds)))
            {
                throw new ArgumentException(
                    "Authoring-style summaries must describe the same published candidate ids as the authoring-policy runtime answer.",
                    nameof(authoringStyleSummaries));
            }

            if (!normalizedPrecedenceSuppressedCandidateIds.ToHashSet(StringComparer.OrdinalIgnoreCase).SetEquals(
                    normalizedAuthoringStyleSummaries.SelectMany(static summary => summary.PrecedenceSuppressedCandidateIds)))
            {
                throw new ArgumentException(
                    "Authoring-style summaries must describe the same precedence-suppressed candidate ids as the authoring-policy runtime answer.",
                    nameof(authoringStyleSummaries));
            }

            if (!normalizedGovernanceSuppressedCandidateIds.ToHashSet(StringComparer.OrdinalIgnoreCase).SetEquals(
                    normalizedAuthoringStyleSummaries.SelectMany(static summary => summary.GovernanceSuppressedCandidateIds)))
            {
                throw new ArgumentException(
                    "Authoring-style summaries must describe the same governance-suppressed candidate ids as the authoring-policy runtime answer.",
                    nameof(authoringStyleSummaries));
            }

            if (!normalizedSuppressedCandidateIds.ToHashSet(StringComparer.OrdinalIgnoreCase).SetEquals(
                    normalizedAuthoringStyleSummaries.SelectMany(static summary => summary.SuppressedCandidateIds)))
            {
                throw new ArgumentException(
                    "Authoring-style summaries must describe the same authoring-policy-suppressed candidate ids as the authoring-policy runtime answer.",
                    nameof(authoringStyleSummaries));
            }

            if (!normalizedHostGovernanceEligibleCandidateIds.ToHashSet(StringComparer.OrdinalIgnoreCase).SetEquals(
                    normalizedAuthoringStyleSummaries.SelectMany(static summary => summary.HostGovernanceEligibleCandidateIds)))
            {
                throw new ArgumentException(
                    "Authoring-style summaries must describe the same host-governance-eligible candidate ids as the authoring-policy runtime answer.",
                    nameof(authoringStyleSummaries));
            }

            if (!normalizedHostGovernanceIneligibleCandidateIds.ToHashSet(StringComparer.OrdinalIgnoreCase).SetEquals(
                    normalizedAuthoringStyleSummaries.SelectMany(static summary => summary.HostGovernanceIneligibleCandidateIds)))
            {
                throw new ArgumentException(
                    "Authoring-style summaries must describe the same host-governance-ineligible candidate ids as the authoring-policy runtime answer.",
                    nameof(authoringStyleSummaries));
            }

            if (!normalizedSkippedSuppressionIds.SequenceEqual(
                    BuildOrderedSkippedRuleIds(normalizedAuthoringStyleSummaries, static summary => summary.SkippedSuppressionIds),
                    StringComparer.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    "Authoring-style summaries must describe the same skipped suppression rule ids as the authoring-policy runtime answer.",
                    nameof(authoringStyleSummaries));
            }

            if (!normalizedSkippedOverrideIds.SequenceEqual(
                    BuildOrderedSkippedRuleIds(normalizedAuthoringStyleSummaries, static summary => summary.SkippedOverrideIds),
                    StringComparer.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    "Authoring-style summaries must describe the same skipped override rule ids as the authoring-policy runtime answer.",
                    nameof(authoringStyleSummaries));
            }

            if (normalizedSuppressionKinds.Length > 0)
            {
                var flattenedSuppressionKinds = normalizedAuthoringStyleSummaries
                    .SelectMany(static summary => summary.SuppressionKinds)
                    .Distinct()
                    .OrderBy(static kind => kind.GetWireName(), StringComparer.Ordinal)
                    .ToArray();
                if (!normalizedSuppressionKinds.SequenceEqual(flattenedSuppressionKinds))
                {
                    throw new ArgumentException(
                        "Authoring-style summaries must describe the same suppression kinds as the authoring-policy runtime answer.",
                        nameof(authoringStyleSummaries));
                }
            }

            if (normalizedSuppressionSummaries.Length > 0)
            {
                var mergedSuppressionSummaries = MergeAuthoringStyleSuppressionSummaries(
                    normalizedAuthoringStyleSummaries);
                if (!SuppressionSummariesMatch(
                        normalizedSuppressionSummaries,
                        mergedSuppressionSummaries))
                {
                    throw new ArgumentException(
                        "Authoring-style summaries must describe the same grouped suppression outcomes as the authoring-policy runtime answer.",
                        nameof(authoringStyleSummaries));
                }
            }
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
        HostGovernanceEligibleCandidateIds = normalizedHostGovernanceEligibleCandidateIds;
        HostGovernanceIneligibleCandidateIds = normalizedHostGovernanceIneligibleCandidateIds;
        SkippedSuppressionIds = normalizedSkippedSuppressionIds;
        SkippedOverrideIds = normalizedSkippedOverrideIds;
        AuthoringStyleSummaries = normalizedAuthoringStyleSummaries;
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

    /// <summary>
    /// Gets the ordered candidate identifiers whose original projections allowed host governance to participate.
    /// </summary>
    public IReadOnlyList<string> HostGovernanceEligibleCandidateIds { get; }

    /// <summary>
    /// Gets the ordered candidate identifiers whose original projections kept host governance out of scope.
    /// </summary>
    public IReadOnlyList<string> HostGovernanceIneligibleCandidateIds { get; }

    /// <summary>
    /// Gets the ordered suppression-rule identifiers that targeted host-governance-ineligible candidates in this behavior boundary.
    /// </summary>
    public IReadOnlyList<string> SkippedSuppressionIds { get; }

    /// <summary>
    /// Gets the ordered override-rule identifiers that targeted host-governance-ineligible candidates in this behavior boundary.
    /// </summary>
    public IReadOnlyList<string> SkippedOverrideIds { get; }

    /// <summary>
    /// Gets the per-authoring-style runtime buckets that explain how this policy's outcomes
    /// distribute across authoring styles.
    /// </summary>
    public IReadOnlyList<RestEndpointAuthoringPolicyAuthoringStyleDescriptor> AuthoringStyleSummaries { get; }

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

    private static RestEndpointAuthoringPolicyAuthoringStyleDescriptor[] NormalizeAuthoringStyleSummaries(
        IReadOnlyList<RestEndpointAuthoringPolicyAuthoringStyleDescriptor>? values,
        string parameterName)
    {
        if (values is null || values.Count == 0)
        {
            return [];
        }

        var result = new List<RestEndpointAuthoringPolicyAuthoringStyleDescriptor>(values.Count);
        var seenAuthoringStyles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var value in values
                     .Where(static value => value is not null)
                     .OrderBy(static value => value.AuthoringStyle, StringComparer.OrdinalIgnoreCase))
        {
            if (!seenAuthoringStyles.Add(value.AuthoringStyle))
            {
                throw new ArgumentException(
                    "An authoring-policy runtime answer can only declare one authoring-style summary per authoring style.",
                    parameterName);
            }

            result.Add(new RestEndpointAuthoringPolicyAuthoringStyleDescriptor(
                value.AuthoringStyle,
                value.CandidateIds,
                value.RetainedCandidateIds,
                value.PublishedCandidateIds,
                value.PrecedenceSuppressedCandidateIds,
                value.GovernanceSuppressedCandidateIds,
                value.SuppressedCandidateIds,
                value.SuppressionKinds,
                value.SuppressionSummaries));
        }

        return result.ToArray();
    }

    private static RestEndpointAuthoringPolicySuppressionSummaryDescriptor[] MergeAuthoringStyleSuppressionSummaries(
        IReadOnlyList<RestEndpointAuthoringPolicyAuthoringStyleDescriptor> authoringStyleSummaries)
    {
        ArgumentNullException.ThrowIfNull(authoringStyleSummaries);

        var candidateIdsByKind = new Dictionary<RestEndpointAuthoringPolicySuppressionKind, List<string>>();
        foreach (var summary in authoringStyleSummaries)
        {
            foreach (var suppressionSummary in summary.SuppressionSummaries)
            {
                if (!candidateIdsByKind.TryGetValue(suppressionSummary.Kind, out var candidateIds))
                {
                    candidateIds = [];
                    candidateIdsByKind[suppressionSummary.Kind] = candidateIds;
                }

                candidateIds.AddRange(suppressionSummary.CandidateIds);
            }
        }

        return candidateIdsByKind
            .OrderBy(static pair => pair.Key.GetWireName(), StringComparer.Ordinal)
            .Select(static pair => new RestEndpointAuthoringPolicySuppressionSummaryDescriptor(pair.Key, pair.Value))
            .ToArray();
    }

    private static string[] BuildOrderedSkippedRuleIds(
        IReadOnlyList<RestEndpointAuthoringPolicyAuthoringStyleDescriptor> authoringStyleSummaries,
        Func<RestEndpointAuthoringPolicyAuthoringStyleDescriptor, IReadOnlyList<string>> selector)
    {
        ArgumentNullException.ThrowIfNull(authoringStyleSummaries);
        ArgumentNullException.ThrowIfNull(selector);

        var ordered = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var summary in authoringStyleSummaries)
        {
            foreach (var value in selector(summary))
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                var trimmed = value.Trim();
                if (seen.Add(trimmed))
                {
                    ordered.Add(trimmed);
                }
            }
        }

        return ordered.ToArray();
    }

    private static bool SuppressionSummariesMatch(
        RestEndpointAuthoringPolicySuppressionSummaryDescriptor[] left,
        RestEndpointAuthoringPolicySuppressionSummaryDescriptor[] right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        if (left.Length != right.Length)
        {
            return false;
        }

        var rightByKind = right.ToDictionary(static summary => summary.Kind);
        foreach (var summary in left)
        {
            if (!rightByKind.TryGetValue(summary.Kind, out var other))
            {
                return false;
            }

            if (!summary.CandidateIds.ToHashSet(StringComparer.OrdinalIgnoreCase)
                    .SetEquals(other.CandidateIds))
            {
                return false;
            }
        }

        return true;
    }
}
