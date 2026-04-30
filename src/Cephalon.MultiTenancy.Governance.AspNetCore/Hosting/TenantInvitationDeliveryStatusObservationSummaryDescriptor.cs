namespace Cephalon.MultiTenancy.Governance.AspNetCore.Hosting;

/// <summary>
/// Describes one aggregate bucket in a tenant-invitation delivery status observation read.
/// </summary>
/// <remarks>
/// Observation summaries are derived from the normalized observation store after endpoint filters are applied and
/// before the response limit is applied. They are operator rollups over recorded observations, not a provider callback
/// inbox, distributed replay ledger, or exactly-once delivery proof.
/// </remarks>
public sealed class TenantInvitationDeliveryStatusObservationSummaryDescriptor
{
    /// <summary>
    /// Creates a tenant-invitation delivery status observation summary descriptor.
    /// </summary>
    /// <param name="dimension">The summarized observation dimension, such as <c>status</c> or <c>source</c>.</param>
    /// <param name="value">The normalized bucket value for the dimension.</param>
    /// <param name="count">The number of observations in the bucket.</param>
    /// <param name="reconciledCount">The number of reconciled observations in the bucket.</param>
    /// <param name="recordedCount">The number of observations that recorded invitation delivery metadata in the bucket.</param>
    /// <param name="latestObservedAtUtc">The latest provider observation timestamp in the bucket.</param>
    /// <param name="latestRecordedAtUtc">The latest Cephalon record timestamp in the bucket.</param>
    public TenantInvitationDeliveryStatusObservationSummaryDescriptor(
        string dimension,
        string value,
        int count,
        int reconciledCount,
        int recordedCount,
        DateTimeOffset latestObservedAtUtc,
        DateTimeOffset latestRecordedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(dimension))
        {
            throw new ArgumentException("Summary dimension is required.", nameof(dimension));
        }

        Dimension = dimension.Trim();
        Value = string.IsNullOrWhiteSpace(value) ? "none" : value.Trim();
        Count = count;
        ReconciledCount = reconciledCount;
        RecordedCount = recordedCount;
        LatestObservedAtUtc = latestObservedAtUtc;
        LatestRecordedAtUtc = latestRecordedAtUtc;
    }

    /// <summary>
    /// Gets the summarized observation dimension, such as <c>status</c>, <c>attention</c>, <c>outcome</c>,
    /// <c>source</c>, <c>channel</c>, <c>sender</c>, or <c>tenant</c>.
    /// </summary>
    public string Dimension { get; }

    /// <summary>
    /// Gets the normalized bucket value for the dimension.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Gets the number of observations in the bucket.
    /// </summary>
    public int Count { get; }

    /// <summary>
    /// Gets the number of reconciled observations in the bucket.
    /// </summary>
    public int ReconciledCount { get; }

    /// <summary>
    /// Gets the number of observations that recorded invitation delivery metadata in the bucket.
    /// </summary>
    public int RecordedCount { get; }

    /// <summary>
    /// Gets the latest provider observation timestamp in the bucket.
    /// </summary>
    public DateTimeOffset LatestObservedAtUtc { get; }

    /// <summary>
    /// Gets the latest Cephalon record timestamp in the bucket.
    /// </summary>
    public DateTimeOffset LatestRecordedAtUtc { get; }
}
