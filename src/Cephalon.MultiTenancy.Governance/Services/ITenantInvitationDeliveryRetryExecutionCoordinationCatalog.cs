namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Exposes runtime state for process-local tenant-invitation delivery retry execution coordination.
/// </summary>
/// <remarks>
/// The catalog reports the in-process overlap guard used by bounded retry runner passes. It does not
/// represent distributed retry leases, cross-node exactly-once delivery, or provider-specific sender ownership.
/// </remarks>
public interface ITenantInvitationDeliveryRetryExecutionCoordinationCatalog
{
    /// <summary>
    /// Gets the latest tenant-invitation delivery retry execution coordination snapshot.
    /// </summary>
    TenantInvitationDeliveryRetryExecutionCoordinationSnapshot Current { get; }
}
