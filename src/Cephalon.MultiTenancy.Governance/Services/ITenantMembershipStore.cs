namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Stores runtime tenant memberships managed by the multi-tenancy governance companion pack.
/// </summary>
public interface ITenantMembershipStore
{
    /// <summary>
    /// Gets the operator-facing store kind.
    /// </summary>
    string StoreKind { get; }

    /// <summary>
    /// Gets a value indicating whether membership state survives process restarts.
    /// </summary>
    bool IsDurable { get; }

    /// <summary>
    /// Gets the ownership mode for the store implementation.
    /// </summary>
    string Ownership { get; }

    /// <summary>
    /// Gets the stored runtime tenant memberships.
    /// </summary>
    IReadOnlyList<TenantMembershipDescriptor> Memberships { get; }

    /// <summary>
    /// Gets the number of stored runtime tenant memberships.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Creates or replaces one stored runtime tenant membership.
    /// </summary>
    /// <param name="membership">The tenant membership to store.</param>
    void Upsert(TenantMembershipDescriptor membership);
}
