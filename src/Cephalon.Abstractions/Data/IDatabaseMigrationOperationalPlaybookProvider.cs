namespace Cephalon.Abstractions.Data;

/// <summary>
/// Creates the engine-owned ordered operator playbook for the current database-migration catalog.
/// </summary>
public interface IDatabaseMigrationOperationalPlaybookProvider
{
    /// <summary>
    /// Creates the current database-migration playbook.
    /// </summary>
    /// <returns>The current ordered operator playbook for database migrations.</returns>
    DatabaseMigrationOperationalPlaybook CreatePlaybook();
}
