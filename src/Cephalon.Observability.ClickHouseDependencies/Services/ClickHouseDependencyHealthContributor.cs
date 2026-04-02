using Cephalon.Abstractions.Health;

namespace Cephalon.Observability.ClickHouseDependencies.Services;

internal sealed class ClickHouseDependencyHealthContributor(ClickHouseDependencyHealthStore store) : IDependencyHealthContributor
{
    public IReadOnlyList<DependencyHealthReport> GetDependencyHealth() => store.GetReports();
}
