using Cephalon.Abstractions.Behaviors;
using Cephalon.Behaviors.Builders;
using Cephalon.Behaviors.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cephalon.Behaviors.Services;

/// <summary>
/// Default implementation of <see cref="IBehaviorCollectionBuilder" />.
/// Registers behavior types in DI, populates the type registry,
/// and optionally contributes a fluent topology descriptor at Layer 4.
/// </summary>
public sealed class BehaviorCollectionBuilder : IBehaviorCollectionBuilder
{
    private readonly BehaviorTypeRegistry _typeRegistry;

    /// <summary>
    /// Initializes the builder with the target service collection and shared type registry.
    /// </summary>
    /// <param name="services">The service collection to register behaviors into.</param>
    /// <param name="typeRegistry">The type registry to populate with behavior id-to-type mappings.</param>
    public BehaviorCollectionBuilder(IServiceCollection services, BehaviorTypeRegistry typeRegistry)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(typeRegistry);
        Services = services;
        _typeRegistry = typeRegistry;
    }

    /// <inheritdoc />
    public IServiceCollection Services { get; }

    /// <summary>
    /// Registers a behavior of type <typeparamref name="TBehavior" /> with the runtime.
    /// <list type="number">
    ///   <item><description>Resolves the behavior id from <see cref="AppBehaviorAttribute" />.</description></item>
    ///   <item><description>Registers <typeparamref name="TBehavior" /> as a transient service in DI.</description></item>
    ///   <item><description>Records the id-to-type mapping in the shared <see cref="BehaviorTypeRegistry" />.</description></item>
    ///   <item><description>When <paramref name="configureTopology" /> is provided, adds a <see cref="FluentBehaviorContributor" /> at Layer 4.</description></item>
    /// </list>
    /// </summary>
    /// <typeparam name="TBehavior">
    /// The concrete behavior type. Must be decorated with <see cref="AppBehaviorAttribute" />
    /// and implement <see cref="IAppBehavior{TIn,TOut}" />.
    /// </typeparam>
    /// <param name="configureTopology">
    /// An optional callback that configures the behavior's transport topology at Layer 4 (highest priority).
    /// When <see langword="null" />, topology is resolved from configuration layers only.
    /// </param>
    /// <returns>The same builder for fluent chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <typeparamref name="TBehavior" /> is not decorated with <see cref="AppBehaviorAttribute" />.
    /// </exception>
    public IBehaviorCollectionBuilder Register<TBehavior>(
        Action<BehaviorTopologyBuilder>? configureTopology = null)
        where TBehavior : class
    {
        var behaviorType = typeof(TBehavior);

        var attr = (AppBehaviorAttribute?)Attribute.GetCustomAttribute(
            behaviorType, typeof(AppBehaviorAttribute));

        if (attr is null)
        {
            throw new InvalidOperationException(
                $"Cannot register '{behaviorType.Name}': it is not decorated with [AppBehavior(id)].");
        }

        var behaviorId = attr.Id;

        // 1. Register the type in DI as transient
        Services.TryAddTransient<TBehavior>();

        // 2. Populate the type registry
        _typeRegistry.Register(behaviorId, behaviorType);

        // 3. If fluent topology provided, add a Layer-4 contributor
        BehaviorTopologyDescriptor? descriptor = null;
        if (configureTopology is not null)
        {
            var builder = new BehaviorTopologyBuilder();
            configureTopology(builder);
            descriptor = builder.Build(behaviorId);
        }

        descriptor = BehaviorRestTransportDeclarationResolver.Resolve(behaviorId, behaviorType, descriptor);
        if (descriptor is not null)
        {
            Services.AddSingleton<IBehaviorContributor>(new FluentBehaviorContributor(descriptor));
        }

        return this;
    }
}
