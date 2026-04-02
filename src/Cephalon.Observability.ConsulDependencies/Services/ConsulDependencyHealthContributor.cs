using Cephalon.Abstractions.Health;

namespace Cephalon.Observability.ConsulDependencies.Services;

internal sealed class ConsulDependencyHealthContributor(ConsulDependencyHealthStore store) : IDependencyHealthContributor
{
    public IReadOnlyList<DependencyHealthReport> GetDependencyHealth() => store.GetReports();
}
