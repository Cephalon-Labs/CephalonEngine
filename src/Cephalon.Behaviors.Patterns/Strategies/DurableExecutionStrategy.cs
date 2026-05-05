using Cephalon.Abstractions.Behaviors;
using Cephalon.Abstractions.EventSourcing;
using Cephalon.Abstractions.Execution;
using Cephalon.Behaviors.Patterns.Abstractions;
using Cephalon.Behaviors.Patterns.Runtime;

namespace Cephalon.Behaviors.Patterns.Strategies;

/// <summary>
/// Executes replayable durable workflows by rebuilding state from an event-store stream,
/// invoking the workflow step, and appending the emitted domain events with optimistic concurrency.
/// </summary>
public sealed class DurableExecutionStrategy : IBehaviorExecutionStrategy
{
    private readonly Dictionary<Type, DurableExecutionSlot> generatedSlots;
    private readonly IDurableExecutionRuntimeReporter? runtimeReporter;

    /// <summary>
    /// Creates a durable execution strategy without pre-registered durable execution slots.
    /// </summary>
    /// <param name="runtimeStateCatalog">
    /// An optional runtime-state catalog that can also accept operator-facing observations for active durable streams.
    /// </param>
    /// <remarks>
    /// Executing a durable workflow requires a source-generated or explicitly registered
    /// <see cref="DurableExecutionSlot" /> supplied through dependency injection or <see cref="CreateWithSlots" />.
    /// </remarks>
    public DurableExecutionStrategy(IDurableExecutionRuntimeStateCatalog? runtimeStateCatalog = null)
        : this(runtimeStateCatalog, Array.Empty<DurableExecutionSlot>())
    {
    }

    /// <summary>
    /// Creates a durable execution strategy with source-generated or explicitly registered durable execution slots.
    /// </summary>
    /// <param name="runtimeStateCatalog">
    /// An optional runtime-state catalog that can also accept operator-facing observations for active durable streams.
    /// </param>
    /// <param name="executionSlots">
    /// The durable execution slots registered by behavior source generation or by an explicit host/module registration.
    /// </param>
    /// <returns>A durable execution strategy that can execute behaviors matching the supplied slots.</returns>
    public static DurableExecutionStrategy CreateWithSlots(
        IDurableExecutionRuntimeStateCatalog? runtimeStateCatalog,
        IEnumerable<DurableExecutionSlot> executionSlots)
    {
        return new DurableExecutionStrategy(runtimeStateCatalog, executionSlots);
    }

    /// <summary>
    /// Creates a durable execution strategy with source-generated or explicitly registered durable execution slots.
    /// </summary>
    /// <param name="runtimeStateCatalog">
    /// An optional runtime-state catalog that can also accept operator-facing observations for active durable streams.
    /// </param>
    /// <param name="executionSlots">
    /// The durable execution slots registered by behavior source generation or by an explicit host/module registration.
    /// </param>
    internal DurableExecutionStrategy(
        IDurableExecutionRuntimeStateCatalog? runtimeStateCatalog,
        IEnumerable<DurableExecutionSlot> executionSlots)
    {
        generatedSlots = BuildSlotMap(executionSlots);
        runtimeReporter = runtimeStateCatalog as IDurableExecutionRuntimeReporter;
    }

    /// <summary>Gets the pattern identifier handled by this strategy.</summary>
    public string Pattern => "durable-execution";

