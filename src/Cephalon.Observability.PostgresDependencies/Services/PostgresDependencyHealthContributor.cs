using Cephalon.Abstractions.Health;

namespace Cephalon.Observability.PostgresDependencies.Services;

internal sealed class PostgresDependencyHealthContributor(PostgresDependencyHealthStore store) : IDependencyHealthContributor
{
    public IReadOnlyList<DependencyHealthReport> GetDependencyHealth() => store.GetReports();
}
