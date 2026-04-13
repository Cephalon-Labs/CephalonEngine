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
        var executionGroups = CreateExecutionGroups(steps);

        return new DatabaseMigrationOperationalPlaybook(generatedAtUtc, steps, executionGroups);
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

    private static DatabaseMigrationOperationalExecutionGroup[] CreateExecutionGroups(
        IReadOnlyList<DatabaseMigrationOperationalStep> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);

        return steps
            .GroupBy(
                static step => step.PhysicalTargetId ?? $"migration:{step.DatabaseMigrationId}",
                StringComparer.OrdinalIgnoreCase)
            .Select(static group =>
            {
                var orderedSteps = group
                    .OrderBy(static step => step.Order)
                    .ThenBy(static step => step.DatabaseMigrationId, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                var firstStep = orderedSteps[0];

                return new DatabaseMigrationOperationalExecutionGroup(
                    order: orderedSteps.Min(static step => step.Order),
                    physicalTargetId: group.Key,
                    physicalTargetDisplayName: firstStep.PhysicalTargetDisplayName ?? $"Logical migration target '{firstStep.DatabaseMigrationId}'",
                    status: ResolveGroupStatus(orderedSteps),
                    databaseMigrationIds: orderedSteps.Select(static step => step.DatabaseMigrationId).ToArray(),
                    requestedRoleIds: orderedSteps.Select(static step => step.RequestedRoleId).ToArray(),
                    resolvedRoleIds: orderedSteps.Select(static step => step.ResolvedRoleId).ToArray(),
                    productionReadyTargetCount: orderedSteps.Count(static step => step.HasProductionRecommendedCommand),
                    manualPathTargetCount: orderedSteps.Count(static step => step.ManualCommand is not null),
                    applyOnStartupTargetCount: orderedSteps.Count(static step => step.ApplyOnStartup),
                    coordinationHint: orderedSteps.Length > 1
                        ? BuildExecutionGroupCoordinationHint(
                            firstStep.PhysicalTargetDisplayName ?? $"physical target '{group.Key}'",
                            orderedSteps.Select(static step => step.DatabaseMigrationId).ToArray())
                        : null);
            })
            .OrderBy(static group => group.Order)
            .ThenBy(static group => group.PhysicalTargetId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static DatabaseMigrationStatus ResolveGroupStatus(
        IReadOnlyList<DatabaseMigrationOperationalStep> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);

        if (steps.Any(static step => step.Status == DatabaseMigrationStatus.Failed))
        {
            return DatabaseMigrationStatus.Failed;
        }

        if (steps.Any(static step => step.Status == DatabaseMigrationStatus.Running))
        {
            return DatabaseMigrationStatus.Running;
        }

        if (steps.Any(static step => step.Status == DatabaseMigrationStatus.Unsupported))
        {
            return DatabaseMigrationStatus.Unsupported;
        }

        if (steps.Any(static step => step.Status == DatabaseMigrationStatus.Planned))
        {
            return DatabaseMigrationStatus.Planned;
        }

        return DatabaseMigrationStatus.Succeeded;
    }

    private static string BuildExecutionGroupCoordinationHint(
        string physicalTargetDisplayName,
        IReadOnlyList<string> databaseMigrationIds)
    {
        return $"Migration targets {string.Join(", ", databaseMigrationIds.Select(static migrationId => $"'{migrationId}'"))} share {physicalTargetDisplayName}. Execute them as one coordinated physical-target batch by keeping bundle/script outputs or separate migrations projects aligned before deploy-time execution.";
    }

    private sealed record PhysicalTargetGroup(
        string PhysicalTargetId,
        IReadOnlyList<string> MigrationIds,
        string PhysicalTargetDisplayName);
}
