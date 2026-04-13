using Cephalon.Abstractions.Data;

namespace Cephalon.Engine.Data;

internal sealed class DatabaseMigrationOperationalPlaybookProvider(
    IDatabaseMigrationCatalog databaseMigrationCatalog,
    IDatabaseRoleCatalog databaseRoleCatalog) : IDatabaseMigrationOperationalPlaybookProvider
{
    public DatabaseMigrationOperationalPlaybook CreatePlaybook()
    {
        var generatedAtUtc = DateTimeOffset.UtcNow;
        var rolesById = databaseRoleCatalog.DatabaseRoles
            .ToDictionary(static role => role.Id, StringComparer.OrdinalIgnoreCase);
        var physicalTargetGroups = databaseMigrationCatalog.DatabaseMigrations
            .Select(migration =>
            {
                rolesById.TryGetValue(migration.Id, out var role);
                return new
                {
                    migration.Id,
                    PhysicalTargetId = role?.PhysicalTargetId ?? $"migration:{migration.Id}",
                    PhysicalTargetDisplayName = role?.PhysicalTargetDisplayName ?? $"Logical migration target '{migration.Id}'"
                };
            })
            .GroupBy(static entry => entry.PhysicalTargetId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => new PhysicalTargetGroup(
                    group.Key,
                    group.Select(static entry => entry.Id)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(static migrationId => migrationId, StringComparer.OrdinalIgnoreCase)
                        .ToArray(),
                    group.Select(static entry => entry.PhysicalTargetDisplayName)
                        .FirstOrDefault(static displayName => !string.IsNullOrWhiteSpace(displayName)) ?? "Physical database target"),
                StringComparer.OrdinalIgnoreCase);
        var steps = databaseMigrationCatalog.DatabaseMigrations
            .OrderBy(static migration => migration.RecommendedExecutionOrder ?? int.MaxValue)
            .ThenBy(static migration => migration.Id, StringComparer.OrdinalIgnoreCase)
            .Select((migration, index) => CreateStep(
                migration,
                rolesById.GetValueOrDefault(migration.Id),
                physicalTargetGroups,
                index))
            .ToArray();

        return new DatabaseMigrationOperationalPlaybook(generatedAtUtc, steps);
    }

    private static DatabaseMigrationOperationalStep CreateStep(
        DatabaseMigrationDescriptor migration,
        DatabaseRoleDescriptor? role,
        IReadOnlyDictionary<string, PhysicalTargetGroup> physicalTargetGroups,
        int index)
    {
        ArgumentNullException.ThrowIfNull(migration);
        ArgumentNullException.ThrowIfNull(physicalTargetGroups);

        var productionCommand = migration.Commands.FirstOrDefault(static command => command.RecommendedForProduction) ??
            migration.Commands.FirstOrDefault(static command =>
                string.Equals(command.ExecutionCategory, "deploy-time", StringComparison.OrdinalIgnoreCase));
        var manualCommand = migration.Commands.FirstOrDefault(static command =>
                string.Equals(command.ExecutionCategory, "manual", StringComparison.OrdinalIgnoreCase)) ??
            migration.Commands.FirstOrDefault(static command => !command.RecommendedForProduction);
        var physicalTargetId = role?.PhysicalTargetId ?? $"migration:{migration.Id}";
        var physicalTargetDisplayName = role?.PhysicalTargetDisplayName ?? $"Logical migration target '{migration.Id}'";
        physicalTargetGroups.TryGetValue(physicalTargetId, out var physicalTargetGroup);
        var coordinatedMigrationIds = physicalTargetGroup?.MigrationIds
            .Where(migrationId => !string.Equals(migrationId, migration.Id, StringComparison.OrdinalIgnoreCase))
            .ToArray() ?? [];
        var coordinationHint = coordinatedMigrationIds.Length == 0
            ? null
            : BuildCoordinationHint(migration.Id, physicalTargetDisplayName, coordinatedMigrationIds);

        return new DatabaseMigrationOperationalStep(
            order: index + 1,
            databaseMigrationId: migration.Id,
            requestedRoleId: migration.RequestedRoleId,
            resolvedRoleId: migration.ResolvedRoleId,
            status: migration.Status,
            executionMode: migration.ExecutionMode,
            applyOnStartup: migration.ApplyOnStartup,
            physicalTargetId: physicalTargetId,
            physicalTargetDisplayName: physicalTargetDisplayName,
            coordinatedMigrationIds: coordinatedMigrationIds,
            coordinationHint: coordinationHint,
            productionCommand: productionCommand,
            manualCommand: manualCommand);
    }

    private static string BuildCoordinationHint(
        string migrationId,
        string physicalTargetDisplayName,
        IReadOnlyList<string> coordinatedMigrationIds)
    {
        return $"Migration target '{migrationId}' shares {physicalTargetDisplayName} with {string.Join(", ", coordinatedMigrationIds)}. Keep bundle/script outputs or separate migrations projects coordinated before deploy-time execution.";
    }

    private sealed record PhysicalTargetGroup(
        string PhysicalTargetId,
        IReadOnlyList<string> MigrationIds,
        string PhysicalTargetDisplayName);
}
