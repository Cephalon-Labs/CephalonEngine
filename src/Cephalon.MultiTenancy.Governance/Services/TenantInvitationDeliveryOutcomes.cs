namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Defines stable outcomes for tenant invitation delivery dispatch.
/// </summary>
public static class TenantInvitationDeliveryOutcomes
{
    /// <summary>
    /// The invitation was dispatched through a configured sender.
    /// </summary>
    public const string Dispatched = "dispatched";

    /// <summary>
    /// Invitation delivery dispatch is disabled.
    /// </summary>
    public const string Disabled = "disabled";

    /// <summary>
    /// The requested invitation was not found.
    /// </summary>
    public const string InvitationNotFound = "invitation-not-found";

    /// <summary>
    /// The requested invitation is no longer pending.
    /// </summary>
    public const string InvitationNotPending = "invitation-not-pending";

    /// <summary>
    /// The requested invitation expired before dispatch.
    /// </summary>
    public const string InvitationExpired = "invitation-expired";

    /// <summary>
    /// No matching delivery sender was registered.
    /// </summary>
    public const string SenderNotConfigured = "sender-not-configured";

    /// <summary>
    /// The sender failed or returned a failed outcome.
    /// </summary>
    public const string SenderFailed = "sender-failed";

    /// <summary>
    /// The sender deliberately suppressed delivery.
    /// </summary>
    public const string Suppressed = "suppressed";

    /// <summary>
    /// The dispatch outcome could not be persisted.
    /// </summary>
    public const string StoreFailed = "store-failed";
}
