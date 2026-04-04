namespace Cephalon.Abstractions.Audit;

/// <summary>
/// Exposes the audit-store surfaces visible to the current runtime.
/// </summary>
public interface IAuditStoreCatalog
{
    /// <summary>
    /// Gets all audit-store surfaces visible to the current runtime.
    /// </summary>
    IReadOnlyList<AuditStoreDescriptor> AuditStores { get; }

    /// <summary>
    /// Gets one audit store by its stable identifier.
    /// </summary>
    /// <param name="auditStoreId">The audit-store identifier to resolve.</param>
    /// <returns>The matching audit store, or <see langword="null" /> when it is not active.</returns>
    AuditStoreDescriptor? GetById(string auditStoreId);

    /// <summary>
    /// Gets all audit stores contributed by the requested module.
    /// </summary>
    /// <param name="sourceModuleId">The source module identifier to filter by.</param>
    /// <returns>The matching audit stores, or an empty list when the module contributed none.</returns>
    IReadOnlyList<AuditStoreDescriptor> GetBySourceModule(string sourceModuleId);

    /// <summary>
    /// Gets all audit stores backed by the requested provider identifier.
    /// </summary>
    /// <param name="provider">The provider identifier to filter by.</param>
    /// <returns>The matching audit stores, or an empty list when the provider contributes none.</returns>
    IReadOnlyList<AuditStoreDescriptor> GetByProvider(string provider);
}
