using Cephalon.Abstractions.Data;

namespace Cephalon.Data.EntityFramework.Services;

internal sealed class EntityFrameworkDatabaseRoleRuntimeContributorAdapter(
    EntityFrameworkDatabaseRoleRuntimeContributor contributor) : IDatabaseRoleRuntimeContributor
{
    private readonly EntityFrameworkDatabaseRoleRuntimeContributor contributor = contributor
        ?? throw new ArgumentNullException(nameof(contributor));

    public IReadOnlyList<DatabaseRoleRuntimeDescriptor> DescribeDatabaseRoleRuntime()
    {
        return contributor.DescribeDatabaseRoleRuntime();
    }
}
