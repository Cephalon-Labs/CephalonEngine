namespace Cephalon.Abstractions.Patterns;

/// <summary>
/// Allows a module to contribute strangler-fig routes into the active runtime.
/// </summary>
public interface IStranglerFigRouteContributor
{
    /// <summary>
    /// Registers one or more strangler-fig route descriptors with the supplied registry.
    /// </summary>
    /// <param name="routes">The registry that collects contributed route descriptors.</param>
    void RegisterRoutes(IStranglerFigRouteRegistry routes);
}
