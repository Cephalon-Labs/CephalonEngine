using Cephalon.Abstractions.AppModel;
using Cephalon.Abstractions.Audit;
using Cephalon.Audit.EntityFramework.Configuration;
using Cephalon.Engine.AppModel;
using Cephalon.Engine.Configuration;
using System.Globalization;

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
        var resolution = DatabaseTopologyRoleResolver.Resolve(appProfile.Databases, databaseRole);
        var target = resolution.EffectiveTarget;
        if (!target.HasValues)
        {
            return [];
        }

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["writeMode"] = "transactional-table",
            ["queryMode"] = "filtered-page-reader",
            ["exportMode"] = appProfile.Audit.History.Export.Enabled == true
                ? "ndjson-stream"
                : "disabled",
            ["durability"] = "durable",
            ["databaseRole"] = resolution.RequestedRoleId,
            ["resolvedDatabaseRole"] = resolution.ResolvedRoleId,
            ["resolutionMode"] = resolution.ResolutionMode,
            ["usesRoleReference"] = resolution.UsesRoleReference ? "true" : "false",
            ["databaseProvider"] = target.Provider ?? "unknown",
            ["connectionMode"] = GetConnectionMode(target),
            ["dbContext"] = GetTypeName(options.DbContextType),
            ["topologySource"] = options.UsesEngineDatabaseTopology ? "engine-databases" : "registration-callbacks"
        };

        if (resolution.UseRole is not null)
        {
            metadata["useRole"] = resolution.UseRole;
        }

        if (appProfile.Audit.History.Export.Enabled == true &&
            appProfile.Audit.History.Export.MaxEntries is { } maxEntries)
        {
            metadata["exportMaxEntries"] = maxEntries.ToString(CultureInfo.InvariantCulture);
        }

        ApplyRetentionMetadata(metadata, appProfile.Audit.History.Retention);

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

    private static void ApplyRetentionMetadata(
        Dictionary<string, string> metadata,
        AuditHistoryRetentionSelection retention)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(retention);

        if (retention.Enabled != true)
        {
            metadata["retentionMode"] = "disabled";
            return;
        }

        metadata["retentionMode"] = retention.RunIntervalMinutes is > 0
            ? retention.ApplyOnStartup == true ? "startup-and-interval" : "interval"
            : "startup-only";
        metadata["retentionMaxAgeDays"] = retention.MaxAgeDays!.Value.ToString(CultureInfo.InvariantCulture);
        metadata["retentionDeleteBatchSize"] = (retention.DeleteBatchSize ?? AuditHistoryRetentionSettings.DefaultDeleteBatchSize)
            .ToString(CultureInfo.InvariantCulture);
        metadata["retentionApplyOnStartup"] = retention.ApplyOnStartup == true
            ? "true"
            : "false";

        if (retention.RunIntervalMinutes is > 0)
        {
            metadata["retentionRunIntervalMinutes"] = retention.RunIntervalMinutes.Value.ToString(CultureInfo.InvariantCulture);
        }
    }
}
