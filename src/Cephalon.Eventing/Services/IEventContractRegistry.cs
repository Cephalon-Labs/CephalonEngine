namespace Cephalon.Eventing.Services;

/// <summary>
/// Collects event contract descriptors contributed by a host or module.
/// </summary>
public interface IEventContractRegistry
{
    /// <summary>
    /// Adds one event contract descriptor to the active eventing catalog.
    /// </summary>
    /// <param name="contract">The contract descriptor to add.</param>
    void Add(EventContractDescriptor contract);
}
