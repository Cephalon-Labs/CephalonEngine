namespace Cephalon.Abstractions.Patterns;

/// <summary>
/// Exposes the strangler-fig routes visible to the current runtime.
/// </summary>
public interface IStranglerFigRuntimeCatalog
{
    /// <summary>
    /// Gets all strangler-fig routes visible to the current runtime.
    /// </summary>
    IReadOnlyList<StranglerFigRouteDescriptor> Routes { get; }

    /// <summary>
    /// Gets one strangler-fig route by its stable identifier.
    /// </summary>
    /// <param name="routeId">The route identifier to resolve.</param>
    /// <returns>The matching route descriptor, or <see langword="null" /> when it is not active.</returns>
    StranglerFigRouteDescriptor? GetById(string routeId);

    /// <summary>
    /// Gets all strangler-fig routes owned by the requested module.
    /// </summary>
    /// <param name="sourceModuleId">The module identifier to filter by.</param>
    /// <returns>The matching route descriptors, or an empty list when none are active.</returns>
    IReadOnlyList<StranglerFigRouteDescriptor> GetBySourceModule(string sourceModuleId);
}
