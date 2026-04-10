using Cephalon.Abstractions.Data;

namespace Cephalon.Data.EntityFramework.Services;

internal sealed class EntityFrameworkDatabaseMigrationContributorAdapter(
    EntityFrameworkDatabaseMigrationCatalog catalog) : IDatabaseMigrationContributor
{
    private readonly EntityFrameworkDatabaseMigrationCatalog catalog = catalog
        ?? throw new ArgumentNullException(nameof(catalog));

    public IReadOnlyList<DatabaseMigrationDescriptor> DescribeDatabaseMigrations()
    {
        return catalog.DatabaseMigrations;
    }
}
