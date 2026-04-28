namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Stores runtime tenant-governance actions created or transitioned by the governance action workflow.
/// </summary>
public interface ITenantGovernanceActionStore
{
    /// <summary>
    /// Gets the operator-facing store kind.
    /// </summary>
    string StoreKind { get; }

    /// <summary>
    /// Gets a value indicating whether action state survives process restarts.
    /// </summary>
    bool IsDurable { get; }

    /// <summary>
    /// Gets the ownership mode for the store implementation.
    /// </summary>
    string Ownership { get; }

    /// <summary>
    /// Gets the stored runtime tenant-governance actions.
    /// </summary>
    IReadOnlyList<TenantGovernanceActionDescriptor> Actions { get; }

    /// <summary>
    /// Gets the number of stored runtime tenant-governance actions.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Creates or replaces one stored runtime tenant-governance action.
    /// </summary>
    /// <param name="action">The tenant-governance action to store.</param>
    void Upsert(TenantGovernanceActionDescriptor action);
}
