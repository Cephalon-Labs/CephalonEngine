using Cephalon.Abstractions.AppModel;
using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Health;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;

namespace Cephalon.Data.EntityFramework.Services;

internal sealed class EntityFrameworkDatabaseRoleRuntimeContributor : IDatabaseRoleRuntimeContributor
{
    private readonly object syncRoot = new();
    private readonly Dictionary<string, Type[]> dbContextTypesByRole;
    private readonly Dictionary<string, RoleRuntimeState> roleStates;
    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly TimeProvider timeProvider;

    public EntityFrameworkDatabaseRoleRuntimeContributor(
        AppProfile appProfile,
        IEnumerable<EntityFrameworkDatabaseMigrationRegistration> registrations,
        IServiceScopeFactory serviceScopeFactory,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(appProfile);
        ArgumentNullException.ThrowIfNull(registrations);
        this.serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));

        this.timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

        dbContextTypesByRole = registrations
            .SelectMany(static registration => registration.TargetRoleIds.Select(roleId => (RoleId: roleId, registration.DbContextType)))
            .GroupBy(static entry => entry.RoleId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => group
                    .Select(static entry => entry.DbContextType)
                    .Distinct()
                    .OrderBy(static dbContextType => dbContextType.FullName ?? dbContextType.Name, StringComparer.Ordinal)
                    .ToArray(),
                StringComparer.OrdinalIgnoreCase);

        var migrationSelection = appProfile.Databases.Migrations;
        var requestedTargets = migrationSelection.HasValues
            ? EntityFrameworkDatabaseMigrationTargetResolver.ResolveRequestedTargets(
                migrationSelection,
                dbContextTypesByRole.Keys)
            : [];

        roleStates = dbContextTypesByRole.ToDictionary(
            static pair => pair.Key,
            pair => CreateInitialState(pair.Key, pair.Value, migrationSelection, requestedTargets),
            StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<DatabaseRoleRuntimeDescriptor> DescribeDatabaseRoleRuntime()
    {
        RoleRuntimeState[] snapshots;

        lock (syncRoot)
        {
            snapshots = roleStates
                .OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .Select(static pair => pair.Value.Clone())
                .ToArray();
        }

        return snapshots
            .Select(snapshot => Merge(snapshot, Probe(snapshot.DatabaseRoleId)))
            .ToArray();
    }

    public void ReportRunning(IReadOnlyList<string> roleIds, Type dbContextType)
    {
        Update(roleIds, dbContextType, static (state, dbContextName, observedAtUtc) =>
        {
            state.HealthState = HealthState.Degraded;
            state.HealthDescription = $"Entity Framework startup schema apply is running for DbContext '{dbContextName}'.";
            state.MigrationState = "running";
            state.MigrationDescription = $"Entity Framework startup schema apply is running for DbContext '{dbContextName}'.";
            state.ObservedAtUtc = observedAtUtc;
            state.RuntimeMetadata["lastDbContext"] = dbContextName;
            state.RuntimeMetadata["lastExecutionMode"] = "startup-hosted-service";
        });
    }

    public void ReportSucceeded(IReadOnlyList<string> roleIds, Type dbContextType, string appliedMode)
    {
        Update(roleIds, dbContextType, (state, dbContextName, observedAtUtc) =>
        {
            state.HealthState = HealthState.Healthy;
            state.HealthDescription = $"Entity Framework startup schema apply succeeded for DbContext '{dbContextName}'.";
            state.MigrationState = "succeeded";
            state.MigrationDescription = $"Entity Framework startup schema apply succeeded through '{appliedMode}' for DbContext '{dbContextName}'.";
            state.ObservedAtUtc = observedAtUtc;
            state.RuntimeMetadata["lastDbContext"] = dbContextName;
            state.RuntimeMetadata["lastExecutionMode"] = appliedMode;
            state.RuntimeMetadata["lastOutcome"] = "succeeded";
            state.RuntimeMetadata["lastAppliedAtUtc"] = observedAtUtc.ToString("O");
            state.RuntimeMetadata.Remove("lastError");
        });
    }

    public void ReportFailed(IReadOnlyList<string> roleIds, Type dbContextType, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        Update(roleIds, dbContextType, (state, dbContextName, observedAtUtc) =>
        {
            state.HealthState = HealthState.Unhealthy;
            state.HealthDescription = $"Entity Framework startup schema apply failed for DbContext '{dbContextName}'.";
            state.MigrationState = "failed";
            state.MigrationDescription = $"Entity Framework startup schema apply failed for DbContext '{dbContextName}'.";
            state.ObservedAtUtc = observedAtUtc;
            state.RuntimeMetadata["lastDbContext"] = dbContextName;
            state.RuntimeMetadata["lastExecutionMode"] = "startup-hosted-service";
            state.RuntimeMetadata["lastOutcome"] = "failed";
            state.RuntimeMetadata["lastError"] = exception.Message;
            state.RuntimeMetadata["lastErrorType"] = exception.GetType().FullName ?? exception.GetType().Name;
        });
    }

    private void Update(
        IReadOnlyList<string> roleIds,
        Type dbContextType,
        Action<RoleRuntimeState, string, DateTimeOffset> apply)
    {
        ArgumentNullException.ThrowIfNull(roleIds);
        ArgumentNullException.ThrowIfNull(dbContextType);
        ArgumentNullException.ThrowIfNull(apply);

        var dbContextName = dbContextType.FullName ?? dbContextType.Name;
        var observedAtUtc = timeProvider.GetUtcNow();

        lock (syncRoot)
        {
            foreach (var roleId in roleIds)
            {
                if (!roleStates.TryGetValue(roleId, out var state))
                {
                    continue;
                }

                apply(state, dbContextName, observedAtUtc);
            }
        }
    }

    private static DatabaseRoleRuntimeDescriptor Merge(
        RoleRuntimeState state,
        RoleProbeResult? probe)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (probe is null)
        {
            return state.ToDescriptor();
        }

        var metadata = new Dictionary<string, string>(state.RuntimeMetadata, StringComparer.OrdinalIgnoreCase);
        foreach (var pair in probe.Metadata)
        {
            metadata[pair.Key] = pair.Value;
        }

        var migrationState = state.MigrationState;
        var migrationDescription = state.MigrationDescription;
        var healthState = ResolveHealthState(state, probe);
        var healthDescription = ResolveHealthDescription(state, probe);
        var observedAtUtc = ResolveObservedAtUtc(state.ObservedAtUtc, probe.ObservedAtUtc);

        return new DatabaseRoleRuntimeDescriptor(
            databaseRoleId: state.DatabaseRoleId,
            healthState: healthState,
            healthDescription: healthDescription,
            migrationState: migrationState,
            migrationDescription: migrationDescription,
            observedAtUtc: observedAtUtc,
            metadata: metadata);
    }

    private RoleProbeResult? Probe(string roleId)
    {
        if (!dbContextTypesByRole.TryGetValue(roleId, out var dbContextTypes) ||
            dbContextTypes.Length == 0)
        {
            return null;
        }

        var observedAtUtc = timeProvider.GetUtcNow();
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["probeOutcome"] = "succeeded",
            ["probeMode"] = "connectivity-and-migrations",
            ["probeCount"] = dbContextTypes.Length.ToString(CultureInfo.InvariantCulture),
            ["lastProbeAtUtc"] = observedAtUtc.ToString("O", CultureInfo.InvariantCulture),
            ["probeDbContexts"] = string.Join(",", dbContextTypes
                .Select(static dbContextType => dbContextType.FullName ?? dbContextType.Name)
                .OrderBy(static dbContextName => dbContextName, StringComparer.Ordinal))
        };
        var providerNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pendingMigrationIds = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        var appliedMigrationCount = 0;
        string? lastAppliedMigration = null;

        foreach (var dbContextType in dbContextTypes.OrderBy(static dbContextType => dbContextType.FullName ?? dbContextType.Name, StringComparer.Ordinal))
        {
            var dbContextName = dbContextType.FullName ?? dbContextType.Name;

            try
            {
                using var scope = serviceScopeFactory.CreateScope();
                var dbContext = (DbContext)scope.ServiceProvider.GetRequiredService(dbContextType);
                var providerName = dbContext.Database.ProviderName ?? "unknown";
                providerNames.Add(providerName);

                var canConnect = dbContext.Database.CanConnect();
                if (!canConnect)
                {
                    metadata["probeOutcome"] = "failed";
                    metadata["lastProbeDbContext"] = dbContextName;
                    metadata["lastProbeProvider"] = providerName;
                    metadata["lastProbeError"] = "Entity Framework connectivity probe returned false.";
                    metadata["lastProbeErrorType"] = "CanConnectReturnedFalse";

                    return new RoleProbeResult(
                        HealthState: HealthState.Unhealthy,
                        HealthDescription: $"Entity Framework connectivity probe returned false for DbContext '{dbContextName}'.",
                        ObservedAtUtc: observedAtUtc,
                        Metadata: metadata);
                }

                metadata[$"probe.{dbContextName}.providerName"] = providerName;
                metadata[$"probe.{dbContextName}.canConnect"] = "true";

                if (!dbContext.Database.IsRelational())
                {
                    metadata[$"probe.{dbContextName}.pendingMigrationCount"] = "0";
                    continue;
                }

                var pendingMigrations = dbContext.Database.GetPendingMigrations().ToArray();
                var appliedMigrations = dbContext.Database.GetAppliedMigrations().ToArray();

                metadata[$"probe.{dbContextName}.pendingMigrationCount"] = pendingMigrations.Length.ToString(CultureInfo.InvariantCulture);
                metadata[$"probe.{dbContextName}.appliedMigrationCount"] = appliedMigrations.Length.ToString(CultureInfo.InvariantCulture);

                foreach (var migrationId in pendingMigrations)
                {
                    pendingMigrationIds.Add(migrationId);
                }

                appliedMigrationCount += appliedMigrations.Length;
                if (appliedMigrations.Length > 0)
                {
                    lastAppliedMigration = appliedMigrations[^1];
                    metadata[$"probe.{dbContextName}.lastAppliedMigration"] = lastAppliedMigration;
                }
            }
            catch (Exception exception)
            {
                metadata["probeOutcome"] = "failed";
                metadata["lastProbeDbContext"] = dbContextName;
                metadata["lastProbeError"] = exception.Message;
                metadata["lastProbeErrorType"] = exception.GetType().FullName ?? exception.GetType().Name;

                return new RoleProbeResult(
                    HealthState: HealthState.Unhealthy,
                    HealthDescription: $"Entity Framework connectivity probe failed for DbContext '{dbContextName}'.",
                    ObservedAtUtc: observedAtUtc,
                    Metadata: metadata);
            }
        }

        metadata["providerNames"] = string.Join(",", providerNames.OrderBy(static providerName => providerName, StringComparer.OrdinalIgnoreCase));
        metadata["pendingMigrationCount"] = pendingMigrationIds.Count.ToString(CultureInfo.InvariantCulture);
        metadata["appliedMigrationCount"] = appliedMigrationCount.ToString(CultureInfo.InvariantCulture);

        if (pendingMigrationIds.Count > 0)
        {
            metadata["pendingMigrationIds"] = string.Join(",", pendingMigrationIds);
        }

        if (!string.IsNullOrWhiteSpace(lastAppliedMigration))
        {
            metadata["lastAppliedMigration"] = lastAppliedMigration;
        }

        return new RoleProbeResult(
            HealthState: HealthState.Healthy,
            HealthDescription: $"Entity Framework connectivity probe succeeded for the '{roleId}' database role.",
            ObservedAtUtc: observedAtUtc,
            Metadata: metadata);
    }

    private static HealthState? ResolveHealthState(
        RoleRuntimeState state,
        RoleProbeResult probe)
    {
        return state.MigrationState switch
        {
            "failed" => HealthState.Unhealthy,
            "running" when probe.HealthState == HealthState.Unhealthy => HealthState.Unhealthy,
            "running" => HealthState.Degraded,
            _ => probe.HealthState ?? state.HealthState
        };
    }

    private static string? ResolveHealthDescription(
        RoleRuntimeState state,
        RoleProbeResult probe)
    {
        return state.MigrationState switch
        {
            "failed" when probe.HealthState == HealthState.Unhealthy => CombineDescriptions(state.HealthDescription, probe.HealthDescription),
            "failed" => state.HealthDescription,
            "running" when probe.HealthState == HealthState.Unhealthy => CombineDescriptions(state.HealthDescription, probe.HealthDescription),
            "running" => state.HealthDescription,
            _ => probe.HealthDescription ?? state.HealthDescription
        };
    }

    private static DateTimeOffset? ResolveObservedAtUtc(
        DateTimeOffset? currentObservedAtUtc,
        DateTimeOffset? probedObservedAtUtc)
    {
        if (!currentObservedAtUtc.HasValue)
        {
            return probedObservedAtUtc;
        }

        if (!probedObservedAtUtc.HasValue)
        {
            return currentObservedAtUtc;
        }

        return currentObservedAtUtc >= probedObservedAtUtc
            ? currentObservedAtUtc
            : probedObservedAtUtc;
    }

    private static string? CombineDescriptions(string? first, string? second)
    {
        if (string.IsNullOrWhiteSpace(first))
        {
            return string.IsNullOrWhiteSpace(second) ? null : second.Trim();
        }

        if (string.IsNullOrWhiteSpace(second))
        {
            return first.Trim();
        }

        return string.Equals(first, second, StringComparison.OrdinalIgnoreCase)
            ? first.Trim()
            : $"{first.Trim()} | {second.Trim()}";
    }

    private static RoleRuntimeState CreateInitialState(
        string roleId,
        IReadOnlyList<Type> dbContextTypes,
        DatabaseMigrationsSelection migrationSelection,
        HashSet<string> requestedTargets)
    {
        var dbContextNames = dbContextTypes
            .Select(static dbContextType => dbContextType.FullName ?? dbContextType.Name)
            .ToArray();
        var migrationConfigured = migrationSelection.HasValues;
        var migrationTargeted = migrationConfigured && requestedTargets.Contains(roleId);
        var startupApplyEnabled = migrationSelection.ApplyOnStartup == true;

        string migrationState;
        string migrationDescription;
        var healthState = HealthState.Healthy;
        var healthDescription = $"Entity Framework runtime wiring is active for the '{roleId}' database role.";
        var runtimeMetadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["providerPack"] = "entity-framework",
            ["migrationRegistered"] = "true",
            ["dbContexts"] = string.Join(",", dbContextNames),
            ["migrationConfigured"] = migrationConfigured ? "true" : "false",
            ["migrationTargeted"] = migrationTargeted ? "true" : "false",
            ["startupApplyEnabled"] = startupApplyEnabled ? "true" : "false"
        };

        if (!migrationConfigured)
        {
            migrationState = "not-configured";
            migrationDescription = "No Engine:Databases:Migrations policy is configured for this Entity Framework database role.";
            runtimeMetadata["executionMode"] = "not-configured";
        }
        else if (!migrationTargeted)
        {
            migrationState = "not-targeted";
            migrationDescription = "Entity Framework migrations are registered for this role, but it is not targeted by Engine:Databases:Migrations.";
            runtimeMetadata["executionMode"] = "not-targeted";
        }
        else if (startupApplyEnabled)
        {
            migrationState = "pending-startup-apply";
            migrationDescription = "Entity Framework startup schema apply is configured for this database role.";
            runtimeMetadata["executionMode"] = "startup-hosted-service";
        }
        else
        {
            migrationState = "manual-or-deploy-time";
            migrationDescription = "This role is targeted by Engine:Databases:Migrations, but startup apply is disabled so schema changes stay manual or deploy-time.";
            runtimeMetadata["executionMode"] = "manual-or-deploy-time";
        }

        return new RoleRuntimeState(
            roleId,
            healthState,
            healthDescription,
            migrationState,
            migrationDescription,
            observedAtUtc: null,
            runtimeMetadata);
    }

    private sealed class RoleRuntimeState(
        string databaseRoleId,
        HealthState? healthState,
        string? healthDescription,
        string? migrationState,
        string? migrationDescription,
        DateTimeOffset? observedAtUtc,
        Dictionary<string, string> runtimeMetadata)
    {
        public string DatabaseRoleId { get; } = databaseRoleId;

        public HealthState? HealthState { get; set; } = healthState;

        public string? HealthDescription { get; set; } = healthDescription;

        public string? MigrationState { get; set; } = migrationState;

        public string? MigrationDescription { get; set; } = migrationDescription;

        public DateTimeOffset? ObservedAtUtc { get; set; } = observedAtUtc;

        public Dictionary<string, string> RuntimeMetadata { get; } = runtimeMetadata;

        public RoleRuntimeState Clone()
        {
            return new RoleRuntimeState(
                databaseRoleId: DatabaseRoleId,
                healthState: HealthState,
                healthDescription: HealthDescription,
                migrationState: MigrationState,
                migrationDescription: MigrationDescription,
                observedAtUtc: ObservedAtUtc,
                runtimeMetadata: new Dictionary<string, string>(RuntimeMetadata, StringComparer.OrdinalIgnoreCase));
        }

        public DatabaseRoleRuntimeDescriptor ToDescriptor()
        {
            return new DatabaseRoleRuntimeDescriptor(
                databaseRoleId: DatabaseRoleId,
                healthState: HealthState,
                healthDescription: HealthDescription,
                migrationState: MigrationState,
                migrationDescription: MigrationDescription,
                observedAtUtc: ObservedAtUtc,
                metadata: RuntimeMetadata);
        }
    }

    private sealed record RoleProbeResult(
        HealthState? HealthState,
        string? HealthDescription,
        DateTimeOffset? ObservedAtUtc,
        IReadOnlyDictionary<string, string> Metadata);
}
