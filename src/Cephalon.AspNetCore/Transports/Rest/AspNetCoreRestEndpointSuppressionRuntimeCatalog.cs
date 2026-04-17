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
        var selectionBasesByRule = new Dictionary<string, List<RestEndpointGovernanceRuleSelectionBasis>>(Comparer);

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

                if (candidate.SuppressionSelectionBasis.HasValue)
                {
                    AddDistinctEnumValue(
                        selectionBasesByRule,
                        candidate.SuppressedBySuppressionId,
                        candidate.SuppressionSelectionBasis.Value);
                }
            }

            foreach (var suppressionId in candidate.SkippedSuppressionIds)
            {
                AddDistinctStringValue(skippedCandidateIdsByRule, suppressionId, candidate.Id);
            }
        }

        var suppressions = options.Suppressions
            .Select(suppression => new RestEndpointSuppressionDescriptor(
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
                suppression.BindingFallbackModes,
                suppression.TargetBindings,
                GetStringValues(matchedCandidateIdsByRule, suppression.Id),
                GetStringValues(suppressedCandidateIdsByRule, suppression.Id),
                GetStringValues(skippedCandidateIdsByRule, suppression.Id),
                GetEnumValues(selectionBasesByRule, suppression.Id)))
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

    private static void AddDistinctEnumValue<TEnum>(
        Dictionary<string, List<TEnum>> valuesByRuleId,
        string? ruleId,
        TEnum value)
        where TEnum : struct, Enum
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

        if (!values.Contains(value))
        {
            values.Add(value);
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

    private static List<TEnum> GetEnumValues<TEnum>(
        Dictionary<string, List<TEnum>> valuesByRuleId,
        string ruleId)
        where TEnum : struct, Enum
    {
        return valuesByRuleId.TryGetValue(ruleId, out var values)
            ? values
            : [];
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
