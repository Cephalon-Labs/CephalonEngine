using Cephalon.Abstractions.Health;

namespace Cephalon.Observability.MqttDependencies.Services;

internal sealed class MqttDependencyHealthContributor(MqttDependencyHealthStore store) : IDependencyHealthContributor
{
    public IReadOnlyList<DependencyHealthReport> GetDependencyHealth() => store.GetReports();
}
