using System.Text.Json;
using Cephalon.Abstractions.Behaviors;

namespace Cephalon.Behaviors.Services;

/// <summary>
/// A compiled, type-safe invocation delegate for a concrete <see cref="IAppBehavior{TIn,TOut}" /> implementation.
/// Slots are created once at dispatcher construction time and reused for every dispatch call.
/// </summary>
public sealed class BehaviorExecutionSlot
{
    private static readonly JsonSerializerOptions WebJsonSerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly Func<object, object, IBehaviorContext, CancellationToken, Task<object?>> _invoke;

    private BehaviorExecutionSlot(Func<object, object, IBehaviorContext, CancellationToken, Task<object?>> invoke)
    {
        _invoke = invoke;
    }

    /// <summary>
    /// Creates a <see cref="BehaviorExecutionSlot" /> for a behavior whose generic type arguments are known at compile time.
    /// </summary>
    /// <typeparam name="TBehavior">The concrete behavior type.</typeparam>
    /// <typeparam name="TIn">The input message type.</typeparam>
    /// <typeparam name="TOut">The output message type.</typeparam>
    /// <returns>A compiled execution slot for the behavior.</returns>
    public static BehaviorExecutionSlot For<TBehavior, TIn, TOut>()
        where TBehavior : IAppBehavior<TIn, TOut>
        where TIn : notnull
    {
        return new BehaviorExecutionSlot(async (behavior, input, context, ct) =>
        {
            // When input arrives as a JsonElement (e.g. from an HTTP transport), coerce it to TIn.
            TIn typedInput = input is JsonElement je
                ? JsonSerializer.Deserialize<TIn>(je.GetRawText(), WebJsonSerializerOptions)!
                : (TIn)input;
            var result = await ((TBehavior)behavior).HandleAsync(typedInput, context, ct).ConfigureAwait(false);
            return result;
        });
    }

    internal static BehaviorExecutionSlot FromDelegate(
        Func<object, object, IBehaviorContext, CancellationToken, Task<object?>> invoke)
    {
        ArgumentNullException.ThrowIfNull(invoke);
        return new BehaviorExecutionSlot(invoke);
    }

    /// <summary>
    /// Invokes the compiled behavior delegate.
    /// </summary>
    /// <param name="behavior">The resolved behavior instance.</param>
    /// <param name="input">The input message object.</param>
    /// <param name="context">The behavior execution context.</param>
    /// <param name="ct">A token that cancels the invocation.</param>
    /// <returns>A task that resolves to the behavior output, boxed as <see cref="object" />.</returns>
    public Task<object?> InvokeAsync(
        object behavior,
        object input,
        IBehaviorContext context,
        CancellationToken ct = default)
    {
        return _invoke(behavior, input, context, ct);
    }
}
