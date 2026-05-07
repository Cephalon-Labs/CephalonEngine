using System.Reflection;
using System.Text.Json;
using Cephalon.Abstractions.Behaviors;

namespace Cephalon.Engine.Composition;

internal sealed class OwnedBehaviorModuleBuilder(string sourceModuleId) : IBehaviorModuleBuilder
{
    private static readonly Type AppBehaviorOpenGeneric = typeof(IAppBehavior<,>);
    private static readonly JsonSerializerOptions WebJsonSerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly string sourceModuleId = NormalizeRequired(sourceModuleId);
    private readonly List<OwnedBehaviorRegistration> registrations = [];

    public IBehaviorModuleBuilder Add<TBehavior>()
        where TBehavior : class
        => Add(typeof(TBehavior));

    public IBehaviorModuleBuilder Add<TBehavior, TInput, TOutput>()
        where TBehavior : class, IAppBehavior<TInput, TOutput>
        where TInput : notnull
        => AddCore(
            typeof(TBehavior),
            configureTopology: null,
            CreateExecutionDelegate<TBehavior, TInput, TOutput>());

    public IBehaviorModuleBuilder Add<TBehavior>(Action<IBehaviorTopologyBuilder> configureTopology)
        where TBehavior : class
    {
        ArgumentNullException.ThrowIfNull(configureTopology);
        return Add(typeof(TBehavior), configureTopology);
    }

    public IBehaviorModuleBuilder Add<TBehavior, TInput, TOutput>(
        Action<IBehaviorTopologyBuilder> configureTopology)
        where TBehavior : class, IAppBehavior<TInput, TOutput>
        where TInput : notnull
    {
        ArgumentNullException.ThrowIfNull(configureTopology);
        return AddCore(
            typeof(TBehavior),
            configureTopology,
            CreateExecutionDelegate<TBehavior, TInput, TOutput>());
    }

    public IBehaviorModuleBuilder Add(Type behaviorType)
        => AddCore(behaviorType, configureTopology: null);

    public IBehaviorModuleBuilder Add(Type behaviorType, Type inputType, Type outputType)
        => AddCore(
            behaviorType,
            configureTopology: null,
            CreateExecutionDelegate(behaviorType, inputType, outputType));

    public IBehaviorModuleBuilder Add(Type behaviorType, Action<IBehaviorTopologyBuilder> configureTopology)
    {
        ArgumentNullException.ThrowIfNull(configureTopology);
        return AddCore(behaviorType, configureTopology);
    }

    public IBehaviorModuleBuilder Add(
        Type behaviorType,
        Type inputType,
        Type outputType,
        Action<IBehaviorTopologyBuilder> configureTopology)
    {
        ArgumentNullException.ThrowIfNull(configureTopology);
        return AddCore(
            behaviorType,
            configureTopology,
            CreateExecutionDelegate(behaviorType, inputType, outputType));
    }

    internal IReadOnlyList<OwnedBehaviorRegistration> Build()
        => registrations.ToArray();

    private OwnedBehaviorModuleBuilder AddCore(
        Type behaviorType,
        Action<IBehaviorTopologyBuilder>? configureTopology,
        Func<object, object, IBehaviorContext, CancellationToken, Task<object?>>? executionDelegate = null)
    {
        ArgumentNullException.ThrowIfNull(behaviorType);

        var attribute = behaviorType.GetCustomAttributes(typeof(AppBehaviorAttribute), inherit: false)
            .OfType<AppBehaviorAttribute>()
            .SingleOrDefault()
            ?? throw new InvalidOperationException(
                $"Cannot declare '{behaviorType.FullName}' as a module-owned behavior because it is missing [AppBehavior(id)].");

        if (!behaviorType.GetInterfaces().Any(static candidate =>
                candidate.IsGenericType &&
                candidate.GetGenericTypeDefinition() == AppBehaviorOpenGeneric))
        {
            throw new InvalidOperationException(
                $"Cannot declare '{behaviorType.FullName}' as a module-owned behavior because it does not implement IAppBehavior<TInput, TOutput>.");
        }

        if (registrations.Any(existing =>
                string.Equals(existing.BehaviorId, attribute.Id, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                $"Module '{sourceModuleId}' declared behavior '{attribute.Id}' more than once.");
        }

        registrations.Add(new OwnedBehaviorRegistration(
            sourceModuleId,
            attribute.Id,
            behaviorType,
            configureTopology,
            executionDelegate));

        return this;
    }

    private static Func<object, object, IBehaviorContext, CancellationToken, Task<object?>> CreateExecutionDelegate<TBehavior, TInput, TOutput>()
        where TBehavior : class, IAppBehavior<TInput, TOutput>
        where TInput : notnull
    {
        return static async (behavior, input, context, cancellationToken) =>
        {
            TInput typedInput = input is JsonElement jsonElement
                ? JsonSerializer.Deserialize<TInput>(jsonElement.GetRawText(), WebJsonSerializerOptions)!
                : (TInput)input;
            var result = await ((TBehavior)behavior)
                .HandleAsync(typedInput, context, cancellationToken)
                .ConfigureAwait(false);
            return result;
        };
    }

    private static Func<object, object, IBehaviorContext, CancellationToken, Task<object?>> CreateExecutionDelegate(
        Type behaviorType,
        Type inputType,
        Type outputType)
    {
        ArgumentNullException.ThrowIfNull(behaviorType);
        ArgumentNullException.ThrowIfNull(inputType);
        ArgumentNullException.ThrowIfNull(outputType);

        var closedBehaviorContract = AppBehaviorOpenGeneric.MakeGenericType(inputType, outputType);
        if (!closedBehaviorContract.IsAssignableFrom(behaviorType))
        {
            throw new InvalidOperationException(
                $"Cannot declare '{behaviorType.FullName}' as a module-owned behavior with input '{inputType.FullName}' and output '{outputType.FullName}' because it does not implement IAppBehavior<TInput, TOutput> for that closed contract.");
        }

        var factory = typeof(OwnedBehaviorModuleBuilder)
            .GetMethod(nameof(CreateExecutionDelegateCore), BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("The behavior execution delegate factory could not be found.");
        var closedFactory = factory.MakeGenericMethod(behaviorType, inputType, outputType);
        return (Func<object, object, IBehaviorContext, CancellationToken, Task<object?>>)closedFactory.Invoke(null, null)!;
    }

    private static Func<object, object, IBehaviorContext, CancellationToken, Task<object?>> CreateExecutionDelegateCore<TBehavior, TInput, TOutput>()
        where TBehavior : class, IAppBehavior<TInput, TOutput>
        where TInput : notnull
        => CreateExecutionDelegate<TBehavior, TInput, TOutput>();

    private static string NormalizeRequired(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A non-empty module id is required.", nameof(value));
        }

        return value.Trim();
    }
}
