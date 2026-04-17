using System.Text.Json.Serialization;

namespace Cephalon.Abstractions.Transports;

/// <summary>
/// Describes the grouped public REST publication outcome for one behavior across all of its visible candidates.
/// </summary>
public sealed class RestEndpointPublicationGroupDescriptor
{
    /// <summary>
    /// Creates a grouped REST endpoint publication descriptor.
    /// </summary>
    /// <param name="behaviorId">The stable behavior identifier for the grouped publication answer.</param>
    /// <param name="sourceModuleIds">The distinct source-module identifiers that contributed the grouped candidates.</param>
    /// <param name="winningPrecedenceRank">
    /// The winning precedence rank for the published candidates when one or more candidates remain published.
    /// </param>
    /// <param name="publishedCandidateIds">The candidate identifiers that remain published for this behavior.</param>
    /// <param name="precedenceSuppressedCandidateIds">
    /// The candidate identifiers that were suppressed by another candidate through precedence resolution.
    /// </param>
    /// <param name="governanceSuppressedCandidateIds">
    /// The candidate identifiers that were suppressed by host-level REST governance.
    /// </param>
    /// <param name="candidates">The ordered candidate set that produced this grouped publication answer.</param>
    /// <param name="authoringPolicy">
    /// The effective authoring-policy intent for this behavior-level publication group.
    /// </param>
    /// <param name="authoringPolicySuppressedCandidateIds">
    /// The candidate identifiers that were suppressed by behavior-level authoring-policy enforcement.
    /// </param>
    /// <param name="authoringPolicySuppressionSummaries">
    /// The grouped authoring-policy suppression outcomes summarized by suppression kind.
    /// </param>
    /// <param name="hostGovernanceEligibleCandidateIds">
    /// The candidate identifiers whose original projections allowed host governance to participate.
    /// </param>
    /// <param name="hostGovernanceIneligibleCandidateIds">
    /// The candidate identifiers whose original projections kept host governance out of scope.
    /// </param>
    /// <param name="skippedSuppressionIds">
    /// The ordered suppression-rule identifiers that targeted ineligible candidates in this behavior group.
    /// </param>
    /// <param name="skippedOverrideIds">
    /// The ordered override-rule identifiers that targeted ineligible candidates in this behavior group.
    /// </param>
    /// <param name="governanceSuppressionSummaries">
    /// The grouped host-governance suppression-rule outcomes summarized by rule.
    /// </param>
    /// <param name="governanceOverrideSummaries">
    /// The grouped host-governance override-rule outcomes summarized by rule.
    /// </param>
    public RestEndpointPublicationGroupDescriptor(
        string behaviorId,
        IReadOnlyList<string>? sourceModuleIds = null,
        int? winningPrecedenceRank = null,
        IReadOnlyList<string>? publishedCandidateIds = null,
        IReadOnlyList<string>? precedenceSuppressedCandidateIds = null,
        IReadOnlyList<string>? governanceSuppressedCandidateIds = null,
        IReadOnlyList<RestEndpointCandidateRuntimeDescriptor>? candidates = null,
        RestEndpointPublicationGroupAuthoringPolicyDescriptor? authoringPolicy = null,
        IReadOnlyList<string>? authoringPolicySuppressedCandidateIds = null,
        IReadOnlyList<RestEndpointPublicationGroupAuthoringPolicySuppressionDescriptor>? authoringPolicySuppressionSummaries = null,
        IReadOnlyList<string>? hostGovernanceEligibleCandidateIds = null,
        IReadOnlyList<string>? hostGovernanceIneligibleCandidateIds = null,
        IReadOnlyList<string>? skippedSuppressionIds = null,
        IReadOnlyList<string>? skippedOverrideIds = null,
        IReadOnlyList<RestEndpointPublicationGroupGovernanceSuppressionSummaryDescriptor>? governanceSuppressionSummaries = null,
        IReadOnlyList<RestEndpointPublicationGroupGovernanceOverrideSummaryDescriptor>? governanceOverrideSummaries = null)
        : this(
            behaviorId,
            sourceModuleIds,
            winningPrecedenceRank,
            publishedCandidateIds,
            precedenceSuppressedCandidateIds,
            governanceSuppressedCandidateIds,
            candidates,
            authoringPolicy,
            authoringPolicySuppressedCandidateIds,
            authoringPolicySuppressionSummaries,
            hostGovernanceEligibleCandidateIds,
            hostGovernanceIneligibleCandidateIds,
            skippedSuppressionIds,
            skippedOverrideIds,
            governanceSuppressionSummaries,
            governanceOverrideSummaries,
            skippedSuppressionSummaries: null,
            skippedOverrideSummaries: null)
    {
    }

