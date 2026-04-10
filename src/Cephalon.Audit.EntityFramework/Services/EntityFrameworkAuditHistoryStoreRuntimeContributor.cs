using Cephalon.Abstractions.AppModel;
using Cephalon.Abstractions.Audit;
using Cephalon.Audit.EntityFramework.Configuration;
using Cephalon.Engine.Configuration;

namespace Cephalon.Audit.EntityFramework.Services;

internal sealed class EntityFrameworkAuditHistoryStoreRuntimeContributor<TDbContext>(
    AppProfile appProfile,
    EntityFrameworkAuditHistoryOptions options) : IAuditStoreRuntimeContributor
    where TDbContext : Microsoft.EntityFrameworkCore.DbContext, IEntityFrameworkAuditHistoryContext
{
    public IReadOnlyList<AuditStoreDescriptor> DescribeAuditStores()
    {
        if (appProfile.Audit.Enabled == false ||
            appProfile.Audit.History.Enabled != true ||
            !EntityFrameworkAuditHistorySelection.MatchesProvider(appProfile.Audit.History.Provider))
        {
            return [];
        }

        var databaseRole = EntityFrameworkAuditHistorySelection.ResolveDatabaseRole(appProfile);
        var target = ResolveTarget(appProfile.Databases, databaseRole);
        if (!target.HasValues)
        {
            return [];
        }

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["writeMode"] = "transactional-table",
            ["queryMode"] = "not-configured",
            ["retentionMode"] = "not-configured",
            ["durability"] = "durable",
            ["databaseRole"] = databaseRole,
            ["databaseProvider"] = target.Provider ?? "unknown",
            ["connectionMode"] = GetConnectionMode(target),
            ["dbContext"] = GetTypeName(options.DbContextType),
            ["topologySource"] = options.UsesEngineDatabaseTopology ? "engine-databases" : "registration-callbacks"
        };

        if (target.ConnectionStringName is not null)
        {
            metadata["connectionStringName"] = target.ConnectionStringName;
        }

        if (target.Schema is not null)
        {
            metadata["schema"] = target.Schema;
        }

        return
        [
            new AuditStoreDescriptor(
                id: EntityFrameworkAuditHistoryRuntimeIds.AuditStoreId,
                displayName: "Entity Framework Audit History",
                description: "Persists durable audit entries through an Entity Framework Core DbContext bound to the configured audit-history database role.",
                sourceModuleId: EntityFrameworkAuditHistoryRuntimeIds.SourceModuleId,
                provider: EntityFrameworkAuditHistoryOptions.ProviderId,
                mode: "transactional-table",
                tags: ["audit", "history", "durable", "entity-framework"],
                metadata: metadata)
        ];
    }

    private static DatabaseTargetSelection ResolveTarget(
        DatabaseTopologySelection databases,
        string databaseRole)
    {
        var normalizedRole = NormalizeRoleKey(databaseRole);

        return normalizedRole switch
        {
            "WRITE" => databases.Write,
            "READ" => databases.Read,
            "OUTBOX" => databases.Outbox,
            "HISTORY" => databases.History,
            _ => DatabaseTargetSelection.Empty
        };
    }

    private static string GetConnectionMode(DatabaseTargetSelection target)
    {
        if (target.ConnectionStringName is not null)
        {
            return "named";
        }

        if (target.ConnectionString is not null)
        {
            return "inline";
        }

        return "unresolved";
    }

    private static string GetTypeName(Type type)
    {
        return type.FullName ?? type.Name;
    }

    private static string NormalizeRoleKey(string value)
    {
        return new string(value
            .Where(char.IsLetterOrDigit)
            .Select(char.ToUpperInvariant)
            .ToArray());
    }
}
