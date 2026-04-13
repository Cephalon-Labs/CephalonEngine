using Cephalon.Abstractions.Data;

namespace Cephalon.Engine.Data;

internal sealed class DatabaseMigrationOperationalPlaybookProvider(
    IDatabaseMigrationCatalog databaseMigrationCatalog) : IDatabaseMigrationOperationalPlaybookProvider
{
    public DatabaseMigrationOperationalPlaybook CreatePlaybook()
    {
        var generatedAtUtc = DateTimeOffset.UtcNow;
        var steps = databaseMigrationCatalog.DatabaseMigrations
            .OrderBy(static migration => migration.RecommendedExecutionOrder ?? int.MaxValue)
            .ThenBy(static migration => migration.Id, StringComparer.OrdinalIgnoreCase)
            .Select(CreateStep)
            .ToArray();

        return new DatabaseMigrationOperationalPlaybook(generatedAtUtc, steps);
    }

    private static DatabaseMigrationOperationalStep CreateStep(
        DatabaseMigrationDescriptor migration,
        int index)
    {
        ArgumentNullException.ThrowIfNull(migration);

        var productionCommand = migration.Commands.FirstOrDefault(static command => command.RecommendedForProduction) ??
            migration.Commands.FirstOrDefault(static command =>
                string.Equals(command.ExecutionCategory, "deploy-time", StringComparison.OrdinalIgnoreCase));
        var manualCommand = migration.Commands.FirstOrDefault(static command =>
                string.Equals(command.ExecutionCategory, "manual", StringComparison.OrdinalIgnoreCase)) ??
            migration.Commands.FirstOrDefault(static command => !command.RecommendedForProduction);

        return new DatabaseMigrationOperationalStep(
            order: index + 1,
            databaseMigrationId: migration.Id,
            requestedRoleId: migration.RequestedRoleId,
            resolvedRoleId: migration.ResolvedRoleId,
            status: migration.Status,
            executionMode: migration.ExecutionMode,
            applyOnStartup: migration.ApplyOnStartup,
            productionCommand: productionCommand,
            manualCommand: manualCommand);
    }
}
