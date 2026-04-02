using Cephalon.Abstractions.Health;

namespace Cephalon.Observability.RedisDependencies.Services;

internal sealed class RedisDependencyHealthContributor(RedisDependencyHealthStore store) : IDependencyHealthContributor
{
    public IReadOnlyList<DependencyHealthReport> GetDependencyHealth() => store.GetReports();
}
