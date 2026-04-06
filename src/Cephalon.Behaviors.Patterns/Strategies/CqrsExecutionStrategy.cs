using Cephalon.Behaviors.Patterns.Abstractions;

namespace Cephalon.Behaviors.Patterns.Strategies;

/// <summary>
/// Executes behaviors that follow the CQRS (Command Query Responsibility Segregation) pattern.
/// Commands produce no output (returns HTTP 202), queries return a value (HTTP 200).
/// </summary>
public sealed class CqrsExecutionStrategy : IBehaviorExecutionStrategy
{
    /// <summary>Gets the pattern identifier handled by this strategy.</summary>
    public string Pattern => "cqrs";

    /// <summary>
    /// Invokes the behavior slot and determines the HTTP status code based on whether output was produced.
    /// A null output is treated as a command (202 Accepted); a non-null output is treated as a query (200 OK).
    /// </summary>
    /// <param name="context">The execution context for this invocation.</param>
    /// <param name="ct">A token that cancels the execution.</param>
    /// <returns>A result with HTTP 200 for queries or HTTP 202 for commands.</returns>
    public async Task<BehaviorExecutionResult> ExecuteAsync(
        BehaviorExecutionContext context,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var output = await context.Slot
            .InvokeAsync(context.BehaviorInstance, context.Input, context.BehaviorContext, ct)
            .ConfigureAwait(false);

        return new BehaviorExecutionResult
        {
            Output = output,
            HttpStatusCode = output is null ? 202 : 200,
            IsFireAndForget = false
        };
    }
}
