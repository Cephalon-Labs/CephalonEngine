using Cephalon.Abstractions.AppModel;
using Cephalon.Abstractions.Data;
using Cephalon.Engine.AppModel;

namespace Cephalon.Engine.Data;

internal sealed class DatabaseRoleCatalogSnapshot : IDatabaseRoleCatalog
{
    private static readonly string[] KnownRoleIds = ["write", "read", "outbox", "history"];
    private readonly IReadOnlyList<DatabaseRoleDescriptor> databaseRoles;
    private readonly Dictionary<string, DatabaseRoleDescriptor> databaseRolesById;
    private readonly Dictionary<string, IReadOnlyList<DatabaseRoleDescriptor>> databaseRolesByResolvedRole;
    private readonly Dictionary<string, IReadOnlyList<DatabaseRoleDescriptor>> databaseRolesByProvider;

    public DatabaseRoleCatalogSnapshot(AppProfile appProfile)
    {
        ArgumentNullException.ThrowIfNull(appProfile);

        databaseRoles = BuildDatabaseRoles(appProfile);
        databaseRolesById = databaseRoles.ToDictionary(static role => role.Id, StringComparer.OrdinalIgnoreCase);
        databaseRolesByResolvedRole = databaseRoles
            .GroupBy(static role => role.ResolvedRoleId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<DatabaseRoleDescriptor>)group.ToArray(),
                StringComparer.OrdinalIgnoreCase);
        databaseRolesByProvider = databaseRoles
            .GroupBy(static role => role.Provider, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<DatabaseRoleDescriptor>)group.ToArray(),
                StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<DatabaseRoleDescriptor> DatabaseRoles => databaseRoles;

    public DatabaseRoleDescriptor? GetById(string databaseRoleId)
    {
        if (string.IsNullOrWhiteSpace(databaseRoleId))
        {
            return null;
        }

        return databaseRolesById.TryGetValue(databaseRoleId.Trim(), out var databaseRole)
            ? databaseRole
            : null;
    }

    public IReadOnlyList<DatabaseRoleDescriptor> GetByResolvedRole(string resolvedRoleId)
    {
        if (string.IsNullOrWhiteSpace(resolvedRoleId))
        {
            return [];
        }

        return databaseRolesByResolvedRole.TryGetValue(resolvedRoleId.Trim(), out var matches)
            ? matches
            : [];
    }

    public IReadOnlyList<DatabaseRoleDescriptor> GetByProvider(string provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            return [];
        }

        return databaseRolesByProvider.TryGetValue(provider.Trim(), out var matches)
            ? matches
            : [];
    }

    private static DatabaseRoleDescriptor[] BuildDatabaseRoles(AppProfile appProfile)
    {
        var configuredRoles = KnownRoleIds
            .Where(roleId => GetTarget(appProfile.Databases, roleId).HasValues)
            .Select(roleId => (RoleId: roleId, Resolution: DatabaseTopologyRoleResolver.Resolve(appProfile.Databases, roleId)))
            .ToArray();

        return configuredRoles
            .Select(role => CreateDescriptor(appProfile, role.RoleId, role.Resolution, configuredRoles))
            .ToArray();
    }

    private static DatabaseRoleDescriptor CreateDescriptor(
        AppProfile appProfile,
        string roleId,
        DatabaseTopologyRoleResolution resolution,
        IReadOnlyList<(string RoleId, DatabaseTopologyRoleResolution Resolution)> configuredRoles)
    {
        var runtime = MergeRuntime(appProfile.Databases.Runtime, resolution.EffectiveTarget.Runtime);
        var consumers = GetConsumers(appProfile, roleId);
        var referencedByRoles = configuredRoles
            .Where(other => !string.Equals(other.RoleId, roleId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(other.Resolution.UseRole, roleId, StringComparison.OrdinalIgnoreCase))
            .Select(other => other.RoleId)
            .ToArray();
        var coLocatedRoles = configuredRoles
            .Where(other => !string.Equals(other.RoleId, roleId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(other.Resolution.ResolvedRoleId, resolution.ResolvedRoleId, StringComparison.OrdinalIgnoreCase))
            .Select(other => other.RoleId)
            .ToArray();

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["topologySource"] = "engine-databases"
        };

        if (string.Equals(appProfile.Audit.History.DatabaseRole, roleId, StringComparison.OrdinalIgnoreCase))
        {
            metadata["auditHistoryProvider"] = appProfile.Audit.History.Provider ?? "unknown";
            metadata["auditHistoryExportEnabled"] = appProfile.Audit.History.Export.Enabled == true ? "true" : "false";
            metadata["auditHistoryRetentionEnabled"] = appProfile.Audit.History.Retention.Enabled == true ? "true" : "false";
        }

        return new DatabaseRoleDescriptor(
            id: roleId,
            displayName: $"{ToDisplayName(roleId)} Database Role",
            description: BuildDescription(roleId, resolution),
            provider: resolution.EffectiveTarget.Provider ?? "unknown",
            requestedRoleId: resolution.RequestedRoleId,
            resolvedRoleId: resolution.ResolvedRoleId,
            resolutionMode: resolution.ResolutionMode,
            runtime: runtime,
            usesRoleReference: resolution.UsesRoleReference,
            useRole: resolution.UseRole,
            connectionMode: GetConnectionMode(resolution.EffectiveTarget),
            connectionStringName: resolution.EffectiveTarget.ConnectionStringName,
            schema: resolution.EffectiveTarget.Schema,
            consumers: consumers,
            referencedByRoles: referencedByRoles,
            coLocatedRoles: coLocatedRoles,
            metadata: metadata);
    }

    private static string[] GetConsumers(AppProfile appProfile, string roleId)
    {
        var consumers = new List<string>();

        if (string.Equals(roleId, "outbox", StringComparison.OrdinalIgnoreCase))
        {
            consumers.Add("outbox");
        }

        if (string.Equals(appProfile.Audit.History.DatabaseRole, roleId, StringComparison.OrdinalIgnoreCase))
        {
            consumers.Add("audit-history");
        }

        if (appProfile.Databases.Migrations.Targets.Contains(roleId, StringComparer.OrdinalIgnoreCase))
        {
            consumers.Add("migrations");
        }

        return consumers
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static consumer => consumer, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static DatabaseTargetSelection GetTarget(DatabaseTopologySelection databases, string roleId)
    {
        return roleId switch
        {
            "write" => databases.Write,
            "read" => databases.Read,
            "outbox" => databases.Outbox,
            "history" => databases.History,
            _ => DatabaseTargetSelection.Empty
        };
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
            maxBatchSize: roleRuntime.MaxBatchSize ?? sharedRuntime.MaxBatchSize);
    }

    private static string BuildDescription(string roleId, DatabaseTopologyRoleResolution resolution)
    {
        if (resolution.UsesRoleReference)
        {
            return $"Logical '{roleId}' database role resolved through '{resolution.ResolvedRoleId}' from Engine:Databases.";
        }

        return $"Logical '{roleId}' database role resolved directly from Engine:Databases.";
    }

    private static string? GetConnectionMode(DatabaseTargetSelection target)
    {
        if (target.ConnectionStringName is not null)
        {
            return "named";
        }

        if (target.ConnectionString is not null)
        {
            return "inline";
        }

        return null;
    }

    private static string ToDisplayName(string role)
    {
        return char.ToUpperInvariant(role[0]) + role[1..];
    }
}
