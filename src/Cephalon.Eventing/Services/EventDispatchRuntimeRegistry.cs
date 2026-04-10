using Cephalon.Abstractions.Data;

namespace Cephalon.Eventing.Services;

internal sealed class EventDispatchRuntimeRegistry : IEventDispatchRuntimeRegistry
{
    private readonly List<EventDispatchRuntimeDescriptor> runtimes = [];

    public void Add(EventDispatchRuntimeDescriptor dispatchRuntime)
    {
        ArgumentNullException.ThrowIfNull(dispatchRuntime);

        runtimes.Add(dispatchRuntime);
    }

    public IReadOnlyList<EventDispatchRuntimeDescriptor> Build()
    {
        return runtimes
            .GroupBy(static runtime => runtime.Id, StringComparer.OrdinalIgnoreCase)
            .Select(static group => group.First())
            .OrderBy(static runtime => runtime.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
