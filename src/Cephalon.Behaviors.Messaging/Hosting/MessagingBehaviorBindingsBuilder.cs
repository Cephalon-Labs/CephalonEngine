using Cephalon.Behaviors.Messaging.Abstractions;
using Cephalon.Behaviors.Messaging.Bindings;
using Cephalon.Behaviors.Messaging.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cephalon.Behaviors.Messaging.Hosting;

/// <summary>
/// Fluent builder for selectively registering messaging transport bindings.
/// </summary>
/// <remarks>
/// <para>
/// Use this builder to register only the transports your application actually requires
/// instead of registering all built-in transports unconditionally.
/// </para>
/// <para>
/// When an <see cref="IConfiguration" /> instance is available, each transport automatically
/// binds its options from the conventional configuration section (<c>Engine:Messaging:{Transport}</c>).
/// An optional <see cref="Action{T}" /> callback can be passed to override or extend the bound values.
/// </para>
/// <code>
/// // Auto-bind from config — no manual mapping needed
/// behaviors.AddMessagingBehaviorBindings(config)
///     .AddInMemory()
///     .AddRabbitMq()
///     .AddKafka();
///
/// // Override specific values after config bind
/// behaviors.AddMessagingBehaviorBindings(config)
///     .AddRabbitMq(rmq => rmq.VirtualHost = "/custom");
/// </code>
/// </remarks>
public sealed class MessagingBehaviorBindingsBuilder
{
    private readonly IServiceCollection _services;
    private readonly IConfiguration? _configuration;

    internal MessagingBehaviorBindingsBuilder(IServiceCollection services, IConfiguration? configuration = null)
    {
        _services = services;
        _configuration = configuration;
    }

    /// <summary>
    /// Registers the in-memory messaging transport binding.
    /// Options are bound from <c>Engine:Messaging:InMemory</c> when configuration is available.
    /// </summary>
    /// <param name="configure">Optional callback to override or extend configuration-bound options.</param>
    /// <returns>The same builder for fluent chaining.</returns>
    public MessagingBehaviorBindingsBuilder AddInMemory(Action<InMemoryTransportOptions>? configure = null)
    {
        var options = new InMemoryTransportOptions();
        _configuration?.GetSection("Engine:Messaging:InMemory").Bind(options);
        configure?.Invoke(options);

        _services.TryAddSingleton(options);
        _services.TryAddSingleton<InMemoryTransportBinding>();
        _services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IMessagingBehaviorBinding, InMemoryTransportBinding>());

        return this;
    }

    /// <summary>
    /// Registers the RabbitMQ messaging transport binding.
    /// Options are bound from <c>Engine:Messaging:RabbitMQ</c> when configuration is available.
    /// </summary>
    /// <param name="configure">Optional callback to override or extend configuration-bound options.</param>
    /// <returns>The same builder for fluent chaining.</returns>
    public MessagingBehaviorBindingsBuilder AddRabbitMq(Action<RabbitMqTransportOptions>? configure = null)
    {
        var options = new RabbitMqTransportOptions();
        _configuration?.GetSection("Engine:Messaging:RabbitMQ").Bind(options);
        configure?.Invoke(options);

        _services.TryAddSingleton(options);
        _services.TryAddSingleton<RabbitMqTransportBinding>();
        _services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IMessagingBehaviorBinding, RabbitMqTransportBinding>());

        return this;
    }

    /// <summary>
    /// Registers the Kafka messaging transport binding.
    /// Options are bound from <c>Engine:Messaging:Kafka</c> when configuration is available.
    /// </summary>
    /// <param name="configure">Optional callback to override or extend configuration-bound options.</param>
    /// <returns>The same builder for fluent chaining.</returns>
    public MessagingBehaviorBindingsBuilder AddKafka(Action<KafkaTransportOptions>? configure = null)
    {
        var options = new KafkaTransportOptions();
        _configuration?.GetSection("Engine:Messaging:Kafka").Bind(options);
        configure?.Invoke(options);

        _services.TryAddSingleton(options);
        _services.TryAddSingleton<KafkaTransportBinding>();
        _services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IMessagingBehaviorBinding, KafkaTransportBinding>());

        return this;
    }
}
