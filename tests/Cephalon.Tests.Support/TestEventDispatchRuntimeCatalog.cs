using Cephalon.Abstractions.Data;

namespace Cephalon.Tests.Support;

public sealed class TestEventDispatchRuntimeCatalog : IEventDispatchRuntimeCatalog
{
    private readonly IReadOnlyList<EventDispatchRuntimeState> states;
    private readonly Dictionary<string, EventDispatchRuntimeState> statesByOutboxId;

    public TestEventDispatchRuntimeCatalog(params EventDispatchRuntimeState[] states)
    {
        ArgumentNullException.ThrowIfNull(states);

        this.states = states
            .OrderBy(static state => state.OutboxId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        statesByOutboxId = this.states.ToDictionary(static state => state.OutboxId, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<EventDispatchRuntimeState> States => states;

    public EventDispatchRuntimeState? GetByOutboxId(string outboxId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outboxId);

        return statesByOutboxId.TryGetValue(outboxId.Trim(), out var state)
            ? state
            : null;
    }

    public bool TryGet(string outboxId, out EventDispatchRuntimeState? state)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outboxId);

        return statesByOutboxId.TryGetValue(outboxId.Trim(), out state);
    }
}
