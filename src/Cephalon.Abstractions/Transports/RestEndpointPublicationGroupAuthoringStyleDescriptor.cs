using System.Text.Json.Serialization;

namespace Cephalon.Abstractions.Transports;

/// <summary>
/// Describes the grouped publication outcome for one authoring style within a behavior-level REST publication group.
/// </summary>
public sealed class RestEndpointPublicationGroupAuthoringStyleDescriptor
{
    /// <summary>
    /// Creates a grouped publication descriptor for one authoring style.
    /// </summary>
    /// <param name="authoringStyle">The normalized authoring style that contributed the grouped candidates.</param>
    /// <param name="sourceModuleIds">The distinct source-module identifiers that contributed candidates for this authoring style.</param>
    /// <param name="precedenceRanks">The distinct precedence ranks visible for this authoring style within the group.</param>
    /// <param name="candidateIds">The ordered candidate identifiers contributed by this authoring style.</param>
    /// <param name="publishedCandidateIds">The ordered candidate identifiers that remain published for this authoring style.</param>
    /// <param name="precedenceSuppressedCandidateIds">
    /// The ordered candidate identifiers that were suppressed by another candidate through precedence resolution.
    /// </param>
    /// <param name="governanceSuppressedCandidateIds">
    /// The ordered candidate identifiers that were suppressed by host-level REST governance.
    /// </param>
    /// <param name="authoringPolicySuppressedCandidateIds">
    /// The ordered candidate identifiers that were suppressed by behavior-level authoring-policy enforcement.
    /// </param>
    /// <param name="authoringPolicySuppressionSummaries">
    /// The grouped authoring-policy suppression outcomes summarized by suppression kind for this authoring style.
    /// </param>
    /// <param name="hostGovernanceEligibleCandidateIds">
    /// The ordered candidate identifiers whose original projections allowed host governance to participate.
    /// </param>
    /// <param name="hostGovernanceIneligibleCandidateIds">
    /// The ordered candidate identifiers whose original projections kept host governance out of scope.
    /// </param>
    /// <param name="skippedSuppressionIds">
    /// The ordered suppression-rule identifiers that targeted ineligible candidates for this authoring style.
    /// </param>
    /// <param name="skippedOverrideIds">
    /// The ordered override-rule identifiers that targeted ineligible candidates for this authoring style.
    /// </param>
    /// <param name="governanceSuppressionSummaries">
    /// The grouped host-governance suppression-rule outcomes summarized by rule for this authoring style.
    /// </param>
    /// <param name="governanceOverrideSummaries">
    /// The grouped host-governance override-rule outcomes summarized by rule for this authoring style.
    /// </param>
    public RestEndpointPublicationGroupAuthoringStyleDescriptor(
        string authoringStyle,
        IReadOnlyList<string>? sourceModuleIds = null,
        IReadOnlyList<int>? precedenceRanks = null,
        IReadOnlyList<string>? candidateIds = null,
        IReadOnlyList<string>? publishedCandidateIds = null,
        IReadOnlyList<string>? precedenceSuppressedCandidateIds = null,
        IReadOnlyList<string>? governanceSuppressedCandidateIds = null,
        IReadOnlyList<string>? authoringPolicySuppressedCandidateIds = null,
        IReadOnlyList<RestEndpointPublicationGroupAuthoringPolicySuppressionDescriptor>? authoringPolicySuppressionSummaries = null,
        IReadOnlyList<string>? hostGovernanceEligibleCandidateIds = null,
        IReadOnlyList<string>? hostGovernanceIneligibleCandidateIds = null,
        IReadOnlyList<string>? skippedSuppressionIds = null,
        IReadOnlyList<string>? skippedOverrideIds = null,
        IReadOnlyList<RestEndpointPublicationGroupGovernanceSuppressionSummaryDescriptor>? governanceSuppressionSummaries = null,
        IReadOnlyList<RestEndpointPublicationGroupGovernanceOverrideSummaryDescriptor>? governanceOverrideSummaries = null)
        : this(
            authoringStyle,
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
            skippedSuppressionSummaries: null,
            skippedOverrideSummaries: null)
    {
    }

