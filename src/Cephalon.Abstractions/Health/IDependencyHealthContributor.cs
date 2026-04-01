namespace Cephalon.Abstractions.Health;

public interface IDependencyHealthContributor
{
    IReadOnlyList<DependencyHealthReport> GetDependencyHealth();
}
