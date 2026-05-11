namespace Cephalon.Eventing.Services;

/// <summary>
/// Provides the merged event contract descriptors visible to the active eventing runtime.
/// </summary>
public interface IEventContractCatalog
{
    /// <summary>
    /// Gets all registered event contract descriptors.
    /// </summary>
    IReadOnlyList<EventContractDescriptor> Contracts { get; }

    /// <summary>
    /// Gets all contract descriptors registered for the supplied event type.
    /// </summary>
    /// <param name="eventType">The logical event type identifier.</param>
    /// <returns>The matching contract descriptors ordered by version.</returns>
    IReadOnlyList<EventContractDescriptor> GetByEventType(string eventType);

    /// <summary>
    /// Attempts to resolve a contract by its stable identifier.
    /// </summary>
    /// <param name="contractId">The stable contract identifier.</param>
    /// <param name="contract">When found, the matching contract descriptor.</param>
    /// <returns><see langword="true" /> when a contract with the identifier exists.</returns>
    bool TryGet(string contractId, out EventContractDescriptor contract);

    /// <summary>
    /// Attempts to resolve a specific contract version for an event type.
    /// </summary>
    /// <param name="eventType">The logical event type identifier.</param>
    /// <param name="version">The event contract version.</param>
    /// <param name="contract">When found, the matching contract descriptor.</param>
    /// <returns><see langword="true" /> when a contract with the event type and version exists.</returns>
    bool TryGetVersion(string eventType, string version, out EventContractDescriptor contract);
}
