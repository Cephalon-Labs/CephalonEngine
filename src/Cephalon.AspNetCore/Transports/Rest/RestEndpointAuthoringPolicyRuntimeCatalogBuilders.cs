using Cephalon.Abstractions.Transports;

namespace Cephalon.AspNetCore.Transports.Rest;

internal static class RestEndpointAuthoringPolicyRuntimeCatalogBuilders
{
    private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;

    internal static RestEndpointGovernanceSuppressionSummaryDescriptor[] BuildGovernanceSuppressionSummaries(
        IReadOnlyList<RestEndpointCandidateRuntimeDescriptor> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        var matchedCandidateIdsByRuleId = new Dictionary<string, List<string>>(Comparer);
        var suppressedCandidateIdsByRuleId = new Dictionary<string, List<string>>(Comparer);

        foreach (var candidate in candidates)
        {
            foreach (var ruleId in candidate.MatchedSuppressionIds)
            {
                AddCandidateId(matchedCandidateIdsByRuleId, ruleId, candidate.Id);
            }

            if (!string.IsNullOrWhiteSpace(candidate.SuppressedBySuppressionId))
            {
                AddCandidateId(matchedCandidateIdsByRuleId, candidate.SuppressedBySuppressionId, candidate.Id);
                AddCandidateId(suppressedCandidateIdsByRuleId, candidate.SuppressedBySuppressionId, candidate.Id);
            }
        }

        return matchedCandidateIdsByRuleId
            .OrderBy(static pair => pair.Key, Comparer)
            .Select(pair =>
            {
                var suppressedCandidateIds = suppressedCandidateIdsByRuleId.TryGetValue(pair.Key, out var value)
                    ? value
                    : [];
                var suppressedCandidatesForRule = suppressedCandidateIds.Count == 0
                    ? []
                    : candidates.Where(candidate =>
                            string.Equals(candidate.SuppressedBySuppressionId, pair.Key, StringComparison.OrdinalIgnoreCase))
                        .ToArray();

                return new RestEndpointGovernanceSuppressionSummaryDescriptor(
                    pair.Key,
                    pair.Value,
                    suppressedCandidateIds,
                    BuildSelectionBasisSummaries(
                        suppressedCandidatesForRule,
                        static candidate => candidate.SuppressionSelectionBasis));
            })
            .ToArray();
    }

