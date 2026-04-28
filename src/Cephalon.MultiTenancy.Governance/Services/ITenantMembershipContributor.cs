namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Allows a module to contribute tenant memberships into the active governance runtime.
/// </summary>
public interface ITenantMembershipContributor
{
    /// <summary>
    /// Registers one or more tenant memberships with the supplied registry.
    /// </summary>
    /// <param name="memberships">The registry that collects contributed memberships.</param>
    void RegisterMemberships(ITenantMembershipRegistry memberships);
}
