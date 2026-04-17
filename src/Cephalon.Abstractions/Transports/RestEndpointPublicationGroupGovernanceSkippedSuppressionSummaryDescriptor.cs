namespace Cephalon.Abstractions.Transports;

/// <summary>
/// Describes one grouped host-governance-skipped suppression-rule outcome within a REST publication-group answer.
/// </summary>
public sealed class RestEndpointPublicationGroupGovernanceSkippedSuppressionSummaryDescriptor
{
    /// <summary>
    /// Creates a grouped host-governance-skipped suppression-rule summary.
    /// </summary>
    /// <param name="ruleId">The stable host-level suppression-rule identifier summarized by this entry.</param>
    /// <param name="candidateIds">
    /// The ordered candidate identifiers that this suppression rule targeted before host governance was skipped.
    /// </param>
    public RestEndpointPublicationGroupGovernanceSkippedSuppressionSummaryDescriptor(
        string ruleId,
        IReadOnlyList<string>? candidateIds = null)
    {
        if (string.IsNullOrWhiteSpace(ruleId))
        {
            throw new ArgumentException("A non-empty suppression rule id is required.", nameof(ruleId));
        }

        var normalizedCandidateIds = NormalizeOrderedList(candidateIds);
        if (normalizedCandidateIds.Length == 0)
        {
            throw new ArgumentException(
                "At least one candidate id is required for a grouped skipped suppression-rule summary.",
                nameof(candidateIds));
        }

        RuleId = ruleId.Trim();
        CandidateIds = normalizedCandidateIds;
    }

    /// <summary>
    /// Gets the stable host-level suppression-rule identifier summarized by this entry.
    /// </summary>
    public string RuleId { get; }

    /// <summary>
    /// Gets the ordered candidate identifiers that this suppression rule targeted before host governance was skipped.
    /// </summary>
    public IReadOnlyList<string> CandidateIds { get; }

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

internal static class RestEndpointPublicationGroupGovernanceSkippedSuppressionSummaryBuilder
{
    internal static RestEndpointPublicationGroupGovernanceSkippedSuppressionSummaryDescriptor[] BuildFromCandidates(
        IReadOnlyList<RestEndpointCandidateRuntimeDescriptor> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        var orderedRuleIds = new List<string>();
        var candidateIdsByRuleId = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var candidate in candidates)
        {
            foreach (var ruleId in candidate.SkippedSuppressionIds)
            {
                AddCandidateId(candidateIdsByRuleId, orderedRuleIds, ruleId, candidate.Id);
            }
        }

        return orderedRuleIds
            .Select(ruleId =>
                new RestEndpointPublicationGroupGovernanceSkippedSuppressionSummaryDescriptor(
                    ruleId,
                    candidateIdsByRuleId[ruleId]))
            .ToArray();
    }

    internal static RestEndpointPublicationGroupGovernanceSkippedSuppressionSummaryDescriptor[] Normalize(
        IReadOnlyList<RestEndpointPublicationGroupGovernanceSkippedSuppressionSummaryDescriptor>? summaries,
        IReadOnlySet<string> candidateIds,
        string parameterName)
    {
        ArgumentNullException.ThrowIfNull(candidateIds);

        if (summaries is null || summaries.Count == 0)
        {
            return [];
        }

        var result = new List<RestEndpointPublicationGroupGovernanceSkippedSuppressionSummaryDescriptor>(summaries.Count);
        var seenRuleIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var summary in summaries.Where(static summary => summary is not null))
        {
            if (!seenRuleIds.Add(summary.RuleId))
            {
                throw new ArgumentException(
                    "A grouped skipped suppression-rule summary can only declare one entry per rule id.",
                    parameterName);
            }

            if (!candidateIds.IsSupersetOf(summary.CandidateIds))
            {
                throw new ArgumentException(
                    "Grouped skipped suppression-rule summary candidate ids must refer to candidates in the grouped publication answer.",
                    parameterName);
            }

            result.Add(new RestEndpointPublicationGroupGovernanceSkippedSuppressionSummaryDescriptor(
                summary.RuleId,
                summary.CandidateIds));
        }

        return result.ToArray();
    }

    internal static string[] BuildRuleIds(
        IReadOnlyList<RestEndpointPublicationGroupGovernanceSkippedSuppressionSummaryDescriptor> summaries)
    {
        ArgumentNullException.ThrowIfNull(summaries);

        return summaries
            .Select(static summary => summary.RuleId)
            .ToArray();
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

        if (!candidateIds.Contains(candidateId, StringComparer.OrdinalIgnoreCase))
        {
            candidateIds.Add(candidateId);
        }
    }
}
