using Cephalon.Abstractions.AppModel;
using Cephalon.Abstractions.Data;

namespace Cephalon.Data.EntityFramework.Services;

internal sealed class EntityFrameworkDatabaseMigrationCatalog : IDatabaseMigrationCatalog
{
    private readonly Lock gate = new();
    private readonly Dictionary<string, DatabaseMigrationDescriptor> databaseMigrationsById;

    public EntityFrameworkDatabaseMigrationCatalog(
        AppProfile appProfile,
        IEnumerable<EntityFrameworkDatabaseMigrationRegistration> registrations,
        IDatabaseRoleCatalog databaseRoleCatalog)
    {
        ArgumentNullException.ThrowIfNull(appProfile);
        ArgumentNullException.ThrowIfNull(registrations);
        ArgumentNullException.ThrowIfNull(databaseRoleCatalog);

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
            lock (gate)
            {
                return databaseMigrationsById.Values
                    .OrderBy(static entry => entry.Id, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
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
            return databaseMigrationsById.GetValueOrDefault(databaseMigrationId.Trim());
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
            lastError: error,
            metadata: CreateMetadata(targetRoleId, role, hasRegistration, registration));
    }

    private static Dictionary<string, string> CreateMetadata(
        string targetRoleId,
        DatabaseRoleDescriptor? role,
        bool hasRegistration,
        EntityFrameworkDatabaseMigrationRegistration? registration)
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
        }
        else
        {
            metadata["dbContext"] = "<unregistered>";
            metadata["registeredTargets"] = targetRoleId;
        }

        return metadata;
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
        string? lastError = null)
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
            metadata: entry.Metadata);
    }
}