    /// <summary>
    /// Creates a grouped REST endpoint publication descriptor, including grouped skipped-governance summaries.
    /// </summary>
    /// <param name="behaviorId">The stable behavior identifier for the grouped publication answer.</param>
    /// <param name="sourceModuleIds">The distinct source-module identifiers that contributed the grouped candidates.</param>
    /// <param name="winningPrecedenceRank">
    /// The winning precedence rank for the published candidates when one or more candidates remain published.
    /// </param>
    /// <param name="publishedCandidateIds">The candidate identifiers that remain published for this behavior.</param>
    /// <param name="precedenceSuppressedCandidateIds">
    /// The candidate identifiers that were suppressed by another candidate through precedence resolution.
    /// </param>
    /// <param name="governanceSuppressedCandidateIds">
    /// The candidate identifiers that were suppressed by host-level REST governance.
    /// </param>
    /// <param name="candidates">The ordered candidate set that produced this grouped publication answer.</param>
    /// <param name="authoringPolicy">
    /// The effective authoring-policy intent for this behavior-level publication group.
    /// </param>
    /// <param name="authoringPolicySuppressedCandidateIds">
    /// The candidate identifiers that were suppressed by behavior-level authoring-policy enforcement.
    /// </param>
    /// <param name="authoringPolicySuppressionSummaries">
    /// The grouped authoring-policy suppression outcomes summarized by suppression kind.
    /// </param>
    /// <param name="hostGovernanceEligibleCandidateIds">
    /// The candidate identifiers whose original projections allowed host governance to participate.
    /// </param>
    /// <param name="hostGovernanceIneligibleCandidateIds">
    /// The candidate identifiers whose original projections kept host governance out of scope.
    /// </param>
    /// <param name="skippedSuppressionIds">
    /// The ordered suppression-rule identifiers that targeted ineligible candidates in this behavior group.
    /// </param>
    /// <param name="skippedOverrideIds">
    /// The ordered override-rule identifiers that targeted ineligible candidates in this behavior group.
    /// </param>
    /// <param name="governanceSuppressionSummaries">
    /// The grouped host-governance suppression-rule outcomes summarized by rule.
    /// </param>
    /// <param name="governanceOverrideSummaries">
    /// The grouped host-governance override-rule outcomes summarized by rule.
    /// </param>
    /// <param name="skippedSuppressionSummaries">
    /// The grouped host-governance-skipped suppression-rule outcomes summarized by rule.
    /// </param>
    /// <param name="skippedOverrideSummaries">
    /// The grouped host-governance-skipped override-rule outcomes summarized by rule.
    /// </param>
    [JsonConstructor]
    public RestEndpointPublicationGroupDescriptor(
        string behaviorId,
        IReadOnlyList<string>? sourceModuleIds,
        int? winningPrecedenceRank,
        IReadOnlyList<string>? publishedCandidateIds,
        IReadOnlyList<string>? precedenceSuppressedCandidateIds,
        IReadOnlyList<string>? governanceSuppressedCandidateIds,
        IReadOnlyList<RestEndpointCandidateRuntimeDescriptor>? candidates,
        RestEndpointPublicationGroupAuthoringPolicyDescriptor? authoringPolicy,
        IReadOnlyList<string>? authoringPolicySuppressedCandidateIds,
        IReadOnlyList<RestEndpointPublicationGroupAuthoringPolicySuppressionDescriptor>? authoringPolicySuppressionSummaries,
        IReadOnlyList<string>? hostGovernanceEligibleCandidateIds,
        IReadOnlyList<string>? hostGovernanceIneligibleCandidateIds,
        IReadOnlyList<string>? skippedSuppressionIds,
        IReadOnlyList<string>? skippedOverrideIds,
        IReadOnlyList<RestEndpointPublicationGroupGovernanceSuppressionSummaryDescriptor>? governanceSuppressionSummaries,
        IReadOnlyList<RestEndpointPublicationGroupGovernanceOverrideSummaryDescriptor>? governanceOverrideSummaries,
        IReadOnlyList<RestEndpointPublicationGroupGovernanceSkippedSuppressionSummaryDescriptor>? skippedSuppressionSummaries,
        IReadOnlyList<RestEndpointPublicationGroupGovernanceSkippedOverrideSummaryDescriptor>? skippedOverrideSummaries)
    {
        if (string.IsNullOrWhiteSpace(behaviorId))
        {
            throw new ArgumentException("A non-empty behavior id is required.", nameof(behaviorId));
        }

        if (winningPrecedenceRank.HasValue && winningPrecedenceRank.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(winningPrecedenceRank),
                winningPrecedenceRank,
                "The winning precedence rank must be positive when it is provided.");
        }

