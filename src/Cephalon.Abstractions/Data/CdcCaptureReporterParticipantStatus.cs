namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes one reporter participant currently visible in the CDC reporter-coordination story for one execution runtime.
/// </summary>
public sealed record CdcCaptureReporterParticipantStatus
{
    /// <summary>
    /// Creates a new CDC reporter participant status.
    /// </summary>
    /// <param name="reporterId">The stable reporter identity.</param>
    /// <param name="role">
    /// The stable reporter role, such as <c>active</c>, <c>standby</c>, or <c>rejected</c>.
    /// </param>
    /// <param name="description">An optional operator-facing participant summary.</param>
    public CdcCaptureReporterParticipantStatus(
        string reporterId,
        string role,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(reporterId))
        {
            throw new ArgumentException("Reporter id is required.", nameof(reporterId));
        }

        if (string.IsNullOrWhiteSpace(role))
        {
            throw new ArgumentException("Reporter role is required.", nameof(role));
        }

        ReporterId = reporterId.Trim();
        Role = role.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    /// <summary>
    /// Gets the stable reporter identity.
    /// </summary>
    public string ReporterId { get; }

    /// <summary>
    /// Gets the stable reporter role visible in the current coordination story.
    /// </summary>
    public string Role { get; }

    /// <summary>
    /// Gets an optional operator-facing participant summary.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the UTC timestamp when this reporter most recently produced an accepted or rejected observation.
    /// </summary>
    public DateTimeOffset? LastObservedAtUtc { get; init; }

    /// <summary>
    /// Gets the latest observed lease expiry for the reporter when one is known.
    /// </summary>
    public DateTimeOffset? LeaseExpiresAtUtc { get; init; }

    /// <summary>
    /// Gets the latest CDC capture identifier associated with this reporter when one is known.
    /// </summary>
    public string? LastCdcCaptureId { get; init; }

    /// <summary>
    /// Gets the edge-node identifiers most recently associated with this reporter.
    /// </summary>
    public IReadOnlyList<string> ObservedEdgeNodeIds { get; init; } = [];
}
