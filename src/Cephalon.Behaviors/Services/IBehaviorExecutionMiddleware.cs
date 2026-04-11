namespace Cephalon.Behaviors.Services;

internal delegate ValueTask<object?> BehaviorExecutionDelegate(
    BehaviorExecutionInvocation invocation,
    CancellationToken cancellationToken);

/// <summary>
/// Represents one middleware component in the internal behavior execution pipeline.
/// </summary>
internal interface IBehaviorExecutionMiddleware
{
    ValueTask<object?> InvokeAsync(
        BehaviorExecutionInvocation invocation,
        BehaviorExecutionDelegate next,
        CancellationToken cancellationToken);
}
