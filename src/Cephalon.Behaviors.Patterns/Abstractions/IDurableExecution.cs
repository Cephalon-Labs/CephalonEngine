using Cephalon.Abstractions.Behaviors;
using Cephalon.Abstractions.EventSourcing;

namespace Cephalon.Behaviors.Patterns.Abstractions;

/// <summary>
/// Describes the replayable state contract for one durable execution workflow.
/// </summary>
/// <typeparam name="TState">The workflow state shape.</typeparam>
public interface IDurableExecution<TState> : IAggregate<TState>
{
    /// <summary>
    /// Creates the initial workflow state used before any events exist for the execution stream.
    /// </summary>
    /// <returns>The initial workflow state.</returns>
    TState CreateInitialState();

    /// <summary>
    /// Resolves the event-store stream identifier that owns this workflow execution.
    /// </summary>
    /// <param name="behaviorId">The stable behavior identifier.</param>
    /// <param name="context">The ambient behavior context for the current invocation.</param>
    /// <returns>The stable stream identifier used for replay and append operations.</returns>
    string ResolveStreamId(string behaviorId, IBehaviorContext context);
}

/// <summary>
/// Defines one replayable durable-execution workflow step on top of the shared behavior model.
/// </summary>
/// <typeparam name="TInput">The workflow input type.</typeparam>
/// <typeparam name="TState">The workflow state shape.</typeparam>
/// <typeparam name="TOutput">The local output type returned to the caller.</typeparam>
public interface IDurableExecution<TInput, TState, TOutput> : IDurableExecution<TState>, IAppBehavior<TInput, TOutput>
{
    /// <summary>
    /// Executes one durable workflow step against the replayed state snapshot.
    /// </summary>
    /// <param name="input">The workflow input for this invocation.</param>
    /// <param name="execution">The replayed durable execution state.</param>
    /// <param name="context">The ambient behavior context for the current invocation.</param>
    /// <param name="cancellationToken">The token that cancels the workflow step.</param>
    /// <returns>
    /// A task that resolves to the step output plus the ordered events that should be appended durably.
    /// </returns>
    Task<DurableExecutionStepResult<TOutput>> ExecuteDurablyAsync(
        TInput input,
        DurableExecutionState<TState> execution,
        IBehaviorContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Durable-execution behaviors are expected to run through the durable strategy rather than the direct slot path.
    /// </summary>
    Task<TOutput> IAppBehavior<TInput, TOutput>.HandleAsync(
        TInput input,
        IBehaviorContext context,
        CancellationToken cancellationToken)
    {
        return Task.FromException<TOutput>(
            new NotSupportedException(
                "Durable execution behaviors must run through the 'durable-execution' pattern strategy."));
    }
}
