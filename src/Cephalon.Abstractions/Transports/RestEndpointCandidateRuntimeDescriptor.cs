namespace Cephalon.Abstractions.Transports;

/// <summary>
/// Describes one module-owned REST endpoint candidate and whether it was published or suppressed.
/// </summary>
public sealed class RestEndpointCandidateRuntimeDescriptor
{
    /// <summary>
    /// Creates a REST endpoint candidate runtime descriptor.
    /// </summary>
    /// <param name="id">
    /// The stable candidate identifier derived from the original projection before any host-level
    /// overrides are applied.
    /// </param>
    /// <param name="projectedEndpoint">
    /// The resolved endpoint shape the candidate would publish when it wins precedence.
    /// </param>
    /// <param name="originalProjection">
    /// The original projection shape contributed by the source authoring path before host-level overrides are applied.
    /// </param>
    /// <param name="authoringStyle">The normalized authoring style such as <c>behavior-module-dsl</c>.</param>
    /// <param name="precedenceRank">
    /// The precedence rank used during publication resolution. Lower values win.
    /// </param>
    /// <param name="status">The publication status assigned to the candidate.</param>
    /// <param name="suppressedByCandidateId">
    /// The winning candidate identifier when this candidate was suppressed.
    /// </param>
    /// <param name="suppressedBySuppressionId">
    /// The host-level suppression identifier when this candidate was suppressed by REST governance.
    /// </param>
    /// <param name="appliedOverrideId">
    /// The host-level override identifier when this candidate shape was rewritten by REST governance.
    /// </param>
    /// <param name="matchedSuppressionIds">
    /// The ordered suppression-rule identifiers that matched this candidate before one winner was selected.
    /// </param>
    /// <param name="matchedOverrideIds">
    /// The ordered override-rule identifiers that matched this candidate before one winner was selected.
    /// </param>
    /// <param name="suppressionReason">The operator-facing suppression reason when one is available.</param>
    /// <param name="suppressedByAuthoringPolicyKind">
    /// The authoring-policy suppression kind when this candidate was suppressed by behavior-level
    /// authoring-policy enforcement.
    /// </param>
    /// <param name="selectedOverrideId">
    /// The selected host-level override identifier when one winning override rule was resolved for
    /// this candidate, even if that winning rule became a runtime no-op.
    /// </param>
    /// <param name="suppressionSelectionBasis">
    /// The earliest decisive specificity rule that selected the winning suppression rule when this
    /// candidate was suppressed by REST governance.
    /// </param>
    /// <param name="overrideSelectionBasis">
    /// The earliest decisive specificity rule that selected the winning override rule when one was
    /// resolved for this candidate.
    /// </param>
    /// <param name="skippedSuppressionIds">
    /// The ordered suppression-rule identifiers that otherwise target this candidate but were
    /// skipped because the original projection did not allow host governance to participate.
    /// </param>
    /// <param name="skippedOverrideIds">
    /// The ordered override-rule identifiers that otherwise target this candidate but were skipped
    /// because the original projection did not allow host governance to participate.
    /// </param>
    /// <param name="selectedOverrideActionKinds">
    /// The normalized action dimensions declared by the selected override rule when one winning
    /// override rule was resolved for this candidate.
    /// </param>
    /// <param name="appliedOverrideActionKinds">
    /// The normalized action dimensions that materially changed the candidate's effective runtime
    /// answer when the selected override rule was not a runtime no-op.
    /// </param>
    public RestEndpointCandidateRuntimeDescriptor(
        string id,
        RestEndpointRuntimeDescriptor projectedEndpoint,
        RestEndpointCandidateProjectionDescriptor originalProjection,
        string authoringStyle,
        int precedenceRank,
        RestEndpointCandidateStatus status,
        string? suppressedByCandidateId = null,
        string? suppressedBySuppressionId = null,
        string? appliedOverrideId = null,
        IReadOnlyList<string>? matchedSuppressionIds = null,
        IReadOnlyList<string>? matchedOverrideIds = null,
        string? suppressionReason = null,
        RestEndpointAuthoringPolicySuppressionKind? suppressedByAuthoringPolicyKind = null,
        string? selectedOverrideId = null,
        RestEndpointGovernanceRuleSelectionBasis? suppressionSelectionBasis = null,
        RestEndpointGovernanceRuleSelectionBasis? overrideSelectionBasis = null,
        IReadOnlyList<string>? skippedSuppressionIds = null,
        IReadOnlyList<string>? skippedOverrideIds = null,
        IReadOnlyList<RestEndpointOverrideActionKind>? selectedOverrideActionKinds = null,
        IReadOnlyList<RestEndpointOverrideActionKind>? appliedOverrideActionKinds = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("A non-empty candidate id is required.", nameof(id));
        }

