namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes the provider-facing freshness posture currently visible for one CDC capture.
/// </summary>
public sealed record CdcCaptureFreshnessStatus
{
    /// <summary>
    /// Creates a new CDC freshness status.
    /// </summary>
    /// <param name="state">
    /// The stable freshness-state identifier, such as <c>unknown</c>, <c>fresh</c>, or <c>stale</c>.
    /// </param>
    /// <param name="freshUntilUtc">
    /// The UTC timestamp until which the active runtime expects the current observation to remain fresh when one is known.
    /// </param>
    /// <param name="description">An optional operator-facing freshness summary.</param>
    public CdcCaptureFreshnessStatus(
        string state,
        DateTimeOffset? freshUntilUtc = null,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            throw new ArgumentException("Freshness state is required.", nameof(state));
        }

        State = state.Trim();
        FreshUntilUtc = freshUntilUtc;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    /// <summary>
    /// Gets the stable freshness-state identifier.
    /// </summary>
    public string State { get; }

    /// <summary>
    /// Gets the UTC timestamp until which the current capture observation remains fresh when one is known.
    /// </summary>
    public DateTimeOffset? FreshUntilUtc { get; }

    /// <summary>
    /// Gets an optional operator-facing freshness summary.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets a value indicating whether a freshness window is currently known.
    /// </summary>
    public bool HasWindow => FreshUntilUtc.HasValue;
}
