using System.Collections.Concurrent;
using System.Text.Json;
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
    private static readonly JsonSerializerOptions WebJsonSerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly ConcurrentDictionary<Type, IDurableExecutionAdapter> Adapters = new();
    private readonly IDurableExecutionRuntimeReporter? runtimeReporter;

    /// <summary>
    /// Creates a durable execution strategy.
    /// </summary>
    /// <param name="runtimeStateCatalog">
    /// An optional runtime-state catalog that can also accept operator-facing observations for active durable streams.
    /// </param>
    public DurableExecutionStrategy(IDurableExecutionRuntimeStateCatalog? runtimeStateCatalog = null)
    {
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
    /// Thrown when durable execution is selected for a behavior that does not implement
    /// <c>IDurableExecution&lt;TInput, TState, TOutput&gt;</c>, when the behavior context does not carry an event store,
    /// or when the returned events do not match the expected stream identity or version sequence.
    /// </exception>
    public async Task<BehaviorExecutionResult> ExecuteAsync(
        BehaviorExecutionContext context,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var adapter = Adapters.GetOrAdd(
            context.BehaviorInstance.GetType(),
            static behaviorType => DurableExecutionAdapterFactory.Create(behaviorType));
        var eventStore = context.BehaviorContext.EventStore
            ?? throw new InvalidOperationException(
                $"DurableExecutionStrategy requires IBehaviorContext.EventStore for behavior '{context.Descriptor.Id}'.");
        var streamId = adapter.ResolveStreamId(context.BehaviorInstance, context.Descriptor.Id, context.BehaviorContext);
        if (string.IsNullOrWhiteSpace(streamId))
        {
            throw new InvalidOperationException(
                $"Durable execution behavior '{context.Descriptor.Id}' returned an empty stream id.");
        }

        streamId = streamId.Trim();
        var state = adapter.CreateInitialState(context.BehaviorInstance);
        var metadata = CreateReportMetadata(context.BehaviorContext);
        long? version = null;
        try
        {
            version = await eventStore.GetVersionAsync(streamId, ct).ConfigureAwait(false);

            if (version >= 0)
            {
                await foreach (var domainEvent in eventStore.ReadStreamAsync(streamId, 0, ct))
                {
                    state = adapter.Apply(context.BehaviorInstance, state, domainEvent);
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
            step = await adapter.ExecuteAsync(
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

    private interface IDurableExecutionAdapter
    {
        string ResolveStreamId(object behavior, string behaviorId, IBehaviorContext context);

        object? CreateInitialState(object behavior);

        object? Apply(object behavior, object? currentState, IDomainEvent domainEvent);

        Task<DurableExecutionStepEnvelope> ExecuteAsync(
            object behavior,
            object input,
            object? currentState,
            string streamId,
            long version,
            IBehaviorContext context,
            CancellationToken cancellationToken);
    }

    private static class DurableExecutionAdapterFactory
    {
        internal static IDurableExecutionAdapter Create(Type behaviorType)
        {
            ArgumentNullException.ThrowIfNull(behaviorType);

            var durableInterface = behaviorType
                .GetInterfaces()
                .FirstOrDefault(static candidate =>
                    candidate.IsGenericType &&
                    candidate.GetGenericTypeDefinition() == typeof(IDurableExecution<,,>))
                ?? throw new InvalidOperationException(
                    $"Behavior type '{behaviorType.FullName}' selected the 'durable-execution' pattern but does not implement IDurableExecution<TInput, TState, TOutput>.");

            var typeArguments = durableInterface.GetGenericArguments();
            var adapterType = typeof(DurableExecutionAdapter<,,,>).MakeGenericType(
                behaviorType,
                typeArguments[0],
                typeArguments[1],
                typeArguments[2]);

            return (IDurableExecutionAdapter)Activator.CreateInstance(adapterType)!;
        }
    }

    private sealed class DurableExecutionAdapter<TBehavior, TInput, TState, TOutput> : IDurableExecutionAdapter
        where TBehavior : class, IDurableExecution<TInput, TState, TOutput>
    {
        public string ResolveStreamId(object behavior, string behaviorId, IBehaviorContext context)
        {
            ArgumentNullException.ThrowIfNull(behavior);
            ArgumentException.ThrowIfNullOrWhiteSpace(behaviorId);
            ArgumentNullException.ThrowIfNull(context);

            return ((TBehavior)behavior).ResolveStreamId(behaviorId, context);
        }

        public object? CreateInitialState(object behavior)
        {
            ArgumentNullException.ThrowIfNull(behavior);
            return ((TBehavior)behavior).CreateInitialState();
        }

        public object? Apply(object behavior, object? currentState, IDomainEvent domainEvent)
        {
            ArgumentNullException.ThrowIfNull(behavior);
            ArgumentNullException.ThrowIfNull(domainEvent);

            return ((TBehavior)behavior).Apply(
                currentState is null ? default! : (TState)currentState,
                domainEvent);
        }

        public async Task<DurableExecutionStepEnvelope> ExecuteAsync(
            object behavior,
            object input,
            object? currentState,
            string streamId,
            long version,
            IBehaviorContext context,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(behavior);
            ArgumentNullException.ThrowIfNull(context);
            ArgumentException.ThrowIfNullOrWhiteSpace(streamId);

            var typedInput = input is JsonElement jsonElement
                ? JsonSerializer.Deserialize<TInput>(jsonElement.GetRawText(), WebJsonSerializerOptions)!
                : (TInput)input;
            var executionState = new DurableExecutionState<TState>(
                streamId,
                currentState is null ? default! : (TState)currentState,
                version);
            var result = await ((TBehavior)behavior)
                .ExecuteDurablyAsync(typedInput, executionState, context, cancellationToken)
                .ConfigureAwait(false);

            return new DurableExecutionStepEnvelope(
                result.Output,
                result.Events,
                result.IsCompleted,
                result.PendingTimers,
                result.PendingSignals,
                result.CompensationActions);
        }
    }

    private sealed class DurableExecutionStepEnvelope
    {
        internal DurableExecutionStepEnvelope(
            object? output,
            IReadOnlyList<IDomainEvent> events,
            bool isCompleted,
            IReadOnlyList<DurableExecutionPendingTimer> pendingTimers,
            IReadOnlyList<DurableExecutionPendingSignal> pendingSignals,
            IReadOnlyList<DurableExecutionCompensationAction> compensationActions)
        {
            Output = output;
            Events = events;
            IsCompleted = isCompleted;
            PendingTimers = pendingTimers;
            PendingSignals = pendingSignals;
            CompensationActions = compensationActions;
        }

        internal object? Output { get; }

        internal IReadOnlyList<IDomainEvent> Events { get; }

        internal bool IsCompleted { get; }

        internal IReadOnlyList<DurableExecutionPendingTimer> PendingTimers { get; }

        internal IReadOnlyList<DurableExecutionPendingSignal> PendingSignals { get; }

        internal IReadOnlyList<DurableExecutionCompensationAction> CompensationActions { get; }
    }
}
