namespace Cephalon.Abstractions.Transports;

/// <summary>
/// Collects resolved public REST endpoints while the active host materializes transport routes.
/// </summary>
/// <remarks>
/// Host adapters and transport helpers use this registry to publish the resolved Cephalon-owned
/// REST endpoint surface into <see cref="IRestEndpointRuntimeCatalog"/>. Consumer code should
/// normally read the catalog instead of mutating the registry directly.
/// </remarks>
public interface IRestEndpointRuntimeRegistry
{
    /// <summary>
    /// Clears any previously registered runtime endpoints before a host rematerializes its REST surface.
    /// </summary>
    void Clear();

    /// <summary>
    /// Registers one resolved public REST endpoint with the runtime catalog.
    /// </summary>
    /// <param name="endpoint">The resolved endpoint descriptor to register.</param>
    void Register(RestEndpointRuntimeDescriptor endpoint);
}
