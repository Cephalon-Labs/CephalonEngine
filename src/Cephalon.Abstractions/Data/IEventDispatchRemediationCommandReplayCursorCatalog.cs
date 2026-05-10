namespace Cephalon.Abstractions.Data;

/// <summary>
/// Exposes durable replay-cursor reads over an event-dispatch remediation command journal.
/// </summary>
public interface IEventDispatchRemediationCommandReplayCursorCatalog
{
    /// <summary>
    /// Gets the replay cursor for the newest command record currently visible to the journal.
    /// </summary>
    EventDispatchRemediationCommandReplayCursor? LatestReplayCursor { get; }

    /// <summary>
    /// Gets command records after an optional replay cursor in stable oldest-first order.
    /// </summary>
    /// <param name="cursor">The last replayed command cursor, or <see langword="null" /> to start from the oldest retained command.</param>
    /// <param name="maxCount">The maximum number of command records to return.</param>
    /// <returns>The next command records after the cursor.</returns>
    IReadOnlyList<EventDispatchRemediationRuntimeState> GetAfterReplayCursor(
        EventDispatchRemediationCommandReplayCursor? cursor,
        int maxCount);
}
