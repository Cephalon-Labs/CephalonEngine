namespace Cephalon.Eventing.Services;

/// <summary>
/// Collects event channel descriptors contributed to the active eventing runtime pack.
/// </summary>
public interface IEventChannelRegistry
{
    /// <summary>
    /// Adds an event channel descriptor to the registry.
    /// </summary>
    /// <param name="channel">The channel descriptor to contribute.</param>
    void Add(EventChannelDescriptor channel);
}
