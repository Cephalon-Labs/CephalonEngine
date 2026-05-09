using Cephalon.Abstractions.Data;
using Cephalon.Eventing.Configuration;
using System.Collections.ObjectModel;

namespace Cephalon.Eventing.Services;

internal sealed class EventDispatchRemediationRuntimeCatalog(
    EventingOptions options) : IEventDispatchRemediationRuntimeCatalog
{
    private static readonly IReadOnlyDictionary<string, string> EmptyMetadata =
        new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

    private readonly Lock gate = new();
    private readonly Dictionary<string, EventDispatchRemediationRuntimeState> statesByCommandId = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<EventDispatchRemediationRuntimeState> states = [];

    public IReadOnlyList<EventDispatchRemediationRuntimeState> States
    {
        get
        {
            lock (gate)
            {
                return states
                    .OrderByDescending(static state => state.ObservedAtUtc)
                    .ThenBy(static state => state.CommandId, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
        }
    }

    public EventDispatchRemediationRuntimeState? GetByCommandId(string commandId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandId);

        lock (gate)
        {
            return statesByCommandId.GetValueOrDefault(commandId.Trim());
        }
    }

    public IReadOnlyList<EventDispatchRemediationRuntimeState> GetByOutboxId(string outboxId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outboxId);
        var normalizedOutboxId = outboxId.Trim();

        lock (gate)
        {
            return states
                .Where(state => string.Equals(state.OutboxId, normalizedOutboxId, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(static state => state.ObservedAtUtc)
                .ThenBy(static state => state.CommandId, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }

    public IReadOnlyList<EventDispatchRemediationRuntimeState> GetByOutcome(string outcome)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outcome);
        var normalizedOutcome = outcome.Trim();

        lock (gate)
        {
            return states
                .Where(state => string.Equals(state.Outcome, normalizedOutcome, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(static state => state.ObservedAtUtc)
                .ThenBy(static state => state.CommandId, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }

    public void Record(EventDispatchRemediationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        var limit = options.RemediationCommandHistoryLimit;
        if (limit == 0)
        {
            return;
        }

        var metadata = result.Metadata.Count == 0
            ? EmptyMetadata
            : new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(result.Metadata, StringComparer.OrdinalIgnoreCase));
        var state = new EventDispatchRemediationRuntimeState(
            CommandId: result.CommandId,
            OutboxId: result.OutboxId,
            MessageId: result.MessageId,
            ChannelId: result.ChannelId,
            OperationId: result.OperationId,
            Outcome: result.Outcome,
            DispatchOutcome: result.DispatchOutcome,
            ObservedAtUtc: result.ObservedAtUtc,
            Error: result.Error,
            Metadata: metadata);

        lock (gate)
        {
            if (statesByCommandId.Remove(state.CommandId))
            {
                states.RemoveAll(existing => string.Equals(existing.CommandId, state.CommandId, StringComparison.OrdinalIgnoreCase));
            }

            states.Add(state);
            statesByCommandId[state.CommandId] = state;

            while (states.Count > limit)
            {
                var oldest = states
                    .OrderBy(static existing => existing.ObservedAtUtc)
                    .ThenBy(static existing => existing.CommandId, StringComparer.OrdinalIgnoreCase)
                    .First();
                states.Remove(oldest);
                statesByCommandId.Remove(oldest.CommandId);
            }
        }
    }
}
