using Cephalon.Abstractions.Health;

namespace Cephalon.Observability.ElasticsearchDependencies.Services;

internal sealed class ElasticsearchDependencyHealthContributor(ElasticsearchDependencyHealthStore store) : IDependencyHealthContributor
{
    public IReadOnlyList<DependencyHealthReport> GetDependencyHealth() => store.GetReports();
}