    /// <summary>
    /// Replays the durable workflow stream, executes one workflow step, validates the returned domain events,
    /// and appends them through <see cref="IBehaviorContext.EventStore" />.
    /// </summary>
    /// <param name="context">The execution context for this invocation.</param>
    /// <param name="ct">A token that cancels the execution.</param>
    /// <returns>
    /// A result with HTTP 200 when local output exists, HTTP 202 when durable continuation work or
    /// pending timer/signal coordination remains without local output, or HTTP 204 when no local
    /// output remains and the step completed without follow-up work.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when durable execution is selected for a behavior without a registered durable execution slot, when the
    /// behavior context does not carry an event store, or when the returned events do not match the expected stream identity
    /// or version sequence.
    /// </exception>
    public async Task<BehaviorExecutionResult> ExecuteAsync(
        BehaviorExecutionContext context,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var slot = ResolveSlot(context.Descriptor.Id, context.BehaviorInstance.GetType());
        var eventStore = context.BehaviorContext.EventStore
            ?? throw new InvalidOperationException(
                $"DurableExecutionStrategy requires IBehaviorContext.EventStore for behavior '{context.Descriptor.Id}'.");
        var streamId = slot.ResolveStreamId(context.BehaviorInstance, context.Descriptor.Id, context.BehaviorContext);
        if (string.IsNullOrWhiteSpace(streamId))
        {
            throw new InvalidOperationException(
                $"Durable execution behavior '{context.Descriptor.Id}' returned an empty stream id.");
        }

        streamId = streamId.Trim();
        var state = slot.CreateInitialState(context.BehaviorInstance);
        var metadata = CreateReportMetadata(context.BehaviorContext);
        long? version = null;
        try
        {
            version = await eventStore.GetVersionAsync(streamId, ct).ConfigureAwait(false);

            if (version >= 0)
            {
                await foreach (var domainEvent in eventStore.ReadStreamAsync(streamId, 0, ct))
                {
                    state = slot.Apply(context.BehaviorInstance, state, domainEvent);
                }
            }
        }
        catch (Exception exception)
        {
            await ReportFailureAsync(
                    context,
                    streamId,
                    DurableExecutionRuntimeStages.Replay,
                    version,
                    version,
                    appendedEventCount: 0,
                    producedOutput: false,
                    isCompleted: false,
                    exception,
                    metadata,
                    ct)
                .ConfigureAwait(false);
            throw;
        }

        await ReportAsync(
                new DurableExecutionExecutionReport(
                    context.Descriptor.Id,
                    streamId,
                    DurableExecutionRuntimeOutcomes.Started,
                    DurableExecutionRuntimeStages.Execute,
                    DateTimeOffset.UtcNow,
                    replayedVersion: version,
                    knownVersion: version,
                    metadata: metadata),
                ct)
            .ConfigureAwait(false);

        DurableExecutionStepEnvelope step;
        try
        {
            step = await slot.ExecuteAsync(
                    context.BehaviorInstance,
                    context.Input,
                    state,
                    streamId,
                    version ?? -1,
                    context.BehaviorContext,
                    ct)
                .ConfigureAwait(false);

            ValidateReturnedEvents(
                context.Descriptor.Id,
                streamId,
                version ?? -1,
                step.Events);
        }
        catch (Exception exception)
        {
            await ReportFailureAsync(
                    context,
                    streamId,
                    DurableExecutionRuntimeStages.Execute,
                    version,
                    version,
                    appendedEventCount: 0,
                    producedOutput: false,
                    isCompleted: false,
                    exception,
                    metadata,
                    ct)
                .ConfigureAwait(false);
            throw;
        }

        long? knownVersion = version;
        if (step.Events.Count > 0)
        {
            try
            {
                await eventStore.AppendAsync(streamId, step.Events, version ?? -1, ct).ConfigureAwait(false);
                knownVersion = (version ?? -1) + step.Events.Count;
            }
            catch (Exception exception)
            {
                await ReportFailureAsync(
                        context,
                        streamId,
                        DurableExecutionRuntimeStages.Append,
                        version,
                        knownVersion,
                        appendedEventCount: step.Events.Count,
                        producedOutput: step.Output is not null,
                        isCompleted: step.IsCompleted,
                        exception,
                        metadata,
                        ct)
                    .ConfigureAwait(false);
                throw;
            }
        }

        var httpStatusCode = ResolveHttpStatusCode(step);
        await ReportAsync(
                new DurableExecutionExecutionReport(
                    context.Descriptor.Id,
                    streamId,
                    ResolveOutcome(step),
                    step.Events.Count > 0 ? DurableExecutionRuntimeStages.Append : DurableExecutionRuntimeStages.Execute,
                    DateTimeOffset.UtcNow,
                    replayedVersion: version,
                    knownVersion: knownVersion,
                    httpStatusCode: httpStatusCode,
                    appendedEventCount: step.Events.Count,
                    producedOutput: step.Output is not null,
                    isCompleted: step.IsCompleted,
                    pendingTimers: step.PendingTimers,
                    pendingSignals: step.PendingSignals,
                    compensationActions: step.CompensationActions,
                    metadata: metadata),
                ct)
            .ConfigureAwait(false);

        return new BehaviorExecutionResult
        {
            Output = step.Output,
            HttpStatusCode = httpStatusCode,
            IsFireAndForget = false
        };
    }

    private static int ResolveHttpStatusCode(DurableExecutionStepEnvelope step)
    {
        ArgumentNullException.ThrowIfNull(step);

        if (step.Output is not null)
        {
            return 200;
        }

        if (!step.IsCompleted && (step.Events.Count > 0 || step.PendingTimers.Count > 0 || step.PendingSignals.Count > 0))
        {
            return 202;
        }

        return 204;
    }

    private static string ResolveOutcome(DurableExecutionStepEnvelope step)
    {
        ArgumentNullException.ThrowIfNull(step);

        if (step.Output is null && step.Events.Count > 0 && !step.IsCompleted)
        {
            return DurableExecutionRuntimeOutcomes.ContinuationStaged;
        }

        if (step.Output is null && step.Events.Count == 0 && !step.IsCompleted &&
            (step.PendingTimers.Count > 0 || step.PendingSignals.Count > 0))
        {
            return DurableExecutionRuntimeOutcomes.Waiting;
        }

        if (step.Output is null && step.Events.Count == 0 && step.IsCompleted)
        {
            return DurableExecutionRuntimeOutcomes.Completed;
        }

        return DurableExecutionRuntimeOutcomes.Succeeded;
    }

