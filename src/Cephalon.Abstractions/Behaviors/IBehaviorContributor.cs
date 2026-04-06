namespace Cephalon.Abstractions.Behaviors;

/// <summary>
/// Contributes behavior topology descriptors to the active runtime's catalog.
/// Implementations are collected via dependency injection enumeration.
/// </summary>
public interface IBehaviorContributor
{
    /// <summary>
    /// Returns the behavior topology descriptors contributed by this instance.
    /// </summary>
    /// <returns>The contributed descriptors.</returns>
    IReadOnlyList<BehaviorTopologyDescriptor> Contribute();
}
