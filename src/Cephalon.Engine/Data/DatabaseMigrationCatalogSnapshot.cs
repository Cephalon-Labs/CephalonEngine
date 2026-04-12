using Cephalon.Abstractions.Data;

namespace Cephalon.Engine.Data;

internal sealed class DatabaseMigrationCatalogSnapshot(
    IEnumerable<IDatabaseMigrationContributor>? contributors = null) : IDatabaseMigrationCatalog
{
    private readonly IDatabaseMigrationContributor[] contributors = contributors?.ToArray() ?? [];

    public IReadOnlyList<DatabaseMigrationDescriptor> DatabaseMigrations => CreateState().DatabaseMigrations;

    public DatabaseMigrationDescriptor? GetById(string databaseMigrationId)
    {
        if (string.IsNullOrWhiteSpace(databaseMigrationId))
        {
            return null;
        }

        return CreateState().DatabaseMigrationsById.TryGetValue(databaseMigrationId.Trim(), out var databaseMigration)
            ? databaseMigration
            : null;
    }

    private CatalogState CreateState()
    {
        var databaseMigrations = contributors
            .SelectMany(static contributor => contributor.DescribeDatabaseMigrations())
            .OrderBy(static entry => entry.RecommendedExecutionOrder ?? int.MaxValue)
            .ThenBy(static entry => entry.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var duplicate = databaseMigrations
            .GroupBy(static entry => entry.Id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(static group => group.Skip(1).Any());

        if (duplicate is not null)
        {
            throw new InvalidOperationException(
                $"Database migration '{duplicate.Key}' is contributed multiple times.");
        }

        return new CatalogState(
            databaseMigrations,
            databaseMigrations.ToDictionary(static entry => entry.Id, StringComparer.OrdinalIgnoreCase));
    }

    private sealed record CatalogState(
        IReadOnlyList<DatabaseMigrationDescriptor> DatabaseMigrations,
        Dictionary<string, DatabaseMigrationDescriptor> DatabaseMigrationsById);
}
