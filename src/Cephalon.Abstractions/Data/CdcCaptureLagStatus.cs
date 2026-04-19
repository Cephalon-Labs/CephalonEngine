namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes the provider-facing lag posture currently visible for one CDC capture.
/// </summary>
public sealed record CdcCaptureLagStatus
{
    /// <summary>
    /// Creates a new CDC lag status.
    /// </summary>
    /// <param name="state">
    /// The stable lag-state identifier, such as <c>unknown</c>, <c>current</c>, <c>lagging</c>, or <c>backfilling</c>.
    /// </param>
    /// <param name="pendingChangeCount">
    /// The number of source-side changes still pending capture when the provider can report that answer.
    /// </param>
    /// <param name="description">An optional operator-facing lag summary.</param>
    public CdcCaptureLagStatus(
        string state,
        long? pendingChangeCount = null,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            throw new ArgumentException("Lag state is required.", nameof(state));
        }

        if (pendingChangeCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pendingChangeCount),
                pendingChangeCount,
                "Pending change count must be greater than or equal to 0.");
        }

        State = state.Trim();
        PendingChangeCount = pendingChangeCount;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    /// <summary>
    /// Gets the stable lag-state identifier.
    /// </summary>
    public string State { get; }

    /// <summary>
    /// Gets the number of source-side changes still pending capture when the provider reports that answer.
    /// </summary>
    public long? PendingChangeCount { get; }

    /// <summary>
    /// Gets an optional operator-facing lag summary.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets a value indicating whether the capture still has pending source-side changes.
    /// </summary>
    public bool HasPendingChanges => PendingChangeCount is > 0;
}
