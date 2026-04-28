namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Exposes the merged tenant-invitation set available to the active governance runtime.
/// </summary>
public interface ITenantInvitationCatalog
{
    /// <summary>
    /// Gets the effective invitation set after host options and module contributors have both been applied.
    /// </summary>
    IReadOnlyList<TenantInvitationDescriptor> Invitations { get; }

    /// <summary>
    /// Gets invitations for one tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier to resolve.</param>
    /// <returns>The matching invitations.</returns>
    IReadOnlyList<TenantInvitationDescriptor> GetByTenantId(string tenantId);

    /// <summary>
    /// Gets invitations for one invitee across all tenants.
    /// </summary>
    /// <param name="inviteeId">The invitee identifier to resolve.</param>
    /// <returns>The matching invitations.</returns>
    IReadOnlyList<TenantInvitationDescriptor> GetByInviteeId(string inviteeId);

    /// <summary>
    /// Gets invitations by invitation identifier across all tenants.
    /// </summary>
    /// <param name="invitationId">The invitation identifier to resolve.</param>
    /// <returns>The matching invitations.</returns>
    IReadOnlyList<TenantInvitationDescriptor> GetByInvitationId(string invitationId);

    /// <summary>
    /// Gets invitations by tenant and invitation identifier.
    /// </summary>
    /// <param name="tenantId">The tenant identifier to resolve.</param>
    /// <param name="invitationId">The invitation identifier to resolve.</param>
    /// <returns>The matching invitations.</returns>
    IReadOnlyList<TenantInvitationDescriptor> GetByTenantAndInvitation(string tenantId, string invitationId);
}
