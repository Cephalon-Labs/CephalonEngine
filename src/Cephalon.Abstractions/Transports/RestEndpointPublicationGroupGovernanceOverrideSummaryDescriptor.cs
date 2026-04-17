namespace Cephalon.Abstractions.Transports;

/// <summary>
/// Describes one grouped host-governance override-rule outcome within a REST publication-group answer.
/// </summary>
public sealed class RestEndpointPublicationGroupGovernanceOverrideSummaryDescriptor
{
    /// <summary>
    /// Creates a grouped host-governance override-rule summary.
    /// </summary>
    /// <param name="ruleId">The stable host-level override-rule identifier summarized by this entry.</param>
    /// <param name="matchedCandidateIds">
    /// The ordered candidate identifiers that matched this override rule before one winner was selected.
    /// </param>
    /// <param name="selectedCandidateIds">
    /// The ordered candidate identifiers that selected this override rule, including runtime no-op selections.
    /// </param>
    /// <param name="appliedCandidateIds">
    /// The ordered candidate identifiers whose effective runtime answer was materially changed by this override rule.
    /// </param>
    /// <param name="selectionBasisSummaries">
    /// The grouped decisive selection-basis buckets for the candidates that selected this override rule.
    /// </param>
    /// <param name="selectedActionKindSummaries">
    /// The grouped declared override-action buckets for the candidates that selected this override rule.
    /// </param>
    /// <param name="appliedActionKindSummaries">
    /// The grouped materially applied override-action buckets for the candidates this override rule changed.
    /// </param>
    public RestEndpointPublicationGroupGovernanceOverrideSummaryDescriptor(
        string ruleId,
        IReadOnlyList<string>? matchedCandidateIds = null,
        IReadOnlyList<string>? selectedCandidateIds = null,
        IReadOnlyList<string>? appliedCandidateIds = null,
        IReadOnlyList<RestEndpointPublicationGroupGovernanceSelectionBasisSummaryDescriptor>? selectionBasisSummaries = null,
        IReadOnlyList<RestEndpointPublicationGroupGovernanceOverrideActionKindSummaryDescriptor>? selectedActionKindSummaries = null,
        IReadOnlyList<RestEndpointPublicationGroupGovernanceOverrideActionKindSummaryDescriptor>? appliedActionKindSummaries = null)
    {
        if (string.IsNullOrWhiteSpace(ruleId))
        {
            throw new ArgumentException("A non-empty override rule id is required.", nameof(ruleId));
        }

        var normalizedMatchedCandidateIds = NormalizeOrderedList(matchedCandidateIds);
        if (normalizedMatchedCandidateIds.Length == 0)
        {
            throw new ArgumentException(
                "At least one matched candidate id is required for a grouped override-rule summary.",
                nameof(matchedCandidateIds));
        }

        var normalizedSelectedCandidateIds = NormalizeOrderedList(selectedCandidateIds);
        if (normalizedSelectedCandidateIds.Except(normalizedMatchedCandidateIds, StringComparer.OrdinalIgnoreCase).Any())
        {
            throw new ArgumentException(
                "Selected candidate ids must be a subset of the matched candidate ids.",
                nameof(selectedCandidateIds));
        }

        var normalizedAppliedCandidateIds = NormalizeOrderedList(appliedCandidateIds);
        if (normalizedAppliedCandidateIds.Except(normalizedSelectedCandidateIds, StringComparer.OrdinalIgnoreCase).Any())
        {
            throw new ArgumentException(
                "Applied candidate ids must be a subset of the selected candidate ids.",
                nameof(appliedCandidateIds));
        }

        var normalizedSelectionBasisSummaries =
            RestEndpointPublicationGroupGovernanceSelectionBasisSummaryBuilder.Normalize(
                selectionBasisSummaries,
                normalizedSelectedCandidateIds.ToHashSet(StringComparer.OrdinalIgnoreCase),
                nameof(selectionBasisSummaries));
        var normalizedSelectedActionKindSummaries =
            RestEndpointPublicationGroupGovernanceOverrideActionKindSummaryBuilder.Normalize(
                selectedActionKindSummaries,
                normalizedSelectedCandidateIds.ToHashSet(StringComparer.OrdinalIgnoreCase),
                nameof(selectedActionKindSummaries));
        var normalizedAppliedActionKindSummaries =
            RestEndpointPublicationGroupGovernanceOverrideActionKindSummaryBuilder.Normalize(
                appliedActionKindSummaries,
                normalizedAppliedCandidateIds.ToHashSet(StringComparer.OrdinalIgnoreCase),
                nameof(appliedActionKindSummaries));

        RuleId = ruleId.Trim();
        MatchedCandidateIds = normalizedMatchedCandidateIds;
        SelectedCandidateIds = normalizedSelectedCandidateIds;
        AppliedCandidateIds = normalizedAppliedCandidateIds;
        SelectionBasisSummaries = normalizedSelectionBasisSummaries;
        SelectedActionKindSummaries = normalizedSelectedActionKindSummaries;
        AppliedActionKindSummaries = normalizedAppliedActionKindSummaries;
    }

    /// <summary>
    /// Gets the stable host-level override-rule identifier summarized by this entry.
    /// </summary>
    public string RuleId { get; }

    /// <summary>
    /// Gets the ordered candidate identifiers that matched this override rule before one winner was selected.
    /// </summary>
    public IReadOnlyList<string> MatchedCandidateIds { get; }

    /// <summary>
    /// Gets the ordered candidate identifiers that selected this override rule, including runtime no-op selections.
    /// </summary>
    public IReadOnlyList<string> SelectedCandidateIds { get; }

    /// <summary>
    /// Gets the ordered candidate identifiers whose effective runtime answer was materially changed by this override rule.
    /// </summary>
    public IReadOnlyList<string> AppliedCandidateIds { get; }

