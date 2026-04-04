namespace Cephalon.Abstractions.Audit;

/// <summary>
/// Contributes one or more audit-store descriptors to the active runtime.
/// </summary>
public interface IAuditStoreContributor
{
    /// <summary>
    /// Registers one or more audit-store descriptors with the supplied registry.
    /// </summary>
    /// <param name="auditStores">The registry that collects contributed audit-store descriptors.</param>
    void RegisterAuditStores(IAuditStoreRegistry auditStores);
}
