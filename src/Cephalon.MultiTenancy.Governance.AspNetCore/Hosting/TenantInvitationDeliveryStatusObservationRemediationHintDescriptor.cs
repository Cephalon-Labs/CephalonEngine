namespace Cephalon.MultiTenancy.Governance.AspNetCore.Hosting;

/// <summary>
/// Describes deterministic operator remediation guidance for matched delivery status observations.
/// </summary>
/// <remarks>
/// The descriptor is an aggregate hint over normalized observations that already matched the read filters. It is
/// guidance for an operator or host workflow, not an executed remediation, provider polling result, distributed inbox,
/// or exactly-once delivery guarantee.
/// </remarks>
public sealed class TenantInvitationDeliveryStatusObservationRemediationHintDescriptor
{
    /// <summary>
    /// Creates a tenant-invitation delivery status observation remediation hint descriptor.
    /// </summary>
    /// <param name="attentionCategory">The attention category that produced this hint.</param>
    /// <param name="action">The stable remediation action label.</param>
    /// <param name="displayName">The short operator-facing display name.</param>
    /// <param name="description">The remediation guidance for this attention category.</param>
    /// <param name="count">The number of matched observations in this hint bucket.</param>
    /// <param name="latestObservedAtUtc">The latest observed timestamp in the hint bucket.</param>
    /// <param name="latestRecordedAtUtc">The latest recorded timestamp in the hint bucket.</param>
    /// <param name="filter">A query-string filter that drills into the relevant observations.</param>
    public TenantInvitationDeliveryStatusObservationRemediationHintDescriptor(
        string attentionCategory,
        string action,
        string displayName,
        string description,
        int count,
        DateTimeOffset latestObservedAtUtc,
        DateTimeOffset latestRecordedAtUtc,
        string filter)
    {
        if (string.IsNullOrWhiteSpace(attentionCategory))
        {
            throw new ArgumentException("Attention category is required.", nameof(attentionCategory));
        }

        if (string.IsNullOrWhiteSpace(action))
        {
            throw new ArgumentException("Remediation action is required.", nameof(action));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Description is required.", nameof(description));
        }

        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, "Count cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(filter))
        {
            throw new ArgumentException("Filter is required.", nameof(filter));
        }

        AttentionCategory = attentionCategory.Trim();
        Action = action.Trim();
        DisplayName = displayName.Trim();
        Description = description.Trim();
        Count = count;
        LatestObservedAtUtc = latestObservedAtUtc;
        LatestRecordedAtUtc = latestRecordedAtUtc;
        Filter = filter.Trim();
    }

    /// <summary>
    /// Gets the attention category that produced this hint.
    /// </summary>
    public string AttentionCategory { get; }

    /// <summary>
    /// Gets the stable remediation action label.
    /// </summary>
    public string Action { get; }

    /// <summary>
    /// Gets the short operator-facing display name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the remediation guidance for this attention category.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the number of matched observations in this hint bucket.
    /// </summary>
    public int Count { get; }

    /// <summary>
    /// Gets the latest observed timestamp in the hint bucket.
    /// </summary>
    public DateTimeOffset LatestObservedAtUtc { get; }

    /// <summary>
    /// Gets the latest recorded timestamp in the hint bucket.
    /// </summary>
    public DateTimeOffset LatestRecordedAtUtc { get; }

    /// <summary>
    /// Gets a query-string filter that drills into the relevant observations.
    /// </summary>
    public string Filter { get; }
}
