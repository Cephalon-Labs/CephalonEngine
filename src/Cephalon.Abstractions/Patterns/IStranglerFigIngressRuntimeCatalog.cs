namespace Cephalon.Abstractions.Patterns;

/// <summary>
/// Exposes the normalized strangler-fig ingress materialization answers visible to the current runtime.
/// </summary>
public interface IStranglerFigIngressRuntimeCatalog
{
    /// <summary>
    /// Gets all effective strangler-fig ingress answers visible to the current runtime.
    /// </summary>
    IReadOnlyList<StranglerFigIngressRuntimeDescriptor> Routes { get; }

    /// <summary>
    /// Gets one effective strangler-fig ingress answer by its stable route identifier.
    /// </summary>
    /// <param name="routeId">The route identifier to resolve.</param>
    /// <returns>The matching runtime descriptor, or <see langword="null" /> when it is not active.</returns>
    StranglerFigIngressRuntimeDescriptor? GetById(string routeId);

    /// <summary>
    /// Gets all effective strangler-fig ingress answers owned by the requested module.
    /// </summary>
    /// <param name="sourceModuleId">The module identifier to filter by.</param>
    /// <returns>The matching runtime descriptors, or an empty list when none are active.</returns>
    IReadOnlyList<StranglerFigIngressRuntimeDescriptor> GetBySourceModule(string sourceModuleId);
}
