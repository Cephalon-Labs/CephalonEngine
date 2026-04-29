namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Exposes runtime state for automatic tenant-invitation delivery retry scheduling.
/// </summary>
/// <remarks>
/// The catalog reports the opt-in background retry hosted-service posture. It does not
/// represent distributed retry leases, cross-node exactly-once delivery, or provider-specific sender ownership.
/// </remarks>
public interface ITenantInvitationDeliveryRetryRuntimeCatalog
{
    /// <summary>
    /// Gets the latest tenant-invitation delivery retry scheduling runtime snapshot.
    /// </summary>
    TenantInvitationDeliveryRetryRuntimeSnapshot Current { get; }
}
