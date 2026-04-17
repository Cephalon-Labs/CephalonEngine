namespace Cephalon.Abstractions.Transports;

/// <summary>
/// Describes one grouped authoring-policy suppression outcome for a rule-centric REST runtime answer.
/// </summary>
public sealed class RestEndpointAuthoringPolicySuppressionSummaryDescriptor
{
    /// <summary>
    /// Creates an authoring-policy suppression summary descriptor.
    /// </summary>
    /// <param name="kind">The authoring-policy suppression kind summarized by this entry.</param>
    /// <param name="candidateIds">The ordered candidate identifiers suppressed by this suppression kind.</param>
    public RestEndpointAuthoringPolicySuppressionSummaryDescriptor(
        RestEndpointAuthoringPolicySuppressionKind kind,
        IReadOnlyList<string>? candidateIds = null)
    {
        if (!Enum.IsDefined(kind) || kind == RestEndpointAuthoringPolicySuppressionKind.Unspecified)
        {
            throw new ArgumentException(
                "A supported authoring-policy suppression kind is required.",
                nameof(kind));
        }

        var normalizedCandidateIds = NormalizeOrderedList(candidateIds);
        if (normalizedCandidateIds.Length == 0)
        {
            throw new ArgumentException(
                "At least one candidate id is required for an authoring-policy suppression summary.",
                nameof(candidateIds));
        }

        Kind = kind;
        CandidateIds = normalizedCandidateIds;
    }

    /// <summary>
    /// Gets the authoring-policy suppression kind summarized by this entry.
    /// </summary>
    public RestEndpointAuthoringPolicySuppressionKind Kind { get; }

    /// <summary>
    /// Gets the ordered candidate identifiers suppressed by this suppression kind.
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

internal static class RestEndpointAuthoringPolicySuppressionSummaryBuilder
{
    internal static RestEndpointAuthoringPolicySuppressionSummaryDescriptor[] BuildFromCandidates(
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

    internal static RestEndpointAuthoringPolicySuppressionSummaryDescriptor[] Normalize(
        IReadOnlyList<RestEndpointAuthoringPolicySuppressionSummaryDescriptor>? summaries,
        IReadOnlySet<string> candidateIds,
        string parameterName)
    {
        ArgumentNullException.ThrowIfNull(candidateIds);

        if (summaries is null || summaries.Count == 0)
        {
            return [];
        }

        var result = new List<RestEndpointAuthoringPolicySuppressionSummaryDescriptor>(summaries.Count);
        var seenKinds = new HashSet<RestEndpointAuthoringPolicySuppressionKind>();
        var seenCandidateIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var summary in summaries
                     .Where(static summary => summary is not null)
                     .OrderBy(static summary => summary.Kind.GetWireName(), StringComparer.Ordinal))
        {
            if (!seenKinds.Add(summary.Kind))
            {
                throw new ArgumentException(
                    "An authoring-policy suppression summary can only declare one entry per suppression kind.",
                    parameterName);
            }

            if (!candidateIds.IsSupersetOf(summary.CandidateIds))
            {
                throw new ArgumentException(
                    "Authoring-policy suppression summary candidate ids must refer to candidates in the authoring-policy runtime answer.",
                    parameterName);
            }

            foreach (var candidateId in summary.CandidateIds)
            {
                if (!seenCandidateIds.Add(candidateId))
                {
                    throw new ArgumentException(
                        "An authoring-policy suppression summary cannot classify the same candidate under more than one suppression kind.",
                        parameterName);
                }
            }

            result.Add(new RestEndpointAuthoringPolicySuppressionSummaryDescriptor(summary.Kind, summary.CandidateIds));
        }

        return result.ToArray();
    }

    internal static RestEndpointAuthoringPolicySuppressionKind[] BuildKinds(
        IReadOnlyList<RestEndpointAuthoringPolicySuppressionSummaryDescriptor> summaries)
    {
        ArgumentNullException.ThrowIfNull(summaries);

        return summaries
            .Select(static summary => summary.Kind)
            .Distinct()
            .OrderBy(static kind => kind.GetWireName(), StringComparer.Ordinal)
            .ToArray();
    }

    internal static string[] BuildSuppressedCandidateIds(
        IReadOnlyList<RestEndpointAuthoringPolicySuppressionSummaryDescriptor> summaries)
    {
        ArgumentNullException.ThrowIfNull(summaries);

        return summaries
            .SelectMany(static summary => summary.CandidateIds)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static candidateId => candidateId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
