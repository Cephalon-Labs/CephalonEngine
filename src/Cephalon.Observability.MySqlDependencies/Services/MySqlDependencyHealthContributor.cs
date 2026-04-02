using Cephalon.Abstractions.Health;

namespace Cephalon.Observability.MySqlDependencies.Services;

internal sealed class MySqlDependencyHealthContributor(MySqlDependencyHealthStore store) : IDependencyHealthContributor
{
    public IReadOnlyList<DependencyHealthReport> GetDependencyHealth() => store.GetReports();
}
