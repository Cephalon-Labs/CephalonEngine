namespace Cephalon.Abstractions.Audit;

/// <summary>
/// Receives audit-store descriptors contributed by active modules or packages.
/// </summary>
public interface IAuditStoreRegistry
{
    /// <summary>
    /// Adds an audit store to the current runtime composition.
    /// </summary>
    /// <param name="auditStore">The audit-store descriptor to register.</param>
    void Add(AuditStoreDescriptor auditStore);
}