    private static void ValidateReturnedEvents(
        string behaviorId,
        string streamId,
        long currentVersion,
        IReadOnlyList<IDomainEvent> events)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(behaviorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(streamId);
        ArgumentNullException.ThrowIfNull(events);

        var expectedVersion = currentVersion + 1;
        foreach (var domainEvent in events)
        {
            ArgumentNullException.ThrowIfNull(domainEvent);

            if (!string.Equals(domainEvent.StreamId, streamId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Durable execution behavior '{behaviorId}' returned event '{domainEvent.GetType().Name}' for stream '{domainEvent.StreamId}', but '{streamId}' was expected.");
            }

            if (domainEvent.StreamVersion != expectedVersion)
            {
                throw new InvalidOperationException(
                    $"Durable execution behavior '{behaviorId}' returned event '{domainEvent.GetType().Name}' with stream version {domainEvent.StreamVersion}, but {expectedVersion} was expected.");
            }

            expectedVersion++;
        }
    }

    private static Dictionary<string, string> CreateReportMetadata(IBehaviorContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var metadata = new Dictionary<string, string>(context.Metadata, StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(context.CorrelationId))
        {
            metadata["correlationId"] = context.CorrelationId.Trim();
        }

        return metadata;
    }

    private async ValueTask ReportFailureAsync(
        BehaviorExecutionContext context,
        string streamId,
        string stage,
        long? replayedVersion,
        long? knownVersion,
        int appendedEventCount,
        bool producedOutput,
        bool isCompleted,
        Exception exception,
        IReadOnlyDictionary<string, string> metadata,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(streamId);
        ArgumentException.ThrowIfNullOrWhiteSpace(stage);
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(metadata);

        var failureMetadata = new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase)
        {
            ["exceptionType"] = exception.GetType().FullName ?? exception.GetType().Name
        };

        await ReportAsync(
                new DurableExecutionExecutionReport(
                    context.Descriptor.Id,
                    streamId,
                    DurableExecutionRuntimeOutcomes.Failed,
                    stage,
                    DateTimeOffset.UtcNow,
                    replayedVersion: replayedVersion,
                    knownVersion: knownVersion,
                    appendedEventCount: appendedEventCount,
                    producedOutput: producedOutput,
                    isCompleted: isCompleted,
                    error: SummarizeException(exception),
                    metadata: failureMetadata),
                cancellationToken)
            .ConfigureAwait(false);
    }

    private static Dictionary<Type, DurableExecutionSlot> BuildSlotMap(
        IEnumerable<DurableExecutionSlot> executionSlots)
    {
        ArgumentNullException.ThrowIfNull(executionSlots);

        var map = new Dictionary<Type, DurableExecutionSlot>();
        foreach (var slot in executionSlots)
        {
            ArgumentNullException.ThrowIfNull(slot);

            if (map.TryGetValue(slot.BehaviorType, out var existingSlot))
            {
                if (existingSlot.InputType == slot.InputType &&
                    existingSlot.StateType == slot.StateType &&
                    existingSlot.OutputType == slot.OutputType)
                {
                    continue;
                }

                throw new InvalidOperationException(
                    $"A durable execution slot for behavior type '{slot.BehaviorType.FullName}' has already been registered with a different durable contract.");
            }

            map.Add(slot.BehaviorType, slot);
        }

        return map;
    }

    private DurableExecutionSlot ResolveSlot(string behaviorId, Type behaviorType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(behaviorId);
        ArgumentNullException.ThrowIfNull(behaviorType);

        if (generatedSlots.TryGetValue(behaviorType, out var generatedSlot))
        {
            return generatedSlot;
        }

        throw new InvalidOperationException(
            $"Durable execution behavior '{behaviorId}' with implementation type '{behaviorType.FullName}' requires a source-generated or explicitly registered DurableExecutionSlot. Rebuild with Cephalon.Behaviors.SourceGen or register DurableExecutionSlot.For<TBehavior, TInput, TState, TOutput>() before executing the workflow.");
    }

    private ValueTask ReportAsync(
        DurableExecutionExecutionReport report,
        CancellationToken cancellationToken)
    {
        return runtimeReporter is null
            ? ValueTask.CompletedTask
            : runtimeReporter.ReportAsync(report, cancellationToken);
    }

    private static string SummarizeException(Exception exception)
    {
        var message = exception.Message?.Trim();
        return string.IsNullOrWhiteSpace(message)
            ? exception.GetType().Name
            : message;
    }

}
