namespace Cephalon.Eventing.Services;

/// <summary>
/// Exposes the merged set of event channels available to the active eventing runtime.
/// </summary>
public interface IEventChannelCatalog
{
    /// <summary>
    /// Gets the effective channel set after host options and module contributors have both been applied.
    /// </summary>
    IReadOnlyList<EventChannelDescriptor> Channels { get; }

    /// <summary>
    /// Attempts to resolve an event channel descriptor by identifier.
    /// </summary>
    /// <param name="channelId">The channel identifier to resolve.</param>
    /// <param name="channel">When this method returns, contains the resolved channel if found.</param>
    /// <returns><see langword="true" /> when the channel exists; otherwise <see langword="false" />.</returns>
    bool TryGet(string channelId, out EventChannelDescriptor channel);
}
