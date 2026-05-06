using Cephalon.Behaviors.Patterns.Abstractions;
using Cephalon.Behaviors.Patterns.Publishers;
using Cephalon.Behaviors.Patterns.Registry;
using Cephalon.Behaviors.Patterns.Runtime;
using Cephalon.Behaviors.Patterns.Stores;
using Cephalon.Behaviors.Patterns.Strategies;
using Cephalon.Behaviors.Services;
using Cephalon.Abstractions.Behaviors;
using Cephalon.Abstractions.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cephalon.Behaviors.Patterns.Hosting;

/// <summary>
/// Extension methods for registering pattern execution strategies with the behavior collection builder.
/// </summary>
public static class PatternBehaviorExtensions
{
    /// <summary>
    /// Registers all built-in pattern execution strategies, their default in-memory stores,
    /// the default in-memory choreography publisher fallback,
    /// and the <see cref="ExecutionStrategyRegistry"/> on the service collection.
    /// </summary>
    /// <param name="builder">The behavior collection builder to configure.</param>
    /// <returns>The same builder for fluent chaining.</returns>
    public static IBehaviorCollectionBuilder AddBehaviorPatterns(
        this IBehaviorCollectionBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Register default stores and the in-memory choreography publisher only as fallbacks so
        // explicit host registrations or bridge packages can remain authoritative.
        builder.Services.TryAddSingleton<ISagaStateStore, InMemorySagaStateStore>();
        builder.Services.TryAddSingleton<IProcessCheckpointStore, InMemoryProcessCheckpointStore>();
        builder.Services.TryAddSingleton<ISagaChoreographyPublisher, InMemorySagaChoreographyPublisher>();
        builder.Services.TryAddSingleton<ISagaChoreographyRuntimeCatalog>(static serviceProvider =>
            new SagaChoreographyRuntimeCatalogSnapshot(
                serviceProvider.GetRequiredService<IBehaviorCatalog>(),
                serviceProvider.GetRequiredService<IBehaviorTypeRegistry>(),
                serviceProvider.GetServices<SagaChoreographyRuntimeSlot>()));
        builder.Services.TryAddSingleton<SagaChoreographyPublicationRuntimeStateCatalog>();
        builder.Services.TryAddSingleton<ISagaChoreographyPublicationRuntimeStateCatalog>(static serviceProvider =>
            serviceProvider.GetRequiredService<SagaChoreographyPublicationRuntimeStateCatalog>());
        builder.Services.TryAddSingleton<ISagaChoreographyPublicationRuntimeReporter>(static serviceProvider =>
            serviceProvider.GetRequiredService<SagaChoreographyPublicationRuntimeStateCatalog>());
        builder.Services.TryAddSingleton<IDurableExecutionRuntimeCatalog>(static serviceProvider =>
            new DurableExecutionRuntimeCatalogSnapshot(
                serviceProvider.GetRequiredService<IBehaviorCatalog>(),
                serviceProvider.GetRequiredService<IBehaviorTypeRegistry>(),
                serviceProvider.GetServices<DurableExecutionSlot>()));
        builder.Services.TryAddSingleton<DurableExecutionRuntimeStateCatalog>();
        builder.Services.TryAddSingleton<IDurableExecutionRuntimeStateCatalog>(static serviceProvider =>
            serviceProvider.GetRequiredService<DurableExecutionRuntimeStateCatalog>());
        builder.Services.TryAddSingleton<IDurableExecutionRuntimeReporter>(static serviceProvider =>
            serviceProvider.GetRequiredService<DurableExecutionRuntimeStateCatalog>());

        // Register the built-in strategies.
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IBehaviorExecutionStrategy, CqrsExecutionStrategy>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IBehaviorExecutionStrategy, EventDrivenExecutionStrategy>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IBehaviorExecutionStrategy, SagaExecutionStrategy>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IBehaviorExecutionStrategy, ChoreographySagaExecutionStrategy>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IBehaviorExecutionStrategy, ProcessManagerExecutionStrategy>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IBehaviorExecutionStrategy, DurableExecutionStrategy>(static serviceProvider =>
            new DurableExecutionStrategy(
                serviceProvider.GetService<IDurableExecutionRuntimeStateCatalog>(),
                serviceProvider.GetServices<DurableExecutionSlot>())));
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IBehaviorExecutionStrategy, DirectExecutionStrategy>());

        // Register the registry — resolved from all IBehaviorExecutionStrategy registrations.
        builder.Services.TryAddSingleton<ExecutionStrategyRegistry>(sp =>
        {
            var strategies = sp.GetServices<IBehaviorExecutionStrategy>();
            return new ExecutionStrategyRegistry(strategies);
        });

        return builder;
    }
}
