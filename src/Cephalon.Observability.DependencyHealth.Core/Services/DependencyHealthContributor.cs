using Cephalon.Abstractions.Health;

namespace Cephalon.Observability.DependencyHealth.Core.Services;

/// <summary>Implements <see cref="IDependencyHealthContributor"/> by delegating to a <see cref="DependencyHealthStore"/>.</summary>
internal sealed class DependencyHealthContributor(DependencyHealthStore store) : IDependencyHealthContributor
{
    /// <inheritdoc />
    public IReadOnlyList<DependencyHealthReport> GetDependencyHealth() => store.GetReports();
}
