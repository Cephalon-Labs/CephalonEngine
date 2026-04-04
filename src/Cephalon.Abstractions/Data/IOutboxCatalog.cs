namespace Cephalon.Abstractions.Data;

/// <summary>
/// Exposes the outbox surfaces visible to the current runtime.
/// </summary>
public interface IOutboxCatalog
{
    /// <summary>
    /// Gets all outbox surfaces visible to the current runtime.
    /// </summary>
    IReadOnlyList<OutboxDescriptor> Outboxes { get; }

    /// <summary>
    /// Gets one outbox by its stable identifier.
    /// </summary>
    /// <param name="outboxId">The outbox identifier to resolve.</param>
    /// <returns>The matching outbox, or <see langword="null" /> when it is not active.</returns>
    OutboxDescriptor? GetById(string outboxId);

    /// <summary>
    /// Gets all outboxes contributed by the requested module.
    /// </summary>
    /// <param name="sourceModuleId">The source module identifier to filter by.</param>
    /// <returns>The matching outboxes, or an empty list when the module contributed none.</returns>
    IReadOnlyList<OutboxDescriptor> GetBySourceModule(string sourceModuleId);

    /// <summary>
    /// Gets all outboxes backed by the requested provider identifier.
    /// </summary>
    /// <param name="provider">The provider identifier to filter by.</param>
    /// <returns>The matching outboxes, or an empty list when the provider contributes none.</returns>
    IReadOnlyList<OutboxDescriptor> GetByProvider(string provider);

    /// <summary>
    /// Gets all outboxes that explicitly declare the requested channel identifier.
    /// </summary>
    /// <param name="channelId">The channel identifier to filter by.</param>
    /// <returns>The matching outboxes, or an empty list when no outbox declares that channel.</returns>
    IReadOnlyList<OutboxDescriptor> GetByChannelId(string channelId);
}
