using Cephalon.Behaviors.Patterns.Abstractions;

namespace Cephalon.Behaviors.Patterns.Strategies;

/// <summary>
/// Executes behaviors that follow the direct (request/response) pattern.
/// Returns HTTP 200 when output is non-null, or HTTP 204 No Content when output is null.
/// </summary>
public sealed class DirectExecutionStrategy : IBehaviorExecutionStrategy
{
    /// <summary>Gets the pattern identifier handled by this strategy.</summary>
    public string Pattern => "direct";

    /// <summary>
    /// Invokes the behavior slot directly and returns the output.
    /// Returns HTTP 200 for non-null output and HTTP 204 for null output.
    /// </summary>
    /// <param name="context">The execution context for this invocation.</param>
    /// <param name="ct">A token that cancels the execution.</param>
    /// <returns>A result with HTTP 200 for non-null output or HTTP 204 for null output.</returns>
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
            HttpStatusCode = output is not null ? 200 : 204,
            IsFireAndForget = false
        };
    }
}
