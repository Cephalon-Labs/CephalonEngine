namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Exposes runtime state for tenant-domain ownership proof polling.
/// </summary>
/// <remarks>
/// The catalog reports the background polling hosted-service posture. It does not
/// represent DNS or HTTP proof publication ownership.
/// </remarks>
public interface ITenantDomainOwnershipProofPollingRuntimeCatalog
{
    /// <summary>
    /// Gets the latest tenant-domain ownership proof polling runtime snapshot.
    /// </summary>
    TenantDomainOwnershipProofPollingRuntimeSnapshot Current { get; }
}
