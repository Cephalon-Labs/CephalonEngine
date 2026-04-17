namespace Cephalon.Abstractions.Transports;

/// <summary>
/// Describes one grouped selection-basis bucket within a REST publication-group governance summary.
/// </summary>
public sealed class RestEndpointPublicationGroupGovernanceSelectionBasisSummaryDescriptor
{
    /// <summary>
    /// Creates a grouped selection-basis bucket for a REST publication-group governance summary.
    /// </summary>
    /// <param name="selectionBasis">
    /// The decisive specificity basis that selected the winning governance rule for the grouped candidates.
    /// </param>
    /// <param name="candidateIds">
    /// The ordered candidate identifiers that resolved the winning governance rule with this selection basis.
    /// </param>
    public RestEndpointPublicationGroupGovernanceSelectionBasisSummaryDescriptor(
        RestEndpointGovernanceRuleSelectionBasis selectionBasis,
        IReadOnlyList<string>? candidateIds = null)
    {
        if (!Enum.IsDefined(selectionBasis) ||
            selectionBasis == RestEndpointGovernanceRuleSelectionBasis.Unspecified)
        {
            throw new ArgumentException(
                "A supported REST endpoint governance rule selection basis is required.",
                nameof(selectionBasis));
        }

        var normalizedCandidateIds = NormalizeOrderedList(candidateIds);
        if (normalizedCandidateIds.Length == 0)
        {
            throw new ArgumentException(
                "At least one candidate id is required for a grouped selection-basis summary.",
                nameof(candidateIds));
        }

        SelectionBasis = selectionBasis;
        CandidateIds = normalizedCandidateIds;
    }

    /// <summary>
    /// Gets the decisive specificity basis that selected the winning governance rule for the grouped candidates.
    /// </summary>
    public RestEndpointGovernanceRuleSelectionBasis SelectionBasis { get; }

    /// <summary>
    /// Gets the ordered candidate identifiers that resolved the winning governance rule with this selection basis.
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

internal static class RestEndpointPublicationGroupGovernanceSelectionBasisSummaryBuilder
{
    internal static RestEndpointPublicationGroupGovernanceSelectionBasisSummaryDescriptor[] BuildFromCandidates(
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

            AddCandidateId(candidateIdsBySelectionBasis, selectionBasis.Value, candidate.Id);
        }

        return candidateIdsBySelectionBasis
            .OrderBy(static pair => pair.Key)
            .Select(pair =>
                new RestEndpointPublicationGroupGovernanceSelectionBasisSummaryDescriptor(
                    pair.Key,
                    pair.Value))
            .ToArray();
    }

    internal static RestEndpointPublicationGroupGovernanceSelectionBasisSummaryDescriptor[] Normalize(
        IReadOnlyList<RestEndpointPublicationGroupGovernanceSelectionBasisSummaryDescriptor>? summaries,
        IReadOnlySet<string> candidateIds,
        string parameterName)
    {
        ArgumentNullException.ThrowIfNull(candidateIds);

        if (summaries is null || summaries.Count == 0)
        {
            return [];
        }

        var result = new List<RestEndpointPublicationGroupGovernanceSelectionBasisSummaryDescriptor>(summaries.Count);
        var seenSelectionBases = new HashSet<RestEndpointGovernanceRuleSelectionBasis>();
        var seenCandidateIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var summary in summaries
                     .Where(static summary => summary is not null)
                     .OrderBy(static summary => summary.SelectionBasis))
        {
            if (!seenSelectionBases.Add(summary.SelectionBasis))
            {
                throw new ArgumentException(
                    "A grouped selection-basis summary can only declare one entry per selection basis.",
                    parameterName);
            }

            if (!candidateIds.IsSupersetOf(summary.CandidateIds))
            {
                throw new ArgumentException(
                    "Grouped selection-basis summary candidate ids must refer to candidates in the grouped publication answer.",
                    parameterName);
            }

            foreach (var candidateId in summary.CandidateIds)
            {
                if (!seenCandidateIds.Add(candidateId))
                {
                    throw new ArgumentException(
                        "A grouped selection-basis summary cannot classify the same candidate under more than one selection basis.",
                        parameterName);
                }
            }

            result.Add(new RestEndpointPublicationGroupGovernanceSelectionBasisSummaryDescriptor(
                summary.SelectionBasis,
                summary.CandidateIds));
        }

        return result.ToArray();
    }

    private static void AddCandidateId(
        Dictionary<RestEndpointGovernanceRuleSelectionBasis, List<string>> candidateIdsBySelectionBasis,
        RestEndpointGovernanceRuleSelectionBasis selectionBasis,
        string candidateId)
    {
        if (!candidateIdsBySelectionBasis.TryGetValue(selectionBasis, out var candidateIds))
        {
            candidateIds = [];
            candidateIdsBySelectionBasis[selectionBasis] = candidateIds;
        }

        if (!candidateIds.Contains(candidateId, StringComparer.OrdinalIgnoreCase))
        {
            candidateIds.Add(candidateId);
        }
    }
}
