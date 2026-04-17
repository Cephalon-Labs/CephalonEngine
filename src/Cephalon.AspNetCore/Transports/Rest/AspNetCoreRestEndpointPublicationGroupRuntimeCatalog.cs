using Cephalon.AspNetCore.Hosting;
using Cephalon.Abstractions.Transports;

namespace Cephalon.AspNetCore.Transports.Rest;

internal sealed class AspNetCoreRestEndpointPublicationGroupRuntimeCatalog(
    IRestEndpointCandidateRuntimeCatalog candidateRuntimeCatalog,
    RestApiGovernanceOptions governanceOptions) : IRestEndpointPublicationGroupRuntimeCatalog
{
    private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;

    public IReadOnlyList<RestEndpointPublicationGroupDescriptor> Groups => BuildGroups(
        candidateRuntimeCatalog.Candidates,
        governanceOptions.AuthoringPolicies);

    public RestEndpointPublicationGroupDescriptor? GetByBehaviorId(string behaviorId)
    {
        if (string.IsNullOrWhiteSpace(behaviorId))
        {
            return null;
        }

        return Groups.FirstOrDefault(group =>
            string.Equals(group.BehaviorId, behaviorId.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static RestEndpointPublicationGroupDescriptor[] BuildGroups(
        IReadOnlyList<RestEndpointCandidateRuntimeDescriptor> candidates,
        IReadOnlyList<RestEndpointPublicationGroupAuthoringPolicyDescriptor> authoringPolicies)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(authoringPolicies);

        var authoringPoliciesByBehaviorId = authoringPolicies
            .ToDictionary(static policy => policy.BehaviorId, Comparer);

        return candidates
            .Where(static candidate => !string.IsNullOrWhiteSpace(candidate.ProjectedEndpoint.BehaviorId))
            .GroupBy(static candidate => candidate.ProjectedEndpoint.BehaviorId!, Comparer)
            .Select(group => CreateGroupDescriptor(group.Key, group, authoringPoliciesByBehaviorId))
            .OrderBy(static group => group.BehaviorId, Comparer)
            .ToArray();
    }

    private static RestEndpointPublicationGroupDescriptor CreateGroupDescriptor(
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
        var sourceModuleIds = orderedCandidates
            .Select(static candidate => candidate.ProjectedEndpoint.SourceModuleId)
            .Where(static sourceModuleId => !string.IsNullOrWhiteSpace(sourceModuleId))
            .Select(static sourceModuleId => sourceModuleId!.Trim())
            .Distinct(Comparer)
            .OrderBy(static sourceModuleId => sourceModuleId, Comparer)
            .ToArray();
        var winningPrecedenceRank = orderedCandidates
            .Where(static candidate => candidate.Status == RestEndpointCandidateStatus.Published)
            .Select(static candidate => (int?)candidate.PrecedenceRank)
            .Min();
        var authoringPolicy = ResolveAuthoringPolicy(behaviorId, authoringPoliciesByBehaviorId);

        return new RestEndpointPublicationGroupDescriptor(
            behaviorId,
            sourceModuleIds,
            winningPrecedenceRank,
            publishedCandidateIds,
            precedenceSuppressedCandidateIds,
            governanceSuppressedCandidateIds,
            orderedCandidates,
            authoringPolicy,
            authoringPolicySuppressedCandidateIds);
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
}
