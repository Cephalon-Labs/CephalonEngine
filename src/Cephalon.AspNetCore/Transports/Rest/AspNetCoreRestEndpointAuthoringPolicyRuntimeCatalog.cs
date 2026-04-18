using Cephalon.AspNetCore.Hosting;
using Cephalon.Abstractions.Transports;

namespace Cephalon.AspNetCore.Transports.Rest;

internal sealed class AspNetCoreRestEndpointAuthoringPolicyRuntimeCatalog(
    IRestEndpointCandidateRuntimeCatalog candidateRuntimeCatalog,
    RestApiGovernanceOptions governanceOptions) : IRestEndpointAuthoringPolicyRuntimeCatalog
{
    private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;

    public IReadOnlyList<RestEndpointAuthoringPolicyDescriptor> Policies => BuildPolicies(
        candidateRuntimeCatalog.Candidates,
        governanceOptions.AuthoringPolicies);

    public RestEndpointAuthoringPolicyDescriptor? GetByBehaviorId(string behaviorId)
    {
        if (string.IsNullOrWhiteSpace(behaviorId))
        {
            return null;
        }

        return Policies.FirstOrDefault(policy =>
            string.Equals(policy.BehaviorId, behaviorId.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static RestEndpointAuthoringPolicyDescriptor[] BuildPolicies(
        IReadOnlyList<RestEndpointCandidateRuntimeDescriptor> candidates,
        IReadOnlyList<RestEndpointPublicationGroupAuthoringPolicyDescriptor> authoringPolicies)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(authoringPolicies);

        var authoringPoliciesByBehaviorId = authoringPolicies
            .ToDictionary(static policy => policy.BehaviorId, Comparer);
        var behaviorIds = candidates
            .Where(static candidate => !string.IsNullOrWhiteSpace(candidate.ProjectedEndpoint.BehaviorId))
            .Select(static candidate => candidate.ProjectedEndpoint.BehaviorId!.Trim())
            .Concat(authoringPolicies.Select(static policy => policy.BehaviorId))
            .Distinct(Comparer)
            .OrderBy(static behaviorId => behaviorId, Comparer)
            .ToArray();

        return behaviorIds
            .Select(behaviorId => CreateDescriptor(
                behaviorId,
                candidates.Where(candidate =>
                    string.Equals(candidate.ProjectedEndpoint.BehaviorId, behaviorId, StringComparison.OrdinalIgnoreCase)),
                authoringPoliciesByBehaviorId))
            .ToArray();
    }

    private static RestEndpointAuthoringPolicyDescriptor CreateDescriptor(
        string behaviorId,
        IEnumerable<RestEndpointCandidateRuntimeDescriptor> candidates,
        IReadOnlyDictionary<string, RestEndpointPublicationGroupAuthoringPolicyDescriptor> authoringPoliciesByBehaviorId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(behaviorId);
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(authoringPoliciesByBehaviorId);

        var orderedCandidates = candidates
            .OrderBy(static candidate => candidate.PrecedenceRank)
            .ThenBy(static candidate => candidate.Status == RestEndpointCandidateStatus.Published ? 0 : 1)
            .ThenBy(static candidate => candidate.AuthoringStyle, Comparer)
            .ThenBy(static candidate => candidate.ProjectedEndpoint.RoutePattern, Comparer)
            .ThenBy(static candidate => candidate.ProjectedEndpoint.Method, Comparer)
            .ThenBy(static candidate => candidate.Id, Comparer)
            .ToArray();
        var candidateIds = orderedCandidates
            .Select(static candidate => candidate.Id)
            .ToArray();
        var retainedCandidateIds = orderedCandidates
            .Where(static candidate => candidate.SuppressedByAuthoringPolicyKind is null)
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
        var suppressedCandidateIds = orderedCandidates
            .Where(static candidate =>
                candidate.Status == RestEndpointCandidateStatus.Suppressed &&
                candidate.SuppressedByAuthoringPolicyKind.HasValue)
            .Select(static candidate => candidate.Id)
            .ToArray();
        var suppressionKinds = orderedCandidates
            .Where(static candidate => candidate.SuppressedByAuthoringPolicyKind.HasValue)
            .Select(static candidate => candidate.SuppressedByAuthoringPolicyKind!.Value)
            .Distinct()
            .OrderBy(static kind => kind.GetWireName(), StringComparer.Ordinal)
            .ToArray();
        var suppressionSummaries = BuildSuppressionSummaries(orderedCandidates);
        var authoringStyleSummaries = BuildAuthoringStyleSummaries(orderedCandidates);
        var authoringPolicy = ResolveAuthoringPolicy(behaviorId, authoringPoliciesByBehaviorId);

        return new RestEndpointAuthoringPolicyDescriptor(
            authoringPolicy.BehaviorId,
            authoringPolicy.IsConfigured,
            authoringPolicy.AllowMultiplePublishedCandidates,
            authoringPolicy.PreferredAuthoringStyle,
            authoringPolicy.AllowedAuthoringStyles,
            authoringPolicy.DisallowedAuthoringStyles,
            candidateIds,
            retainedCandidateIds,
            publishedCandidateIds,
            precedenceSuppressedCandidateIds,
            governanceSuppressedCandidateIds,
            suppressedCandidateIds,
            suppressionKinds,
            suppressionSummaries,
            hostGovernanceEligibleCandidateIds,
            hostGovernanceIneligibleCandidateIds,
            skippedSuppressionIds,
            skippedOverrideIds,
            authoringStyleSummaries);
    }

    private static RestEndpointPublicationGroupAuthoringPolicyDescriptor ResolveAuthoringPolicy(
        string behaviorId,
        IReadOnlyDictionary<string, RestEndpointPublicationGroupAuthoringPolicyDescriptor> authoringPoliciesByBehaviorId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(behaviorId);
        ArgumentNullException.ThrowIfNull(authoringPoliciesByBehaviorId);

        if (!authoringPoliciesByBehaviorId.TryGetValue(behaviorId.Trim(), out var authoringPolicy))
        {
            return new RestEndpointPublicationGroupAuthoringPolicyDescriptor(behaviorId);
        }

        return new RestEndpointPublicationGroupAuthoringPolicyDescriptor(
            authoringPolicy.BehaviorId,
            authoringPolicy.IsConfigured,
            authoringPolicy.AllowMultiplePublishedCandidates,
            authoringPolicy.PreferredAuthoringStyle,
            authoringPolicy.AllowedAuthoringStyles,
            authoringPolicy.DisallowedAuthoringStyles);
    }

    private static RestEndpointAuthoringPolicySuppressionSummaryDescriptor[] BuildSuppressionSummaries(
        IReadOnlyList<RestEndpointCandidateRuntimeDescriptor> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        var candidateIdsByKind = new Dictionary<RestEndpointAuthoringPolicySuppressionKind, List<string>>();
        foreach (var candidate in candidates)
        {
            if (candidate.SuppressedByAuthoringPolicyKind is not { } kind)
            {
                continue;
            }

            if (!candidateIdsByKind.TryGetValue(kind, out var candidateIds))
            {
                candidateIds = [];
                candidateIdsByKind[kind] = candidateIds;
            }

            candidateIds.Add(candidate.Id);
        }

        return candidateIdsByKind
            .OrderBy(static pair => pair.Key.GetWireName(), StringComparer.Ordinal)
            .Select(static pair => new RestEndpointAuthoringPolicySuppressionSummaryDescriptor(pair.Key, pair.Value))
            .ToArray();
    }

    private static RestEndpointAuthoringPolicyAuthoringStyleDescriptor[] BuildAuthoringStyleSummaries(
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
                var suppressionSummaries = BuildSuppressionSummaries(orderedCandidates);
                var suppressionKinds = suppressionSummaries.Length == 0
                    ? []
                    : suppressionSummaries
                        .Select(static summary => summary.Kind)
                        .Distinct()
                        .OrderBy(static kind => kind.GetWireName(), StringComparer.Ordinal)
                        .ToArray();

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
                    suppressionSummaries: suppressionSummaries,
                    hostGovernanceEligibleCandidateIds: BuildHostGovernanceCandidateIds(
                        orderedCandidates,
                        allowsHostGovernance: true),
                    hostGovernanceIneligibleCandidateIds: BuildHostGovernanceCandidateIds(
                        orderedCandidates,
                        allowsHostGovernance: false),
                    skippedSuppressionIds: BuildOrderedSkippedRuleIds(
                        orderedCandidates,
                        static candidate => candidate.SkippedSuppressionIds),
                    skippedOverrideIds: BuildOrderedSkippedRuleIds(
                        orderedCandidates,
                        static candidate => candidate.SkippedOverrideIds));
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
        var seen = new HashSet<string>(Comparer);
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
}
