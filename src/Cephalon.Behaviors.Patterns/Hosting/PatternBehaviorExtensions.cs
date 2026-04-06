using Cephalon.Behaviors.Patterns.Abstractions;
using Cephalon.Behaviors.Patterns.Registry;
using Cephalon.Behaviors.Patterns.Stores;
using Cephalon.Behaviors.Patterns.Strategies;
using Cephalon.Behaviors.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Behaviors.Patterns.Hosting;

/// <summary>
/// Extension methods for registering M4 pattern execution strategies with the behavior collection builder.
/// </summary>
public static class PatternBehaviorExtensions
{
    /// <summary>
    /// Registers all five built-in pattern execution strategies, their default in-memory stores,
    /// and the <see cref="ExecutionStrategyRegistry"/> as singletons on the service collection.
    /// </summary>
    /// <param name="builder">The behavior collection builder to configure.</param>
    /// <returns>The same builder for fluent chaining.</returns>
    public static IBehaviorCollectionBuilder AddBehaviorPatterns(
        this IBehaviorCollectionBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Register default stores.
        builder.Services.AddSingleton<ISagaStateStore, InMemorySagaStateStore>();
        builder.Services.AddSingleton<IProcessCheckpointStore, InMemoryProcessCheckpointStore>();

        // Register all five strategies.
        builder.Services.AddSingleton<IBehaviorExecutionStrategy, CqrsExecutionStrategy>();
        builder.Services.AddSingleton<IBehaviorExecutionStrategy, EventDrivenExecutionStrategy>();
        builder.Services.AddSingleton<IBehaviorExecutionStrategy, SagaExecutionStrategy>();
        builder.Services.AddSingleton<IBehaviorExecutionStrategy, ProcessManagerExecutionStrategy>();
        builder.Services.AddSingleton<IBehaviorExecutionStrategy, DirectExecutionStrategy>();

        // Register the registry — resolved from all IBehaviorExecutionStrategy registrations.
        builder.Services.AddSingleton<ExecutionStrategyRegistry>(sp =>
        {
            var strategies = sp.GetServices<IBehaviorExecutionStrategy>();
            return new ExecutionStrategyRegistry(strategies);
        });

        return builder;
    }
}
