using Cephalon.Abstractions.AppModel;
using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Health;

namespace Cephalon.Data.EntityFramework.Services;

internal sealed class EntityFrameworkDatabaseRoleRuntimeContributor : IDatabaseRoleRuntimeContributor
{
    private readonly object syncRoot = new();
    private readonly Dictionary<string, RoleRuntimeState> roleStates;
    private readonly TimeProvider timeProvider;

    public EntityFrameworkDatabaseRoleRuntimeContributor(
        AppProfile appProfile,
        IEnumerable<EntityFrameworkDatabaseMigrationRegistration> registrations,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(appProfile);
        ArgumentNullException.ThrowIfNull(registrations);

        this.timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

        var registrationsByRole = registrations
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
                registrationsByRole.Keys)
            : [];

        roleStates = registrationsByRole.ToDictionary(
            static pair => pair.Key,
            pair => CreateInitialState(pair.Key, pair.Value, migrationSelection, requestedTargets),
            StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<DatabaseRoleRuntimeDescriptor> DescribeDatabaseRoleRuntime()
    {
        lock (syncRoot)
        {
            return roleStates
                .OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .Select(static pair => pair.Value.ToDescriptor())
                .ToArray();
        }
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
}
