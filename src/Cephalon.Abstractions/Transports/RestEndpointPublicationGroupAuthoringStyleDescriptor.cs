namespace Cephalon.Abstractions.Transports;

/// <summary>
/// Describes the grouped publication outcome for one authoring style within a behavior-level REST publication group.
/// </summary>
public sealed class RestEndpointPublicationGroupAuthoringStyleDescriptor
{
    /// <summary>
    /// Creates a grouped publication descriptor for one authoring style.
    /// </summary>
    /// <param name="authoringStyle">The normalized authoring style that contributed the grouped candidates.</param>
    /// <param name="sourceModuleIds">The distinct source-module identifiers that contributed candidates for this authoring style.</param>
    /// <param name="precedenceRanks">The distinct precedence ranks visible for this authoring style within the group.</param>
    /// <param name="candidateIds">The ordered candidate identifiers contributed by this authoring style.</param>
    /// <param name="publishedCandidateIds">The ordered candidate identifiers that remain published for this authoring style.</param>
    /// <param name="precedenceSuppressedCandidateIds">
    /// The ordered candidate identifiers that were suppressed by another candidate through precedence resolution.
    /// </param>
    /// <param name="governanceSuppressedCandidateIds">
    /// The ordered candidate identifiers that were suppressed by host-level REST governance.
    /// </param>
    /// <param name="authoringPolicySuppressedCandidateIds">
    /// The ordered candidate identifiers that were suppressed by behavior-level authoring-policy enforcement.
    /// </param>
    public RestEndpointPublicationGroupAuthoringStyleDescriptor(
        string authoringStyle,
        IReadOnlyList<string>? sourceModuleIds = null,
        IReadOnlyList<int>? precedenceRanks = null,
        IReadOnlyList<string>? candidateIds = null,
        IReadOnlyList<string>? publishedCandidateIds = null,
        IReadOnlyList<string>? precedenceSuppressedCandidateIds = null,
        IReadOnlyList<string>? governanceSuppressedCandidateIds = null,
        IReadOnlyList<string>? authoringPolicySuppressedCandidateIds = null)
    {
        if (string.IsNullOrWhiteSpace(authoringStyle))
        {
            throw new ArgumentException("A non-empty authoring style is required.", nameof(authoringStyle));
        }

        var normalizedPrecedenceRanks = NormalizePrecedenceRanks(precedenceRanks);
        var normalizedCandidateIds = NormalizeOrderedList(candidateIds);
        var normalizedPublishedCandidateIds = NormalizeOrderedList(publishedCandidateIds);
        var normalizedPrecedenceSuppressedCandidateIds = NormalizeOrderedList(precedenceSuppressedCandidateIds);
        var normalizedGovernanceSuppressedCandidateIds = NormalizeOrderedList(governanceSuppressedCandidateIds);
        var normalizedAuthoringPolicySuppressedCandidateIds = NormalizeOrderedList(authoringPolicySuppressedCandidateIds);

        if (normalizedCandidateIds.Length == 0)
        {
            throw new ArgumentException(
                "At least one candidate id is required for an authoring-style publication answer.",
                nameof(candidateIds));
        }

        var candidateIdSet = normalizedCandidateIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!candidateIdSet.IsSupersetOf(normalizedPublishedCandidateIds))
        {
            throw new ArgumentException(
                "Published candidate ids must refer to candidates in the grouped authoring-style answer.",
                nameof(publishedCandidateIds));
        }

        if (!candidateIdSet.IsSupersetOf(normalizedPrecedenceSuppressedCandidateIds))
        {
            throw new ArgumentException(
                "Precedence-suppressed candidate ids must refer to candidates in the grouped authoring-style answer.",
                nameof(precedenceSuppressedCandidateIds));
        }

        if (!candidateIdSet.IsSupersetOf(normalizedGovernanceSuppressedCandidateIds))
        {
            throw new ArgumentException(
                "Governance-suppressed candidate ids must refer to candidates in the grouped authoring-style answer.",
                nameof(governanceSuppressedCandidateIds));
        }

        if (!candidateIdSet.IsSupersetOf(normalizedAuthoringPolicySuppressedCandidateIds))
        {
            throw new ArgumentException(
                "Authoring-policy-suppressed candidate ids must refer to candidates in the grouped authoring-style answer.",
                nameof(authoringPolicySuppressedCandidateIds));
        }

