using Cephalon.Abstractions.Health;

namespace Cephalon.Observability.Neo4jDependencies.Services;

internal sealed class Neo4jDependencyHealthContributor(Neo4jDependencyHealthStore store) : IDependencyHealthContributor
{
    public IReadOnlyList<DependencyHealthReport> GetDependencyHealth() => store.GetReports();
}
