using Cephalon.Abstractions.AppModel;
using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Health;
using Cephalon.Engine.AppModel;

namespace Cephalon.Engine.Data;

internal sealed class DatabaseRoleCatalogSnapshot(
    AppProfile appProfile,
    IEnumerable<IDatabaseRoleRuntimeContributor>? runtimeContributors = null) : IDatabaseRoleCatalog
{
    private static readonly string[] KnownRoleIds = ["write", "read", "outbox", "history"];
    private readonly AppProfile appProfile = appProfile ?? throw new ArgumentNullException(nameof(appProfile));
    private readonly IDatabaseRoleRuntimeContributor[] runtimeContributors = runtimeContributors?.ToArray() ?? [];

    public IReadOnlyList<DatabaseRoleDescriptor> DatabaseRoles => CreateState().DatabaseRoles;

    public DatabaseRoleDescriptor? GetById(string databaseRoleId)
    {
        if (string.IsNullOrWhiteSpace(databaseRoleId))
        {
            return null;
        }

        return CreateState().DatabaseRolesById.TryGetValue(databaseRoleId.Trim(), out var databaseRole)
            ? databaseRole
            : null;
    }

    public IReadOnlyList<DatabaseRoleDescriptor> GetByResolvedRole(string resolvedRoleId)
    {
        if (string.IsNullOrWhiteSpace(resolvedRoleId))
        {
            return [];
        }

        return CreateState().DatabaseRolesByResolvedRole.TryGetValue(resolvedRoleId.Trim(), out var matches)
            ? matches
            : [];
    }

    public IReadOnlyList<DatabaseRoleDescriptor> GetByProvider(string provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            return [];
        }

        return CreateState().DatabaseRolesByProvider.TryGetValue(provider.Trim(), out var matches)
            ? matches
            : [];
    }

    private CatalogState CreateState()
    {
        var runtimeDescriptors = runtimeContributors
            .SelectMany(static contributor => contributor.DescribeDatabaseRoleRuntime())
            .ToArray();
        var databaseRoles = BuildDatabaseRoles(appProfile, runtimeDescriptors);

        return new CatalogState(
            databaseRoles,
            databaseRoles.ToDictionary(static role => role.Id, StringComparer.OrdinalIgnoreCase),
            databaseRoles
                .GroupBy(static role => role.ResolvedRoleId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    static group => group.Key,
                    static group => (IReadOnlyList<DatabaseRoleDescriptor>)group.ToArray(),
                    StringComparer.OrdinalIgnoreCase),
            databaseRoles
                .GroupBy(static role => role.Provider, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    static group => group.Key,
                    static group => (IReadOnlyList<DatabaseRoleDescriptor>)group.ToArray(),
                    StringComparer.OrdinalIgnoreCase));
    }

    private static DatabaseRoleDescriptor[] BuildDatabaseRoles(
        AppProfile appProfile,
        IReadOnlyList<DatabaseRoleRuntimeDescriptor> runtimeDescriptors)
    {
        var configuredRoles = KnownRoleIds
            .Where(roleId => GetTarget(appProfile.Databases, roleId).HasValues)
            .Select(roleId => (RoleId: roleId, Resolution: DatabaseTopologyRoleResolver.Resolve(appProfile.Databases, roleId)))
            .ToArray();
        var runtimeByRole = runtimeDescriptors
            .Where(static descriptor => !string.IsNullOrWhiteSpace(descriptor.DatabaseRoleId))
            .GroupBy(static descriptor => descriptor.DatabaseRoleId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<DatabaseRoleRuntimeDescriptor>)group.ToArray(),
                StringComparer.OrdinalIgnoreCase);

        return configuredRoles
            .Select(role =>
            {
                var roleRuntime = ResolveRuntimeDescriptors(role.RoleId, role.Resolution, runtimeByRole);
                return CreateDescriptor(appProfile, role.RoleId, role.Resolution, configuredRoles, roleRuntime);
            })
            .ToArray();
    }

    private static DatabaseRoleDescriptor CreateDescriptor(
        AppProfile appProfile,
        string roleId,
        DatabaseTopologyRoleResolution resolution,
        IReadOnlyList<(string RoleId, DatabaseTopologyRoleResolution Resolution)> configuredRoles,
        IReadOnlyList<DatabaseRoleRuntimeDescriptor> runtimeDescriptors)
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

        if (resolution.UsesRoleReference)
        {
            metadata["inheritsResolvedRoleRuntime"] = "true";
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
            metadata: metadata,
            healthState: ResolveHealthState(runtimeDescriptors),
            healthDescription: ResolveDescription(
                runtimeDescriptors.Select(static descriptor => descriptor.HealthDescription)),
            migrationState: runtimeDescriptors
                .Select(static descriptor => descriptor.MigrationState)
                .LastOrDefault(static state => !string.IsNullOrWhiteSpace(state)),
            migrationDescription: ResolveDescription(
                runtimeDescriptors.Select(static descriptor => descriptor.MigrationDescription)),
            observedAtUtc: ResolveObservedAtUtc(runtimeDescriptors),
            runtimeMetadata: MergeRuntimeMetadata(runtimeDescriptors));
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
            maxBatchSize: roleRuntime.MaxBatchSize ?? sharedRuntime.MaxBatchSize,
            roleProbeFreshnessSeconds: roleRuntime.RoleProbeFreshnessSeconds ?? sharedRuntime.RoleProbeFreshnessSeconds);
    }

    private static List<DatabaseRoleRuntimeDescriptor> ResolveRuntimeDescriptors(
        string roleId,
        DatabaseTopologyRoleResolution resolution,
        Dictionary<string, IReadOnlyList<DatabaseRoleRuntimeDescriptor>> runtimeByRole)
    {
        var descriptors = new List<DatabaseRoleRuntimeDescriptor>();

        if (resolution.UsesRoleReference &&
            runtimeByRole.TryGetValue(resolution.ResolvedRoleId, out var resolvedRoleRuntime))
        {
            descriptors.AddRange(resolvedRoleRuntime);
        }

        if (runtimeByRole.TryGetValue(roleId, out var directRuntime))
        {
            descriptors.AddRange(directRuntime);
        }

        return descriptors;
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

    private static HealthState? ResolveHealthState(IReadOnlyList<DatabaseRoleRuntimeDescriptor> runtimeDescriptors)
    {
        var states = runtimeDescriptors
            .Where(static descriptor => descriptor.HealthState.HasValue)
            .Select(static descriptor => descriptor.HealthState!.Value)
            .ToArray();

        if (states.Length == 0)
        {
            return null;
        }

        if (states.Contains(HealthState.Unhealthy))
        {
            return HealthState.Unhealthy;
        }

        if (states.Contains(HealthState.Degraded))
        {
            return HealthState.Degraded;
        }

        return HealthState.Healthy;
    }

    private static string? ResolveDescription(IEnumerable<string?> descriptions)
    {
        var resolved = descriptions
            .Where(static description => !string.IsNullOrWhiteSpace(description))
            .Select(static description => description!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return resolved.Length switch
        {
            0 => null,
            1 => resolved[0],
            _ => string.Join(" | ", resolved)
        };
    }

    private static Dictionary<string, string> MergeRuntimeMetadata(
        IReadOnlyList<DatabaseRoleRuntimeDescriptor> runtimeDescriptors)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var descriptor in runtimeDescriptors)
        {
            foreach (var pair in descriptor.Metadata)
            {
                metadata[pair.Key] = pair.Value;
            }
        }

        return metadata;
    }

    private static DateTimeOffset? ResolveObservedAtUtc(IReadOnlyList<DatabaseRoleRuntimeDescriptor> runtimeDescriptors)
    {
        var timestamps = runtimeDescriptors
            .Where(static descriptor => descriptor.ObservedAtUtc.HasValue)
            .Select(static descriptor => descriptor.ObservedAtUtc!.Value)
            .OrderByDescending(static timestamp => timestamp)
            .ToArray();

        return timestamps.Length > 0
            ? timestamps[0]
            : null;
    }

    private sealed record CatalogState(
        IReadOnlyList<DatabaseRoleDescriptor> DatabaseRoles,
        Dictionary<string, DatabaseRoleDescriptor> DatabaseRolesById,
        Dictionary<string, IReadOnlyList<DatabaseRoleDescriptor>> DatabaseRolesByResolvedRole,
        Dictionary<string, IReadOnlyList<DatabaseRoleDescriptor>> DatabaseRolesByProvider);
}
