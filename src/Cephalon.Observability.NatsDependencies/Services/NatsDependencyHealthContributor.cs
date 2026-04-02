using Cephalon.Abstractions.Health;

namespace Cephalon.Observability.NatsDependencies.Services;

internal sealed class NatsDependencyHealthContributor(NatsDependencyHealthStore store) : IDependencyHealthContributor
{
    public IReadOnlyList<DependencyHealthReport> GetDependencyHealth() => store.GetReports();
}
