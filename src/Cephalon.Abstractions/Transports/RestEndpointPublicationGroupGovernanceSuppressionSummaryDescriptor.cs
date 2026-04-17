namespace Cephalon.Abstractions.Transports;

/// <summary>
/// Describes one grouped host-governance suppression-rule outcome within a REST publication-group answer.
/// </summary>
public sealed class RestEndpointPublicationGroupGovernanceSuppressionSummaryDescriptor
{
    /// <summary>
    /// Creates a grouped host-governance suppression-rule summary.
    /// </summary>
    /// <param name="ruleId">The stable host-level suppression-rule identifier summarized by this entry.</param>
    /// <param name="matchedCandidateIds">
    /// The ordered candidate identifiers that matched this suppression rule before one winner was selected.
    /// </param>
    /// <param name="suppressedCandidateIds">
    /// The ordered candidate identifiers that this suppression rule ultimately suppressed.
    /// </param>
    /// <param name="selectionBasisSummaries">
    /// The grouped decisive selection-basis buckets for the candidates this suppression rule ultimately suppressed.
    /// </param>
    public RestEndpointPublicationGroupGovernanceSuppressionSummaryDescriptor(
        string ruleId,
        IReadOnlyList<string>? matchedCandidateIds = null,
        IReadOnlyList<string>? suppressedCandidateIds = null,
        IReadOnlyList<RestEndpointPublicationGroupGovernanceSelectionBasisSummaryDescriptor>? selectionBasisSummaries = null)
    {
        if (string.IsNullOrWhiteSpace(ruleId))
        {
            throw new ArgumentException("A non-empty suppression rule id is required.", nameof(ruleId));
        }

        var normalizedMatchedCandidateIds = NormalizeOrderedList(matchedCandidateIds);
        if (normalizedMatchedCandidateIds.Length == 0)
        {
            throw new ArgumentException(
                "At least one matched candidate id is required for a grouped suppression-rule summary.",
                nameof(matchedCandidateIds));
        }

        var normalizedSuppressedCandidateIds = NormalizeOrderedList(suppressedCandidateIds);
        if (normalizedSuppressedCandidateIds.Except(normalizedMatchedCandidateIds, StringComparer.OrdinalIgnoreCase).Any())
        {
            throw new ArgumentException(
                "Suppressed candidate ids must be a subset of the matched candidate ids.",
                nameof(suppressedCandidateIds));
        }

        var normalizedSelectionBasisSummaries =
            RestEndpointPublicationGroupGovernanceSelectionBasisSummaryBuilder.Normalize(
                selectionBasisSummaries,
                normalizedSuppressedCandidateIds.ToHashSet(StringComparer.OrdinalIgnoreCase),
                nameof(selectionBasisSummaries));

        RuleId = ruleId.Trim();
        MatchedCandidateIds = normalizedMatchedCandidateIds;
        SuppressedCandidateIds = normalizedSuppressedCandidateIds;
        SelectionBasisSummaries = normalizedSelectionBasisSummaries;
    }

    /// <summary>
    /// Gets the stable host-level suppression-rule identifier summarized by this entry.
    /// </summary>
    public string RuleId { get; }

    /// <summary>
    /// Gets the ordered candidate identifiers that matched this suppression rule before one winner was selected.
    /// </summary>
    public IReadOnlyList<string> MatchedCandidateIds { get; }

    /// <summary>
    /// Gets the ordered candidate identifiers that this suppression rule ultimately suppressed.
    /// </summary>
    public IReadOnlyList<string> SuppressedCandidateIds { get; }

    /// <summary>
    /// Gets the grouped decisive selection-basis buckets for the candidates this suppression rule ultimately suppressed.
    /// </summary>
    public IReadOnlyList<RestEndpointPublicationGroupGovernanceSelectionBasisSummaryDescriptor> SelectionBasisSummaries { get; }

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

internal static class RestEndpointPublicationGroupGovernanceSuppressionSummaryBuilder
{
    internal static RestEndpointPublicationGroupGovernanceSuppressionSummaryDescriptor[] BuildFromCandidates(
        IReadOnlyList<RestEndpointCandidateRuntimeDescriptor> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        var matchedCandidateIdsByRuleId = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var suppressedCandidateIdsByRuleId = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

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
            .OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase)
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

                return new RestEndpointPublicationGroupGovernanceSuppressionSummaryDescriptor(
                    pair.Key,
                    pair.Value,
                    suppressedCandidateIds,
                    RestEndpointPublicationGroupGovernanceSelectionBasisSummaryBuilder.BuildFromCandidates(
                        suppressedCandidatesForRule,
                        static candidate => candidate.SuppressionSelectionBasis));
            })
            .ToArray();
    }

    internal static RestEndpointPublicationGroupGovernanceSuppressionSummaryDescriptor[] Normalize(
        IReadOnlyList<RestEndpointPublicationGroupGovernanceSuppressionSummaryDescriptor>? summaries,
        IReadOnlySet<string> candidateIds,
        string parameterName)
    {
        ArgumentNullException.ThrowIfNull(candidateIds);

        if (summaries is null || summaries.Count == 0)
        {
            return [];
        }

        var result = new List<RestEndpointPublicationGroupGovernanceSuppressionSummaryDescriptor>(summaries.Count);
        var seenRuleIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenSuppressedCandidateIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var summary in summaries
                     .Where(static summary => summary is not null)
                     .OrderBy(static summary => summary.RuleId, StringComparer.OrdinalIgnoreCase))
        {
            if (!seenRuleIds.Add(summary.RuleId))
            {
                throw new ArgumentException(
                    "A grouped suppression-rule summary can only declare one entry per rule id.",
                    parameterName);
            }

            if (!candidateIds.IsSupersetOf(summary.MatchedCandidateIds))
            {
                throw new ArgumentException(
                    "Grouped suppression-rule summary matched candidate ids must refer to candidates in the grouped publication answer.",
                    parameterName);
            }

            if (!candidateIds.IsSupersetOf(summary.SuppressedCandidateIds))
            {
                throw new ArgumentException(
                    "Grouped suppression-rule summary suppressed candidate ids must refer to candidates in the grouped publication answer.",
                    parameterName);
            }

            foreach (var candidateId in summary.SuppressedCandidateIds)
            {
                if (!seenSuppressedCandidateIds.Add(candidateId))
                {
                    throw new ArgumentException(
                        "A grouped suppression-rule summary cannot classify the same candidate under more than one winning suppression rule.",
                        parameterName);
                }
            }

            result.Add(new RestEndpointPublicationGroupGovernanceSuppressionSummaryDescriptor(
                summary.RuleId,
                summary.MatchedCandidateIds,
                summary.SuppressedCandidateIds,
                summary.SelectionBasisSummaries));
        }

        return result.ToArray();
    }

    internal static string[] BuildSuppressedCandidateIds(
        IReadOnlyList<RestEndpointPublicationGroupGovernanceSuppressionSummaryDescriptor> summaries)
    {
        ArgumentNullException.ThrowIfNull(summaries);

        return summaries
            .SelectMany(static summary => summary.SuppressedCandidateIds)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static candidateId => candidateId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
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
