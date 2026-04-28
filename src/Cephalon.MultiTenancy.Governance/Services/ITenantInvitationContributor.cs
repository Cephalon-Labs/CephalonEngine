namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Allows a module to contribute tenant invitations into the active governance runtime.
/// </summary>
public interface ITenantInvitationContributor
{
    /// <summary>
    /// Registers one or more tenant invitations with the supplied registry.
    /// </summary>
    /// <param name="invitations">The registry that collects contributed invitations.</param>
    void RegisterInvitations(ITenantInvitationRegistry invitations);
}
