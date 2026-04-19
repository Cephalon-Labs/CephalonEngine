using Cephalon.Abstractions.Patterns;

namespace Cephalon.Engine.Patterns;

internal sealed class BackendForFrontendRuntimeCatalogSnapshot : IBackendForFrontendRuntimeCatalog
{
    private readonly BackendForFrontendClientBindingDescriptor[] bindings;
    private readonly Dictionary<string, BackendForFrontendClientBindingDescriptor> bindingsById;
    private readonly Dictionary<string, IReadOnlyList<BackendForFrontendClientBindingDescriptor>> bindingsByClientId;
    private readonly Dictionary<string, IReadOnlyList<BackendForFrontendClientBindingDescriptor>> bindingsBySourceModule;
    private readonly Dictionary<string, IReadOnlyList<BackendForFrontendClientBindingDescriptor>> bindingsByTransportId;

    public BackendForFrontendRuntimeCatalogSnapshot(IEnumerable<BackendForFrontendClientBindingDescriptor> bindings)
    {
        ArgumentNullException.ThrowIfNull(bindings);

        this.bindings = bindings
            .OrderBy(static binding => binding.ClientId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static binding => binding.TransportId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static binding => binding.SourceModuleId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static binding => binding.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        ValidateDuplicateIds(this.bindings);

        bindingsById = this.bindings.ToDictionary(static binding => binding.Id, StringComparer.OrdinalIgnoreCase);
        bindingsByClientId = CreateIndex(this.bindings, static binding => binding.ClientId);
        bindingsBySourceModule = CreateIndex(this.bindings, static binding => binding.SourceModuleId);
        bindingsByTransportId = CreateIndex(this.bindings, static binding => binding.TransportId);
    }

    public IReadOnlyList<BackendForFrontendClientBindingDescriptor> Bindings => bindings;

    public BackendForFrontendClientBindingDescriptor? GetById(string bindingId)
    {
        if (string.IsNullOrWhiteSpace(bindingId))
        {
            return null;
        }

        return bindingsById.TryGetValue(bindingId.Trim(), out var binding)
            ? binding
            : null;
    }

    public IReadOnlyList<BackendForFrontendClientBindingDescriptor> GetByClientId(string clientId)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return [];
        }

        return bindingsByClientId.TryGetValue(clientId.Trim(), out var matches)
            ? matches
            : [];
    }

    public IReadOnlyList<BackendForFrontendClientBindingDescriptor> GetBySourceModule(string sourceModuleId)
    {
        if (string.IsNullOrWhiteSpace(sourceModuleId))
        {
            return [];
        }

        return bindingsBySourceModule.TryGetValue(sourceModuleId.Trim(), out var matches)
            ? matches
            : [];
    }

    public IReadOnlyList<BackendForFrontendClientBindingDescriptor> GetByTransportId(string transportId)
    {
        if (string.IsNullOrWhiteSpace(transportId))
        {
            return [];
        }

        return bindingsByTransportId.TryGetValue(transportId.Trim(), out var matches)
            ? matches
            : [];
    }

    private static Dictionary<string, IReadOnlyList<BackendForFrontendClientBindingDescriptor>> CreateIndex(
        IReadOnlyList<BackendForFrontendClientBindingDescriptor> bindings,
        Func<BackendForFrontendClientBindingDescriptor, string> keySelector)
    {
        return bindings
            .GroupBy(keySelector, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<BackendForFrontendClientBindingDescriptor>)group.ToArray(),
                StringComparer.OrdinalIgnoreCase);
    }

    private static void ValidateDuplicateIds(IReadOnlyList<BackendForFrontendClientBindingDescriptor> bindings)
    {
        var duplicateId = bindings
            .GroupBy(static binding => binding.Id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(static group => group.Count() > 1);
        if (duplicateId is null)
        {
            return;
        }

        var owners = duplicateId
            .Select(static binding => binding.SourceModuleId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase);

        throw new InvalidOperationException(
            $"Backend-for-frontend binding '{duplicateId.Key}' is registered multiple times by: {string.Join(", ", owners)}.");
    }
}
