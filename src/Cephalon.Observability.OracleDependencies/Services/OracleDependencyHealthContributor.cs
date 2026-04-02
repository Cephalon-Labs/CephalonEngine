using Cephalon.Abstractions.Health;

namespace Cephalon.Observability.OracleDependencies.Services;

internal sealed class OracleDependencyHealthContributor(OracleDependencyHealthStore store) : IDependencyHealthContributor
{
    public IReadOnlyList<DependencyHealthReport> GetDependencyHealth() => store.GetReports();
}
