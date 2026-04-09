using Cephalon.Abstractions.Behaviors;

namespace Cephalon.Engine.Composition;

internal sealed class OwnedBehaviorModuleBuilder(string sourceModuleId) : IBehaviorModuleBuilder
{
    private static readonly Type AppBehaviorOpenGeneric = typeof(IAppBehavior<,>);
    private readonly string sourceModuleId = NormalizeRequired(sourceModuleId);
    private readonly List<OwnedBehaviorRegistration> registrations = [];

    public IBehaviorModuleBuilder Add<TBehavior>()
        where TBehavior : class
        => AddCore<TBehavior>(configureTopology: null);

    public IBehaviorModuleBuilder Add<TBehavior>(Action<IBehaviorTopologyBuilder> configureTopology)
        where TBehavior : class
    {
        ArgumentNullException.ThrowIfNull(configureTopology);
        return AddCore<TBehavior>(configureTopology);
    }

    internal IReadOnlyList<OwnedBehaviorRegistration> Build()
        => registrations.ToArray();

    private OwnedBehaviorModuleBuilder AddCore<TBehavior>(Action<IBehaviorTopologyBuilder>? configureTopology)
        where TBehavior : class
    {
        var behaviorType = typeof(TBehavior);
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
            configureTopology));

        return this;
    }

    private static string NormalizeRequired(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A non-empty module id is required.", nameof(value));
        }

        return value.Trim();
    }
}
