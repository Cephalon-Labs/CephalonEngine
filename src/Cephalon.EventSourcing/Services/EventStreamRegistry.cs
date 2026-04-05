using Cephalon.Abstractions.EventSourcing;

namespace Cephalon.EventSourcing.Services;

/// <summary>
/// Collects event-stream descriptors registered by the active host and companion packs.
/// </summary>
public sealed class EventStreamRegistry : IEventStoreRegistry, IEventStoreContributor
{
    private readonly List<EventStreamDescriptor> descriptors = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="EventStreamRegistry" /> class.
    /// </summary>
    public EventStreamRegistry()
    {
    }

    /// <summary>
    /// Registers one event-stream descriptor with the registry.
    /// </summary>
    /// <param name="descriptor">The descriptor to register.</param>
    public void Register(EventStreamDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        descriptors.Add(descriptor);
    }

    /// <summary>
    /// Returns the descriptors that have been registered with the current registry instance.
    /// </summary>
    /// <returns>The registered descriptors.</returns>
    public IReadOnlyList<EventStreamDescriptor> Contribute()
    {
        return descriptors.ToArray();
    }
}
