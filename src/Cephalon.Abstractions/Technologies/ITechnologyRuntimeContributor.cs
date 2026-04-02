namespace Cephalon.Abstractions.Technologies;

/// <summary>
/// Contributes one runtime surface projected by an active technology pack.
/// </summary>
public interface ITechnologyRuntimeContributor
{
    /// <summary>
    /// Describes the runtime surface projected by the contributor.
    /// </summary>
    /// <returns>The runtime surface description.</returns>
    TechnologyRuntimeSurface DescribeRuntimeSurface();
}
