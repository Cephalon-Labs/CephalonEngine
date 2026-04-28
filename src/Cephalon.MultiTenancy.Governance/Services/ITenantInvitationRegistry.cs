namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Collects tenant invitations contributed to the active governance runtime.
/// </summary>
public interface ITenantInvitationRegistry
{
    /// <summary>
    /// Adds a tenant-invitation descriptor to the registry.
    /// </summary>
    /// <param name="invitation">The invitation descriptor to contribute.</param>
    void Add(TenantInvitationDescriptor invitation);
}
