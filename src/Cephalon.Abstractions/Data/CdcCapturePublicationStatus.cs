namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes the publication posture currently visible for one CDC capture.
/// </summary>
public sealed record CdcCapturePublicationStatus
{
    /// <summary>
    /// Creates a new CDC publication status.
    /// </summary>
    /// <param name="state">
    /// The stable publication-state identifier, such as <c>unknown</c>, <c>pending-publication</c>, or <c>dispatch-retry-pending</c>.
    /// </param>
    /// <param name="pendingPublicationCount">
    /// The number of pending publications still waiting to flow through the linked outbox path when the provider can report that answer.
    /// </param>
    /// <param name="description">An optional operator-facing publication summary.</param>
    public CdcCapturePublicationStatus(
        string state,
        long? pendingPublicationCount = null,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            throw new ArgumentException("Publication state is required.", nameof(state));
        }

        if (pendingPublicationCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pendingPublicationCount),
                pendingPublicationCount,
                "Pending publication count must be greater than or equal to 0.");
        }

        State = state.Trim();
        PendingPublicationCount = pendingPublicationCount;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    /// <summary>
    /// Gets the stable publication-state identifier.
    /// </summary>
    public string State { get; }

    /// <summary>
    /// Gets the number of pending publications still waiting to flow through the linked outbox path when one is known.
    /// </summary>
    public long? PendingPublicationCount { get; }

    /// <summary>
    /// Gets an optional operator-facing publication summary.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets a value indicating whether the capture still has pending publications.
    /// </summary>
    public bool HasPendingPublications => PendingPublicationCount is > 0;
}
