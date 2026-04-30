namespace Cephalon.MultiTenancy.Governance.AspNetCore.Hosting;

/// <summary>
/// Defines stable attention categories for tenant-invitation delivery status observation reads.
/// </summary>
/// <remarks>
/// Attention categories are derived from normalized observations already stored by the governance core. They are
/// operator drill-down labels only; they do not represent provider polling, callback inbox ownership, distributed
/// replay, or exactly-once delivery semantics.
/// </remarks>
public static class TenantInvitationDeliveryStatusObservationAttentionCategories
{
    /// <summary>
    /// The observation reports a failed or bounced delivery status.
    /// </summary>
    public const string DeliveryFailed = "delivery-failed";

    /// <summary>
    /// The observation reports a deferred delivery status.
    /// </summary>
    public const string DeliveryDeferred = "delivery-deferred";

    /// <summary>
    /// The observation reports a suppressed delivery status.
    /// </summary>
    public const string DeliverySuppressed = "delivery-suppressed";

    /// <summary>
    /// The observation reports an unknown delivery status.
    /// </summary>
    public const string DeliveryUnknown = "delivery-unknown";

    /// <summary>
    /// The observation did not reconcile into invitation state.
    /// </summary>
    public const string ReconciliationGap = "reconciliation-gap";

    /// <summary>
    /// The observation did not record invitation delivery status metadata.
    /// </summary>
    public const string RecordingGap = "recording-gap";

    internal static string KnownValues =>
        string.Join(
            ", ",
            [
                DeliveryFailed,
                DeliveryDeferred,
                DeliverySuppressed,
                DeliveryUnknown,
                ReconciliationGap,
                RecordingGap
            ]);

    internal static string? Normalize(string category)
    {
        var normalized = category.Trim().ToLowerInvariant();
        return normalized switch
        {
            DeliveryFailed => DeliveryFailed,
            DeliveryDeferred => DeliveryDeferred,
            DeliverySuppressed => DeliverySuppressed,
            DeliveryUnknown => DeliveryUnknown,
            ReconciliationGap => ReconciliationGap,
            RecordingGap => RecordingGap,
            _ => null
        };
    }
}
