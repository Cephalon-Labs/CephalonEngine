namespace Cephalon.Abstractions.Transports;

/// <summary>
/// Describes the grouped public REST publication outcome for one behavior across all of its visible candidates.
/// </summary>
public sealed class RestEndpointPublicationGroupDescriptor
{
    /// <summary>
    /// Creates a grouped REST endpoint publication descriptor.
    /// </summary>
    /// <param name="behaviorId">The stable behavior identifier for the grouped publication answer.</param>
    /// <param name="sourceModuleIds">The distinct source-module identifiers that contributed the grouped candidates.</param>
    /// <param name="winningPrecedenceRank">
    /// The winning precedence rank for the published candidates when one or more candidates remain published.
    /// </param>
    /// <param name="publishedCandidateIds">The candidate identifiers that remain published for this behavior.</param>
    /// <param name="precedenceSuppressedCandidateIds">
    /// The candidate identifiers that were suppressed by another candidate through precedence resolution.
    /// </param>
    /// <param name="governanceSuppressedCandidateIds">
    /// The candidate identifiers that were suppressed by host-level REST governance.
    /// </param>
    /// <param name="candidates">The ordered candidate set that produced this grouped publication answer.</param>
    /// <param name="authoringPolicy">
    /// The effective authoring-policy intent for this behavior-level publication group.
    /// </param>
    public RestEndpointPublicationGroupDescriptor(
        string behaviorId,
        IReadOnlyList<string>? sourceModuleIds = null,
        int? winningPrecedenceRank = null,
        IReadOnlyList<string>? publishedCandidateIds = null,
        IReadOnlyList<string>? precedenceSuppressedCandidateIds = null,
        IReadOnlyList<string>? governanceSuppressedCandidateIds = null,
        IReadOnlyList<RestEndpointCandidateRuntimeDescriptor>? candidates = null,
        RestEndpointPublicationGroupAuthoringPolicyDescriptor? authoringPolicy = null)
    {
        if (string.IsNullOrWhiteSpace(behaviorId))
        {
            throw new ArgumentException("A non-empty behavior id is required.", nameof(behaviorId));
        }

        if (winningPrecedenceRank.HasValue && winningPrecedenceRank.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(winningPrecedenceRank),
                winningPrecedenceRank,
                "The winning precedence rank must be positive when it is provided.");
        }

        var normalizedCandidates = NormalizeCandidates(candidates);
        if (normalizedCandidates.Length == 0)
        {
            throw new ArgumentException(
                "At least one candidate is required for a grouped publication answer.",
                nameof(candidates));
        }

