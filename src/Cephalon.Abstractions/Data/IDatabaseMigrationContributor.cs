namespace Cephalon.Abstractions.Data;

/// <summary>
/// Contributes one or more database-migration descriptors to the active Cephalon runtime.
/// </summary>
public interface IDatabaseMigrationContributor
{
    /// <summary>
    /// Describes the database-migration targets that should appear in the active runtime catalog.
    /// </summary>
    /// <returns>The migration descriptors contributed by the current provider or module pack.</returns>
    IReadOnlyList<DatabaseMigrationDescriptor> DescribeDatabaseMigrations();
}
