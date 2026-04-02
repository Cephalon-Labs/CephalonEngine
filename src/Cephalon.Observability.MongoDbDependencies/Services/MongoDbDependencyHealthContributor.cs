using Cephalon.Abstractions.Health;

namespace Cephalon.Observability.MongoDbDependencies.Services;

internal sealed class MongoDbDependencyHealthContributor(MongoDbDependencyHealthStore store) : IDependencyHealthContributor
{
    public IReadOnlyList<DependencyHealthReport> GetDependencyHealth() => store.GetReports();
}
