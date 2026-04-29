namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Exposes runtime tenant invitation delivery dispatch attempts observed by the governance companion pack.
/// </summary>
public interface ITenantInvitationDeliveryRunCatalog
{
    /// <summary>
    /// Gets the recorded tenant invitation delivery dispatch attempts.
    /// </summary>
    IReadOnlyList<TenantInvitationDeliveryRunDescriptor> Runs { get; }

    /// <summary>
    /// Gets the latest recorded tenant invitation delivery dispatch attempt when one exists.
    /// </summary>
    TenantInvitationDeliveryRunDescriptor? LatestRun { get; }

    /// <summary>
    /// Gets the number of recorded tenant invitation delivery dispatch attempts.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Gets recorded tenant invitation delivery dispatch attempts for one tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <returns>The matching dispatch attempts.</returns>
    IReadOnlyList<TenantInvitationDeliveryRunDescriptor> GetByTenantId(string tenantId);

    /// <summary>
    /// Gets recorded tenant invitation delivery dispatch attempts for one invitation identifier.
    /// </summary>
    /// <param name="invitationId">The invitation identifier.</param>
    /// <returns>The matching dispatch attempts.</returns>
    IReadOnlyList<TenantInvitationDeliveryRunDescriptor> GetByInvitationId(string invitationId);
}
