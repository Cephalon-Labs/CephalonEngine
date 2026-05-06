using System.Text.Json;
using Cephalon.Abstractions.Behaviors;
using Cephalon.Behaviors.Services;

namespace Cephalon.Tests.Behaviors;

internal static class BehaviorExecutionTestSlots
{
    private static readonly JsonSerializerOptions WebJsonSerializerOptions = new(JsonSerializerDefaults.Web);

    public static BehaviorExecutionSlot For<TBehavior>(TBehavior behavior)
        where TBehavior : class
    {
        ArgumentNullException.ThrowIfNull(behavior);

        var behaviorInterface = typeof(TBehavior)
            .GetInterfaces()
            .FirstOrDefault(static candidate =>
                candidate.IsGenericType &&
                candidate.GetGenericTypeDefinition() == typeof(IAppBehavior<,>))
            ?? throw new InvalidOperationException(
                $"Test behavior '{typeof(TBehavior).FullName}' does not implement IAppBehavior<TInput, TOutput>.");

        var typeArgs = behaviorInterface.GetGenericArguments();
        var method = typeof(BehaviorExecutionTestSlots)
            .GetMethod(nameof(ForCore), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .MakeGenericMethod(typeArgs);
        return (BehaviorExecutionSlot)method.Invoke(null, [behavior])!;
    }

    private static BehaviorExecutionSlot ForCore<TInput, TOutput>(IAppBehavior<TInput, TOutput> behavior)
        where TInput : notnull
    {
        ArgumentNullException.ThrowIfNull(behavior);

        return BehaviorExecutionSlot.FromDelegate(static async (behaviorInstance, input, context, cancellationToken) =>
        {
            TInput typedInput = input is JsonElement jsonElement
                ? JsonSerializer.Deserialize<TInput>(jsonElement.GetRawText(), WebJsonSerializerOptions)!
                : (TInput)input;
            var result = await ((IAppBehavior<TInput, TOutput>)behaviorInstance)
                .HandleAsync(typedInput, context, cancellationToken)
                .ConfigureAwait(false);
            return result;
        });
    }
}
