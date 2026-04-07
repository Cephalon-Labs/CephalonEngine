using Cephalon.Behaviors.Modules;
using Cephalon.Behaviors.Services;
using Cephalon.Engine.Composition;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Behaviors.Hosting;

/// <summary>Extends <see cref="EngineBuilder"/> with behavior topology registration.</summary>
public static class BehaviorEngineBuilderExtensions
{
    /// <summary>
    /// Adds the behavior topology system to the engine, including catalog, dispatcher, compatibility matrix, and built-in rules.
    /// </summary>
    /// <param name="engine">The engine builder to extend.</param>
    /// <param name="configure">An optional callback to register behaviors fluently.</param>
    /// <returns>The same engine builder for fluent composition.</returns>
    public static EngineBuilder AddBehaviors(
        this EngineBuilder engine,
        Action<IBehaviorCollectionBuilder>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(engine);

        var configuration = ResolveConfiguration(engine.Services);
        engine.AddModule(new BehaviorModule(configuration, configureOptions: null, configureBehaviors: configure));

        return engine;
    }

    /// <summary>
    /// Adds the behavior topology system to the engine with custom options and behavior registration.
    /// </summary>
    /// <param name="engine">The engine builder to extend.</param>
    /// <param name="configureOptions">An optional callback to configure behavior topology options.</param>
    /// <param name="configure">An optional callback to register behaviors fluently.</param>
    /// <returns>The same engine builder for fluent composition.</returns>
    public static EngineBuilder AddBehaviors(
        this EngineBuilder engine,
        Action<Cephalon.Behaviors.Configuration.BehaviorOptions>? configureOptions,
        Action<IBehaviorCollectionBuilder>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(engine);

        var configuration = ResolveConfiguration(engine.Services);
        engine.AddModule(new BehaviorModule(configuration, configureOptions, configure));

        return engine;
    }

    /// <summary>
    /// Resolves <see cref="IConfiguration"/> from the service collection if it has been registered
    /// (e.g. by ASP.NET Core or the generic host). Returns <see langword="null"/> when unavailable.
    /// </summary>
    private static IConfiguration? ResolveConfiguration(IServiceCollection services)
    {
        // IConfiguration is registered as a singleton instance by the host builder
        var descriptor = services.FirstOrDefault(sd =>
            sd.ServiceType == typeof(IConfiguration) &&
            sd.ImplementationInstance is not null);

        return descriptor?.ImplementationInstance as IConfiguration;
    }
}
