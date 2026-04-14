namespace Cephalon.Abstractions.Transports;

/// <summary>
/// Describes one module-owned REST endpoint candidate and whether it was published or suppressed.
/// </summary>
public sealed class RestEndpointCandidateRuntimeDescriptor
{
    /// <summary>
    /// Creates a REST endpoint candidate runtime descriptor.
    /// </summary>
    /// <param name="id">The stable candidate identifier.</param>
    /// <param name="projectedEndpoint">
    /// The resolved endpoint shape the candidate would publish when it wins precedence.
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
    /// <param name="suppressionReason">The operator-facing suppression reason when one is available.</param>
    public RestEndpointCandidateRuntimeDescriptor(
        string id,
        RestEndpointRuntimeDescriptor projectedEndpoint,
        string authoringStyle,
        int precedenceRank,
        RestEndpointCandidateStatus status,
        string? suppressedByCandidateId = null,
        string? suppressedBySuppressionId = null,
        string? suppressionReason = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("A non-empty candidate id is required.", nameof(id));
        }

        ArgumentNullException.ThrowIfNull(projectedEndpoint);

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

        if (status == RestEndpointCandidateStatus.Published &&
            (!string.IsNullOrWhiteSpace(suppressedByCandidateId) ||
            !string.IsNullOrWhiteSpace(suppressedBySuppressionId) ||
            !string.IsNullOrWhiteSpace(suppressionReason)))
        {
            throw new ArgumentException(
                "Published candidates cannot declare suppression metadata.",
                nameof(status));
        }

        if (!string.IsNullOrWhiteSpace(suppressedByCandidateId) &&
            !string.IsNullOrWhiteSpace(suppressedBySuppressionId))
        {
            throw new ArgumentException(
                "A suppressed candidate cannot declare both precedence and governance suppression ids.",
                nameof(suppressedByCandidateId));
        }

        Id = id.Trim();
        ProjectedEndpoint = projectedEndpoint;
        AuthoringStyle = authoringStyle.Trim();
        PrecedenceRank = precedenceRank;
        Status = status;
        SuppressedByCandidateId = string.IsNullOrWhiteSpace(suppressedByCandidateId)
            ? null
            : suppressedByCandidateId.Trim();
        SuppressedBySuppressionId = string.IsNullOrWhiteSpace(suppressedBySuppressionId)
            ? null
            : suppressedBySuppressionId.Trim();
        SuppressionReason = string.IsNullOrWhiteSpace(suppressionReason)
            ? null
            : suppressionReason.Trim();
    }

    /// <summary>
    /// Gets the stable candidate identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the resolved endpoint shape the candidate would publish when it wins precedence.
    /// </summary>
    public RestEndpointRuntimeDescriptor ProjectedEndpoint { get; }

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
    /// Gets the operator-facing suppression reason when one is available.
    /// </summary>
    public string? SuppressionReason { get; }
}
