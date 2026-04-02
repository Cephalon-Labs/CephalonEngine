using Cephalon.Abstractions.Health;

namespace Cephalon.Observability.RabbitMqDependencies.Services;

internal sealed class RabbitMqDependencyHealthContributor(RabbitMqDependencyHealthStore store) : IDependencyHealthContributor
{
    public IReadOnlyList<DependencyHealthReport> GetDependencyHealth() => store.GetReports();
}
