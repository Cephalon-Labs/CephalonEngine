namespace Cephalon.Abstractions.Transports;

/// <summary>
/// Exposes the client-aware published REST endpoint projections derived from the active
/// backend-for-frontend bindings.
/// </summary>
/// <remarks>
/// This surface keeps backend-for-frontend client bindings and published REST endpoint material
/// separate from the broader binding catalog so hosts can answer which REST endpoints are visible
/// to each client binding without inventing a host-only registry outside the shared runtime truth.
/// </remarks>
public interface IBackendForFrontendRestRuntimeCatalog
{
    /// <summary>
    /// Gets all client-aware published REST endpoint projections visible to the current runtime.
    /// </summary>
    IReadOnlyList<BackendForFrontendRestEndpointRuntimeDescriptor> Endpoints { get; }

    /// <summary>
    /// Gets one client-aware published REST endpoint projection by its stable identifier.
    /// </summary>
    /// <param name="runtimeEndpointId">The binding-plus-endpoint identifier to resolve.</param>
    /// <returns>
    /// The matching runtime descriptor, or <see langword="null" /> when it is not active.
    /// </returns>
    BackendForFrontendRestEndpointRuntimeDescriptor? GetById(string runtimeEndpointId);

    /// <summary>
    /// Gets all client-aware published REST endpoint projections owned by the requested binding.
    /// </summary>
    /// <param name="bindingId">The backend-for-frontend binding identifier to filter by.</param>
    /// <returns>The matching runtime descriptors, or an empty list when the binding is not active.</returns>
    IReadOnlyList<BackendForFrontendRestEndpointRuntimeDescriptor> GetByBindingId(string bindingId);

    /// <summary>
    /// Gets all client-aware published REST endpoint projections owned by the requested client.
    /// </summary>
    /// <param name="clientId">The client identifier to filter by.</param>
    /// <returns>The matching runtime descriptors, or an empty list when the client is not active.</returns>
    IReadOnlyList<BackendForFrontendRestEndpointRuntimeDescriptor> GetByClientId(string clientId);

    /// <summary>
    /// Gets all client-aware published REST endpoint projections contributed by the requested
    /// binding-owner module.
    /// </summary>
    /// <param name="sourceModuleId">The binding-owner module identifier to filter by.</param>
    /// <returns>The matching runtime descriptors, or an empty list when the module is not active.</returns>
    IReadOnlyList<BackendForFrontendRestEndpointRuntimeDescriptor> GetBySourceModule(string sourceModuleId);

    /// <summary>
    /// Gets all client-aware runtime projections that expose the requested published REST endpoint.
    /// </summary>
    /// <param name="restEndpointId">The published REST endpoint identifier to filter by.</param>
    /// <returns>The matching runtime descriptors, or an empty list when the endpoint is not visible.</returns>
    IReadOnlyList<BackendForFrontendRestEndpointRuntimeDescriptor> GetByRestEndpointId(string restEndpointId);
}
