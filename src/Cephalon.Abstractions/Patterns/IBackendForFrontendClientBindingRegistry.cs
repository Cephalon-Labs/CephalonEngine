namespace Cephalon.Abstractions.Patterns;

/// <summary>
/// Collects backend-for-frontend client binding descriptors contributed to the active runtime.
/// </summary>
public interface IBackendForFrontendClientBindingRegistry
{
    /// <summary>
    /// Adds a backend-for-frontend client binding descriptor to the current runtime composition.
    /// </summary>
    /// <param name="binding">The client binding descriptor to register.</param>
    void Add(BackendForFrontendClientBindingDescriptor binding);
}
