using Cephalon.Abstractions.Health;

namespace Cephalon.Observability.OpenSearchDependencies.Services;

internal sealed class OpenSearchDependencyHealthContributor(OpenSearchDependencyHealthStore store) : IDependencyHealthContributor
{
    public IReadOnlyList<DependencyHealthReport> GetDependencyHealth() => store.GetReports();
}
