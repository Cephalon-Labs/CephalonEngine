using Cephalon.Abstractions.Behaviors;
using Cephalon.Behaviors.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Behaviors.Hosting;

/// <summary>Implementation of <see cref="IBehaviorCollectionBuilder"/> for configuring the behavior collection.</summary>
internal sealed class BehaviorCollectionBuilder : IBehaviorCollectionBuilder
{
    private readonly IServiceCollection _services;

    /// <summary>Initializes a new instance of <see cref="BehaviorCollectionBuilder"/>.</summary>
    public BehaviorCollectionBuilder(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        _services = services;
    }

    /// <inheritdoc />
    public IBehaviorCollectionBuilder WithDefaults(Action<BehaviorDefaultsBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var defaults = new BehaviorDefaultsBuilder();
        configure(defaults);

        // Register a contributor that applies the fluent defaults to the resolver
        _services.AddSingleton<IBehaviorContributor>(
            new FluentDefaultsContributor(defaults.Pattern, defaults.Transports));

        return this;
    }

    /// <inheritdoc />
    public IBehaviorCollectionBuilder Register<TBehavior>(Action<IBehaviorTopologyBuilder>? configure = null)
        where TBehavior : class
    {
        _services.AddSingleton<TBehavior>();
        _services.AddSingleton<IBehaviorContributor>(sp =>
            new FluentBehaviorContributor<TBehavior>(configure));
        return this;
    }
}
