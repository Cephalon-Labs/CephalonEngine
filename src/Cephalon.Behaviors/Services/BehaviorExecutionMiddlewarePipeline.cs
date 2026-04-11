namespace Cephalon.Behaviors.Services;

internal static class BehaviorExecutionMiddlewarePipeline
{
    public static BehaviorExecutionDelegate Compose(
        IReadOnlyList<IBehaviorExecutionMiddleware> middlewares,
        BehaviorExecutionDelegate terminal)
    {
        ArgumentNullException.ThrowIfNull(middlewares);
        ArgumentNullException.ThrowIfNull(terminal);

        var next = terminal;
        for (var index = middlewares.Count - 1; index >= 0; index--)
        {
            var middleware = middlewares[index];
            var currentNext = next;
            next = (invocation, cancellationToken) =>
                middleware.InvokeAsync(invocation, currentNext, cancellationToken);
        }

        return next;
    }
}
