namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Exposes the merged tenant-membership set available to the active governance runtime.
/// </summary>
public interface ITenantMembershipCatalog
{
    /// <summary>
    /// Gets the effective membership set after host options and module contributors have both been applied.
    /// </summary>
    IReadOnlyList<TenantMembershipDescriptor> Memberships { get; }

    /// <summary>
    /// Gets memberships for one tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier to resolve.</param>
    /// <returns>The matching memberships.</returns>
    IReadOnlyList<TenantMembershipDescriptor> GetByTenantId(string tenantId);

    /// <summary>
    /// Gets memberships for one principal across all tenants.
    /// </summary>
    /// <param name="principalId">The principal identifier to resolve.</param>
    /// <returns>The matching memberships.</returns>
    IReadOnlyList<TenantMembershipDescriptor> GetByPrincipalId(string principalId);

    /// <summary>
    /// Gets memberships for one principal in one tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier to resolve.</param>
    /// <param name="principalId">The principal identifier to resolve.</param>
    /// <returns>The matching memberships.</returns>
    IReadOnlyList<TenantMembershipDescriptor> GetByTenantAndPrincipal(string tenantId, string principalId);

    /// <summary>
    /// Gets memberships for one principal kind and principal identifier in one tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier to resolve.</param>
    /// <param name="principalKind">The principal kind to resolve.</param>
    /// <param name="principalId">The principal identifier to resolve.</param>
    /// <returns>The matching memberships.</returns>
    IReadOnlyList<TenantMembershipDescriptor> GetByTenantPrincipalAndKind(string tenantId, string principalKind, string principalId);
}
