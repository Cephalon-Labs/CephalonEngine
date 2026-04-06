using System.Linq.Expressions;
using Cephalon.Abstractions.Behaviors;

namespace Cephalon.Behaviors.Services;

/// <summary>Compiled Expression.Lambda invoker for a behavior handler. Zero reflection at runtime.</summary>
public sealed class BehaviorExecutionSlot
{
    private readonly Func<object, object, IBehaviorContext, CancellationToken, Task<object?>> _invoker;

    private BehaviorExecutionSlot(Func<object, object, IBehaviorContext, CancellationToken, Task<object?>> invoker)
        => _invoker = invoker;

    /// <summary>Compiles an invoker for the specified behavior type. Call once at startup; cache the result.</summary>
    public static BehaviorExecutionSlot For<TBehavior, TIn, TOut>()
        where TBehavior : IAppBehavior<TIn, TOut>
    {
        // Compile Expression.Lambda once
        var behaviorParam = Expression.Parameter(typeof(object), "behavior");
        var inputParam    = Expression.Parameter(typeof(object), "input");
        var ctxParam      = Expression.Parameter(typeof(IBehaviorContext), "ctx");
        var ctParam       = Expression.Parameter(typeof(CancellationToken), "ct");

        var castBehavior = Expression.Convert(behaviorParam, typeof(TBehavior));
        var castInput    = Expression.Convert(inputParam, typeof(TIn));
        var call         = Expression.Call(castBehavior,
            typeof(TBehavior).GetMethod(nameof(IAppBehavior<TIn, TOut>.HandleAsync))!,
            castInput, ctxParam, ctParam);

        // Wrap Task<TOut> → Task<object?>
        var wrapper = Expression.Call(
            typeof(BehaviorExecutionSlot),
            nameof(WrapAsync),
            [typeof(TOut)],
            call);

        var lambda = Expression.Lambda<Func<object, object, IBehaviorContext, CancellationToken, Task<object?>>>(
            wrapper, behaviorParam, inputParam, ctxParam, ctParam);

        return new BehaviorExecutionSlot(lambda.Compile());
    }

    private static async Task<object?> WrapAsync<TOut>(Task<TOut> task) => await task;

    /// <summary>Invokes the compiled behavior handler asynchronously.</summary>
    public Task<object?> InvokeAsync(object behavior, object input, IBehaviorContext ctx, CancellationToken ct)
        => _invoker(behavior, input, ctx, ct);
}