        var normalizedBehaviorId = behaviorId.Trim();
        if (normalizedCandidates.Any(candidate =>
                !string.Equals(candidate.ProjectedEndpoint.BehaviorId, normalizedBehaviorId, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException(
                "Every grouped candidate must target the same behavior id.",
                nameof(candidates));
        }

        var normalizedSourceModuleIds = NormalizeList(sourceModuleIds);
        var normalizedPublishedCandidateIds = NormalizeList(publishedCandidateIds);
        var normalizedPrecedenceSuppressedCandidateIds = NormalizeList(precedenceSuppressedCandidateIds);
        var normalizedGovernanceSuppressedCandidateIds = NormalizeList(governanceSuppressedCandidateIds);
        var candidateIds = normalizedCandidates
            .Select(static candidate => candidate.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!candidateIds.IsSupersetOf(normalizedPublishedCandidateIds))
        {
            throw new ArgumentException(
                "Published candidate ids must refer to candidates in the grouped publication answer.",
                nameof(publishedCandidateIds));
        }

        if (!candidateIds.IsSupersetOf(normalizedPrecedenceSuppressedCandidateIds))
        {
            throw new ArgumentException(
                "Precedence-suppressed candidate ids must refer to candidates in the grouped publication answer.",
                nameof(precedenceSuppressedCandidateIds));
        }

        if (!candidateIds.IsSupersetOf(normalizedGovernanceSuppressedCandidateIds))
        {
            throw new ArgumentException(
                "Governance-suppressed candidate ids must refer to candidates in the grouped publication answer.",
                nameof(governanceSuppressedCandidateIds));
        }

        if (normalizedPrecedenceSuppressedCandidateIds.Intersect(normalizedGovernanceSuppressedCandidateIds, StringComparer.OrdinalIgnoreCase).Any())
        {
            throw new ArgumentException(
                "A grouped publication answer cannot classify the same candidate as both precedence-suppressed and governance-suppressed.",
                nameof(precedenceSuppressedCandidateIds));
        }

        BehaviorId = normalizedBehaviorId;
        SourceModuleIds = normalizedSourceModuleIds;
        WinningPrecedenceRank = winningPrecedenceRank;
        PublishedCandidateIds = normalizedPublishedCandidateIds;
        PrecedenceSuppressedCandidateIds = normalizedPrecedenceSuppressedCandidateIds;
        GovernanceSuppressedCandidateIds = normalizedGovernanceSuppressedCandidateIds;
        Candidates = normalizedCandidates;
        AuthoringStyleSummaries = BuildAuthoringStyleSummaries(normalizedCandidates);
        AuthoringPolicy = NormalizeAuthoringPolicy(authoringPolicy, normalizedBehaviorId);
    }

    /// <summary>
    /// Gets the stable behavior identifier for the grouped publication answer.
    /// </summary>
    public string BehaviorId { get; }

    /// <summary>
    /// Gets the distinct source-module identifiers that contributed the grouped candidates.
    /// </summary>
    public IReadOnlyList<string> SourceModuleIds { get; }

    /// <summary>
    /// Gets the winning precedence rank for the published candidates when one or more remain published.
    /// </summary>
    public int? WinningPrecedenceRank { get; }

    /// <summary>
    /// Gets the candidate identifiers that remain published for this behavior.
    /// </summary>
    public IReadOnlyList<string> PublishedCandidateIds { get; }

    /// <summary>
    /// Gets the candidate identifiers that were suppressed by precedence resolution.
    /// </summary>
    public IReadOnlyList<string> PrecedenceSuppressedCandidateIds { get; }

    /// <summary>
    /// Gets the candidate identifiers that were suppressed by host-level REST governance.
    /// </summary>
    public IReadOnlyList<string> GovernanceSuppressedCandidateIds { get; }

    /// <summary>
    /// Gets the ordered candidate set that produced this grouped publication answer.
    /// </summary>
    public IReadOnlyList<RestEndpointCandidateRuntimeDescriptor> Candidates { get; }

    /// <summary>
    /// Gets the grouped publication outcome summarized by authoring style for this behavior.
    /// </summary>
    public IReadOnlyList<RestEndpointPublicationGroupAuthoringStyleDescriptor> AuthoringStyleSummaries { get; }

    /// <summary>
    /// Gets the effective authoring-policy intent for this behavior-level publication group.
    /// </summary>
    public RestEndpointPublicationGroupAuthoringPolicyDescriptor AuthoringPolicy { get; }

    private static string[] NormalizeList(IReadOnlyList<string>? values)
    {
        return values?
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }

    private static RestEndpointCandidateRuntimeDescriptor[] NormalizeCandidates(
        IReadOnlyList<RestEndpointCandidateRuntimeDescriptor>? candidates)
    {
        return candidates?
            .Where(static candidate => candidate is not null)
            .ToArray() ?? [];
    }

    private static RestEndpointPublicationGroupAuthoringStyleDescriptor[] BuildAuthoringStyleSummaries(
        IReadOnlyList<RestEndpointCandidateRuntimeDescriptor> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        return candidates
            .GroupBy(static candidate => candidate.AuthoringStyle, StringComparer.OrdinalIgnoreCase)
            .Select(static group =>
            {
                var orderedCandidates = group.ToArray();
                var sourceModuleIds = orderedCandidates
                    .Select(static candidate => candidate.ProjectedEndpoint.SourceModuleId)
                    .Where(static sourceModuleId => !string.IsNullOrWhiteSpace(sourceModuleId))
                    .Select(static sourceModuleId => sourceModuleId!.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(static sourceModuleId => sourceModuleId, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                var precedenceRanks = orderedCandidates
                    .Select(static candidate => candidate.PrecedenceRank)
                    .Distinct()
                    .OrderBy(static rank => rank)
                    .ToArray();
                var candidateIds = orderedCandidates
                    .Select(static candidate => candidate.Id)
                    .ToArray();
                var publishedCandidateIds = orderedCandidates
                    .Where(static candidate => candidate.Status == RestEndpointCandidateStatus.Published)
                    .Select(static candidate => candidate.Id)
                    .ToArray();
                var precedenceSuppressedCandidateIds = orderedCandidates
                    .Where(static candidate =>
                        candidate.Status == RestEndpointCandidateStatus.Suppressed &&
                        !string.IsNullOrWhiteSpace(candidate.SuppressedByCandidateId))
                    .Select(static candidate => candidate.Id)
                    .ToArray();
                var governanceSuppressedCandidateIds = orderedCandidates
                    .Where(static candidate =>
                        candidate.Status == RestEndpointCandidateStatus.Suppressed &&
                        !string.IsNullOrWhiteSpace(candidate.SuppressedBySuppressionId))
                    .Select(static candidate => candidate.Id)
                    .ToArray();

                return new RestEndpointPublicationGroupAuthoringStyleDescriptor(
                    group.Key,
                    sourceModuleIds,
                    precedenceRanks,
                    candidateIds,
                    publishedCandidateIds,
                    precedenceSuppressedCandidateIds,
                    governanceSuppressedCandidateIds);
            })
            .ToArray();
    }

    private static RestEndpointPublicationGroupAuthoringPolicyDescriptor NormalizeAuthoringPolicy(
        RestEndpointPublicationGroupAuthoringPolicyDescriptor? authoringPolicy,
        string behaviorId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(behaviorId);

        if (authoringPolicy is null)
        {
            return new RestEndpointPublicationGroupAuthoringPolicyDescriptor(behaviorId);
        }

        if (!string.Equals(authoringPolicy.BehaviorId, behaviorId, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "The grouped authoring policy must target the same behavior id as the publication group.",
                nameof(authoringPolicy));
        }

        return new RestEndpointPublicationGroupAuthoringPolicyDescriptor(
            authoringPolicy.BehaviorId,
            authoringPolicy.IsConfigured,
            authoringPolicy.AllowMultiplePublishedCandidates,
            authoringPolicy.PreferredAuthoringStyle,
            authoringPolicy.AllowedAuthoringStyles,
            authoringPolicy.DisallowedAuthoringStyles);
    }
}
