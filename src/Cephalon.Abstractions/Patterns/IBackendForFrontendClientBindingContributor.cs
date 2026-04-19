namespace Cephalon.Abstractions.Patterns;

/// <summary>
/// Allows a module to contribute backend-for-frontend client bindings into the active runtime.
/// </summary>
public interface IBackendForFrontendClientBindingContributor
{
    /// <summary>
    /// Registers one or more backend-for-frontend client bindings with the supplied registry.
    /// </summary>
    /// <param name="bindings">The registry that collects contributed client-binding descriptors.</param>
    void RegisterClientBindings(IBackendForFrontendClientBindingRegistry bindings);
}
