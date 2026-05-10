using Cephalon.Abstractions.Data;
using System.Globalization;

namespace Cephalon.Eventing.Services;

internal sealed class EventDispatchRemediationDispatcher(
    IEnumerable<IEventDispatchStore> dispatchStores,
    IEventDispatchRuntimeCatalog runtimeCatalog,
    IEventDispatchRuntimeReporter runtimeReporter,
    IEventChannelCatalog channels,
    EventDispatchRemediationRuntimeCatalog commandCatalog) : IEventDispatchRemediationDispatcher
{
    private const string CommandSource = "cephalon-event-dispatch-remediation-dispatcher";

    public async ValueTask<EventDispatchRemediationResult> DispatchAsync(
        EventDispatchRemediationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var observedAtUtc = request.RequestedAtUtc == default
            ? DateTimeOffset.UtcNow
            : request.RequestedAtUtc;
        var operationId = NormalizeOperationId(request.OperationId);
        var dispatchOutcome = ResolveDispatchOutcome(operationId);
        var metadata = CreateMetadata(request, operationId, dispatchOutcome, observedAtUtc);
        var attempt = Math.Max(
            1,
            (runtimeCatalog.GetByOutboxId(request.OutboxId)?.LastAttempt ?? 0) + 1);

        var dispatchStore = ResolveDispatchStore(request.OutboxId);
        if (dispatchStore is null)
        {
            return Record(CreateRejectedResult(
                request,
                operationId,
                dispatchOutcome,
                observedAtUtc,
                $"Outbox '{request.OutboxId}' is not owned by the active event dispatch remediation dispatcher.",
                metadata));
        }

        if (!channels.TryGet(request.ChannelId, out _))
        {
            return Record(CreateRejectedResult(
                request,
                operationId,
                dispatchOutcome,
                observedAtUtc,
                $"Event channel '{request.ChannelId}' is not registered in the active eventing runtime.",
                metadata));
        }

        if (string.Equals(operationId, EventDispatchRemediationOperationIds.RetryLater, StringComparison.OrdinalIgnoreCase) &&
            request.NextAttemptAtUtc is null)
        {
            return Record(CreateRejectedResult(
                request,
                operationId,
                dispatchOutcome,
                observedAtUtc,
                "Retry-later event-dispatch remediation requires a next attempt timestamp.",
                metadata));
        }

        var report = new EventDispatchExecutionReport(
            outboxId: request.OutboxId,
            channelId: request.ChannelId,
            outcome: dispatchOutcome,
            observedAtUtc: observedAtUtc,
            messageId: request.MessageId,
            attempt: attempt,
            error: ResolveError(request, operationId),
            metadata: metadata);

        try
        {
            await dispatchStore.ApplyReportAsync(report, cancellationToken).ConfigureAwait(false);
            await runtimeReporter.ReportAsync(report, cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidOperationException exception)
        {
            return Record(CreateRejectedResult(
                request,
                operationId,
                dispatchOutcome,
                observedAtUtc,
                exception.Message,
                metadata));
        }

        return Record(new EventDispatchRemediationResult(
            CommandId: request.CommandId,
            OutboxId: request.OutboxId,
            MessageId: request.MessageId,
            ChannelId: request.ChannelId,
            OperationId: operationId,
            Outcome: EventDispatchRemediationOutcomes.Accepted,
            DispatchOutcome: dispatchOutcome,
            ObservedAtUtc: observedAtUtc,
            Error: null,
            Metadata: metadata));
    }

    private EventDispatchRemediationResult Record(EventDispatchRemediationResult result)
    {
        commandCatalog.Record(result);
        return result;
    }

    private IEventDispatchStore? ResolveDispatchStore(string outboxId)
    {
        var matchingStores = dispatchStores
            .Where(store => store.OutboxIds.Contains(outboxId, StringComparer.OrdinalIgnoreCase))
            .ToArray();

        if (matchingStores.Length == 0)
        {
            return null;
        }

        if (matchingStores.Length > 1)
        {
            throw new InvalidOperationException(
                $"Outbox '{outboxId}' is owned by multiple event dispatch stores; select one dispatch-store owner before accepting remediation commands.");
        }

        return matchingStores[0];
    }

    private static EventDispatchRemediationResult CreateRejectedResult(
        EventDispatchRemediationRequest request,
        string operationId,
        string dispatchOutcome,
        DateTimeOffset observedAtUtc,
        string error,
        IReadOnlyDictionary<string, string> metadata)
    {
        return new EventDispatchRemediationResult(
            CommandId: request.CommandId,
            OutboxId: request.OutboxId,
            MessageId: request.MessageId,
            ChannelId: request.ChannelId,
            OperationId: operationId,
            Outcome: EventDispatchRemediationOutcomes.Rejected,
            DispatchOutcome: dispatchOutcome,
            ObservedAtUtc: observedAtUtc,
            Error: error,
            Metadata: metadata);
    }

    private static Dictionary<string, string> CreateMetadata(
        EventDispatchRemediationRequest request,
        string operationId,
        string dispatchOutcome,
        DateTimeOffset observedAtUtc)
    {
        var metadata = new Dictionary<string, string>(request.Metadata, StringComparer.OrdinalIgnoreCase)
        {
            ["operatorCommandId"] = request.CommandId,
            ["operatorCommand"] = operationId,
            ["operatorCommandSource"] = CommandSource,
            ["operatorCommandOutcome"] = dispatchOutcome,
            ["operatorCommandRequestedAtUtc"] = observedAtUtc.ToString("O", CultureInfo.InvariantCulture)
        };

        if (!string.IsNullOrWhiteSpace(request.Reason))
        {
            metadata["operatorCommandReason"] = request.Reason;
        }

        if (!string.IsNullOrWhiteSpace(request.ActorId))
        {
            metadata["operatorActorId"] = request.ActorId;
        }

        if (!string.IsNullOrWhiteSpace(request.CorrelationId))
        {
            metadata["operatorCorrelationId"] = request.CorrelationId;
        }

        switch (operationId)
        {
            case EventDispatchRemediationOperationIds.RetryNow:
                metadata[EventDispatchRuntimeMetadataKeys.NextRetryAtUtc] = observedAtUtc.ToString("O", CultureInfo.InvariantCulture);
                metadata[EventDispatchRuntimeMetadataKeys.RetryOutcome] = "operator-retry-now";
                metadata[EventDispatchRuntimeMetadataKeys.RetryScope] = "operator-command";
                metadata[EventDispatchRuntimeMetadataKeys.RetryDurability] = "dispatch-store";
                break;

            case EventDispatchRemediationOperationIds.RetryLater:
                if (request.NextAttemptAtUtc is { } nextAttemptAtUtc)
                {
                    metadata[EventDispatchRuntimeMetadataKeys.NextRetryAtUtc] = nextAttemptAtUtc.ToString("O", CultureInfo.InvariantCulture);
                }

                metadata[EventDispatchRuntimeMetadataKeys.RetryOutcome] = "operator-retry-later";
                metadata[EventDispatchRuntimeMetadataKeys.RetryScope] = "operator-command";
                metadata[EventDispatchRuntimeMetadataKeys.RetryDurability] = "dispatch-store";
                break;

            case EventDispatchRemediationOperationIds.Quarantine:
                metadata[EventDispatchRuntimeMetadataKeys.RetryOutcome] = "operator-quarantine";
                metadata[EventDispatchRuntimeMetadataKeys.RetryExhausted] = "true";
                metadata[EventDispatchRuntimeMetadataKeys.TerminalFailure] = "true";
                break;

            case EventDispatchRemediationOperationIds.DeadLetter:
                metadata[EventDispatchRuntimeMetadataKeys.RetryOutcome] = "operator-dead-letter";
                metadata[EventDispatchRuntimeMetadataKeys.RetryExhausted] = "true";
                metadata[EventDispatchRuntimeMetadataKeys.TerminalFailure] = "true";
                metadata[EventDispatchRuntimeMetadataKeys.DeadLetterOutcome] = "operator-dispatch-store-dead-letter";
                metadata[EventDispatchRuntimeMetadataKeys.DeadLetterScope] = "dispatch-store";
                metadata[EventDispatchRuntimeMetadataKeys.DeadLetterDurability] = "dispatch-store";
                metadata[EventDispatchRuntimeMetadataKeys.BrokerDeadLetter] = "false";
                break;

            case EventDispatchRemediationOperationIds.Skip:
                metadata["skipOutcome"] = "operator-skip";
                break;
        }

        return metadata;
    }

    private static string ResolveError(EventDispatchRemediationRequest request, string operationId)
    {
        if (!string.IsNullOrWhiteSpace(request.Reason))
        {
            return request.Reason;
        }

        return operationId switch
        {
            EventDispatchRemediationOperationIds.RetryNow => "Operator requested immediate event-dispatch retry.",
            EventDispatchRemediationOperationIds.RetryLater => "Operator requested delayed event-dispatch retry.",
            EventDispatchRemediationOperationIds.Quarantine => "Operator quarantined event dispatch as terminal failure.",
            EventDispatchRemediationOperationIds.DeadLetter => "Operator marked event dispatch as dispatch-store dead-letter intent.",
            EventDispatchRemediationOperationIds.Skip => "Operator skipped event dispatch.",
            _ => "Operator requested event-dispatch remediation."
        };
    }

    private static string ResolveDispatchOutcome(string operationId) =>
        operationId switch
        {
            EventDispatchRemediationOperationIds.RetryNow => EventDispatchExecutionOutcomes.RetryScheduled,
            EventDispatchRemediationOperationIds.RetryLater => EventDispatchExecutionOutcomes.RetryScheduled,
            EventDispatchRemediationOperationIds.Skip => EventDispatchExecutionOutcomes.Skipped,
            EventDispatchRemediationOperationIds.Quarantine => EventDispatchExecutionOutcomes.Failed,
            EventDispatchRemediationOperationIds.DeadLetter => EventDispatchExecutionOutcomes.Failed,
            _ => throw new InvalidOperationException($"Event-dispatch remediation operation '{operationId}' is not supported.")
        };

    private static string NormalizeOperationId(string operationId)
    {
        var normalized = operationId.Trim().ToLowerInvariant();
        return normalized switch
        {
            EventDispatchRemediationOperationIds.RetryNow => EventDispatchRemediationOperationIds.RetryNow,
            EventDispatchRemediationOperationIds.RetryLater => EventDispatchRemediationOperationIds.RetryLater,
            EventDispatchRemediationOperationIds.Skip => EventDispatchRemediationOperationIds.Skip,
            EventDispatchRemediationOperationIds.Quarantine => EventDispatchRemediationOperationIds.Quarantine,
            EventDispatchRemediationOperationIds.DeadLetter => EventDispatchRemediationOperationIds.DeadLetter,
            _ => throw new ArgumentException(
                $"Event-dispatch remediation operation '{operationId}' is not supported.",
                nameof(operationId))
        };
    }
}
