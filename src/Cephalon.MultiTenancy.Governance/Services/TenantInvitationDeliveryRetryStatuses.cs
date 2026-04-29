namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Defines stable statuses for tenant invitation delivery retry queue entries.
/// </summary>
public static class TenantInvitationDeliveryRetryStatuses
{
    /// <summary>
    /// The retry entry is waiting for another attempt.
    /// </summary>
    public const string Pending = "pending";

    /// <summary>
    /// The retry entry was dispatched and removed from the active retry queue.
    /// </summary>
    public const string Dispatched = "dispatched";

    /// <summary>
    /// The retry entry exhausted its configured retry budget.
    /// </summary>
    public const string Exhausted = "exhausted";

    /// <summary>
    /// The retry entry hit a terminal invitation state that should not be retried.
    /// </summary>
    public const string Terminal = "terminal";
}
