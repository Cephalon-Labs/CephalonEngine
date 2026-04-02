using Cephalon.Abstractions.Health;

namespace Cephalon.Observability.SqlServerDependencies.Services;

internal sealed class SqlServerDependencyHealthContributor(SqlServerDependencyHealthStore store) : IDependencyHealthContributor
{
    public IReadOnlyList<DependencyHealthReport> GetDependencyHealth() => store.GetReports();
}