        ArgumentNullException.ThrowIfNull(projectedEndpoint);
        ArgumentNullException.ThrowIfNull(originalProjection);

        if (string.IsNullOrWhiteSpace(authoringStyle))
        {
            throw new ArgumentException("A non-empty authoring style is required.", nameof(authoringStyle));
        }

        if (precedenceRank <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(precedenceRank),
                precedenceRank,
                "A positive precedence rank is required.");
        }

        if (!Enum.IsDefined(status) || status == RestEndpointCandidateStatus.Unspecified)
        {
            throw new ArgumentException("A supported candidate status is required.", nameof(status));
        }

        if (suppressedByAuthoringPolicyKind.HasValue &&
            (!Enum.IsDefined(suppressedByAuthoringPolicyKind.Value) ||
             suppressedByAuthoringPolicyKind.Value == RestEndpointAuthoringPolicySuppressionKind.Unspecified))
        {
            throw new ArgumentException(
                "A supported authoring-policy suppression kind is required when one is declared.",
                nameof(suppressedByAuthoringPolicyKind));
        }

        var normalizedMatchedSuppressionIds = NormalizeOrderedList(matchedSuppressionIds);
        var normalizedMatchedOverrideIds = NormalizeOrderedList(matchedOverrideIds);
        var normalizedSkippedSuppressionIds = NormalizeOrderedList(skippedSuppressionIds);
        var normalizedSkippedOverrideIds = NormalizeOrderedList(skippedOverrideIds);
        var normalizedAppliedOverrideId = string.IsNullOrWhiteSpace(appliedOverrideId)
            ? null
            : appliedOverrideId.Trim();
        var normalizedSelectedOverrideId = string.IsNullOrWhiteSpace(selectedOverrideId)
            ? null
            : selectedOverrideId.Trim();
        normalizedSelectedOverrideId ??= normalizedAppliedOverrideId ?? normalizedMatchedOverrideIds.FirstOrDefault();
        var normalizedSelectedOverrideActionKinds = NormalizeActionKinds(
            selectedOverrideActionKinds,
            nameof(selectedOverrideActionKinds));
        var normalizedAppliedOverrideActionKinds = NormalizeActionKinds(
            appliedOverrideActionKinds,
            nameof(appliedOverrideActionKinds));
        if (normalizedSelectedOverrideActionKinds.Length == 0 &&
            normalizedAppliedOverrideActionKinds.Length > 0)
        {
            normalizedSelectedOverrideActionKinds = normalizedAppliedOverrideActionKinds;
        }

        var normalizedSuppressionSelectionBasis = NormalizeSelectionBasis(
            suppressionSelectionBasis,
            nameof(suppressionSelectionBasis));
        var normalizedOverrideSelectionBasis = NormalizeSelectionBasis(
            overrideSelectionBasis,
            nameof(overrideSelectionBasis));

        if (status == RestEndpointCandidateStatus.Published &&
            (!string.IsNullOrWhiteSpace(suppressedByCandidateId) ||
            !string.IsNullOrWhiteSpace(suppressedBySuppressionId) ||
            suppressedByAuthoringPolicyKind.HasValue ||
            normalizedMatchedSuppressionIds.Length > 0 ||
            !string.IsNullOrWhiteSpace(suppressionReason)))
        {
            throw new ArgumentException(
                "Published candidates cannot declare suppression metadata.",
                nameof(status));
        }

        var declaredSuppressionOrigins = 0;
        if (!string.IsNullOrWhiteSpace(suppressedByCandidateId))
        {
            declaredSuppressionOrigins++;
        }

        if (!string.IsNullOrWhiteSpace(suppressedBySuppressionId))
        {
            declaredSuppressionOrigins++;
        }

        if (suppressedByAuthoringPolicyKind.HasValue)
        {
            declaredSuppressionOrigins++;
        }

        if (declaredSuppressionOrigins > 1)
        {
            throw new ArgumentException(
                "A suppressed candidate cannot declare more than one suppression origin.",
                nameof(suppressedByCandidateId));
        }

        if (normalizedMatchedSuppressionIds.Length > 0 &&
            string.IsNullOrWhiteSpace(suppressedBySuppressionId))
        {
            throw new ArgumentException(
                "Matched suppression ids can only be declared for candidates suppressed by REST governance.",
                nameof(matchedSuppressionIds));
        }

        if (!string.IsNullOrWhiteSpace(suppressedBySuppressionId) &&
            !normalizedMatchedSuppressionIds.Contains(suppressedBySuppressionId.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "The selected suppression id must appear in the matched suppression id list when that list is provided.",
                nameof(matchedSuppressionIds));
        }

        if (normalizedSuppressionSelectionBasis.HasValue &&
            string.IsNullOrWhiteSpace(suppressedBySuppressionId))
        {
            throw new ArgumentException(
                "A suppression selection basis can only be declared for candidates suppressed by REST governance.",
                nameof(suppressionSelectionBasis));
        }

        if (!normalizedSuppressionSelectionBasis.HasValue &&
            !string.IsNullOrWhiteSpace(suppressedBySuppressionId) &&
            normalizedMatchedSuppressionIds.Length <= 1)
        {
            normalizedSuppressionSelectionBasis = RestEndpointGovernanceRuleSelectionBasis.SingleMatch;
        }

        if (normalizedSelectedOverrideId is not null &&
            normalizedMatchedOverrideIds.Length > 0 &&
            !normalizedMatchedOverrideIds.Contains(normalizedSelectedOverrideId, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "The selected override id must appear in the matched override id list when that list is provided.",
                nameof(selectedOverrideId));
        }

        if (normalizedAppliedOverrideId is not null &&
            normalizedMatchedOverrideIds.Length > 0 &&
            !normalizedMatchedOverrideIds.Contains(normalizedAppliedOverrideId, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "The applied override id must appear in the matched override id list when that list is provided.",
                nameof(matchedOverrideIds));
        }

        if (normalizedAppliedOverrideId is not null &&
            normalizedSelectedOverrideId is not null &&
            !string.Equals(normalizedAppliedOverrideId, normalizedSelectedOverrideId, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "The applied override id must match the selected override id when both are declared.",
                nameof(appliedOverrideId));
        }

        if (normalizedSelectedOverrideActionKinds.Length > 0 &&
            normalizedSelectedOverrideId is null)
        {
            throw new ArgumentException(
                "Selected override action kinds can only be declared when a winning override rule is available.",
                nameof(selectedOverrideActionKinds));
        }

        if (normalizedAppliedOverrideActionKinds.Length > 0 &&
            normalizedAppliedOverrideId is null)
        {
            throw new ArgumentException(
                "Applied override action kinds can only be declared when an applied override id is available.",
                nameof(appliedOverrideActionKinds));
        }

        if (normalizedAppliedOverrideActionKinds.Except(normalizedSelectedOverrideActionKinds).Any())
        {
            throw new ArgumentException(
                "Applied override action kinds must be a subset of the selected override action kinds when both are declared.",
                nameof(appliedOverrideActionKinds));
        }

        if (normalizedOverrideSelectionBasis.HasValue &&
            normalizedSelectedOverrideId is null)
        {
            throw new ArgumentException(
                "An override selection basis can only be declared when a winning override rule is available.",
                nameof(overrideSelectionBasis));
        }

        if (!normalizedOverrideSelectionBasis.HasValue &&
            normalizedSelectedOverrideId is not null &&
            normalizedMatchedOverrideIds.Length <= 1)
        {
            normalizedOverrideSelectionBasis = RestEndpointGovernanceRuleSelectionBasis.SingleMatch;
        }

        if ((normalizedSkippedSuppressionIds.Length > 0 || normalizedSkippedOverrideIds.Length > 0) &&
            originalProjection.AllowsHostGovernance)
        {
            throw new ArgumentException(
                "Skipped governance-rule ids can only be declared when the original projection does not allow host governance.",
                nameof(skippedSuppressionIds));
        }

        if (normalizedMatchedSuppressionIds.Intersect(normalizedSkippedSuppressionIds, StringComparer.OrdinalIgnoreCase).Any())
        {
            throw new ArgumentException(
                "A candidate cannot classify the same suppression rule as both matched and skipped.",
                nameof(skippedSuppressionIds));
        }

        if (normalizedMatchedOverrideIds.Intersect(normalizedSkippedOverrideIds, StringComparer.OrdinalIgnoreCase).Any())
        {
            throw new ArgumentException(
                "A candidate cannot classify the same override rule as both matched and skipped.",
                nameof(skippedOverrideIds));
        }

        Id = id.Trim();
        ProjectedEndpoint = projectedEndpoint;
        OriginalProjection = originalProjection;
        AuthoringStyle = authoringStyle.Trim();
        PrecedenceRank = precedenceRank;
        Status = status;
        SuppressedByCandidateId = string.IsNullOrWhiteSpace(suppressedByCandidateId)
            ? null
            : suppressedByCandidateId.Trim();
        SuppressedBySuppressionId = string.IsNullOrWhiteSpace(suppressedBySuppressionId)
            ? null
            : suppressedBySuppressionId.Trim();
        AppliedOverrideId = normalizedAppliedOverrideId;
        MatchedSuppressionIds = normalizedMatchedSuppressionIds;
        MatchedOverrideIds = normalizedMatchedOverrideIds;
        SkippedSuppressionIds = normalizedSkippedSuppressionIds;
        SkippedOverrideIds = normalizedSkippedOverrideIds;
        SelectedOverrideId = normalizedSelectedOverrideId;
        SelectedOverrideActionKinds = normalizedSelectedOverrideActionKinds;
        AppliedOverrideActionKinds = normalizedAppliedOverrideActionKinds;
        SuppressionSelectionBasis = normalizedSuppressionSelectionBasis;
        OverrideSelectionBasis = normalizedOverrideSelectionBasis;
        SuppressionReason = string.IsNullOrWhiteSpace(suppressionReason)
            ? null
            : suppressionReason.Trim();
        SuppressedByAuthoringPolicyKind = suppressedByAuthoringPolicyKind;
    }

    /// <summary>
    /// Gets the stable candidate identifier derived from the original projection before any
    /// host-level overrides are applied.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the resolved endpoint shape the candidate would publish when it wins precedence.
    /// </summary>
    public RestEndpointRuntimeDescriptor ProjectedEndpoint { get; }

    /// <summary>
    /// Gets the original projection shape contributed by the source authoring path before host-level overrides are applied.
    /// </summary>
    public RestEndpointCandidateProjectionDescriptor OriginalProjection { get; }

    /// <summary>
    /// Gets the normalized authoring style used to produce the candidate.
    /// </summary>
    public string AuthoringStyle { get; }

    /// <summary>
    /// Gets the precedence rank used during publication resolution. Lower values win.
    /// </summary>
    public int PrecedenceRank { get; }

    /// <summary>
    /// Gets whether the candidate is published or suppressed in the active runtime.
    /// </summary>
    public RestEndpointCandidateStatus Status { get; }

    /// <summary>
    /// Gets the winning candidate identifier when this candidate was suppressed.
    /// </summary>
    public string? SuppressedByCandidateId { get; }

    /// <summary>
    /// Gets the host-level suppression identifier when this candidate was suppressed by REST governance.
    /// </summary>
    public string? SuppressedBySuppressionId { get; }

    /// <summary>
    /// Gets the authoring-policy suppression kind when this candidate was suppressed by behavior-level authoring-policy enforcement.
    /// </summary>
    public RestEndpointAuthoringPolicySuppressionKind? SuppressedByAuthoringPolicyKind { get; }

    /// <summary>
    /// Gets the host-level override identifier when this candidate shape was rewritten by REST governance.
    /// </summary>
    public string? AppliedOverrideId { get; }

    /// <summary>
    /// Gets the selected host-level override identifier when one winning override rule was
    /// resolved for this candidate, even if that winning rule became a runtime no-op.
    /// </summary>
    public string? SelectedOverrideId { get; }

    /// <summary>
    /// Gets the normalized action dimensions declared by the selected override rule when one
    /// winning override rule was resolved for this candidate.
    /// </summary>
    public IReadOnlyList<RestEndpointOverrideActionKind> SelectedOverrideActionKinds { get; }

    /// <summary>
    /// Gets the normalized action dimensions that materially changed the candidate's effective
    /// runtime answer when the selected override rule was not a runtime no-op.
    /// </summary>
    public IReadOnlyList<RestEndpointOverrideActionKind> AppliedOverrideActionKinds { get; }

    /// <summary>
    /// Gets the earliest decisive specificity rule that selected the winning suppression rule when
    /// this candidate was suppressed by REST governance.
    /// </summary>
    public RestEndpointGovernanceRuleSelectionBasis? SuppressionSelectionBasis { get; }

    /// <summary>
    /// Gets the earliest decisive specificity rule that selected the winning override rule when one
    /// was resolved for this candidate.
    /// </summary>
    public RestEndpointGovernanceRuleSelectionBasis? OverrideSelectionBasis { get; }

    /// <summary>
    /// Gets the ordered suppression-rule identifiers that matched this candidate before one winner was selected.
    /// </summary>
    public IReadOnlyList<string> MatchedSuppressionIds { get; }

    /// <summary>
    /// Gets the ordered override-rule identifiers that matched this candidate before one winner was selected.
    /// </summary>
    public IReadOnlyList<string> MatchedOverrideIds { get; }

    /// <summary>
    /// Gets the ordered suppression-rule identifiers that otherwise target this candidate but were
    /// skipped because the original projection did not allow host governance.
    /// </summary>
    public IReadOnlyList<string> SkippedSuppressionIds { get; }

    /// <summary>
    /// Gets the ordered override-rule identifiers that otherwise target this candidate but were
    /// skipped because the original projection did not allow host governance.
    /// </summary>
    public IReadOnlyList<string> SkippedOverrideIds { get; }

    /// <summary>
    /// Gets the operator-facing suppression reason when one is available.
    /// </summary>
    public string? SuppressionReason { get; }

    private static RestEndpointGovernanceRuleSelectionBasis? NormalizeSelectionBasis(
        RestEndpointGovernanceRuleSelectionBasis? value,
        string paramName)
    {
        if (!value.HasValue)
        {
            return null;
        }

        if (!Enum.IsDefined(value.Value) ||
            value.Value == RestEndpointGovernanceRuleSelectionBasis.Unspecified)
        {
            throw new ArgumentException(
                "A supported REST endpoint governance rule selection basis is required when one is declared.",
                paramName);
        }

        return value.Value;
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

    private static RestEndpointOverrideActionKind[] NormalizeActionKinds(
        IReadOnlyList<RestEndpointOverrideActionKind>? values,
        string paramName)
    {
        if (values is null || values.Count == 0)
        {
            return [];
        }

        var normalized = values
            .Distinct()
            .OrderBy(static value => value)
            .ToArray();
        if (normalized.Any(static value =>
                !Enum.IsDefined(value) ||
                value == RestEndpointOverrideActionKind.Unspecified))
        {
            throw new ArgumentException(
                "A supported REST endpoint override action kind is required when one is declared.",
                paramName);
        }

        return normalized;
    }
}
