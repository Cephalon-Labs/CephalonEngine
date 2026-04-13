using Cephalon.Abstractions.AppModel;
using Cephalon.Abstractions.Technologies;
using Cephalon.Data.EntityFramework.Configuration;
using Cephalon.Engine.AppModel;

namespace Cephalon.Data.EntityFramework.Services;

internal sealed class EntityFrameworkDatabaseTopologyRuntimeSurfaceContributor(
    AppProfile appProfile,
    EntityFrameworkDataOptions options) : ITechnologyRuntimeContributor
{
    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        var entries = new List<TechnologyRuntimeEntry>();

        if (options.UsesEngineDatabaseTopology)
        {
            AddRoleEntry(entries, "write", options.WriteDbContextType, appProfile.Databases, appProfile.Databases.Runtime);

            if (options.UsesReadWriteSplit)
            {
                AddRoleEntry(entries, "read", options.ReadDbContextType, appProfile.Databases, appProfile.Databases.Runtime);
            }

            if (options.RegisterOutbox)
            {
                entries.Add(CreateOutboxEntry(appProfile, options));
            }

            if (appProfile.Databases.Migrations.HasValues)
            {
                entries.Add(CreateMigrationsEntry(appProfile, options));
            }
        }

        return new TechnologyRuntimeSurface(
            technologyId: "data-management",
            surfaceId: "database-roles",
            displayName: "Database Roles",
            description: "Entity Framework database-role wiring and migration targeting resolved from Engine:Databases.",
            entries: entries);
    }

    private static void AddRoleEntry(
        List<TechnologyRuntimeEntry> entries,
        string role,
        Type dbContextType,
        DatabaseTopologySelection databases,
        DatabaseRuntimeSelection sharedRuntime)
    {
        var target = role switch
        {
            "write" => databases.Write,
            "read" => databases.Read,
            "outbox" => databases.Outbox,
            "history" => databases.History,
            _ => DatabaseTargetSelection.Empty
        };

        if (!target.HasValues)
        {
            return;
        }

        var resolution = DatabaseTopologyRoleResolver.Resolve(databases, role);
        var runtime = MergeRuntime(sharedRuntime, resolution.EffectiveTarget.Runtime);

        entries.Add(new TechnologyRuntimeEntry(
            id: role,
            displayName: $"{ToDisplayName(role)} Database Role",
            description: $"Entity Framework wiring for the '{role}' database role.",
            metadata: CreateRoleMetadata(role, dbContextType, resolution, runtime)));
    }

    private static TechnologyRuntimeEntry CreateOutboxEntry(
        AppProfile appProfile,
        EntityFrameworkDataOptions options)
    {
        var usesConfiguredOutboxRole = appProfile.Databases.Outbox.HasValues;
        var resolution = usesConfiguredOutboxRole
            ? DatabaseTopologyRoleResolver.Resolve(appProfile.Databases, "outbox")
            : DatabaseTopologyRoleResolver.Resolve(appProfile.Databases, "write");
        var configuredTarget = resolution.EffectiveTarget;

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["sourceModuleId"] = "entity-framework-data",
            ["storageRole"] = "write",
            ["storageDbContext"] = GetTypeName(options.WriteDbContextType),
            ["requestedRole"] = "outbox",
            ["resolvedRole"] = resolution.ResolvedRoleId,
            ["configurationRole"] = usesConfiguredOutboxRole ? "outbox" : "write",
            ["resolutionMode"] = usesConfiguredOutboxRole
                ? resolution.ResolutionMode
                : "implicit-fallback",
            ["usesRoleReference"] = resolution.UsesRoleReference ? "true" : "false",
            ["routingMode"] = usesConfiguredOutboxRole
                ? resolution.UsesRoleReference ? "role-reference" : "application-managed"
                : "write-role-fallback",
            ["provider"] = configuredTarget.Provider ?? "unknown",
            ["connectionMode"] = GetConnectionMode(configuredTarget),
            ["topologySource"] = "engine-databases"
        };

        if (resolution.UseRole is not null)
        {
            metadata["useRole"] = resolution.UseRole;
        }

        if (configuredTarget.ConnectionStringName is not null)
        {
            metadata["connectionStringName"] = configuredTarget.ConnectionStringName;
        }

        if (configuredTarget.Schema is not null)
        {
            metadata["schema"] = configuredTarget.Schema;
        }

        return new TechnologyRuntimeEntry(
            id: EntityFrameworkDataRuntimeIds.OutboxId,
            displayName: "Entity Framework Outbox Routing",
            description: "How the current Entity Framework outbox aligns with the engine-owned database topology.",
            metadata: metadata);
    }

    private static TechnologyRuntimeEntry CreateMigrationsEntry(
        AppProfile appProfile,
        EntityFrameworkDataOptions options)
    {
        var targetRoles = appProfile.Databases.Migrations.Targets;
        var supportedTargets = GetSupportedMigrationTargets(options);

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["applyOnStartup"] = appProfile.Databases.Migrations.ApplyOnStartup == true ? "true" : "false",
            ["exitAfterApply"] = appProfile.Databases.Migrations.ExitAfterApply == true ? "true" : "false",
            ["configuredTargets"] = string.Join(",", targetRoles),
            ["supportedTargets"] = string.Join(",", supportedTargets),
            ["executionMode"] = appProfile.Databases.Migrations.ApplyOnStartup == true
                ? "startup-hosted-service"
                : "manual-or-deploy-time",
            ["startupMechanism"] = "generic-host/ihostedservice",
            ["productionRecommendation"] = "bundle-or-script",
            ["topologySource"] = "engine-databases",
            ["readWriteSplit"] = options.UsesReadWriteSplit ? "true" : "false"
        };

        return new TechnologyRuntimeEntry(
            id: "migrations",
            displayName: "Entity Framework Migration Policy",
            description: "The migration policy currently declared for the Entity Framework relational topology.",
            metadata: metadata);
    }

    private static Dictionary<string, string> CreateRoleMetadata(
        string role,
        Type dbContextType,
        DatabaseTopologyRoleResolution resolution,
        DatabaseRuntimeSelection runtime)
    {
        var target = resolution.EffectiveTarget;
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["role"] = role,
            ["requestedRole"] = resolution.RequestedRoleId,
            ["resolvedRole"] = resolution.ResolvedRoleId,
            ["resolutionMode"] = resolution.ResolutionMode,
            ["usesRoleReference"] = resolution.UsesRoleReference ? "true" : "false",
            ["dbContext"] = GetTypeName(dbContextType),
            ["provider"] = target.Provider ?? "unknown",
            ["connectionMode"] = GetConnectionMode(target),
            ["topologySource"] = "engine-databases"
        };

        if (resolution.UseRole is not null)
        {
            metadata["useRole"] = resolution.UseRole;
        }

        if (target.ConnectionStringName is not null)
        {
            metadata["connectionStringName"] = target.ConnectionStringName;
        }

        if (target.Schema is not null)
        {
            metadata["schema"] = target.Schema;
        }

        AddRuntimeMetadata(metadata, runtime);
        return metadata;
    }

    private static void AddRuntimeMetadata(
        Dictionary<string, string> metadata,
        DatabaseRuntimeSelection runtime)
    {
        if (runtime.EnableDetailedErrors.HasValue)
        {
            metadata["enableDetailedErrors"] = runtime.EnableDetailedErrors.Value ? "true" : "false";
        }

        if (runtime.EnableSensitiveDataLogging.HasValue)
        {
            metadata["enableSensitiveDataLogging"] = runtime.EnableSensitiveDataLogging.Value ? "true" : "false";
        }

        if (runtime.EnableRetryOnFailure.HasValue)
        {
            metadata["enableRetryOnFailure"] = runtime.EnableRetryOnFailure.Value ? "true" : "false";
        }

        if (runtime.MaxRetryCount is { } maxRetryCount)
        {
            metadata["maxRetryCount"] = maxRetryCount.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        if (runtime.MaxRetryDelaySeconds is { } maxRetryDelaySeconds)
        {
            metadata["maxRetryDelaySeconds"] = maxRetryDelaySeconds.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        if (runtime.CommandTimeoutSeconds is { } commandTimeoutSeconds)
        {
            metadata["commandTimeoutSeconds"] = commandTimeoutSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        if (runtime.MaxBatchSize is { } maxBatchSize)
        {
            metadata["maxBatchSize"] = maxBatchSize.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        if (runtime.RoleProbeFreshnessSeconds is { } roleProbeFreshnessSeconds)
        {
            metadata["roleProbeFreshnessSeconds"] = roleProbeFreshnessSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
    }

    private static DatabaseRuntimeSelection MergeRuntime(
        DatabaseRuntimeSelection sharedRuntime,
        DatabaseRuntimeSelection roleRuntime)
    {
        return new DatabaseRuntimeSelection(
            enableDetailedErrors: roleRuntime.EnableDetailedErrors ?? sharedRuntime.EnableDetailedErrors,
            enableSensitiveDataLogging: roleRuntime.EnableSensitiveDataLogging ?? sharedRuntime.EnableSensitiveDataLogging,
            enableRetryOnFailure: roleRuntime.EnableRetryOnFailure ?? sharedRuntime.EnableRetryOnFailure,
            maxRetryCount: roleRuntime.MaxRetryCount ?? sharedRuntime.MaxRetryCount,
            maxRetryDelaySeconds: roleRuntime.MaxRetryDelaySeconds ?? sharedRuntime.MaxRetryDelaySeconds,
            commandTimeoutSeconds: roleRuntime.CommandTimeoutSeconds ?? sharedRuntime.CommandTimeoutSeconds,
            maxBatchSize: roleRuntime.MaxBatchSize ?? sharedRuntime.MaxBatchSize,
            roleProbeFreshnessSeconds: roleRuntime.RoleProbeFreshnessSeconds ?? sharedRuntime.RoleProbeFreshnessSeconds);
    }

    private static string[] GetSupportedMigrationTargets(EntityFrameworkDataOptions options)
    {
        if (options.UsesReadWriteSplit)
        {
            return ["read", "write"];
        }

        return ["write"];
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

    private static string ToDisplayName(string role)
    {
        return char.ToUpperInvariant(role[0]) + role[1..];
    }

    private static string GetTypeName(Type type)
    {
        return type.FullName ?? type.Name;
    }
}
