namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Defines stable outcomes for tenant invitation delivery status reconciliation.
/// </summary>
public static class TenantInvitationDeliveryStatusReconciliationOutcomes
{
    /// <summary>
    /// The delivery status observation was reconciled into invitation metadata.
    /// </summary>
    public const string Reconciled = "reconciled";

    /// <summary>
    /// Delivery status reconciliation is disabled.
    /// </summary>
    public const string Disabled = "disabled";

    /// <summary>
    /// The requested invitation was not found.
    /// </summary>
    public const string InvitationNotFound = "invitation-not-found";

    /// <summary>
    /// The invitation has a recorded provider message identifier, but the reconciliation request did not provide one.
    /// </summary>
    public const string ProviderMessageMissing = "provider-message-missing";

    /// <summary>
    /// The supplied provider message identifier does not match the identifier recorded during dispatch.
    /// </summary>
    public const string ProviderMessageMismatch = "provider-message-mismatch";

    /// <summary>
    /// The delivery status observation could not be persisted.
    /// </summary>
    public const string StoreFailed = "store-failed";
}
