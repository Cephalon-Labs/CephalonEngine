using Cephalon.Abstractions.Patterns;

namespace Cephalon.Engine.Patterns;

internal sealed class BackendForFrontendClientBindingRegistryAdapter(
    string moduleId,
    List<BackendForFrontendClientBindingDescriptor> bindings) : IBackendForFrontendClientBindingRegistry
{
    public void Add(BackendForFrontendClientBindingDescriptor binding)
    {
        ArgumentNullException.ThrowIfNull(binding);

        if (!string.Equals(binding.SourceModuleId, moduleId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Backend-for-frontend binding '{binding.Id}' declared source module '{binding.SourceModuleId}', but it was contributed by module '{moduleId}'.");
        }

        bindings.Add(binding);
    }
}