    /// <summary>
    /// Gets the grouped decisive selection-basis buckets for the candidates that selected this override rule.
    /// </summary>
    public IReadOnlyList<RestEndpointPublicationGroupGovernanceSelectionBasisSummaryDescriptor> SelectionBasisSummaries { get; }

    /// <summary>
    /// Gets the grouped declared override-action buckets for the candidates that selected this override rule.
    /// </summary>
    public IReadOnlyList<RestEndpointPublicationGroupGovernanceOverrideActionKindSummaryDescriptor> SelectedActionKindSummaries { get; }

    /// <summary>
    /// Gets the grouped materially applied override-action buckets for the candidates this override rule changed.
    /// </summary>
    public IReadOnlyList<RestEndpointPublicationGroupGovernanceOverrideActionKindSummaryDescriptor> AppliedActionKindSummaries { get; }

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
}

internal static class RestEndpointPublicationGroupGovernanceOverrideSummaryBuilder
{
    internal static RestEndpointPublicationGroupGovernanceOverrideSummaryDescriptor[] BuildFromCandidates(
        IReadOnlyList<RestEndpointCandidateRuntimeDescriptor> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        var matchedCandidateIdsByRuleId = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var selectedCandidateIdsByRuleId = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var appliedCandidateIdsByRuleId = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

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
            .OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(pair =>
            {
                var selectedCandidatesForRule = candidates
                    .Where(candidate => string.Equals(candidate.SelectedOverrideId, pair.Key, StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                var appliedCandidatesForRule = candidates
                    .Where(candidate => string.Equals(candidate.AppliedOverrideId, pair.Key, StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                return new RestEndpointPublicationGroupGovernanceOverrideSummaryDescriptor(
                    pair.Key,
                    pair.Value,
                    selectedCandidateIdsByRuleId.TryGetValue(pair.Key, out var selectedCandidateIds)
                        ? selectedCandidateIds
                        : [],
                    appliedCandidateIdsByRuleId.TryGetValue(pair.Key, out var appliedCandidateIds)
                        ? appliedCandidateIds
                        : [],
                    RestEndpointPublicationGroupGovernanceSelectionBasisSummaryBuilder.BuildFromCandidates(
                        selectedCandidatesForRule,
                        static candidate => candidate.OverrideSelectionBasis),
                    RestEndpointPublicationGroupGovernanceOverrideActionKindSummaryBuilder.BuildFromCandidates(
                        selectedCandidatesForRule,
                        static candidate => candidate.SelectedOverrideActionKinds),
                    RestEndpointPublicationGroupGovernanceOverrideActionKindSummaryBuilder.BuildFromCandidates(
                        appliedCandidatesForRule,
                        static candidate => candidate.AppliedOverrideActionKinds));
            })
            .ToArray();
    }

    internal static RestEndpointPublicationGroupGovernanceOverrideSummaryDescriptor[] Normalize(
        IReadOnlyList<RestEndpointPublicationGroupGovernanceOverrideSummaryDescriptor>? summaries,
        IReadOnlySet<string> candidateIds,
        string parameterName)
    {
        ArgumentNullException.ThrowIfNull(candidateIds);

        if (summaries is null || summaries.Count == 0)
        {
            return [];
        }

        var result = new List<RestEndpointPublicationGroupGovernanceOverrideSummaryDescriptor>(summaries.Count);
        var seenRuleIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenSelectedCandidateIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenAppliedCandidateIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var summary in summaries
                     .Where(static summary => summary is not null)
                     .OrderBy(static summary => summary.RuleId, StringComparer.OrdinalIgnoreCase))
        {
            if (!seenRuleIds.Add(summary.RuleId))
            {
                throw new ArgumentException(
                    "A grouped override-rule summary can only declare one entry per rule id.",
                    parameterName);
            }

            if (!candidateIds.IsSupersetOf(summary.MatchedCandidateIds))
            {
                throw new ArgumentException(
                    "Grouped override-rule summary matched candidate ids must refer to candidates in the grouped publication answer.",
                    parameterName);
            }

            if (!candidateIds.IsSupersetOf(summary.SelectedCandidateIds))
            {
                throw new ArgumentException(
                    "Grouped override-rule summary selected candidate ids must refer to candidates in the grouped publication answer.",
                    parameterName);
            }

            if (!candidateIds.IsSupersetOf(summary.AppliedCandidateIds))
            {
                throw new ArgumentException(
                    "Grouped override-rule summary applied candidate ids must refer to candidates in the grouped publication answer.",
                    parameterName);
            }

            foreach (var candidateId in summary.SelectedCandidateIds)
            {
                if (!seenSelectedCandidateIds.Add(candidateId))
                {
                    throw new ArgumentException(
                        "A grouped override-rule summary cannot classify the same candidate under more than one selected override rule.",
                        parameterName);
                }
            }

            foreach (var candidateId in summary.AppliedCandidateIds)
            {
                if (!seenAppliedCandidateIds.Add(candidateId))
                {
                    throw new ArgumentException(
                        "A grouped override-rule summary cannot classify the same candidate under more than one applied override rule.",
                        parameterName);
                }
            }

            result.Add(new RestEndpointPublicationGroupGovernanceOverrideSummaryDescriptor(
                summary.RuleId,
                summary.MatchedCandidateIds,
                summary.SelectedCandidateIds,
                summary.AppliedCandidateIds,
                summary.SelectionBasisSummaries,
                summary.SelectedActionKindSummaries,
                summary.AppliedActionKindSummaries));
        }

        return result.ToArray();
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

        if (!candidateIds.Contains(candidateId, StringComparer.OrdinalIgnoreCase))
        {
            candidateIds.Add(candidateId);
        }
    }
}