    internal static RestEndpointGovernanceOverrideSummaryDescriptor[] BuildGovernanceOverrideSummaries(
        IReadOnlyList<RestEndpointCandidateRuntimeDescriptor> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        var matchedCandidateIdsByRuleId = new Dictionary<string, List<string>>(Comparer);
        var selectedCandidateIdsByRuleId = new Dictionary<string, List<string>>(Comparer);
        var appliedCandidateIdsByRuleId = new Dictionary<string, List<string>>(Comparer);

        foreach (var candidate in candidates)
        {
            foreach (var ruleId in candidate.MatchedOverrideIds)
            {
                AddCandidateId(matchedCandidateIdsByRuleId, ruleId, candidate.Id);
            }

            if (!string.IsNullOrWhiteSpace(candidate.SelectedOverrideId))
            {
                AddCandidateId(matchedCandidateIdsByRuleId, candidate.SelectedOverrideId, candidate.Id);
                AddCandidateId(selectedCandidateIdsByRuleId, candidate.SelectedOverrideId, candidate.Id);
            }

            if (!string.IsNullOrWhiteSpace(candidate.AppliedOverrideId))
            {
                AddCandidateId(matchedCandidateIdsByRuleId, candidate.AppliedOverrideId, candidate.Id);
                AddCandidateId(selectedCandidateIdsByRuleId, candidate.AppliedOverrideId, candidate.Id);
                AddCandidateId(appliedCandidateIdsByRuleId, candidate.AppliedOverrideId, candidate.Id);
            }
        }

        return matchedCandidateIdsByRuleId
            .OrderBy(static pair => pair.Key, Comparer)
            .Select(pair =>
            {
                var selectedCandidatesForRule = candidates
                    .Where(candidate => string.Equals(candidate.SelectedOverrideId, pair.Key, StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                var appliedCandidatesForRule = candidates
                    .Where(candidate => string.Equals(candidate.AppliedOverrideId, pair.Key, StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                return new RestEndpointGovernanceOverrideSummaryDescriptor(
                    pair.Key,
                    pair.Value,
                    selectedCandidateIdsByRuleId.TryGetValue(pair.Key, out var selectedCandidateIds)
                        ? selectedCandidateIds
                        : [],
                    appliedCandidateIdsByRuleId.TryGetValue(pair.Key, out var appliedCandidateIds)
                        ? appliedCandidateIds
                        : [],
                    BuildSelectionBasisSummaries(
                        selectedCandidatesForRule,
                        static candidate => candidate.OverrideSelectionBasis),
                    BuildActionKindSummaries(
                        selectedCandidatesForRule,
                        static candidate => candidate.SelectedOverrideActionKinds),
                    BuildActionKindSummaries(
                        appliedCandidatesForRule,
                        static candidate => candidate.AppliedOverrideActionKinds));
            })
            .ToArray();
    }

    internal static RestEndpointGovernanceSkippedSuppressionSummaryDescriptor[] BuildSkippedSuppressionSummaries(
        IReadOnlyList<RestEndpointCandidateRuntimeDescriptor> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        var orderedRuleIds = new List<string>();
        var candidateIdsByRuleId = new Dictionary<string, List<string>>(Comparer);

        foreach (var candidate in candidates)
        {
            foreach (var ruleId in candidate.SkippedSuppressionIds)
            {
                AddCandidateId(candidateIdsByRuleId, orderedRuleIds, ruleId, candidate.Id);
            }
        }

        return orderedRuleIds
            .Select(ruleId => new RestEndpointGovernanceSkippedSuppressionSummaryDescriptor(
                ruleId,
                candidateIdsByRuleId[ruleId]))
            .ToArray();
    }

    internal static RestEndpointGovernanceSkippedOverrideSummaryDescriptor[] BuildSkippedOverrideSummaries(
        IReadOnlyList<RestEndpointCandidateRuntimeDescriptor> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        var orderedRuleIds = new List<string>();
        var candidateIdsByRuleId = new Dictionary<string, List<string>>(Comparer);

        foreach (var candidate in candidates)
        {
            foreach (var ruleId in candidate.SkippedOverrideIds)
            {
                AddCandidateId(candidateIdsByRuleId, orderedRuleIds, ruleId, candidate.Id);
            }
        }

        return orderedRuleIds
            .Select(ruleId => new RestEndpointGovernanceSkippedOverrideSummaryDescriptor(
                ruleId,
                candidateIdsByRuleId[ruleId]))
            .ToArray();
    }

    internal static RestEndpointAuthoringPolicySuppressionSummaryDescriptor[] BuildAuthoringPolicySuppressionSummaries(
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

    internal static RestEndpointAuthoringPolicyAuthoringStyleDescriptor[] BuildAuthoringStyleSummaries(
        IReadOnlyList<RestEndpointCandidateRuntimeDescriptor> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        return candidates
            .GroupBy(static candidate => candidate.AuthoringStyle, Comparer)
            .OrderBy(static group => group.Key, Comparer)
            .Select(group =>
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
                var suppressionSummaries = BuildAuthoringPolicySuppressionSummaries(orderedCandidates);
                var suppressionKinds = suppressionSummaries
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
                    hostGovernanceEligibleCandidateIds: orderedCandidates
                        .Where(static candidate => candidate.OriginalProjection.AllowsHostGovernance)
                        .Select(static candidate => candidate.Id)
                        .ToArray(),
                    hostGovernanceIneligibleCandidateIds: orderedCandidates
                        .Where(static candidate => !candidate.OriginalProjection.AllowsHostGovernance)
                        .Select(static candidate => candidate.Id)
                        .ToArray(),
                    skippedSuppressionIds: BuildOrderedSkippedRuleIds(
                        orderedCandidates,
                        static candidate => candidate.SkippedSuppressionIds),
                    skippedOverrideIds: BuildOrderedSkippedRuleIds(
                        orderedCandidates,
                        static candidate => candidate.SkippedOverrideIds),
                    governanceSuppressionSummaries: BuildGovernanceSuppressionSummaries(orderedCandidates),
                    governanceOverrideSummaries: BuildGovernanceOverrideSummaries(orderedCandidates),
                    skippedSuppressionSummaries: BuildSkippedSuppressionSummaries(orderedCandidates),
                    skippedOverrideSummaries: BuildSkippedOverrideSummaries(orderedCandidates));
            })
            .ToArray();
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

    private static void AddCandidateId(
        Dictionary<string, List<string>> candidateIdsByRuleId,
        string? ruleId,
        string candidateId)
    {
        if (string.IsNullOrWhiteSpace(ruleId))
        {
            return;
        }

        var normalizedRuleId = ruleId.Trim();
        if (!candidateIdsByRuleId.TryGetValue(normalizedRuleId, out var candidateIds))
        {
            candidateIds = [];
            candidateIdsByRuleId[normalizedRuleId] = candidateIds;
        }

        if (!candidateIds.Contains(candidateId, Comparer))
        {
            candidateIds.Add(candidateId);
        }
    }

    private static void AddCandidateId(
        Dictionary<string, List<string>> candidateIdsByRuleId,
        List<string> orderedRuleIds,
        string? ruleId,
        string candidateId)
    {
        if (string.IsNullOrWhiteSpace(ruleId))
        {
            return;
        }

        var normalizedRuleId = ruleId.Trim();
        if (!candidateIdsByRuleId.TryGetValue(normalizedRuleId, out var candidateIds))
        {
            candidateIds = [];
            candidateIdsByRuleId[normalizedRuleId] = candidateIds;
            orderedRuleIds.Add(normalizedRuleId);
        }

        if (!candidateIds.Contains(candidateId, Comparer))
        {
            candidateIds.Add(candidateId);
        }
    }
}
