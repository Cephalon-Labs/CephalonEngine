namespace Cephalon.Eventing.Services;

/// <summary>
/// Collects event upcaster descriptors contributed by a host or module.
/// </summary>
public interface IEventUpcasterRegistry
{
    /// <summary>
    /// Adds one event upcaster descriptor to the active eventing catalog.
    /// </summary>
    /// <param name="upcaster">The upcaster descriptor to add.</param>
    void Add(EventUpcasterDescriptor upcaster);
}
