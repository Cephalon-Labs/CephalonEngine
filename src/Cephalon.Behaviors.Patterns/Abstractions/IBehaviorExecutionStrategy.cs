namespace Cephalon.Behaviors.Patterns.Abstractions;

/// <summary>Executes a behavior invocation according to a specific architectural pattern.</summary>
public interface IBehaviorExecutionStrategy
{
    /// <summary>Gets the pattern identifier this strategy handles (e.g. "cqrs", "event-driven").</summary>
    string Pattern { get; }

    /// <summary>Executes the behavior using pattern-specific semantics.</summary>
    /// <param name="context">The execution context carrying all information needed for this invocation.</param>
    /// <param name="ct">A token that cancels the execution.</param>
    /// <returns>A task that resolves to the execution result.</returns>
    Task<BehaviorExecutionResult> ExecuteAsync(
        BehaviorExecutionContext context,
        CancellationToken ct = default);
}
