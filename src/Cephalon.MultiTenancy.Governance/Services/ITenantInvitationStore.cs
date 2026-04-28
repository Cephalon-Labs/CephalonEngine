namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Stores runtime tenant invitations managed by the multi-tenancy governance companion pack.
/// </summary>
public interface ITenantInvitationStore
{
    /// <summary>
    /// Gets the operator-facing store kind.
    /// </summary>
    string StoreKind { get; }

    /// <summary>
    /// Gets a value indicating whether invitation state survives process restarts.
    /// </summary>
    bool IsDurable { get; }

    /// <summary>
    /// Gets the ownership mode for the store implementation.
    /// </summary>
    string Ownership { get; }

    /// <summary>
    /// Gets the stored runtime tenant invitations.
    /// </summary>
    IReadOnlyList<TenantInvitationDescriptor> Invitations { get; }

    /// <summary>
    /// Gets the number of stored runtime tenant invitations.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Creates or replaces one stored runtime tenant invitation.
    /// </summary>
    /// <param name="invitation">The tenant invitation to store.</param>
    void Upsert(TenantInvitationDescriptor invitation);
}
