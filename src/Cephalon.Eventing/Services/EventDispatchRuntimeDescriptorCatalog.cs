using Cephalon.Abstractions.Data;

namespace Cephalon.Eventing.Services;

internal sealed class EventDispatchRuntimeDescriptorCatalog : IEventDispatchRuntimeDescriptorCatalog
{
    private readonly Dictionary<string, EventDispatchRuntimeDescriptor> index;

    public EventDispatchRuntimeDescriptorCatalog(IEnumerable<IEventDispatchRuntimeContributor> contributors)
    {
        ArgumentNullException.ThrowIfNull(contributors);

        var registry = new EventDispatchRuntimeRegistry();
        foreach (var contributor in contributors)
        {
            contributor.RegisterDispatchRuntimes(registry);
        }

        Runtimes = registry.Build();
        index = Runtimes.ToDictionary(static runtime => runtime.Id, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<EventDispatchRuntimeDescriptor> Runtimes { get; }

    public EventDispatchRuntimeDescriptor? GetById(string runtimeId)
    {
        if (string.IsNullOrWhiteSpace(runtimeId))
        {
            return null;
        }

        return index.TryGetValue(runtimeId.Trim(), out var runtime)
            ? runtime
            : null;
    }
}
