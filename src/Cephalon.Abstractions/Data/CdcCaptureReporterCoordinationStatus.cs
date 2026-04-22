namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes the reporter-coordination posture currently visible for one CDC capture or execution runtime.
/// </summary>
public sealed record CdcCaptureReporterCoordinationStatus
{
    /// <summary>
    /// Creates a new CDC reporter-coordination status.
    /// </summary>
    /// <param name="state">
    /// The stable reporter-coordination state, such as <c>active</c>, <c>lease-expired</c>, or <c>conflicted</c>.
    /// </param>
    /// <param name="description">An optional operator-facing reporter-coordination summary.</param>
    public CdcCaptureReporterCoordinationStatus(
        string state,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            throw new ArgumentException("Reporter coordination state is required.", nameof(state));
        }

        State = state.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    /// <summary>
    /// Gets the stable reporter-coordination state.
    /// </summary>
    public string State { get; }

    /// <summary>
    /// Gets an optional operator-facing reporter-coordination summary.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the reporter identity that currently holds the active lease when one is known.
    /// </summary>
    public string? ActiveReporterId { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the active reporter lease expires when one is known.
    /// </summary>
    public DateTimeOffset? ActiveReporterLeaseExpiresAtUtc { get; init; }

    /// <summary>
    /// Gets the previous active reporter identity when the current reporter took over after lease expiry.
    /// </summary>
    public string? PreviousReporterId { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the previous reporter lease expired before failover or takeover when one is known.
    /// </summary>
    public DateTimeOffset? LeaseExpiredAtUtc { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the current reporter most recently took over after the previous lease expired.
    /// </summary>
    public DateTimeOffset? LastTakeoverObservedAtUtc { get; init; }

    /// <summary>
    /// Gets the last conflicting reporter identity that was observed or rejected when one is known.
    /// </summary>
    public string? LastConflictingReporterId { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the last conflicting reporter was observed or rejected when one is known.
    /// </summary>
    public DateTimeOffset? LastConflictedAtUtc { get; init; }

    /// <summary>
    /// Gets the reporter participants currently visible in the coordination story.
    /// </summary>
    public IReadOnlyList<CdcCaptureReporterParticipantStatus> ReporterParticipants { get; init; } = [];

    /// <summary>
    /// Gets a value indicating whether the coordination answer currently has one active reporter owner.
    /// </summary>
    public bool HasActiveReporter => !string.IsNullOrWhiteSpace(ActiveReporterId);

    /// <summary>
    /// Gets a value indicating whether the coordination answer currently carries standby reporter evidence.
    /// </summary>
    public bool HasStandbyReporters => ReporterParticipants.Any(static participant =>
        string.Equals(participant.Role, CdcCaptureReporterParticipantRoles.Standby, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Gets a value indicating whether the coordination answer currently carries rejected reporter evidence.
    /// </summary>
    public bool HasRejectedReporters => ReporterParticipants.Any(static participant =>
        string.Equals(participant.Role, CdcCaptureReporterParticipantRoles.Rejected, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Gets a value indicating whether the coordination answer currently reports degraded reporter ownership.
    /// </summary>
    public bool IsDegraded => string.Equals(State, CdcCaptureReporterCoordinationStates.Conflicted, StringComparison.OrdinalIgnoreCase);
}
