using Cephalon.Behaviors.Messaging.Abstractions;
using Cephalon.Behaviors.Messaging.Bindings;
using Cephalon.Behaviors.Messaging.Options;
using Cephalon.Behaviors.Messaging.Registry;
using Cephalon.Behaviors.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cephalon.Behaviors.Messaging.Hosting;

/// <summary>
/// Extension methods that register ABT M3 messaging transport bindings into the DI container.
/// </summary>
public static class MessagingBehaviorBindingServiceCollectionExtensions
{
    /// <summary>
    /// Returns a <see cref="MessagingBehaviorBindingsBuilder" /> that auto-binds transport options
    /// from the supplied <paramref name="configuration" /> under the <c>Engine:Messaging</c> section.
    /// </summary>
    /// <param name="builder">The behavior collection builder.</param>
    /// <param name="configuration">
    /// The application configuration. Each transport reads its options from a conventional section
    /// (e.g. <c>Engine:Messaging:RabbitMQ</c>, <c>Engine:Messaging:Kafka</c>).
    /// </param>
    /// <returns>A fluent builder for registering individual messaging transports.</returns>
    /// <example>
    /// <code>
    /// // Options are auto-bound from config — no manual Bind() needed
    /// behaviors.AddMessagingBehaviorBindings(config)
    ///     .AddInMemory()
    ///     .AddRabbitMq()
    ///     .AddKafka();
    /// </code>
    /// </example>
    public static MessagingBehaviorBindingsBuilder AddMessagingBehaviorBindings(
        this IBehaviorCollectionBuilder builder,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);

        builder.Services.TryAddSingleton<IMessagingBehaviorBindingRegistry>(sp =>
            new MessagingBehaviorBindingRegistry(sp.GetServices<IMessagingBehaviorBinding>()));

        return new MessagingBehaviorBindingsBuilder(builder.Services, configuration);
    }

    /// <summary>
    /// Returns a <see cref="MessagingBehaviorBindingsBuilder" /> that lets you selectively
    /// register only the messaging transports your application requires.
    /// </summary>
    /// <param name="builder">The behavior collection builder.</param>
    /// <returns>A fluent builder for registering individual messaging transports.</returns>
    /// <remarks>
    /// Transport options use their built-in defaults. To auto-bind from configuration,
    /// use the <see cref="AddMessagingBehaviorBindings(IBehaviorCollectionBuilder, IConfiguration)" />
    /// overload instead.
    /// </remarks>
    public static MessagingBehaviorBindingsBuilder AddMessagingBehaviorBindings(
        this IBehaviorCollectionBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.TryAddSingleton<IMessagingBehaviorBindingRegistry>(sp =>
            new MessagingBehaviorBindingRegistry(sp.GetServices<IMessagingBehaviorBinding>()));

        return new MessagingBehaviorBindingsBuilder(builder.Services);
    }

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

        builder.AddMessagingBehaviorBindings()
            .AddInMemory(configureInMemory)
            .AddRabbitMq(configureRabbitMq)
            .AddKafka(configureKafka);

        return builder;
    }
}
