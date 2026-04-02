using Cephalon.Abstractions.Health;

namespace Cephalon.Observability.CassandraDependencies.Services;

internal sealed class CassandraDependencyHealthContributor(CassandraDependencyHealthStore store) : IDependencyHealthContributor
{
    public IReadOnlyList<DependencyHealthReport> GetDependencyHealth() => store.GetReports();
}
