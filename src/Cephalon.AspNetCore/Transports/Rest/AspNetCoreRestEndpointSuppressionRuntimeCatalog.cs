using Cephalon.Abstractions.Transports;
using Cephalon.AspNetCore.Hosting;

namespace Cephalon.AspNetCore.Transports.Rest;

internal sealed class AspNetCoreRestEndpointSuppressionRuntimeCatalog(
    IRestEndpointCandidateRuntimeCatalog candidateRuntimeCatalog,
    RestApiGovernanceOptions options) : IRestEndpointSuppressionRuntimeCatalog
{
    private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;

    private readonly object sync = new();
    private CatalogState? state;

    public IReadOnlyList<RestEndpointSuppressionDescriptor> Suppressions => GetState().Suppressions;

    public RestEndpointSuppressionDescriptor? GetById(string suppressionId)
    {
        if (string.IsNullOrWhiteSpace(suppressionId))
        {
            return null;
        }

        return GetState().SuppressionsById.TryGetValue(suppressionId.Trim(), out var suppression)
            ? suppression
            : null;
    }

    public IReadOnlyList<RestEndpointSuppressionDescriptor> GetBySourceModule(string sourceModuleId)
    {
        if (string.IsNullOrWhiteSpace(sourceModuleId))
        {
            return [];
        }

        return GetState().SuppressionsBySourceModule.TryGetValue(sourceModuleId.Trim(), out var matches)
            ? matches
            : [];
    }

    public IReadOnlyList<RestEndpointSuppressionDescriptor> GetByBehaviorId(string behaviorId)
    {
        if (string.IsNullOrWhiteSpace(behaviorId))
        {
            return [];
        }

        return GetState().SuppressionsByBehaviorId.TryGetValue(behaviorId.Trim(), out var matches)
            ? matches
            : [];
    }

    private CatalogState GetState()
    {
        var currentCandidates = candidateRuntimeCatalog.Candidates;
        var currentState = state;
        if (currentState is not null &&
            ReferenceEquals(currentState.Candidates, currentCandidates))
        {
            return currentState;
        }

        lock (sync)
        {
            currentState = state;
            if (currentState is not null &&
                ReferenceEquals(currentState.Candidates, currentCandidates))
            {
                return currentState;
            }

            currentState = BuildState(currentCandidates, options);
            state = currentState;
            return currentState;
        }
    }

    private static CatalogState BuildState(
        IReadOnlyList<RestEndpointCandidateRuntimeDescriptor> candidates,
        RestApiGovernanceOptions options)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(options);

        var matchedCandidateIdsByRule = new Dictionary<string, List<string>>(Comparer);
        var suppressedCandidateIdsByRule = new Dictionary<string, List<string>>(Comparer);
        var skippedCandidateIdsByRule = new Dictionary<string, List<string>>(Comparer);
        var suppressedCandidatesByRule = new Dictionary<string, List<RestEndpointCandidateRuntimeDescriptor>>(Comparer);

        foreach (var candidate in candidates)
        {
            foreach (var suppressionId in candidate.MatchedSuppressionIds)
            {
                AddDistinctStringValue(matchedCandidateIdsByRule, suppressionId, candidate.Id);
            }

            if (!string.IsNullOrWhiteSpace(candidate.SuppressedBySuppressionId))
            {
                AddDistinctStringValue(matchedCandidateIdsByRule, candidate.SuppressedBySuppressionId, candidate.Id);
                AddDistinctStringValue(suppressedCandidateIdsByRule, candidate.SuppressedBySuppressionId, candidate.Id);
                AddDistinctCandidateValue(suppressedCandidatesByRule, candidate.SuppressedBySuppressionId, candidate);
            }

            foreach (var suppressionId in candidate.SkippedSuppressionIds)
            {
                AddDistinctStringValue(skippedCandidateIdsByRule, suppressionId, candidate.Id);
            }
        }

        var suppressions = options.Suppressions
            .Select(suppression =>
            {
                var selectionBasisSummaries = BuildSelectionBasisSummaries(
                    GetCandidateValues(suppressedCandidatesByRule, suppression.Id),
                    static candidate => candidate.SuppressionSelectionBasis);

                return new RestEndpointSuppressionDescriptor(
                    suppression.Id,
                    suppression.CandidateIds,
                    suppression.BehaviorIds,
                    suppression.SourceModuleIds,
                    suppression.AuthoringStyles,
                    suppression.ApiVersionMajors,
                    suppression.Methods,
                    suppression.RelativePatterns,
                    suppression.RouteGroupPrefixes,
                    suppression.OpenApiDocumentNames,
                    suppression.TagNames,
                    suppression.EndpointNames,
                    suppression.BindingFallbackModes,
                    suppression.TargetBindings,
                    GetStringValues(matchedCandidateIdsByRule, suppression.Id),
                    GetStringValues(suppressedCandidateIdsByRule, suppression.Id),
                    GetStringValues(skippedCandidateIdsByRule, suppression.Id),
                    selectionBasisSummaries.Select(static summary => summary.SelectionBasis).ToArray(),
                    selectionBasisSummaries,
                    hostGovernanceScopes: suppression.HostGovernanceScopes);
            })
            .OrderBy(static suppression => suppression.Id, Comparer)
            .ToArray();

        var suppressionsById = suppressions.ToDictionary(static suppression => suppression.Id, Comparer);
        var suppressionsBySourceModule = suppressions
            .Where(static suppression => suppression.SourceModuleIds.Count > 0)
            .SelectMany(static suppression => suppression.SourceModuleIds.Select(sourceModuleId => new KeyValuePair<string, RestEndpointSuppressionDescriptor>(sourceModuleId, suppression)))
            .GroupBy(static pair => pair.Key, Comparer)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<RestEndpointSuppressionDescriptor>)group.Select(static pair => pair.Value).ToArray(),
                Comparer);
        var suppressionsByBehaviorId = suppressions
            .Where(static suppression => suppression.BehaviorIds.Count > 0)
            .SelectMany(static suppression => suppression.BehaviorIds.Select(behaviorId => new KeyValuePair<string, RestEndpointSuppressionDescriptor>(behaviorId, suppression)))
            .GroupBy(static pair => pair.Key, Comparer)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<RestEndpointSuppressionDescriptor>)group.Select(static pair => pair.Value).ToArray(),
                Comparer);

        return new CatalogState(
            candidates,
            suppressions,
            suppressionsById,
            suppressionsBySourceModule,
            suppressionsByBehaviorId);
    }

    private static void AddDistinctStringValue(
        Dictionary<string, List<string>> valuesByRuleId,
        string? ruleId,
        string candidateId)
    {
        if (string.IsNullOrWhiteSpace(ruleId))
        {
            return;
        }

        var trimmedRuleId = ruleId.Trim();
        if (!valuesByRuleId.TryGetValue(trimmedRuleId, out var values))
        {
            values = [];
            valuesByRuleId[trimmedRuleId] = values;
        }

        if (!values.Contains(candidateId, Comparer))
        {
            values.Add(candidateId);
        }
    }

    private static void AddDistinctCandidateValue(
        Dictionary<string, List<RestEndpointCandidateRuntimeDescriptor>> valuesByRuleId,
        string? ruleId,
        RestEndpointCandidateRuntimeDescriptor candidate)
    {
        if (string.IsNullOrWhiteSpace(ruleId))
        {
            return;
        }

        var trimmedRuleId = ruleId.Trim();
        if (!valuesByRuleId.TryGetValue(trimmedRuleId, out var values))
        {
            values = [];
            valuesByRuleId[trimmedRuleId] = values;
        }

        if (!values.Any(existing => string.Equals(existing.Id, candidate.Id, StringComparison.OrdinalIgnoreCase)))
        {
            values.Add(candidate);
        }
    }

    private static List<string> GetStringValues(
        Dictionary<string, List<string>> valuesByRuleId,
        string ruleId)
    {
        return valuesByRuleId.TryGetValue(ruleId, out var values)
            ? values
            : [];
    }

    private static List<RestEndpointCandidateRuntimeDescriptor> GetCandidateValues(
        Dictionary<string, List<RestEndpointCandidateRuntimeDescriptor>> valuesByRuleId,
        string ruleId)
    {
        return valuesByRuleId.TryGetValue(ruleId, out var values)
            ? values
            : [];
    }

    private static RestEndpointGovernanceSelectionBasisSummaryDescriptor[] BuildSelectionBasisSummaries(
        IReadOnlyList<RestEndpointCandidateRuntimeDescriptor> candidates,
        Func<RestEndpointCandidateRuntimeDescriptor, RestEndpointGovernanceRuleSelectionBasis?> selector)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(selector);

        var candidateIdsBySelectionBasis =
            new Dictionary<RestEndpointGovernanceRuleSelectionBasis, List<string>>();
        foreach (var candidate in candidates)
        {
            var selectionBasis = selector(candidate);
            if (!selectionBasis.HasValue)
            {
                continue;
            }

            if (!candidateIdsBySelectionBasis.TryGetValue(selectionBasis.Value, out var candidateIds))
            {
                candidateIds = [];
                candidateIdsBySelectionBasis[selectionBasis.Value] = candidateIds;
            }

            if (!candidateIds.Contains(candidate.Id, Comparer))
            {
                candidateIds.Add(candidate.Id);
            }
        }

        return candidateIdsBySelectionBasis
            .OrderBy(static pair => pair.Key)
            .Select(pair => new RestEndpointGovernanceSelectionBasisSummaryDescriptor(pair.Key, pair.Value))
            .ToArray();
    }

    private sealed class CatalogState(
        IReadOnlyList<RestEndpointCandidateRuntimeDescriptor> candidates,
        IReadOnlyList<RestEndpointSuppressionDescriptor> suppressions,
        IReadOnlyDictionary<string, RestEndpointSuppressionDescriptor> suppressionsById,
        IReadOnlyDictionary<string, IReadOnlyList<RestEndpointSuppressionDescriptor>> suppressionsBySourceModule,
        IReadOnlyDictionary<string, IReadOnlyList<RestEndpointSuppressionDescriptor>> suppressionsByBehaviorId)
    {
        public IReadOnlyList<RestEndpointCandidateRuntimeDescriptor> Candidates { get; } = candidates;

        public IReadOnlyList<RestEndpointSuppressionDescriptor> Suppressions { get; } = suppressions;

        public IReadOnlyDictionary<string, RestEndpointSuppressionDescriptor> SuppressionsById { get; } = suppressionsById;

        public IReadOnlyDictionary<string, IReadOnlyList<RestEndpointSuppressionDescriptor>> SuppressionsBySourceModule { get; } = suppressionsBySourceModule;

        public IReadOnlyDictionary<string, IReadOnlyList<RestEndpointSuppressionDescriptor>> SuppressionsByBehaviorId { get; } = suppressionsByBehaviorId;
    }
}
