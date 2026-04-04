using Cephalon.Abstractions.Audit;

namespace Cephalon.Audit.Services;

internal sealed class ConfiguredAuditStoreCatalog(IEnumerable<AuditStoreDescriptor> auditStores) : IAuditStoreCatalog
{
    private readonly IReadOnlyList<AuditStoreDescriptor> auditStores = auditStores.ToArray();
    private readonly Dictionary<string, AuditStoreDescriptor> auditStoresById = auditStores
        .ToDictionary(static auditStore => auditStore.Id, StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, IReadOnlyList<AuditStoreDescriptor>> auditStoresBySourceModule = auditStores
        .GroupBy(static auditStore => auditStore.SourceModuleId, StringComparer.OrdinalIgnoreCase)
        .ToDictionary(
            static group => group.Key,
            static group => (IReadOnlyList<AuditStoreDescriptor>)group.ToArray(),
            StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, IReadOnlyList<AuditStoreDescriptor>> auditStoresByProvider = auditStores
        .GroupBy(static auditStore => auditStore.Provider, StringComparer.OrdinalIgnoreCase)
        .ToDictionary(
            static group => group.Key,
            static group => (IReadOnlyList<AuditStoreDescriptor>)group.ToArray(),
            StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<AuditStoreDescriptor> AuditStores => auditStores;

    public AuditStoreDescriptor? GetById(string auditStoreId)
    {
        if (string.IsNullOrWhiteSpace(auditStoreId))
        {
            return null;
        }

        return auditStoresById.TryGetValue(auditStoreId.Trim(), out var auditStore)
            ? auditStore
            : null;
    }

    public IReadOnlyList<AuditStoreDescriptor> GetBySourceModule(string sourceModuleId)
    {
        if (string.IsNullOrWhiteSpace(sourceModuleId))
        {
            return [];
        }

        return auditStoresBySourceModule.TryGetValue(sourceModuleId.Trim(), out var matches)
            ? matches
            : [];
    }

    public IReadOnlyList<AuditStoreDescriptor> GetByProvider(string provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            return [];
        }

        return auditStoresByProvider.TryGetValue(provider.Trim(), out var matches)
            ? matches
            : [];
    }
}
