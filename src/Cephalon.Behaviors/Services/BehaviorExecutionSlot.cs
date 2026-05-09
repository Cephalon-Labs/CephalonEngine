using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Cephalon.Abstractions.Behaviors;

namespace Cephalon.Behaviors.Services;

/// <summary>
/// A compiled, type-safe invocation delegate for a concrete <see cref="IAppBehavior{TIn,TOut}" /> implementation.
/// Slots are created once at dispatcher construction time and reused for every dispatch call.
/// </summary>
public sealed class BehaviorExecutionSlot
{
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
        => For<TBehavior, TIn, TOut>(inputJsonTypeInfo: null);

    /// <summary>
    /// Creates a <see cref="BehaviorExecutionSlot" /> for a behavior whose generic type arguments and input JSON metadata are known at compile time.
    /// </summary>
    /// <typeparam name="TBehavior">The concrete behavior type.</typeparam>
    /// <typeparam name="TIn">The input message type.</typeparam>
    /// <typeparam name="TOut">The output message type.</typeparam>
    /// <param name="inputJsonTypeInfo">The source-generated JSON metadata for <typeparamref name="TIn" />.</param>
    /// <returns>A compiled execution slot for the behavior.</returns>
    public static BehaviorExecutionSlot For<TBehavior, TIn, TOut>(
        JsonTypeInfo<TIn>? inputJsonTypeInfo)
        where TBehavior : IAppBehavior<TIn, TOut>
        where TIn : notnull
    {
        return new BehaviorExecutionSlot(async (behavior, input, context, ct) =>
        {
            // When input arrives as a JsonElement (e.g. from an HTTP transport), coerce it to TIn.
            TIn typedInput = CoerceInput<TIn>(input, inputJsonTypeInfo);
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

    private static TIn CoerceInput<TIn>(
        object input,
        JsonTypeInfo<TIn>? inputJsonTypeInfo)
        where TIn : notnull
    {
        if (input is not JsonElement jsonElement)
        {
            return (TIn)input;
        }

        if (typeof(TIn) == typeof(JsonElement))
        {
            return (TIn)(object)jsonElement;
        }

        if (typeof(TIn) == typeof(object))
        {
            return (TIn)(object)jsonElement;
        }

        if (inputJsonTypeInfo is null)
        {
            throw new InvalidOperationException(
                $"Behavior input type '{typeof(TIn).FullName}' requires source-generated JSON metadata when dispatch input arrives as JsonElement. " +
                "Use BehaviorExecutionSlot.For<TBehavior, TInput, TOutput>(JsonTypeInfo<TInput>) or source-generated behavior registration.");
        }

        return jsonElement.Deserialize(inputJsonTypeInfo)!;
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
