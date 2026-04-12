using Cephalon.Abstractions.AppModel;
using Cephalon.Abstractions.Data;

namespace Cephalon.Data.EntityFramework.Services;

internal sealed class EntityFrameworkDatabaseMigrationCatalog : IDatabaseMigrationCatalog
{
    private readonly Lock gate = new();
    private readonly Dictionary<string, DatabaseMigrationDescriptor> databaseMigrationsById;
    private readonly IDatabaseRoleCatalog databaseRoleCatalog;

    public EntityFrameworkDatabaseMigrationCatalog(
        AppProfile appProfile,
        IEnumerable<EntityFrameworkDatabaseMigrationRegistration> registrations,
        IDatabaseRoleCatalog databaseRoleCatalog)
    {
        ArgumentNullException.ThrowIfNull(appProfile);
        ArgumentNullException.ThrowIfNull(registrations);
        this.databaseRoleCatalog = databaseRoleCatalog ?? throw new ArgumentNullException(nameof(databaseRoleCatalog));

        var migrationSelection = appProfile.Databases.Migrations;
        if (!migrationSelection.HasValues)
        {
            databaseMigrationsById = new Dictionary<string, DatabaseMigrationDescriptor>(StringComparer.OrdinalIgnoreCase);
            return;
        }

        var registrationArray = registrations.ToArray();
        var registrationsByTarget = registrationArray
            .SelectMany(static registration => registration.TargetRoleIds.Select(targetRoleId => (TargetRoleId: targetRoleId, Registration: registration)))
            .ToDictionary(static entry => entry.TargetRoleId, static entry => entry.Registration, StringComparer.OrdinalIgnoreCase);
        var requestedTargets = EntityFrameworkDatabaseMigrationTargetResolver.ResolveRequestedTargets(
            migrationSelection,
            registrationsByTarget.Keys);

        databaseMigrationsById = requestedTargets.ToDictionary(
            static targetRoleId => targetRoleId,
            targetRoleId => CreateDescriptor(appProfile, targetRoleId, registrationsByTarget, databaseRoleCatalog),
            StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<DatabaseMigrationDescriptor> DatabaseMigrations
    {
        get
        {
            DatabaseMigrationDescriptor[] snapshot;

            lock (gate)
            {
                snapshot = databaseMigrationsById.Values
                    .OrderBy(static entry => entry.RecommendedExecutionOrder ?? int.MaxValue)
                    .ThenBy(static entry => entry.Id, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }

            var rolesById = databaseRoleCatalog.DatabaseRoles
                .ToDictionary(static role => role.Id, StringComparer.OrdinalIgnoreCase);

            return snapshot
                .Select(entry => DecorateWithRoleRuntime(entry, rolesById.GetValueOrDefault(entry.Id)))
                .Where(static entry => entry is not null)
                .Select(static entry => entry!)
                .ToArray();
        }
    }

    public DatabaseMigrationDescriptor? GetById(string databaseMigrationId)
    {
        if (string.IsNullOrWhiteSpace(databaseMigrationId))
        {
            return null;
        }

        lock (gate)
        {
            return DecorateWithRoleRuntime(
                databaseMigrationsById.GetValueOrDefault(databaseMigrationId.Trim()),
                databaseRoleCatalog.GetById(databaseMigrationId.Trim()));
        }
    }

    public void MarkRunning(IReadOnlyList<string> targetRoleIds, DateTimeOffset startedAtUtc)
    {
        UpdateTargets(targetRoleIds, entry => Clone(
            entry,
            status: DatabaseMigrationStatus.Running,
            startedAtUtc: startedAtUtc,
            completedAtUtc: null,
            lastError: null));
    }

    public void MarkSucceeded(
        IReadOnlyList<string> targetRoleIds,
        string mechanism,
        DateTimeOffset startedAtUtc,
        DateTimeOffset completedAtUtc)
    {
        UpdateTargets(targetRoleIds, entry => Clone(
            entry,
            status: DatabaseMigrationStatus.Succeeded,
            mechanism: mechanism,
            startedAtUtc: startedAtUtc,
            completedAtUtc: completedAtUtc,
            lastError: null));
    }

    public void MarkFailed(
        IReadOnlyList<string> targetRoleIds,
        string error,
        DateTimeOffset startedAtUtc,
        DateTimeOffset completedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);

        UpdateTargets(targetRoleIds, entry => Clone(
            entry,
            status: DatabaseMigrationStatus.Failed,
            startedAtUtc: startedAtUtc,
            completedAtUtc: completedAtUtc,
            lastError: error.Trim()));
    }

    private void UpdateTargets(
        IReadOnlyList<string> targetRoleIds,
        Func<DatabaseMigrationDescriptor, DatabaseMigrationDescriptor> updater)
    {
        ArgumentNullException.ThrowIfNull(targetRoleIds);
        ArgumentNullException.ThrowIfNull(updater);

        lock (gate)
        {
            foreach (var targetRoleId in targetRoleIds.Where(static targetRoleId => !string.IsNullOrWhiteSpace(targetRoleId)))
            {
                if (databaseMigrationsById.TryGetValue(targetRoleId.Trim(), out var entry))
                {
                    databaseMigrationsById[targetRoleId.Trim()] = updater(entry);
                }
            }
        }
    }

    private static DatabaseMigrationDescriptor CreateDescriptor(
        AppProfile appProfile,
        string targetRoleId,
        Dictionary<string, EntityFrameworkDatabaseMigrationRegistration> registrationsByTarget,
        IDatabaseRoleCatalog databaseRoleCatalog)
    {
        var migrationSelection = appProfile.Databases.Migrations;
        var hasRegistration = registrationsByTarget.TryGetValue(targetRoleId, out var registration);
        var role = databaseRoleCatalog.GetById(targetRoleId);
        var status = hasRegistration ? DatabaseMigrationStatus.Planned : DatabaseMigrationStatus.Unsupported;
        var error = hasRegistration
            ? null
            : $"No registered DbContext can satisfy migration target '{targetRoleId}'.";
        var recommendedExecutionOrder = GetRecommendedExecutionOrder(targetRoleId);

        return new DatabaseMigrationDescriptor(
            id: targetRoleId,
            displayName: $"{ToDisplayName(targetRoleId)} Database Migration",
            description: hasRegistration
                ? $"Migration execution state for the '{targetRoleId}' database role."
                : $"Migration target '{targetRoleId}' is configured but not backed by a registered DbContext.",
            requestedRoleId: role?.RequestedRoleId ?? targetRoleId,
            resolvedRoleId: role?.ResolvedRoleId ?? targetRoleId,
            executionMode: migrationSelection.ApplyOnStartup == true
                ? "startup-hosted-service"
                : "manual-or-deploy-time",
            status: status,
            applyOnStartup: migrationSelection.ApplyOnStartup == true,
            exitAfterApply: migrationSelection.ExitAfterApply == true,
            provider: role?.Provider,
            dbContextType: hasRegistration ? GetTypeName(registration!.DbContextType) : null,
            commands: hasRegistration ? CreateCommands(targetRoleId, registration!) : null,
            lastError: error,
            metadata: CreateMetadata(targetRoleId, role, hasRegistration, registration, recommendedExecutionOrder),
            recommendedExecutionOrder: recommendedExecutionOrder);
    }

    private static IReadOnlyList<DatabaseMigrationCommandDescriptor> CreateCommands(
        string targetRoleId,
        EntityFrameworkDatabaseMigrationRegistration registration)
    {
        var dbContextName = registration.DbContextType.Name;
        var dbContextTypeName = GetTypeName(registration.DbContextType);
        var normalizedTarget = targetRoleId.Trim().ToLowerInvariant();

        return
        [
            new DatabaseMigrationCommandDescriptor(
                id: "bundle",
                displayName: "EF Core migration bundle",
                description: $"Build a deploy-time migration bundle for the '{normalizedTarget}' database role.",
                commandTemplate: $"dotnet ef migrations bundle --context {dbContextName}",
                recommendedForProduction: true,
                metadata: CreateCommandMetadata(normalizedTarget, dbContextTypeName, "bundle", "deploy-time"),
                toolId: "dotnet-ef",
                executionCategory: "deploy-time",
                workingDirectoryHint: "startup-project"),
            new DatabaseMigrationCommandDescriptor(
                id: "script",
                displayName: "EF Core idempotent migration script",
                description: $"Generate an idempotent SQL script for the '{normalizedTarget}' database role.",
                commandTemplate: $"dotnet ef migrations script --context {dbContextName} --idempotent",
                recommendedForProduction: true,
                metadata: CreateCommandMetadata(normalizedTarget, dbContextTypeName, "script", "deploy-time"),
                toolId: "dotnet-ef",
                executionCategory: "deploy-time",
                workingDirectoryHint: "startup-project"),
            new DatabaseMigrationCommandDescriptor(
                id: "update",
                displayName: "EF Core direct database update",
                description: $"Apply pending migrations directly for the '{normalizedTarget}' database role from the startup project.",
                commandTemplate: $"dotnet ef database update --context {dbContextName}",
                recommendedForProduction: false,
                metadata: CreateCommandMetadata(normalizedTarget, dbContextTypeName, "update", "manual"),
                toolId: "dotnet-ef",
                executionCategory: "manual",
                workingDirectoryHint: "startup-project")
        ];
    }

    private static Dictionary<string, string> CreateMetadata(
        string targetRoleId,
        DatabaseRoleDescriptor? role,
        bool hasRegistration,
        EntityFrameworkDatabaseMigrationRegistration? registration,
        int? recommendedExecutionOrder)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["runtimeProvider"] = "entity-framework",
            ["topologySource"] = "engine-databases",
            ["supportedByRuntime"] = hasRegistration ? "true" : "false"
        };

        if (role is not null)
        {
            metadata["requestedRole"] = role.RequestedRoleId;
            metadata["resolvedRole"] = role.ResolvedRoleId;
            metadata["resolutionMode"] = role.ResolutionMode;
            metadata["usesRoleReference"] = role.UsesRoleReference ? "true" : "false";

            if (role.UseRole is not null)
            {
                metadata["useRole"] = role.UseRole;
            }

            if (role.ConnectionMode is not null)
            {
                metadata["connectionMode"] = role.ConnectionMode;
            }

            if (role.ConnectionStringName is not null)
            {
                metadata["connectionStringName"] = role.ConnectionStringName;
            }

            if (role.Schema is not null)
            {
                metadata["schema"] = role.Schema;
            }
        }

        if (hasRegistration && registration is not null)
        {
            metadata["dbContext"] = GetTypeName(registration.DbContextType);
            metadata["registeredTargets"] = string.Join(",", registration.TargetRoleIds);
            metadata["recommendedExecutionMode"] = "bundle-or-script";
            metadata["commandIds"] = "bundle,script,update";
        }
        else
        {
            metadata["dbContext"] = "<unregistered>";
            metadata["registeredTargets"] = targetRoleId;
        }

        if (recommendedExecutionOrder.HasValue)
        {
            metadata["recommendedExecutionOrder"] = recommendedExecutionOrder.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        return metadata;
    }

    private static Dictionary<string, string> CreateCommandMetadata(
        string targetRoleId,
        string dbContextTypeName,
        string commandId,
        string executionCategory)
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["runtimeProvider"] = "entity-framework",
            ["tool"] = "dotnet-ef",
            ["targetRole"] = targetRoleId,
            ["dbContext"] = dbContextTypeName,
            ["commandId"] = commandId,
            ["executionCategory"] = executionCategory,
            ["workingDirectoryHint"] = "startup-project"
        };
    }

    private static string ToDisplayName(string role)
    {
        return char.ToUpperInvariant(role[0]) + role[1..];
    }

    private static string GetTypeName(Type type)
    {
        return type.FullName ?? type.Name;
    }

    private static DatabaseMigrationDescriptor Clone(
        DatabaseMigrationDescriptor entry,
        DatabaseMigrationStatus? status = null,
        string? mechanism = null,
        DateTimeOffset? startedAtUtc = null,
        DateTimeOffset? completedAtUtc = null,
        string? lastError = null,
        IReadOnlyDictionary<string, string>? metadata = null,
        Cephalon.Abstractions.Health.HealthState? roleHealthState = null,
        string? roleHealthDescription = null,
        string? roleMigrationState = null,
        string? roleMigrationDescription = null,
        DateTimeOffset? roleObservedAtUtc = null)
    {
        return new DatabaseMigrationDescriptor(
            id: entry.Id,
            displayName: entry.DisplayName,
            description: entry.Description,
            requestedRoleId: entry.RequestedRoleId,
            resolvedRoleId: entry.ResolvedRoleId,
            executionMode: entry.ExecutionMode,
            status: status ?? entry.Status,
            applyOnStartup: entry.ApplyOnStartup,
            exitAfterApply: entry.ExitAfterApply,
            provider: entry.Provider,
            dbContextType: entry.DbContextType,
            mechanism: mechanism ?? entry.Mechanism,
            startedAtUtc: startedAtUtc ?? entry.StartedAtUtc,
            completedAtUtc: completedAtUtc ?? entry.CompletedAtUtc,
            lastError: lastError ?? entry.LastError,
            commands: entry.Commands,
            metadata: metadata ?? entry.Metadata,
            recommendedExecutionOrder: entry.RecommendedExecutionOrder,
            roleHealthState: roleHealthState ?? entry.RoleHealthState,
            roleHealthDescription: roleHealthDescription ?? entry.RoleHealthDescription,
            roleMigrationState: roleMigrationState ?? entry.RoleMigrationState,
            roleMigrationDescription: roleMigrationDescription ?? entry.RoleMigrationDescription,
            roleObservedAtUtc: roleObservedAtUtc ?? entry.RoleObservedAtUtc);
    }

    private static DatabaseMigrationDescriptor? DecorateWithRoleRuntime(
        DatabaseMigrationDescriptor? entry,
        DatabaseRoleDescriptor? role)
    {
        if (entry is null || role is null)
        {
            return entry;
        }

        var metadata = new Dictionary<string, string>(entry.Metadata, StringComparer.OrdinalIgnoreCase);

        if (role.HealthState.HasValue)
        {
            metadata["roleHealthState"] = role.HealthState.Value.ToString().ToLowerInvariant();
        }

        if (!string.IsNullOrWhiteSpace(role.HealthDescription))
        {
            metadata["roleHealthDescription"] = role.HealthDescription;
        }

        if (!string.IsNullOrWhiteSpace(role.MigrationState))
        {
            metadata["roleMigrationState"] = role.MigrationState;
        }

        if (!string.IsNullOrWhiteSpace(role.MigrationDescription))
        {
            metadata["roleMigrationDescription"] = role.MigrationDescription;
        }

        if (role.ObservedAtUtc.HasValue)
        {
            metadata["roleObservedAtUtc"] = role.ObservedAtUtc.Value.ToString("O");
        }

        foreach (var pair in role.RuntimeMetadata)
        {
            metadata[$"roleRuntime.{pair.Key}"] = pair.Value;
        }

        return Clone(
            entry,
            metadata: metadata,
            roleHealthState: role.HealthState,
            roleHealthDescription: role.HealthDescription,
            roleMigrationState: role.MigrationState,
            roleMigrationDescription: role.MigrationDescription,
            roleObservedAtUtc: role.ObservedAtUtc);
    }

    private static int? GetRecommendedExecutionOrder(string targetRoleId)
    {
        if (string.Equals(targetRoleId, "write", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        if (string.Equals(targetRoleId, "read", StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        if (string.Equals(targetRoleId, "history", StringComparison.OrdinalIgnoreCase))
        {
            return 3;
        }

        if (string.Equals(targetRoleId, "outbox", StringComparison.OrdinalIgnoreCase))
        {
            return 4;
        }

        return null;
    }
}
