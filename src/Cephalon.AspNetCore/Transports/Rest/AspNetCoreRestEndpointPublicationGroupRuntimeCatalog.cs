using Cephalon.Abstractions.Transports;

namespace Cephalon.AspNetCore.Transports.Rest;

internal sealed class AspNetCoreRestEndpointPublicationGroupRuntimeCatalog(
    IRestEndpointCandidateRuntimeCatalog candidateRuntimeCatalog) : IRestEndpointPublicationGroupRuntimeCatalog
{
    private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;

    public IReadOnlyList<RestEndpointPublicationGroupDescriptor> Groups => BuildGroups(candidateRuntimeCatalog.Candidates);

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
        IReadOnlyList<RestEndpointCandidateRuntimeDescriptor> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        return candidates
            .Where(static candidate => !string.IsNullOrWhiteSpace(candidate.ProjectedEndpoint.BehaviorId))
            .GroupBy(static candidate => candidate.ProjectedEndpoint.BehaviorId!, Comparer)
            .Select(static group => CreateGroupDescriptor(group.Key, group))
            .OrderBy(static group => group.BehaviorId, Comparer)
            .ToArray();
    }

    private static RestEndpointPublicationGroupDescriptor CreateGroupDescriptor(
        string behaviorId,
        IEnumerable<RestEndpointCandidateRuntimeDescriptor> candidates)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(behaviorId);
        ArgumentNullException.ThrowIfNull(candidates);

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

        return new RestEndpointPublicationGroupDescriptor(
            behaviorId,
            sourceModuleIds,
            winningPrecedenceRank,
            publishedCandidateIds,
            precedenceSuppressedCandidateIds,
            governanceSuppressedCandidateIds,
            orderedCandidates);
    }
}
