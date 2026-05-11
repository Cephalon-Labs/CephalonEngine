namespace Cephalon.Eventing.Services;

/// <summary>
/// Provides the merged event upcaster descriptors visible to the active eventing runtime.
/// </summary>
public interface IEventUpcasterCatalog
{
    /// <summary>
    /// Gets all registered event upcaster descriptors.
    /// </summary>
    IReadOnlyList<EventUpcasterDescriptor> Upcasters { get; }

    /// <summary>
    /// Gets all upcaster descriptors registered for the supplied event type.
    /// </summary>
    /// <param name="eventType">The logical event type identifier.</param>
    /// <returns>The matching upcaster descriptors ordered by source version, target version, and identifier.</returns>
    IReadOnlyList<EventUpcasterDescriptor> GetByEventType(string eventType);

    /// <summary>
    /// Gets all upcaster descriptors registered for the supplied event type and source version.
    /// </summary>
    /// <param name="eventType">The logical event type identifier.</param>
    /// <param name="fromVersion">The source event contract version.</param>
    /// <returns>The matching upcaster descriptors ordered by target version and identifier.</returns>
    IReadOnlyList<EventUpcasterDescriptor> GetBySourceVersion(string eventType, string fromVersion);

    /// <summary>
    /// Attempts to resolve an upcaster by its stable identifier.
    /// </summary>
    /// <param name="upcasterId">The stable upcaster identifier.</param>
    /// <param name="upcaster">When found, the matching upcaster descriptor.</param>
    /// <returns><see langword="true" /> when an upcaster with the identifier exists.</returns>
    bool TryGet(string upcasterId, out EventUpcasterDescriptor upcaster);

    /// <summary>
    /// Attempts to resolve an upcaster for a specific event type version transition.
    /// </summary>
    /// <param name="eventType">The logical event type identifier.</param>
    /// <param name="fromVersion">The source event contract version.</param>
    /// <param name="toVersion">The target event contract version.</param>
    /// <param name="upcaster">When found, the matching upcaster descriptor.</param>
    /// <returns><see langword="true" /> when an upcaster with the event type and version transition exists.</returns>
    bool TryGetTransition(string eventType, string fromVersion, string toVersion, out EventUpcasterDescriptor upcaster);
}
