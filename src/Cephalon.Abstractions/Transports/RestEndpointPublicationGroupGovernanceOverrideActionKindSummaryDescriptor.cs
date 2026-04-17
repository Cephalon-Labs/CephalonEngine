namespace Cephalon.Abstractions.Transports;

/// <summary>
/// Describes one grouped override-action bucket within a REST publication-group governance summary.
/// </summary>
public sealed class RestEndpointPublicationGroupGovernanceOverrideActionKindSummaryDescriptor
{
    /// <summary>
    /// Creates a grouped override-action bucket for a REST publication-group governance summary.
    /// </summary>
    /// <param name="actionKind">The override action dimension represented by this grouped bucket.</param>
    /// <param name="candidateIds">
    /// The ordered candidate identifiers that selected or materially applied this override action dimension.
    /// </param>
    public RestEndpointPublicationGroupGovernanceOverrideActionKindSummaryDescriptor(
        RestEndpointOverrideActionKind actionKind,
        IReadOnlyList<string>? candidateIds = null)
    {
        if (!Enum.IsDefined(actionKind) ||
            actionKind == RestEndpointOverrideActionKind.Unspecified)
        {
            throw new ArgumentException(
                "A supported REST endpoint override action kind is required.",
                nameof(actionKind));
        }

        var normalizedCandidateIds = NormalizeOrderedList(candidateIds);
        if (normalizedCandidateIds.Length == 0)
        {
            throw new ArgumentException(
                "At least one candidate id is required for a grouped override-action summary.",
                nameof(candidateIds));
        }

        ActionKind = actionKind;
        CandidateIds = normalizedCandidateIds;
    }

    /// <summary>
    /// Gets the override action dimension represented by this grouped bucket.
    /// </summary>
    public RestEndpointOverrideActionKind ActionKind { get; }

    /// <summary>
    /// Gets the ordered candidate identifiers that selected or materially applied this override action dimension.
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

internal static class RestEndpointPublicationGroupGovernanceOverrideActionKindSummaryBuilder
{
    internal static RestEndpointPublicationGroupGovernanceOverrideActionKindSummaryDescriptor[] BuildFromCandidates(
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
                AddCandidateId(candidateIdsByActionKind, actionKind, candidate.Id);
            }
        }

        return candidateIdsByActionKind
            .OrderBy(static pair => pair.Key)
            .Select(pair =>
                new RestEndpointPublicationGroupGovernanceOverrideActionKindSummaryDescriptor(
                    pair.Key,
                    pair.Value))
            .ToArray();
    }

    internal static RestEndpointPublicationGroupGovernanceOverrideActionKindSummaryDescriptor[] Normalize(
        IReadOnlyList<RestEndpointPublicationGroupGovernanceOverrideActionKindSummaryDescriptor>? summaries,
        IReadOnlySet<string> candidateIds,
        string parameterName)
    {
        ArgumentNullException.ThrowIfNull(candidateIds);

        if (summaries is null || summaries.Count == 0)
        {
            return [];
        }

        var result = new List<RestEndpointPublicationGroupGovernanceOverrideActionKindSummaryDescriptor>(summaries.Count);
        var seenActionKinds = new HashSet<RestEndpointOverrideActionKind>();
        foreach (var summary in summaries
                     .Where(static summary => summary is not null)
                     .OrderBy(static summary => summary.ActionKind))
        {
            if (!seenActionKinds.Add(summary.ActionKind))
            {
                throw new ArgumentException(
                    "A grouped override-action summary can only declare one entry per action kind.",
                    parameterName);
            }

            if (!candidateIds.IsSupersetOf(summary.CandidateIds))
            {
                throw new ArgumentException(
                    "Grouped override-action summary candidate ids must refer to candidates in the grouped publication answer.",
                    parameterName);
            }

            result.Add(new RestEndpointPublicationGroupGovernanceOverrideActionKindSummaryDescriptor(
                summary.ActionKind,
                summary.CandidateIds));
        }

        return result.ToArray();
    }

    private static void AddCandidateId(
        Dictionary<RestEndpointOverrideActionKind, List<string>> candidateIdsByActionKind,
        RestEndpointOverrideActionKind actionKind,
        string candidateId)
    {
        if (!candidateIdsByActionKind.TryGetValue(actionKind, out var candidateIds))
        {
            candidateIds = [];
            candidateIdsByActionKind[actionKind] = candidateIds;
        }

        if (!candidateIds.Contains(candidateId, StringComparer.OrdinalIgnoreCase))
        {
            candidateIds.Add(candidateId);
        }
    }
}
