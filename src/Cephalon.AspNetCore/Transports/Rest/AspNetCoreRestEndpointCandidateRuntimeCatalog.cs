using Cephalon.Abstractions.Transports;

namespace Cephalon.AspNetCore.Transports.Rest;

internal sealed class AspNetCoreRestEndpointCandidateRuntimeCatalog : IRestEndpointCandidateRuntimeCatalog, IRestEndpointCandidateRuntimeRegistry
{
    private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;

    private RestEndpointCandidateRuntimeDescriptor[] candidates = [];
    private Dictionary<string, RestEndpointCandidateRuntimeDescriptor> candidatesById = new(Comparer);
    private Dictionary<string, IReadOnlyList<RestEndpointCandidateRuntimeDescriptor>> candidatesBySourceModule = new(Comparer);
    private Dictionary<string, IReadOnlyList<RestEndpointCandidateRuntimeDescriptor>> candidatesByBehaviorId = new(Comparer);

    public IReadOnlyList<RestEndpointCandidateRuntimeDescriptor> Candidates => candidates;

    public RestEndpointCandidateRuntimeDescriptor? GetById(string candidateId)
    {
        if (string.IsNullOrWhiteSpace(candidateId))
        {
            return null;
        }

        return candidatesById.TryGetValue(candidateId.Trim(), out var candidate)
            ? candidate
            : null;
    }

    public IReadOnlyList<RestEndpointCandidateRuntimeDescriptor> GetBySourceModule(string sourceModuleId)
    {
        if (string.IsNullOrWhiteSpace(sourceModuleId))
        {
            return [];
        }

        return candidatesBySourceModule.TryGetValue(sourceModuleId.Trim(), out var matches)
            ? matches
            : [];
    }

    public IReadOnlyList<RestEndpointCandidateRuntimeDescriptor> GetByBehaviorId(string behaviorId)
    {
        if (string.IsNullOrWhiteSpace(behaviorId))
        {
            return [];
        }

        return candidatesByBehaviorId.TryGetValue(behaviorId.Trim(), out var matches)
            ? matches
            : [];
    }

    public void Clear()
    {
        candidates = [];
        candidatesById = new Dictionary<string, RestEndpointCandidateRuntimeDescriptor>(Comparer);
        candidatesBySourceModule = new Dictionary<string, IReadOnlyList<RestEndpointCandidateRuntimeDescriptor>>(Comparer);
        candidatesByBehaviorId = new Dictionary<string, IReadOnlyList<RestEndpointCandidateRuntimeDescriptor>>(Comparer);
    }

    public void Register(RestEndpointCandidateRuntimeDescriptor candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        if (candidatesById.ContainsKey(candidate.Id))
        {
            throw new InvalidOperationException(
                $"Resolved REST endpoint candidate '{candidate.Id}' was registered more than once.");
        }

        var nextCandidates = candidates
            .Append(candidate)
            .OrderBy(static item => item.ProjectedEndpoint.RoutePattern, Comparer)
            .ThenBy(static item => item.ProjectedEndpoint.Method, Comparer)
            .ThenBy(static item => item.Id, Comparer)
            .ToArray();

        candidates = nextCandidates;
        candidatesById = nextCandidates.ToDictionary(static item => item.Id, Comparer);
        candidatesBySourceModule = nextCandidates
            .Where(static item => !string.IsNullOrWhiteSpace(item.ProjectedEndpoint.SourceModuleId))
            .GroupBy(static item => item.ProjectedEndpoint.SourceModuleId!, Comparer)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<RestEndpointCandidateRuntimeDescriptor>)group.ToArray(),
                Comparer);
        candidatesByBehaviorId = nextCandidates
            .Where(static item => !string.IsNullOrWhiteSpace(item.ProjectedEndpoint.BehaviorId))
            .GroupBy(static item => item.ProjectedEndpoint.BehaviorId!, Comparer)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<RestEndpointCandidateRuntimeDescriptor>)group.ToArray(),
                Comparer);
    }
}
