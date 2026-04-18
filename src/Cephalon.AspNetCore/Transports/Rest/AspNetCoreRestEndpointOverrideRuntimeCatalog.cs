using Cephalon.Abstractions.Transports;
using Cephalon.AspNetCore.Hosting;

namespace Cephalon.AspNetCore.Transports.Rest;

internal sealed class AspNetCoreRestEndpointOverrideRuntimeCatalog(
    IRestEndpointCandidateRuntimeCatalog candidateRuntimeCatalog,
    RestApiGovernanceOptions options) : IRestEndpointOverrideRuntimeCatalog
{
    private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;

    private readonly object sync = new();
    private CatalogState? state;

    public IReadOnlyList<RestEndpointOverrideDescriptor> OverrideRules => GetState().OverrideRules;

    public RestEndpointOverrideDescriptor? GetById(string overrideId)
    {
        if (string.IsNullOrWhiteSpace(overrideId))
        {
            return null;
        }

        return GetState().OverridesById.TryGetValue(overrideId.Trim(), out var item)
            ? item
            : null;
    }

    public IReadOnlyList<RestEndpointOverrideDescriptor> GetBySourceModule(string sourceModuleId)
    {
        if (string.IsNullOrWhiteSpace(sourceModuleId))
        {
            return [];
        }

        return GetState().OverridesBySourceModule.TryGetValue(sourceModuleId.Trim(), out var matches)
            ? matches
            : [];
    }

    public IReadOnlyList<RestEndpointOverrideDescriptor> GetByBehaviorId(string behaviorId)
    {
        if (string.IsNullOrWhiteSpace(behaviorId))
        {
            return [];
        }

        return GetState().OverridesByBehaviorId.TryGetValue(behaviorId.Trim(), out var matches)
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
        var selectedCandidateIdsByRule = new Dictionary<string, List<string>>(Comparer);
        var appliedCandidateIdsByRule = new Dictionary<string, List<string>>(Comparer);
        var skippedCandidateIdsByRule = new Dictionary<string, List<string>>(Comparer);
        var selectedCandidatesByRule = new Dictionary<string, List<RestEndpointCandidateRuntimeDescriptor>>(Comparer);
        var appliedCandidatesByRule = new Dictionary<string, List<RestEndpointCandidateRuntimeDescriptor>>(Comparer);

        foreach (var candidate in candidates)
        {
            foreach (var overrideId in candidate.MatchedOverrideIds)
            {
                AddDistinctStringValue(matchedCandidateIdsByRule, overrideId, candidate.Id);
            }

            if (!string.IsNullOrWhiteSpace(candidate.SelectedOverrideId))
            {
                AddDistinctStringValue(matchedCandidateIdsByRule, candidate.SelectedOverrideId, candidate.Id);
                AddDistinctStringValue(selectedCandidateIdsByRule, candidate.SelectedOverrideId, candidate.Id);
                AddDistinctCandidateValue(selectedCandidatesByRule, candidate.SelectedOverrideId, candidate);
            }

            if (!string.IsNullOrWhiteSpace(candidate.AppliedOverrideId))
            {
                AddDistinctStringValue(matchedCandidateIdsByRule, candidate.AppliedOverrideId, candidate.Id);
                AddDistinctStringValue(selectedCandidateIdsByRule, candidate.AppliedOverrideId, candidate.Id);
                AddDistinctStringValue(appliedCandidateIdsByRule, candidate.AppliedOverrideId, candidate.Id);
                AddDistinctCandidateValue(appliedCandidatesByRule, candidate.AppliedOverrideId, candidate);
            }

            foreach (var overrideId in candidate.SkippedOverrideIds)
            {
                AddDistinctStringValue(skippedCandidateIdsByRule, overrideId, candidate.Id);
            }
        }

        var overrideRules = options.Overrides
            .Select(item =>
            {
                var selectionBasisSummaries = BuildSelectionBasisSummaries(
                    GetCandidateValues(selectedCandidatesByRule, item.Id),
                    static candidate => candidate.OverrideSelectionBasis);
                var selectedActionKindSummaries = BuildActionKindSummaries(
                    GetCandidateValues(selectedCandidatesByRule, item.Id),
                    static candidate => candidate.SelectedOverrideActionKinds);
                var appliedActionKindSummaries = BuildActionKindSummaries(
                    GetCandidateValues(appliedCandidatesByRule, item.Id),
                    static candidate => candidate.AppliedOverrideActionKinds);

                return new RestEndpointOverrideDescriptor(
                    item.Id,
                    item.CandidateIds,
                    item.BehaviorIds,
                    item.SourceModuleIds,
                    item.AuthoringStyles,
                    item.ApiVersionMajors,
                    item.Methods,
                    item.RelativePatterns,
                    item.RouteGroupPrefixes,
                    item.ApiVersionMajor,
                    item.Method,
                    item.Pattern,
                    item.RouteGroupPrefix,
                    item.OpenApiDocumentName,
                    item.TagName,
                    item.EndpointName,
                    item.Summary,
                    item.Description,
                    item.RequiredCapabilityKey,
                    item.ClearRequiredCapability,
                    item.Bindings,
                    item.RemovedBindingProperties,
                    item.ClearBindings,
                    item.BindingMode,
                    item.ClearEndpointName,
                    item.ClearSummary,
                    item.ClearDescription,
                    item.OpenApiDocumentNames,
                    item.TagNames,
                    item.EndpointNames,
                    item.BindingFallbackModes,
                    item.TargetBindings,
                    GetStringValues(matchedCandidateIdsByRule, item.Id),
                    GetStringValues(selectedCandidateIdsByRule, item.Id),
                    GetStringValues(appliedCandidateIdsByRule, item.Id),
                    GetStringValues(skippedCandidateIdsByRule, item.Id),
                    selectionBasisSummaries.Select(static summary => summary.SelectionBasis).ToArray(),
                    selectedActionKindSummaries.Select(static summary => summary.ActionKind).ToArray(),
                    appliedActionKindSummaries.Select(static summary => summary.ActionKind).ToArray(),
                    selectionBasisSummaries,
                    selectedActionKindSummaries,
                    appliedActionKindSummaries,
                    hostGovernanceScopes: item.HostGovernanceScopes);
            })
            .OrderBy(static item => item.Id, Comparer)
            .ToArray();

        var overridesById = overrideRules.ToDictionary(static item => item.Id, Comparer);
        var overridesBySourceModule = overrideRules
            .Where(static item => item.SourceModuleIds.Count > 0)
            .SelectMany(static item => item.SourceModuleIds.Select(sourceModuleId => new KeyValuePair<string, RestEndpointOverrideDescriptor>(sourceModuleId, item)))
            .GroupBy(static pair => pair.Key, Comparer)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<RestEndpointOverrideDescriptor>)group.Select(static pair => pair.Value).ToArray(),
                Comparer);
        var overridesByBehaviorId = overrideRules
            .Where(static item => item.BehaviorIds.Count > 0)
            .SelectMany(static item => item.BehaviorIds.Select(behaviorId => new KeyValuePair<string, RestEndpointOverrideDescriptor>(behaviorId, item)))
            .GroupBy(static pair => pair.Key, Comparer)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<RestEndpointOverrideDescriptor>)group.Select(static pair => pair.Value).ToArray(),
                Comparer);

        return new CatalogState(
            candidates,
            overrideRules,
            overridesById,
            overridesBySourceModule,
            overridesByBehaviorId);
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

    private static RestEndpointGovernanceOverrideActionKindSummaryDescriptor[] BuildActionKindSummaries(
        IReadOnlyList<RestEndpointCandidateRuntimeDescriptor> candidates,
        Func<RestEndpointCandidateRuntimeDescriptor, IReadOnlyList<RestEndpointOverrideActionKind>> selector)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(selector);

        var candidateIdsByActionKind =
            new Dictionary<RestEndpointOverrideActionKind, List<string>>();
        foreach (var candidate in candidates)
        {
            foreach (var actionKind in selector(candidate))
            {
                if (!candidateIdsByActionKind.TryGetValue(actionKind, out var candidateIds))
                {
                    candidateIds = [];
                    candidateIdsByActionKind[actionKind] = candidateIds;
                }

                if (!candidateIds.Contains(candidate.Id, Comparer))
                {
                    candidateIds.Add(candidate.Id);
                }
            }
        }

        return candidateIdsByActionKind
            .OrderBy(static pair => pair.Key)
            .Select(pair => new RestEndpointGovernanceOverrideActionKindSummaryDescriptor(pair.Key, pair.Value))
            .ToArray();
    }

    private sealed class CatalogState(
        IReadOnlyList<RestEndpointCandidateRuntimeDescriptor> candidates,
        IReadOnlyList<RestEndpointOverrideDescriptor> overrideRules,
        IReadOnlyDictionary<string, RestEndpointOverrideDescriptor> overridesById,
        IReadOnlyDictionary<string, IReadOnlyList<RestEndpointOverrideDescriptor>> overridesBySourceModule,
        IReadOnlyDictionary<string, IReadOnlyList<RestEndpointOverrideDescriptor>> overridesByBehaviorId)
    {
        public IReadOnlyList<RestEndpointCandidateRuntimeDescriptor> Candidates { get; } = candidates;

        public IReadOnlyList<RestEndpointOverrideDescriptor> OverrideRules { get; } = overrideRules;

        public IReadOnlyDictionary<string, RestEndpointOverrideDescriptor> OverridesById { get; } = overridesById;

        public IReadOnlyDictionary<string, IReadOnlyList<RestEndpointOverrideDescriptor>> OverridesBySourceModule { get; } = overridesBySourceModule;

        public IReadOnlyDictionary<string, IReadOnlyList<RestEndpointOverrideDescriptor>> OverridesByBehaviorId { get; } = overridesByBehaviorId;
    }
}
