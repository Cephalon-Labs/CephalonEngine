namespace Cephalon.Abstractions.Patterns;

/// <summary>
/// Collects strangler-fig route descriptors contributed to the active runtime.
/// </summary>
public interface IStranglerFigRouteRegistry
{
    /// <summary>
    /// Adds a strangler-fig route descriptor to the current runtime composition.
    /// </summary>
    /// <param name="route">The route descriptor to register.</param>
    void Add(StranglerFigRouteDescriptor route);
}
