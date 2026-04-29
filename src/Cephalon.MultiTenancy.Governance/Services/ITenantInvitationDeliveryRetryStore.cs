namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Stores tenant invitation delivery retry entries managed by the governance companion pack.
/// </summary>
public interface ITenantInvitationDeliveryRetryStore
{
    /// <summary>
    /// Gets the storage kind used by the retry queue.
    /// </summary>
    string StoreKind { get; }

    /// <summary>
    /// Gets a value indicating whether retry entries survive process restarts.
    /// </summary>
    bool IsDurable { get; }

    /// <summary>
    /// Gets the runtime ownership label for the retry queue.
    /// </summary>
    string Ownership { get; }

    /// <summary>
    /// Gets every retained retry entry.
    /// </summary>
    IReadOnlyList<TenantInvitationDeliveryRetryDescriptor> Entries { get; }

    /// <summary>
    /// Gets the number of retained retry entries.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Gets the latest retained retry entry when one exists.
    /// </summary>
    TenantInvitationDeliveryRetryDescriptor? LatestEntry { get; }

    /// <summary>
    /// Gets one retry entry by identifier.
    /// </summary>
    /// <param name="retryId">The retry entry identifier.</param>
    /// <returns>The matching retry entry, or <see langword="null" /> when none exists.</returns>
    TenantInvitationDeliveryRetryDescriptor? GetById(string retryId);

    /// <summary>
    /// Gets retry entries that are pending and optionally due at or before the supplied timestamp.
    /// </summary>
    /// <param name="atUtc">The timestamp used to decide due entries.</param>
    /// <param name="limit">The maximum number of entries to return.</param>
    /// <param name="dueOnly">A value indicating whether entries scheduled after <paramref name="atUtc" /> should be skipped.</param>
    /// <returns>The matching pending retry entries.</returns>
    IReadOnlyList<TenantInvitationDeliveryRetryDescriptor> GetPending(
        DateTimeOffset atUtc,
        int limit,
        bool dueOnly = true);

    /// <summary>
    /// Adds or replaces a retry entry.
    /// </summary>
    /// <param name="entry">The retry entry to store.</param>
    void Upsert(TenantInvitationDeliveryRetryDescriptor entry);

    /// <summary>
    /// Removes a retry entry.
    /// </summary>
    /// <param name="retryId">The retry entry identifier.</param>
    /// <returns><see langword="true" /> when an entry was removed; otherwise <see langword="false" />.</returns>
    bool Remove(string retryId);
}
