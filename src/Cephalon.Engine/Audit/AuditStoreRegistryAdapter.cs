using Cephalon.Abstractions.Audit;

namespace Cephalon.Engine.Audit;

internal sealed class AuditStoreRegistryAdapter(
    string moduleId,
    List<AuditStoreDescriptor> auditStores) : IAuditStoreRegistry
{
    public void Add(AuditStoreDescriptor auditStore)
    {
        ArgumentNullException.ThrowIfNull(auditStore);

        if (!string.Equals(auditStore.SourceModuleId, moduleId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Audit store '{auditStore.Id}' declared source module '{auditStore.SourceModuleId}', but it was contributed by module '{moduleId}'.");
        }

        auditStores.Add(new AuditStoreDescriptor(
            auditStore.Id,
            auditStore.DisplayName,
            auditStore.Description,
            auditStore.SourceModuleId,
            auditStore.Provider,
            auditStore.Mode,
            auditStore.Tags,
            auditStore.Metadata));
    }
}
