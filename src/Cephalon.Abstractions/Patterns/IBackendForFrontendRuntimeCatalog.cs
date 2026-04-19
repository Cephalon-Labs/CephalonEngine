namespace Cephalon.Abstractions.Patterns;

/// <summary>
/// Exposes the backend-for-frontend client bindings visible to the current runtime.
/// </summary>
public interface IBackendForFrontendRuntimeCatalog
{
    /// <summary>
    /// Gets all backend-for-frontend client bindings visible to the current runtime.
    /// </summary>
    IReadOnlyList<BackendForFrontendClientBindingDescriptor> Bindings { get; }

    /// <summary>
    /// Gets one backend-for-frontend client binding by its stable identifier.
    /// </summary>
    /// <param name="bindingId">The binding identifier to resolve.</param>
    /// <returns>The matching client binding descriptor, or <see langword="null" /> when it is not active.</returns>
    BackendForFrontendClientBindingDescriptor? GetById(string bindingId);

    /// <summary>
    /// Gets all backend-for-frontend client bindings owned by the requested client identifier.
    /// </summary>
    /// <param name="clientId">The client identifier to filter by.</param>
    /// <returns>The matching client binding descriptors, or an empty list when none are active.</returns>
    IReadOnlyList<BackendForFrontendClientBindingDescriptor> GetByClientId(string clientId);

    /// <summary>
    /// Gets all backend-for-frontend client bindings owned by the requested module.
    /// </summary>
    /// <param name="sourceModuleId">The module identifier to filter by.</param>
    /// <returns>The matching client binding descriptors, or an empty list when none are active.</returns>
    IReadOnlyList<BackendForFrontendClientBindingDescriptor> GetBySourceModule(string sourceModuleId);

    /// <summary>
    /// Gets all backend-for-frontend client bindings that target the requested transport.
    /// </summary>
    /// <param name="transportId">The transport identifier to filter by.</param>
    /// <returns>The matching client binding descriptors, or an empty list when none are active.</returns>
    IReadOnlyList<BackendForFrontendClientBindingDescriptor> GetByTransportId(string transportId);
}
