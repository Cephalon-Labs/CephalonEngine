namespace Cephalon.Abstractions.Audit;

/// <summary>
/// Contributes runtime-resolved audit-store descriptors to the active Cephalon audit surface.
/// </summary>
public interface IAuditStoreRuntimeContributor
{
    /// <summary>
    /// Describes the audit stores that should appear in the active runtime after configuration,
    /// topology, and provider-specific options have been resolved.
    /// </summary>
    /// <returns>The audit-store descriptors that should appear in the active runtime.</returns>
    IReadOnlyList<AuditStoreDescriptor> DescribeAuditStores();
}
