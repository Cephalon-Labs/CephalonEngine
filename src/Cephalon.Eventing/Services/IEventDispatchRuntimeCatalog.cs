namespace Cephalon.Eventing.Services;

/// <summary>
/// Exposes the operator-facing dispatch runtime state currently reported for durable event publication paths.
/// </summary>
public interface IEventDispatchRuntimeCatalog
{
    /// <summary>
    /// Gets the reported dispatch state entries visible to the current runtime.
    /// </summary>
    IReadOnlyList<EventDispatchRuntimeState> States { get; }

    /// <summary>
    /// Gets the latest reported dispatch state for one outbox-backed publication path.
    /// </summary>
    /// <param name="outboxId">The stable outbox identifier to resolve.</param>
    /// <returns>The latest reported state, or <see langword="null" /> when that path has not reported runtime state.</returns>
    EventDispatchRuntimeState? GetByOutboxId(string outboxId);

    /// <summary>
    /// Tries to get the latest reported dispatch state for one outbox-backed publication path.
    /// </summary>
    /// <param name="outboxId">The stable outbox identifier to resolve.</param>
    /// <param name="state">Receives the latest reported state when the path has reported one.</param>
    /// <returns><see langword="true" /> when a reported state exists; otherwise, <see langword="false" />.</returns>
    bool TryGet(string outboxId, out EventDispatchRuntimeState? state);
}