        var normalizedCandidates = NormalizeCandidates(candidates);
        if (normalizedCandidates.Length == 0)
        {
            throw new ArgumentException(
                "At least one candidate is required for a grouped publication answer.",
                nameof(candidates));
        }

        var normalizedBehaviorId = behaviorId.Trim();
        if (normalizedCandidates.Any(candidate =>
                !string.Equals(candidate.ProjectedEndpoint.BehaviorId, normalizedBehaviorId, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException(
                "Every grouped candidate must target the same behavior id.",
                nameof(candidates));
        }

        var normalizedSourceModuleIds = NormalizeList(sourceModuleIds);
        var normalizedPublishedCandidateIds = NormalizeList(publishedCandidateIds);
        var normalizedPrecedenceSuppressedCandidateIds = NormalizeList(precedenceSuppressedCandidateIds);
        var normalizedGovernanceSuppressedCandidateIds = NormalizeList(governanceSuppressedCandidateIds);
        var normalizedHostGovernanceEligibleCandidateIds = hostGovernanceEligibleCandidateIds is null
            ? BuildHostGovernanceCandidateIds(normalizedCandidates, allowsHostGovernance: true)
            : NormalizeList(hostGovernanceEligibleCandidateIds);
        var normalizedHostGovernanceIneligibleCandidateIds = hostGovernanceIneligibleCandidateIds is null
            ? BuildHostGovernanceCandidateIds(normalizedCandidates, allowsHostGovernance: false)
            : NormalizeList(hostGovernanceIneligibleCandidateIds);
        var normalizedSkippedSuppressionIds = skippedSuppressionIds is null
            ? BuildOrderedSkippedRuleIds(normalizedCandidates, static candidate => candidate.SkippedSuppressionIds)
            : NormalizeList(skippedSuppressionIds);
        var normalizedSkippedOverrideIds = skippedOverrideIds is null
            ? BuildOrderedSkippedRuleIds(normalizedCandidates, static candidate => candidate.SkippedOverrideIds)
            : NormalizeList(skippedOverrideIds);
        var candidateIds = normalizedCandidates
            .Select(static candidate => candidate.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var normalizedAuthoringPolicySuppressionSummaries = authoringPolicySuppressionSummaries is null
            ? BuildAuthoringPolicySuppressionSummaries(normalizedCandidates)
            : RestEndpointPublicationGroupAuthoringPolicySuppressionSummaryBuilder.Normalize(
                authoringPolicySuppressionSummaries,
                candidateIds,
                nameof(authoringPolicySuppressionSummaries));
        var normalizedAuthoringPolicySuppressedCandidateIds = NormalizeList(authoringPolicySuppressedCandidateIds);
        if (normalizedAuthoringPolicySuppressedCandidateIds.Length == 0 &&
            normalizedAuthoringPolicySuppressionSummaries.Length > 0)
        {
            normalizedAuthoringPolicySuppressedCandidateIds =
                RestEndpointPublicationGroupAuthoringPolicySuppressionSummaryBuilder.BuildSuppressedCandidateIds(
                    normalizedAuthoringPolicySuppressionSummaries);
        }

        if (normalizedAuthoringPolicySuppressionSummaries.Length > 0)
        {
            var summarizedCandidateIds =
                RestEndpointPublicationGroupAuthoringPolicySuppressionSummaryBuilder.BuildSuppressedCandidateIds(
                    normalizedAuthoringPolicySuppressionSummaries);
            if (!normalizedAuthoringPolicySuppressedCandidateIds.SequenceEqual(
                    summarizedCandidateIds,
                    StringComparer.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    "Authoring-policy suppression summaries must describe the same candidate ids as the grouped authoring-policy suppression bucket.",
                    nameof(authoringPolicySuppressedCandidateIds));
            }
        }

        var normalizedGovernanceSuppressionSummaries = governanceSuppressionSummaries is null
            ? BuildGovernanceSuppressionSummaries(normalizedCandidates)
            : RestEndpointPublicationGroupGovernanceSuppressionSummaryBuilder.Normalize(
                governanceSuppressionSummaries,
                candidateIds,
                nameof(governanceSuppressionSummaries));
        if (normalizedGovernanceSuppressedCandidateIds.Length == 0 &&
            normalizedGovernanceSuppressionSummaries.Length > 0)
        {
            normalizedGovernanceSuppressedCandidateIds =
                RestEndpointPublicationGroupGovernanceSuppressionSummaryBuilder.BuildSuppressedCandidateIds(
                    normalizedGovernanceSuppressionSummaries);
        }

        if (normalizedGovernanceSuppressionSummaries.Length > 0)
        {
            var summarizedCandidateIds =
                RestEndpointPublicationGroupGovernanceSuppressionSummaryBuilder.BuildSuppressedCandidateIds(
                    normalizedGovernanceSuppressionSummaries);
            if (!normalizedGovernanceSuppressedCandidateIds.SequenceEqual(
                    summarizedCandidateIds,
                    StringComparer.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    "Governance suppression summaries must describe the same candidate ids as the grouped governance suppression bucket.",
                    nameof(governanceSuppressedCandidateIds));
            }
        }

        var normalizedGovernanceOverrideSummaries = governanceOverrideSummaries is null
            ? BuildGovernanceOverrideSummaries(normalizedCandidates)
            : RestEndpointPublicationGroupGovernanceOverrideSummaryBuilder.Normalize(
                governanceOverrideSummaries,
                candidateIds,
                nameof(governanceOverrideSummaries));
        var normalizedSkippedSuppressionSummaries = skippedSuppressionSummaries is null
            ? BuildSkippedSuppressionSummaries(normalizedCandidates)
            : RestEndpointPublicationGroupGovernanceSkippedSuppressionSummaryBuilder.Normalize(
                skippedSuppressionSummaries,
                candidateIds,
                nameof(skippedSuppressionSummaries));
        var normalizedSkippedOverrideSummaries = skippedOverrideSummaries is null
            ? BuildSkippedOverrideSummaries(normalizedCandidates)
            : RestEndpointPublicationGroupGovernanceSkippedOverrideSummaryBuilder.Normalize(
                skippedOverrideSummaries,
                candidateIds,
                nameof(skippedOverrideSummaries));

        if (normalizedSkippedSuppressionIds.Length == 0 &&
            normalizedSkippedSuppressionSummaries.Length > 0)
        {
            normalizedSkippedSuppressionIds =
                RestEndpointPublicationGroupGovernanceSkippedSuppressionSummaryBuilder.BuildRuleIds(
                    normalizedSkippedSuppressionSummaries);
        }

        if (normalizedSkippedOverrideIds.Length == 0 &&
            normalizedSkippedOverrideSummaries.Length > 0)
        {
            normalizedSkippedOverrideIds =
                RestEndpointPublicationGroupGovernanceSkippedOverrideSummaryBuilder.BuildRuleIds(
                    normalizedSkippedOverrideSummaries);
        }

        if (normalizedSkippedSuppressionSummaries.Length > 0)
        {
            var summarizedRuleIds =
                RestEndpointPublicationGroupGovernanceSkippedSuppressionSummaryBuilder.BuildRuleIds(
                    normalizedSkippedSuppressionSummaries);
            if (!normalizedSkippedSuppressionIds.SequenceEqual(summarizedRuleIds, StringComparer.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    "Skipped suppression summaries must describe the same rule ids as the grouped skipped suppression bucket.",
                    nameof(skippedSuppressionIds));
            }
        }

        if (normalizedSkippedOverrideSummaries.Length > 0)
        {
            var summarizedRuleIds =
                RestEndpointPublicationGroupGovernanceSkippedOverrideSummaryBuilder.BuildRuleIds(
                    normalizedSkippedOverrideSummaries);
            if (!normalizedSkippedOverrideIds.SequenceEqual(summarizedRuleIds, StringComparer.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    "Skipped override summaries must describe the same rule ids as the grouped skipped override bucket.",
                    nameof(skippedOverrideIds));
            }
        }

        if (!candidateIds.IsSupersetOf(normalizedPublishedCandidateIds))
        {
            throw new ArgumentException(
                "Published candidate ids must refer to candidates in the grouped publication answer.",
                nameof(publishedCandidateIds));
        }

        if (!candidateIds.IsSupersetOf(normalizedPrecedenceSuppressedCandidateIds))
        {
            throw new ArgumentException(
                "Precedence-suppressed candidate ids must refer to candidates in the grouped publication answer.",
                nameof(precedenceSuppressedCandidateIds));
        }

        if (!candidateIds.IsSupersetOf(normalizedGovernanceSuppressedCandidateIds))
        {
            throw new ArgumentException(
                "Governance-suppressed candidate ids must refer to candidates in the grouped publication answer.",
                nameof(governanceSuppressedCandidateIds));
        }

        if (!candidateIds.IsSupersetOf(normalizedAuthoringPolicySuppressedCandidateIds))
        {
            throw new ArgumentException(
                "Authoring-policy-suppressed candidate ids must refer to candidates in the grouped publication answer.",
                nameof(authoringPolicySuppressedCandidateIds));
        }

        if (!candidateIds.IsSupersetOf(normalizedHostGovernanceEligibleCandidateIds))
        {
            throw new ArgumentException(
                "Host-governance-eligible candidate ids must refer to candidates in the grouped publication answer.",
                nameof(hostGovernanceEligibleCandidateIds));
        }

        if (!candidateIds.IsSupersetOf(normalizedHostGovernanceIneligibleCandidateIds))
        {
            throw new ArgumentException(
                "Host-governance-ineligible candidate ids must refer to candidates in the grouped publication answer.",
                nameof(hostGovernanceIneligibleCandidateIds));
        }

        if (normalizedPrecedenceSuppressedCandidateIds.Intersect(normalizedGovernanceSuppressedCandidateIds, StringComparer.OrdinalIgnoreCase).Any())
        {
            throw new ArgumentException(
                "A grouped publication answer cannot classify the same candidate as both precedence-suppressed and governance-suppressed.",
                nameof(precedenceSuppressedCandidateIds));
        }

        if (normalizedPrecedenceSuppressedCandidateIds.Intersect(normalizedAuthoringPolicySuppressedCandidateIds, StringComparer.OrdinalIgnoreCase).Any())
        {
            throw new ArgumentException(
                "A grouped publication answer cannot classify the same candidate as both precedence-suppressed and authoring-policy-suppressed.",
                nameof(precedenceSuppressedCandidateIds));
        }

        if (normalizedGovernanceSuppressedCandidateIds.Intersect(normalizedAuthoringPolicySuppressedCandidateIds, StringComparer.OrdinalIgnoreCase).Any())
        {
            throw new ArgumentException(
                "A grouped publication answer cannot classify the same candidate as both governance-suppressed and authoring-policy-suppressed.",
                nameof(governanceSuppressedCandidateIds));
        }

        if (normalizedHostGovernanceEligibleCandidateIds.Intersect(normalizedHostGovernanceIneligibleCandidateIds, StringComparer.OrdinalIgnoreCase).Any())
        {
            throw new ArgumentException(
                "A grouped publication answer cannot classify the same candidate as both host-governance-eligible and host-governance-ineligible.",
                nameof(hostGovernanceEligibleCandidateIds));
        }

        BehaviorId = normalizedBehaviorId;
        SourceModuleIds = normalizedSourceModuleIds;
        WinningPrecedenceRank = winningPrecedenceRank;
        PublishedCandidateIds = normalizedPublishedCandidateIds;
        PrecedenceSuppressedCandidateIds = normalizedPrecedenceSuppressedCandidateIds;
        GovernanceSuppressedCandidateIds = normalizedGovernanceSuppressedCandidateIds;
        AuthoringPolicySuppressedCandidateIds = normalizedAuthoringPolicySuppressedCandidateIds;
        AuthoringPolicySuppressionSummaries = normalizedAuthoringPolicySuppressionSummaries;
        GovernanceSuppressionSummaries = normalizedGovernanceSuppressionSummaries;
        GovernanceOverrideSummaries = normalizedGovernanceOverrideSummaries;
        SkippedSuppressionSummaries = normalizedSkippedSuppressionSummaries;
        SkippedOverrideSummaries = normalizedSkippedOverrideSummaries;
        HostGovernanceEligibleCandidateIds = normalizedHostGovernanceEligibleCandidateIds;
        HostGovernanceIneligibleCandidateIds = normalizedHostGovernanceIneligibleCandidateIds;
        SkippedSuppressionIds = normalizedSkippedSuppressionIds;
        SkippedOverrideIds = normalizedSkippedOverrideIds;
        Candidates = normalizedCandidates;
        AuthoringStyleSummaries = BuildAuthoringStyleSummaries(normalizedCandidates);
        AuthoringPolicy = NormalizeAuthoringPolicy(authoringPolicy, normalizedBehaviorId);
    }

    /// <summary>
    /// Gets the stable behavior identifier for the grouped publication answer.
    /// </summary>
    public string BehaviorId { get; }

    /// <summary>
    /// Gets the distinct source-module identifiers that contributed the grouped candidates.
    /// </summary>
    public IReadOnlyList<string> SourceModuleIds { get; }

    /// <summary>
    /// Gets the winning precedence rank for the published candidates when one or more remain published.
    /// </summary>
    public int? WinningPrecedenceRank { get; }

    /// <summary>
    /// Gets the candidate identifiers that remain published for this behavior.
    /// </summary>
    public IReadOnlyList<string> PublishedCandidateIds { get; }

    /// <summary>
    /// Gets the candidate identifiers that were suppressed by precedence resolution.
    /// </summary>
    public IReadOnlyList<string> PrecedenceSuppressedCandidateIds { get; }

    /// <summary>
    /// Gets the candidate identifiers that were suppressed by host-level REST governance.
    /// </summary>
    public IReadOnlyList<string> GovernanceSuppressedCandidateIds { get; }

    /// <summary>
    /// Gets the candidate identifiers that were suppressed by behavior-level authoring-policy enforcement.
    /// </summary>
    public IReadOnlyList<string> AuthoringPolicySuppressedCandidateIds { get; }

    /// <summary>
    /// Gets the grouped authoring-policy suppression outcomes summarized by suppression kind.
    /// </summary>
    public IReadOnlyList<RestEndpointPublicationGroupAuthoringPolicySuppressionDescriptor> AuthoringPolicySuppressionSummaries { get; }

    /// <summary>
    /// Gets the grouped host-governance suppression-rule outcomes summarized by rule.
    /// </summary>
    public IReadOnlyList<RestEndpointPublicationGroupGovernanceSuppressionSummaryDescriptor> GovernanceSuppressionSummaries { get; }

    /// <summary>
    /// Gets the grouped host-governance override-rule outcomes summarized by rule.
    /// </summary>
    public IReadOnlyList<RestEndpointPublicationGroupGovernanceOverrideSummaryDescriptor> GovernanceOverrideSummaries { get; }

    /// <summary>
    /// Gets the grouped host-governance-skipped suppression-rule outcomes summarized by rule.
    /// </summary>
    public IReadOnlyList<RestEndpointPublicationGroupGovernanceSkippedSuppressionSummaryDescriptor> SkippedSuppressionSummaries { get; }

    /// <summary>
    /// Gets the grouped host-governance-skipped override-rule outcomes summarized by rule.
    /// </summary>
    public IReadOnlyList<RestEndpointPublicationGroupGovernanceSkippedOverrideSummaryDescriptor> SkippedOverrideSummaries { get; }

    /// <summary>
    /// Gets the candidate identifiers whose original projections allowed host governance to participate.
    /// </summary>
    public IReadOnlyList<string> HostGovernanceEligibleCandidateIds { get; }

    /// <summary>
    /// Gets the candidate identifiers whose original projections kept host governance out of scope.
    /// </summary>
    public IReadOnlyList<string> HostGovernanceIneligibleCandidateIds { get; }

    /// <summary>
    /// Gets the ordered suppression-rule identifiers that targeted ineligible candidates in this behavior group.
    /// </summary>
    public IReadOnlyList<string> SkippedSuppressionIds { get; }

    /// <summary>
    /// Gets the ordered override-rule identifiers that targeted ineligible candidates in this behavior group.
    /// </summary>
    public IReadOnlyList<string> SkippedOverrideIds { get; }

    /// <summary>
    /// Gets the ordered candidate set that produced this grouped publication answer.
    /// </summary>
    public IReadOnlyList<RestEndpointCandidateRuntimeDescriptor> Candidates { get; }

    /// <summary>
    /// Gets the grouped publication outcome summarized by authoring style for this behavior.
    /// </summary>
    public IReadOnlyList<RestEndpointPublicationGroupAuthoringStyleDescriptor> AuthoringStyleSummaries { get; }

    /// <summary>
    /// Gets the effective authoring-policy intent for this behavior-level publication group.
    /// </summary>
    public RestEndpointPublicationGroupAuthoringPolicyDescriptor AuthoringPolicy { get; }

    private static string[] NormalizeList(IReadOnlyList<string>? values)
    {
        return values?
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }

    private static RestEndpointCandidateRuntimeDescriptor[] NormalizeCandidates(
        IReadOnlyList<RestEndpointCandidateRuntimeDescriptor>? candidates)
    {
        return candidates?
            .Where(static candidate => candidate is not null)
            .ToArray() ?? [];
    }

    private static RestEndpointPublicationGroupAuthoringStyleDescriptor[] BuildAuthoringStyleSummaries(
        IReadOnlyList<RestEndpointCandidateRuntimeDescriptor> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        return candidates
            .GroupBy(static candidate => candidate.AuthoringStyle, StringComparer.OrdinalIgnoreCase)
            .Select(static group =>
            {
                var orderedCandidates = group.ToArray();
                var sourceModuleIds = orderedCandidates
                    .Select(static candidate => candidate.ProjectedEndpoint.SourceModuleId)
                    .Where(static sourceModuleId => !string.IsNullOrWhiteSpace(sourceModuleId))
                    .Select(static sourceModuleId => sourceModuleId!.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(static sourceModuleId => sourceModuleId, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                var precedenceRanks = orderedCandidates
                    .Select(static candidate => candidate.PrecedenceRank)
                    .Distinct()
                    .OrderBy(static rank => rank)
                    .ToArray();
                var candidateIds = orderedCandidates
                    .Select(static candidate => candidate.Id)
                    .ToArray();
                var publishedCandidateIds = orderedCandidates
                    .Where(static candidate => candidate.Status == RestEndpointCandidateStatus.Published)
                    .Select(static candidate => candidate.Id)
                    .ToArray();
                var precedenceSuppressedCandidateIds = orderedCandidates
                    .Where(static candidate =>
                        candidate.Status == RestEndpointCandidateStatus.Suppressed &&
                        !string.IsNullOrWhiteSpace(candidate.SuppressedByCandidateId))
                    .Select(static candidate => candidate.Id)
                    .ToArray();
                var governanceSuppressedCandidateIds = orderedCandidates
                    .Where(static candidate =>
                        candidate.Status == RestEndpointCandidateStatus.Suppressed &&
                        !string.IsNullOrWhiteSpace(candidate.SuppressedBySuppressionId))
                    .Select(static candidate => candidate.Id)
                    .ToArray();
                var authoringPolicySuppressedCandidateIds = orderedCandidates
                    .Where(static candidate =>
                        candidate.Status == RestEndpointCandidateStatus.Suppressed &&
                        candidate.SuppressedByAuthoringPolicyKind.HasValue)
                    .Select(static candidate => candidate.Id)
                    .ToArray();
                var authoringPolicySuppressionSummaries = BuildAuthoringPolicySuppressionSummaries(orderedCandidates);
                var governanceSuppressionSummaries = BuildGovernanceSuppressionSummaries(orderedCandidates);
                var governanceOverrideSummaries = BuildGovernanceOverrideSummaries(orderedCandidates);
                var hostGovernanceEligibleCandidateIds = BuildHostGovernanceCandidateIds(
                    orderedCandidates,
                    allowsHostGovernance: true);
                var hostGovernanceIneligibleCandidateIds = BuildHostGovernanceCandidateIds(
                    orderedCandidates,
                    allowsHostGovernance: false);
                var skippedSuppressionIds = BuildOrderedSkippedRuleIds(
                    orderedCandidates,
                    static candidate => candidate.SkippedSuppressionIds);
                var skippedOverrideIds = BuildOrderedSkippedRuleIds(
                    orderedCandidates,
                    static candidate => candidate.SkippedOverrideIds);
                var skippedSuppressionSummaries = BuildSkippedSuppressionSummaries(orderedCandidates);
                var skippedOverrideSummaries = BuildSkippedOverrideSummaries(orderedCandidates);

                return new RestEndpointPublicationGroupAuthoringStyleDescriptor(
                    group.Key,
                    sourceModuleIds,
                    precedenceRanks,
                    candidateIds,
                    publishedCandidateIds,
                    precedenceSuppressedCandidateIds,
                    governanceSuppressedCandidateIds,
                    authoringPolicySuppressedCandidateIds,
                    authoringPolicySuppressionSummaries,
                    hostGovernanceEligibleCandidateIds,
                    hostGovernanceIneligibleCandidateIds,
                    skippedSuppressionIds,
                    skippedOverrideIds,
                    governanceSuppressionSummaries,
                    governanceOverrideSummaries,
                    skippedSuppressionSummaries,
                    skippedOverrideSummaries);
            })
            .ToArray();
    }

    private static string[] BuildHostGovernanceCandidateIds(
        IReadOnlyList<RestEndpointCandidateRuntimeDescriptor> candidates,
        bool allowsHostGovernance)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        return candidates
            .Where(candidate => candidate.OriginalProjection.AllowsHostGovernance == allowsHostGovernance)
            .Select(static candidate => candidate.Id)
            .ToArray();
    }

    private static string[] BuildOrderedSkippedRuleIds(
        IReadOnlyList<RestEndpointCandidateRuntimeDescriptor> candidates,
        Func<RestEndpointCandidateRuntimeDescriptor, IReadOnlyList<string>> selector)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(selector);

        var ordered = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var candidate in candidates)
        {
            foreach (var value in selector(candidate))
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

    private static RestEndpointPublicationGroupAuthoringPolicyDescriptor NormalizeAuthoringPolicy(
        RestEndpointPublicationGroupAuthoringPolicyDescriptor? authoringPolicy,
        string behaviorId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(behaviorId);

        if (authoringPolicy is null)
        {
            return new RestEndpointPublicationGroupAuthoringPolicyDescriptor(behaviorId);
        }

        if (!string.Equals(authoringPolicy.BehaviorId, behaviorId, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "The grouped authoring policy must target the same behavior id as the publication group.",
                nameof(authoringPolicy));
        }

        return new RestEndpointPublicationGroupAuthoringPolicyDescriptor(
            authoringPolicy.BehaviorId,
            authoringPolicy.IsConfigured,
            authoringPolicy.AllowMultiplePublishedCandidates,
            authoringPolicy.PreferredAuthoringStyle,
            authoringPolicy.AllowedAuthoringStyles,
            authoringPolicy.DisallowedAuthoringStyles);
    }

    private static RestEndpointPublicationGroupAuthoringPolicySuppressionDescriptor[] BuildAuthoringPolicySuppressionSummaries(
        IReadOnlyList<RestEndpointCandidateRuntimeDescriptor> candidates)
    {
        return RestEndpointPublicationGroupAuthoringPolicySuppressionSummaryBuilder.BuildFromCandidates(candidates);
    }

    private static RestEndpointPublicationGroupGovernanceSuppressionSummaryDescriptor[] BuildGovernanceSuppressionSummaries(
        IReadOnlyList<RestEndpointCandidateRuntimeDescriptor> candidates)
    {
        return RestEndpointPublicationGroupGovernanceSuppressionSummaryBuilder.BuildFromCandidates(candidates);
    }

    private static RestEndpointPublicationGroupGovernanceOverrideSummaryDescriptor[] BuildGovernanceOverrideSummaries(
        IReadOnlyList<RestEndpointCandidateRuntimeDescriptor> candidates)
    {
        return RestEndpointPublicationGroupGovernanceOverrideSummaryBuilder.BuildFromCandidates(candidates);
    }

    private static RestEndpointPublicationGroupGovernanceSkippedSuppressionSummaryDescriptor[] BuildSkippedSuppressionSummaries(
        IReadOnlyList<RestEndpointCandidateRuntimeDescriptor> candidates)
    {
        return RestEndpointPublicationGroupGovernanceSkippedSuppressionSummaryBuilder.BuildFromCandidates(candidates);
    }

    private static RestEndpointPublicationGroupGovernanceSkippedOverrideSummaryDescriptor[] BuildSkippedOverrideSummaries(
        IReadOnlyList<RestEndpointCandidateRuntimeDescriptor> candidates)
    {
        return RestEndpointPublicationGroupGovernanceSkippedOverrideSummaryBuilder.BuildFromCandidates(candidates);
    }
}
