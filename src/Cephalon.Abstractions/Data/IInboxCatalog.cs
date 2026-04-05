namespace Cephalon.Abstractions.Data;

/// <summary>
/// Exposes the inbox surfaces visible to the current runtime.
/// </summary>
public interface IInboxCatalog
{
    /// <summary>
    /// Gets all inbox surfaces visible to the current runtime.
    /// </summary>
    IReadOnlyList<InboxDescriptor> Inboxes { get; }

    /// <summary>
    /// Gets one inbox by its stable identifier.
    /// </summary>
    /// <param name="inboxId">The inbox identifier to resolve.</param>
    /// <returns>The matching inbox, or <see langword="null" /> when it is not active.</returns>
    InboxDescriptor? GetById(string inboxId);

    /// <summary>
    /// Gets all inboxes contributed by the requested module.
    /// </summary>
    /// <param name="sourceModuleId">The source module identifier to filter by.</param>
    /// <returns>The matching inboxes, or an empty list when the module contributed none.</returns>
    IReadOnlyList<InboxDescriptor> GetBySourceModule(string sourceModuleId);

    /// <summary>
    /// Gets all inboxes backed by the requested provider identifier.
    /// </summary>
    /// <param name="provider">The provider identifier to filter by.</param>
    /// <returns>The matching inboxes, or an empty list when the provider contributes none.</returns>
    IReadOnlyList<InboxDescriptor> GetByProvider(string provider);

    /// <summary>
    /// Gets all inboxes that explicitly declare the requested channel identifier.
    /// </summary>
    /// <param name="channelId">The channel identifier to filter by.</param>
    /// <returns>The matching inboxes, or an empty list when no inbox declares that channel.</returns>
    IReadOnlyList<InboxDescriptor> GetByChannelId(string channelId);
}