        if (normalizedPrecedenceSuppressedCandidateIds.Intersect(
                normalizedGovernanceSuppressedCandidateIds,
                StringComparer.OrdinalIgnoreCase).Any())
        {
            throw new ArgumentException(
                "An authoring-style publication answer cannot classify the same candidate as both precedence-suppressed and governance-suppressed.",
                nameof(precedenceSuppressedCandidateIds));
        }

        if (normalizedPrecedenceSuppressedCandidateIds.Intersect(
                normalizedAuthoringPolicySuppressedCandidateIds,
                StringComparer.OrdinalIgnoreCase).Any())
        {
            throw new ArgumentException(
                "An authoring-style publication answer cannot classify the same candidate as both precedence-suppressed and authoring-policy-suppressed.",
                nameof(precedenceSuppressedCandidateIds));
        }

        if (normalizedGovernanceSuppressedCandidateIds.Intersect(
                normalizedAuthoringPolicySuppressedCandidateIds,
                StringComparer.OrdinalIgnoreCase).Any())
        {
            throw new ArgumentException(
                "An authoring-style publication answer cannot classify the same candidate as both governance-suppressed and authoring-policy-suppressed.",
                nameof(governanceSuppressedCandidateIds));
        }

        AuthoringStyle = authoringStyle.Trim();
        SourceModuleIds = NormalizeSortedList(sourceModuleIds);
        PrecedenceRanks = normalizedPrecedenceRanks;
        CandidateIds = normalizedCandidateIds;
        PublishedCandidateIds = normalizedPublishedCandidateIds;
        PrecedenceSuppressedCandidateIds = normalizedPrecedenceSuppressedCandidateIds;
        GovernanceSuppressedCandidateIds = normalizedGovernanceSuppressedCandidateIds;
        AuthoringPolicySuppressedCandidateIds = normalizedAuthoringPolicySuppressedCandidateIds;
    }

    /// <summary>
    /// Gets the normalized authoring style that contributed the grouped candidates.
    /// </summary>
    public string AuthoringStyle { get; }

    /// <summary>
    /// Gets the distinct source-module identifiers that contributed candidates for this authoring style.
    /// </summary>
    public IReadOnlyList<string> SourceModuleIds { get; }

    /// <summary>
    /// Gets the distinct precedence ranks visible for this authoring style within the group.
    /// </summary>
    public IReadOnlyList<int> PrecedenceRanks { get; }

    /// <summary>
    /// Gets the ordered candidate identifiers contributed by this authoring style.
    /// </summary>
    public IReadOnlyList<string> CandidateIds { get; }

    /// <summary>
    /// Gets the ordered candidate identifiers that remain published for this authoring style.
    /// </summary>
    public IReadOnlyList<string> PublishedCandidateIds { get; }

    /// <summary>
    /// Gets the ordered candidate identifiers that were suppressed by precedence resolution.
    /// </summary>
    public IReadOnlyList<string> PrecedenceSuppressedCandidateIds { get; }

    /// <summary>
    /// Gets the ordered candidate identifiers that were suppressed by host-level REST governance.
    /// </summary>
    public IReadOnlyList<string> GovernanceSuppressedCandidateIds { get; }

    /// <summary>
    /// Gets the ordered candidate identifiers that were suppressed by behavior-level authoring-policy enforcement.
    /// </summary>
    public IReadOnlyList<string> AuthoringPolicySuppressedCandidateIds { get; }

    private static int[] NormalizePrecedenceRanks(IReadOnlyList<int>? precedenceRanks)
    {
        if (precedenceRanks is null || precedenceRanks.Count == 0)
        {
            return [];
        }

        if (precedenceRanks.Any(static rank => rank <= 0))
        {
            throw new ArgumentOutOfRangeException(
                nameof(precedenceRanks),
                precedenceRanks,
                "Every precedence rank must be positive.");
        }

        return precedenceRanks
            .Distinct()
            .OrderBy(static rank => rank)
            .ToArray();
    }

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

    private static string[] NormalizeSortedList(IReadOnlyList<string>? values)
    {
        return values?
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }
}