    /// <summary>
    /// Creates a grouped publication descriptor for one authoring style, including grouped skipped-governance summaries.
    /// </summary>
    /// <param name="authoringStyle">The normalized authoring style that contributed the grouped candidates.</param>
    /// <param name="sourceModuleIds">The distinct source-module identifiers that contributed candidates for this authoring style.</param>
    /// <param name="precedenceRanks">The distinct precedence ranks visible for this authoring style within the group.</param>
    /// <param name="candidateIds">The ordered candidate identifiers contributed by this authoring style.</param>
    /// <param name="publishedCandidateIds">The ordered candidate identifiers that remain published for this authoring style.</param>
    /// <param name="precedenceSuppressedCandidateIds">
    /// The ordered candidate identifiers that were suppressed by another candidate through precedence resolution.
    /// </param>
    /// <param name="governanceSuppressedCandidateIds">
    /// The ordered candidate identifiers that were suppressed by host-level REST governance.
    /// </param>
    /// <param name="authoringPolicySuppressedCandidateIds">
    /// The ordered candidate identifiers that were suppressed by behavior-level authoring-policy enforcement.
    /// </param>
    /// <param name="authoringPolicySuppressionSummaries">
    /// The grouped authoring-policy suppression outcomes summarized by suppression kind for this authoring style.
    /// </param>
    /// <param name="hostGovernanceEligibleCandidateIds">
    /// The ordered candidate identifiers whose original projections allowed host governance to participate.
    /// </param>
    /// <param name="hostGovernanceIneligibleCandidateIds">
    /// The ordered candidate identifiers whose original projections kept host governance out of scope.
    /// </param>
    /// <param name="skippedSuppressionIds">
    /// The ordered suppression-rule identifiers that targeted ineligible candidates for this authoring style.
    /// </param>
    /// <param name="skippedOverrideIds">
    /// The ordered override-rule identifiers that targeted ineligible candidates for this authoring style.
    /// </param>
    /// <param name="governanceSuppressionSummaries">
    /// The grouped host-governance suppression-rule outcomes summarized by rule for this authoring style.
    /// </param>
    /// <param name="governanceOverrideSummaries">
    /// The grouped host-governance override-rule outcomes summarized by rule for this authoring style.
    /// </param>
    /// <param name="skippedSuppressionSummaries">
    /// The grouped host-governance-skipped suppression-rule outcomes summarized by rule for this authoring style.
    /// </param>
    /// <param name="skippedOverrideSummaries">
    /// The grouped host-governance-skipped override-rule outcomes summarized by rule for this authoring style.
    /// </param>
    [JsonConstructor]
    public RestEndpointPublicationGroupAuthoringStyleDescriptor(
        string authoringStyle,
        IReadOnlyList<string>? sourceModuleIds,
        IReadOnlyList<int>? precedenceRanks,
        IReadOnlyList<string>? candidateIds,
        IReadOnlyList<string>? publishedCandidateIds,
        IReadOnlyList<string>? precedenceSuppressedCandidateIds,
        IReadOnlyList<string>? governanceSuppressedCandidateIds,
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
        if (string.IsNullOrWhiteSpace(authoringStyle))
        {
            throw new ArgumentException("A non-empty authoring style is required.", nameof(authoringStyle));
        }

        var normalizedPrecedenceRanks = NormalizePrecedenceRanks(precedenceRanks);
        var normalizedCandidateIds = NormalizeOrderedList(candidateIds);
        var normalizedPublishedCandidateIds = NormalizeOrderedList(publishedCandidateIds);
        var normalizedPrecedenceSuppressedCandidateIds = NormalizeOrderedList(precedenceSuppressedCandidateIds);
        var normalizedGovernanceSuppressedCandidateIds = NormalizeOrderedList(governanceSuppressedCandidateIds);
        var normalizedHostGovernanceEligibleCandidateIds = NormalizeOrderedList(hostGovernanceEligibleCandidateIds);
        var normalizedHostGovernanceIneligibleCandidateIds = NormalizeOrderedList(hostGovernanceIneligibleCandidateIds);
        var normalizedSkippedSuppressionIds = NormalizeOrderedList(skippedSuppressionIds);
        var normalizedSkippedOverrideIds = NormalizeOrderedList(skippedOverrideIds);

        if (normalizedCandidateIds.Length == 0)
        {
            throw new ArgumentException(
                "At least one candidate id is required for an authoring-style publication answer.",
                nameof(candidateIds));
        }

        var candidateIdSet = normalizedCandidateIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var normalizedAuthoringPolicySuppressionSummaries = authoringPolicySuppressionSummaries is null
            ? []
            : RestEndpointPublicationGroupAuthoringPolicySuppressionSummaryBuilder.Normalize(
                authoringPolicySuppressionSummaries,
                candidateIdSet,
                nameof(authoringPolicySuppressionSummaries));
        var normalizedAuthoringPolicySuppressedCandidateIds = NormalizeOrderedList(authoringPolicySuppressedCandidateIds);
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
                    "Authoring-policy suppression summaries must describe the same candidate ids as the grouped authoring-style suppression bucket.",
                    nameof(authoringPolicySuppressedCandidateIds));
            }
        }

        var normalizedGovernanceSuppressionSummaries = governanceSuppressionSummaries is null
            ? []
            : RestEndpointPublicationGroupGovernanceSuppressionSummaryBuilder.Normalize(
                governanceSuppressionSummaries,
                candidateIdSet,
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
                    "Governance suppression summaries must describe the same candidate ids as the grouped authoring-style governance suppression bucket.",
                    nameof(governanceSuppressedCandidateIds));
            }
        }

        var normalizedGovernanceOverrideSummaries = governanceOverrideSummaries is null
            ? []
            : RestEndpointPublicationGroupGovernanceOverrideSummaryBuilder.Normalize(
                governanceOverrideSummaries,
                candidateIdSet,
                nameof(governanceOverrideSummaries));
        var normalizedSkippedSuppressionSummaries = skippedSuppressionSummaries is null
            ? []
            : RestEndpointPublicationGroupGovernanceSkippedSuppressionSummaryBuilder.Normalize(
                skippedSuppressionSummaries,
                candidateIdSet,
                nameof(skippedSuppressionSummaries));
        var normalizedSkippedOverrideSummaries = skippedOverrideSummaries is null
            ? []
            : RestEndpointPublicationGroupGovernanceSkippedOverrideSummaryBuilder.Normalize(
                skippedOverrideSummaries,
                candidateIdSet,
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
                    "Skipped suppression summaries must describe the same rule ids as the grouped authoring-style skipped suppression bucket.",
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
                    "Skipped override summaries must describe the same rule ids as the grouped authoring-style skipped override bucket.",
                    nameof(skippedOverrideIds));
            }
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

        if (!candidateIdSet.IsSupersetOf(normalizedAuthoringPolicySuppressedCandidateIds))
        {
            throw new ArgumentException(
                "Authoring-policy-suppressed candidate ids must refer to candidates in the grouped authoring-style answer.",
                nameof(authoringPolicySuppressedCandidateIds));
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

        if (normalizedPrecedenceSuppressedCandidateIds.Intersect(
                normalizedGovernanceSuppressedCandidateIds,
                StringComparer.OrdinalIgnoreCase).Any())
        {
            throw new ArgumentException(
                "An authoring-style publication answer cannot classify the same candidate as both precedence-suppressed and governance-suppressed.",
                nameof(precedenceSuppressedCandidateIds));
        }

        if (normalizedPrecedenceSuppressedCandidateIds.Intersect(
                normalizedAuthoringPolicySuppressedCandidateIds,
                StringComparer.OrdinalIgnoreCase).Any())
        {
            throw new ArgumentException(
                "An authoring-style publication answer cannot classify the same candidate as both precedence-suppressed and authoring-policy-suppressed.",
                nameof(precedenceSuppressedCandidateIds));
        }

        if (normalizedGovernanceSuppressedCandidateIds.Intersect(
                normalizedAuthoringPolicySuppressedCandidateIds,
                StringComparer.OrdinalIgnoreCase).Any())
        {
            throw new ArgumentException(
                "An authoring-style publication answer cannot classify the same candidate as both governance-suppressed and authoring-policy-suppressed.",
                nameof(governanceSuppressedCandidateIds));
        }

        if (normalizedHostGovernanceEligibleCandidateIds.Intersect(
                normalizedHostGovernanceIneligibleCandidateIds,
                StringComparer.OrdinalIgnoreCase).Any())
        {
            throw new ArgumentException(
                "An authoring-style publication answer cannot classify the same candidate as both host-governance-eligible and host-governance-ineligible.",
                nameof(hostGovernanceEligibleCandidateIds));
        }

        AuthoringStyle = authoringStyle.Trim();
        SourceModuleIds = NormalizeSortedList(sourceModuleIds);
        PrecedenceRanks = normalizedPrecedenceRanks;
        CandidateIds = normalizedCandidateIds;
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
    }

    /// <summary>
    /// Gets the normalized authoring style that contributed the grouped candidates.
    /// </summary>
    public string AuthoringStyle { get; }

    /// <summary>
    /// Gets the distinct source-module identifiers that contributed candidates for this authoring style.
    /// </summary>
    public IReadOnlyList<string> SourceModuleIds { get; }

    /// <summary>
    /// Gets the distinct precedence ranks visible for this authoring style within the group.
    /// </summary>
    public IReadOnlyList<int> PrecedenceRanks { get; }

    /// <summary>
    /// Gets the ordered candidate identifiers contributed by this authoring style.
    /// </summary>
    public IReadOnlyList<string> CandidateIds { get; }

    /// <summary>
    /// Gets the ordered candidate identifiers that remain published for this authoring style.
    /// </summary>
    public IReadOnlyList<string> PublishedCandidateIds { get; }

    /// <summary>
    /// Gets the ordered candidate identifiers that were suppressed by precedence resolution.
    /// </summary>
    public IReadOnlyList<string> PrecedenceSuppressedCandidateIds { get; }

    /// <summary>
    /// Gets the ordered candidate identifiers that were suppressed by host-level REST governance.
    /// </summary>
    public IReadOnlyList<string> GovernanceSuppressedCandidateIds { get; }

    /// <summary>
    /// Gets the ordered candidate identifiers that were suppressed by behavior-level authoring-policy enforcement.
    /// </summary>
    public IReadOnlyList<string> AuthoringPolicySuppressedCandidateIds { get; }

    /// <summary>
    /// Gets the grouped authoring-policy suppression outcomes summarized by suppression kind for this authoring style.
    /// </summary>
    public IReadOnlyList<RestEndpointPublicationGroupAuthoringPolicySuppressionDescriptor> AuthoringPolicySuppressionSummaries { get; }

    /// <summary>
    /// Gets the grouped host-governance suppression-rule outcomes summarized by rule for this authoring style.
    /// </summary>
    public IReadOnlyList<RestEndpointPublicationGroupGovernanceSuppressionSummaryDescriptor> GovernanceSuppressionSummaries { get; }

    /// <summary>
    /// Gets the grouped host-governance override-rule outcomes summarized by rule for this authoring style.
    /// </summary>
    public IReadOnlyList<RestEndpointPublicationGroupGovernanceOverrideSummaryDescriptor> GovernanceOverrideSummaries { get; }

    /// <summary>
    /// Gets the grouped host-governance-skipped suppression-rule outcomes summarized by rule for this authoring style.
    /// </summary>
    public IReadOnlyList<RestEndpointPublicationGroupGovernanceSkippedSuppressionSummaryDescriptor> SkippedSuppressionSummaries { get; }

    /// <summary>
    /// Gets the grouped host-governance-skipped override-rule outcomes summarized by rule for this authoring style.
    /// </summary>
    public IReadOnlyList<RestEndpointPublicationGroupGovernanceSkippedOverrideSummaryDescriptor> SkippedOverrideSummaries { get; }

    /// <summary>
    /// Gets the ordered candidate identifiers whose original projections allowed host governance to participate.
    /// </summary>
    public IReadOnlyList<string> HostGovernanceEligibleCandidateIds { get; }

    /// <summary>
    /// Gets the ordered candidate identifiers whose original projections kept host governance out of scope.
    /// </summary>
    public IReadOnlyList<string> HostGovernanceIneligibleCandidateIds { get; }

    /// <summary>
    /// Gets the ordered suppression-rule identifiers that targeted ineligible candidates for this authoring style.
    /// </summary>
    public IReadOnlyList<string> SkippedSuppressionIds { get; }

    /// <summary>
    /// Gets the ordered override-rule identifiers that targeted ineligible candidates for this authoring style.
    /// </summary>
    public IReadOnlyList<string> SkippedOverrideIds { get; }

    private static int[] NormalizePrecedenceRanks(IReadOnlyList<int>? precedenceRanks)
    {
        if (precedenceRanks is null || precedenceRanks.Count == 0)
        {
            return [];
        }

        if (precedenceRanks.Any(static rank => rank <= 0))
        {
            throw new ArgumentOutOfRangeException(
                nameof(precedenceRanks),
                precedenceRanks,
                "Every precedence rank must be positive.");
        }

        return precedenceRanks
            .Distinct()
            .OrderBy(static rank => rank)
            .ToArray();
    }

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

    private static string[] NormalizeSortedList(IReadOnlyList<string>? values)
    {
        return values?
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }
}
