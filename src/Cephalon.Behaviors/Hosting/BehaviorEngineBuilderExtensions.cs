using Cephalon.Abstractions.Behaviors;
using Cephalon.Behaviors.Configuration;
using Cephalon.Behaviors.Rules;
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
    /// <param name="configure">An optional callback to register behaviors and configure defaults.</param>
    /// <returns>The same engine builder for fluent composition.</returns>
    public static EngineBuilder AddBehaviors(
        this EngineBuilder engine,
        Action<IBehaviorCollectionBuilder>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(engine);

        // Register config-driven defaults resolver
        engine.Services.AddSingleton<BehaviorTopologyResolver>(sp =>
        {
            var configuration = sp.GetService<IConfiguration>();
            var defaults = new BehaviorOptions();
            if (configuration is not null)
            {
                var section = configuration.GetSection(BehaviorOptions.DefaultsSectionName);
                var pattern = section[nameof(BehaviorOptions.Pattern)];
                if (!string.IsNullOrWhiteSpace(pattern))
                    defaults.Pattern = pattern;
                var transportSection = section.GetSection(nameof(BehaviorOptions.Transport));
                foreach (var t in transportSection.GetChildren())
                {
                    if (!string.IsNullOrWhiteSpace(t.Value))
                        defaults.Transport.Add(t.Value);
                }
            }
            return new BehaviorTopologyResolver(defaults);
        });

        // Register core services
        engine.Services.AddSingleton<IBehaviorCatalog, BehaviorCatalog>();
        engine.Services.AddSingleton<BehaviorDispatcher>();
        engine.Services.AddSingleton<CompatibilityMatrix>();

        // Register built-in rules
        engine.Services.AddSingleton<IBehaviorCompatibilityRule, Abt001SagaRequiresStatefulTransportRule>();
        engine.Services.AddSingleton<IBehaviorCompatibilityRule, Abt002EventDrivenWithHttpRestRule>();
        engine.Services.AddSingleton<IBehaviorCompatibilityRule, Abt003ProcessManagerRequiresInboxRule>();
        engine.Services.AddSingleton<IBehaviorCompatibilityRule, Abt004CqrsMultipleTransportsRule>();

        // Register config contributor
        engine.Services.AddSingleton<IBehaviorContributor, ConfigBehaviorContributor>();

        // Apply fluent overrides
        if (configure is not null)
        {
            var builder = new BehaviorCollectionBuilder(engine.Services);
            configure(builder);
        }

        return engine;
    }
}
