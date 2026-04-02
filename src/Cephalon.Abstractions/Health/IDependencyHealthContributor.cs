namespace Cephalon.Abstractions.Health;

/// <summary>
/// Contributes dependency-health information to the runtime.
/// </summary>
public interface IDependencyHealthContributor
{
    /// <summary>
    /// Returns the dependency-health reports currently known to the contributor.
    /// </summary>
    /// <returns>The contributed dependency-health reports.</returns>
    IReadOnlyList<DependencyHealthReport> GetDependencyHealth();
}
