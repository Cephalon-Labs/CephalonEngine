using Cephalon.Behaviors.Messaging.Abstractions;
using Cephalon.Behaviors.Messaging.Bindings;
using Cephalon.Behaviors.Messaging.Options;
using Cephalon.Behaviors.Messaging.Registry;
using Cephalon.Behaviors.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cephalon.Behaviors.Messaging.Hosting;

/// <summary>
/// Extension methods that register ABT M3 messaging transport bindings into the DI container.
/// </summary>
public static class MessagingBehaviorBindingServiceCollectionExtensions
{
    /// <summary>
    /// Registers all built-in messaging transport bindings (InMemory, RabbitMQ, Kafka)
    /// and the <see cref="IMessagingBehaviorBindingRegistry" /> as singleton services.
    /// </summary>
    /// <param name="builder">The behavior collection builder.</param>
    /// <param name="configureInMemory">Optional callback to configure in-memory transport options.</param>
    /// <param name="configureRabbitMq">Optional callback to configure RabbitMQ transport options.</param>
    /// <param name="configureKafka">Optional callback to configure Kafka transport options.</param>
    /// <returns>The same builder for fluent chaining.</returns>
    public static IBehaviorCollectionBuilder AddMessagingBehaviorBindings(
        this IBehaviorCollectionBuilder builder,
        Action<InMemoryTransportOptions>? configureInMemory = null,
        Action<RabbitMqTransportOptions>? configureRabbitMq = null,
        Action<KafkaTransportOptions>? configureKafka = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var services = builder.Services;

        // Configure options
        var inMemoryOptions = new InMemoryTransportOptions();
        configureInMemory?.Invoke(inMemoryOptions);

        var rabbitMqOptions = new RabbitMqTransportOptions();
        configureRabbitMq?.Invoke(rabbitMqOptions);

        var kafkaOptions = new KafkaTransportOptions();
        configureKafka?.Invoke(kafkaOptions);

        services.TryAddSingleton(inMemoryOptions);
        services.TryAddSingleton(rabbitMqOptions);
        services.TryAddSingleton(kafkaOptions);

        // GEN-03: register all bindings as Singleton
        services.TryAddSingleton<InMemoryTransportBinding>();
        services.TryAddSingleton<RabbitMqTransportBinding>();
        services.TryAddSingleton<KafkaTransportBinding>();

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IMessagingBehaviorBinding, InMemoryTransportBinding>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IMessagingBehaviorBinding, RabbitMqTransportBinding>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IMessagingBehaviorBinding, KafkaTransportBinding>());

        // Registry
        services.TryAddSingleton<IMessagingBehaviorBindingRegistry>(sp =>
            new MessagingBehaviorBindingRegistry(sp.GetServices<IMessagingBehaviorBinding>()));

        return builder;
    }
}
