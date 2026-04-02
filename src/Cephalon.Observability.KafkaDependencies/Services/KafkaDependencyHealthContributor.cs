using Cephalon.Abstractions.Health;

namespace Cephalon.Observability.KafkaDependencies.Services;

internal sealed class KafkaDependencyHealthContributor(KafkaDependencyHealthStore store) : IDependencyHealthContributor
{
    public IReadOnlyList<DependencyHealthReport> GetDependencyHealth() => store.GetReports();
}
