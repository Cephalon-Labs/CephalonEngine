using Cephalon.Abstractions.Health;

namespace Cephalon.Observability.MemcachedDependencies.Services;

internal sealed class MemcachedDependencyHealthContributor(MemcachedDependencyHealthStore store) : IDependencyHealthContributor
{
    public IReadOnlyList<DependencyHealthReport> GetDependencyHealth() => store.GetReports();
}
