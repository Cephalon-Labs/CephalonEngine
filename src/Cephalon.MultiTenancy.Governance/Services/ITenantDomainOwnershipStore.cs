namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Stores runtime tenant-domain ownership declarations managed by the multi-tenancy governance companion pack.
/// </summary>
public interface ITenantDomainOwnershipStore
{
    /// <summary>
    /// Gets the operator-facing store kind.
    /// </summary>
    string StoreKind { get; }

    /// <summary>
    /// Gets a value indicating whether tenant-domain ownership state survives process restarts.
    /// </summary>
    bool IsDurable { get; }

    /// <summary>
    /// Gets the ownership mode for the store implementation.
    /// </summary>
    string Ownership { get; }

    /// <summary>
    /// Gets the stored runtime tenant-domain ownership declarations.
    /// </summary>
    IReadOnlyList<TenantDomainOwnershipDescriptor> DomainOwnerships { get; }

    /// <summary>
    /// Gets the number of stored runtime tenant-domain ownership declarations.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Creates or replaces one stored runtime tenant-domain ownership declaration.
    /// </summary>
    /// <param name="domainOwnership">The tenant-domain ownership declaration to store.</param>
    void Upsert(TenantDomainOwnershipDescriptor domainOwnership);
}
