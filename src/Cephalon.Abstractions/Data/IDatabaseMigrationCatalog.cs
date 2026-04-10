namespace Cephalon.Abstractions.Data;

/// <summary>
/// Exposes the active runtime database-migration catalog for the current Cephalon host.
/// </summary>
public interface IDatabaseMigrationCatalog
{
    /// <summary>
    /// Gets every migration target visible to the current runtime.
    /// </summary>
    IReadOnlyList<DatabaseMigrationDescriptor> DatabaseMigrations { get; }

    /// <summary>
    /// Gets one migration target by its logical identifier.
    /// </summary>
    /// <param name="databaseMigrationId">The logical migration-target identifier.</param>
    /// <returns>The matching migration-target descriptor, or <see langword="null" /> when none exists.</returns>
    DatabaseMigrationDescriptor? GetById(string databaseMigrationId);
}
