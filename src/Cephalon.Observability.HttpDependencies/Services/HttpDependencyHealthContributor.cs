using Cephalon.Abstractions.Health;

namespace Cephalon.Observability.HttpDependencies.Services;

internal sealed class HttpDependencyHealthContributor(HttpDependencyHealthStore store) : IDependencyHealthContributor
{
    public IReadOnlyList<DependencyHealthReport> GetDependencyHealth() => store.GetReports();
}
