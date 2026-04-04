using Cephalon.Abstractions.Audit;

namespace Cephalon.Audit.Services;

internal sealed class ConfiguredAuditStoreCatalog : IAuditStoreCatalog
{
    private readonly IReadOnlyList<AuditStoreDescriptor> auditStores;
    private readonly Dictionary<string, AuditStoreDescriptor> auditStoresById;
    private readonly Dictionary<string, IReadOnlyList<AuditStoreDescriptor>> auditStoresBySourceModule;
    private readonly Dictionary<string, IReadOnlyList<AuditStoreDescriptor>> auditStoresByProvider;

    public ConfiguredAuditStoreCatalog(IEnumerable<AuditStoreDescriptor> auditStores)
    {
        ArgumentNullException.ThrowIfNull(auditStores);

        this.auditStores = Deduplicate(auditStores);
        auditStoresById = this.auditStores.ToDictionary(static auditStore => auditStore.Id, StringComparer.OrdinalIgnoreCase);
        auditStoresBySourceModule = this.auditStores
            .GroupBy(static auditStore => auditStore.SourceModuleId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<AuditStoreDescriptor>)group.ToArray(),
                StringComparer.OrdinalIgnoreCase);
        auditStoresByProvider = this.auditStores
            .GroupBy(static auditStore => auditStore.Provider, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<AuditStoreDescriptor>)group.ToArray(),
                StringComparer.OrdinalIgnoreCase);
    }

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

    private static AuditStoreDescriptor[] Deduplicate(IEnumerable<AuditStoreDescriptor> auditStores)
    {
        ArgumentNullException.ThrowIfNull(auditStores);

        var seenAuditStoreIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var deduplicatedAuditStores = new List<AuditStoreDescriptor>();

        foreach (var auditStore in auditStores)
        {
            if (seenAuditStoreIds.Add(auditStore.Id))
            {
                deduplicatedAuditStores.Add(auditStore);
            }
        }

        return deduplicatedAuditStores.ToArray();
    }
}
