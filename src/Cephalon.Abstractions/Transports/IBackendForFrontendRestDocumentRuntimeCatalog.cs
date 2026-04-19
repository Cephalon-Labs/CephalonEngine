namespace Cephalon.Abstractions.Transports;

/// <summary>
/// Exposes the client-aware REST documentation surfaces derived from the active backend-for-frontend
/// bindings and published REST endpoint catalog.
/// </summary>
/// <remarks>
/// This runtime surface keeps filtered OpenAPI JSON and Scalar materialization aligned with the
/// existing backend-for-frontend binding and REST endpoint runtime catalogs instead of introducing
/// a host-only documentation registry.
/// </remarks>
public interface IBackendForFrontendRestDocumentRuntimeCatalog
{
    /// <summary>
    /// Gets all client-aware REST documentation surfaces visible to the current runtime.
    /// </summary>
    IReadOnlyList<BackendForFrontendRestDocumentRuntimeDescriptor> Documents { get; }

    /// <summary>
    /// Gets one client-aware REST documentation surface by its stable identifier.
    /// </summary>
    /// <param name="documentId">The documentation-surface identifier to resolve.</param>
    /// <returns>
    /// The matching runtime descriptor, or <see langword="null" /> when it is not active.
    /// </returns>
    BackendForFrontendRestDocumentRuntimeDescriptor? GetById(string documentId);

    /// <summary>
    /// Gets all binding-scoped REST documentation surfaces owned by the requested binding.
    /// </summary>
    /// <param name="bindingId">The backend-for-frontend binding identifier to filter by.</param>
    /// <returns>The matching runtime descriptors, or an empty list when the binding is not active.</returns>
    IReadOnlyList<BackendForFrontendRestDocumentRuntimeDescriptor> GetByBindingId(string bindingId);

    /// <summary>
    /// Gets all client-scoped REST documentation surfaces owned by the requested client.
    /// </summary>
    /// <param name="clientId">The client identifier to filter by.</param>
    /// <returns>The matching runtime descriptors, or an empty list when the client is not active.</returns>
    IReadOnlyList<BackendForFrontendRestDocumentRuntimeDescriptor> GetByClientId(string clientId);
}
