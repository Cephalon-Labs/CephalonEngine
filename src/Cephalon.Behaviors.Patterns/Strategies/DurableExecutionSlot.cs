using System.Reflection;
using System.Text.Json;
using Cephalon.Abstractions.Behaviors;
using Cephalon.Abstractions.EventSourcing;
using Cephalon.Abstractions.Execution;
using Cephalon.Behaviors.Patterns.Abstractions;

namespace Cephalon.Behaviors.Patterns.Strategies;

/// <summary>
/// A compiled, type-safe durable-execution adapter for one concrete
/// <see cref="IDurableExecution{TInput,TState,TOutput}" /> behavior implementation.
/// </summary>
public sealed class DurableExecutionSlot
{
    private static readonly JsonSerializerOptions WebJsonSerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly Func<object, string, IBehaviorContext, string> resolveStreamId;
    private readonly Func<object, object?> createInitialState;
    private readonly Func<object, object?, IDomainEvent, object?> apply;
    private readonly Func<object, object, object?, string, long, IBehaviorContext, CancellationToken, Task<DurableExecutionStepEnvelope>> executeAsync;

    private DurableExecutionSlot(
        Type behaviorType,
        Type inputType,
        Type stateType,
        Type outputType,
        Func<object, string, IBehaviorContext, string> resolveStreamId,
        Func<object, object?> createInitialState,
        Func<object, object?, IDomainEvent, object?> apply,
        Func<object, object, object?, string, long, IBehaviorContext, CancellationToken, Task<DurableExecutionStepEnvelope>> executeAsync)
    {
        BehaviorType = behaviorType;
        InputType = inputType;
        StateType = stateType;
        OutputType = outputType;
        this.resolveStreamId = resolveStreamId;
        this.createInitialState = createInitialState;
        this.apply = apply;
        this.executeAsync = executeAsync;
    }

    internal Type BehaviorType { get; }

    internal Type InputType { get; }

    internal Type StateType { get; }

    internal Type OutputType { get; }

    /// <summary>
    /// Creates a durable-execution slot for a behavior whose durable generic type arguments are known at compile time.
    /// </summary>
    /// <typeparam name="TBehavior">The concrete durable behavior type.</typeparam>
    /// <typeparam name="TInput">The durable workflow input type.</typeparam>
    /// <typeparam name="TState">The durable workflow state type.</typeparam>
    /// <typeparam name="TOutput">The durable workflow output type.</typeparam>
    /// <returns>A compiled durable-execution slot for the behavior.</returns>
    public static DurableExecutionSlot For<TBehavior, TInput, TState, TOutput>()
        where TBehavior : class, IDurableExecution<TInput, TState, TOutput>
    {
        return new DurableExecutionSlot(
            typeof(TBehavior),
            typeof(TInput),
            typeof(TState),
            typeof(TOutput),
            static (behavior, behaviorId, context) =>
                ((TBehavior)behavior).ResolveStreamId(behaviorId, context),
            static behavior =>
                ((TBehavior)behavior).CreateInitialState(),
            static (behavior, currentState, domainEvent) =>
                ((TBehavior)behavior).Apply(
                    currentState is null ? default! : (TState)currentState,
                    domainEvent),
            static async (behavior, input, currentState, streamId, version, context, cancellationToken) =>
            {
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
            });
    }

    internal static DurableExecutionSlot ForType(Type behaviorType)
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
        var forMethod = typeof(DurableExecutionSlot)
            .GetMethod(nameof(For), BindingFlags.Public | BindingFlags.Static)!
            .MakeGenericMethod(
                behaviorType,
                typeArguments[0],
                typeArguments[1],
                typeArguments[2]);

        return (DurableExecutionSlot)forMethod.Invoke(null, null)!;
    }

    internal string ResolveStreamId(object behavior, string behaviorId, IBehaviorContext context)
    {
        return resolveStreamId(behavior, behaviorId, context);
    }

    internal object? CreateInitialState(object behavior)
    {
        return createInitialState(behavior);
    }

    internal object? Apply(object behavior, object? currentState, IDomainEvent domainEvent)
    {
        return apply(behavior, currentState, domainEvent);
    }

    internal Task<DurableExecutionStepEnvelope> ExecuteAsync(
        object behavior,
        object input,
        object? currentState,
        string streamId,
        long version,
        IBehaviorContext context,
        CancellationToken cancellationToken)
    {
        return executeAsync(behavior, input, currentState, streamId, version, context, cancellationToken);
    }
}

internal sealed class DurableExecutionStepEnvelope
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
