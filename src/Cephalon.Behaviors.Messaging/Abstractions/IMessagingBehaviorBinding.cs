using Cephalon.Abstractions.Behaviors;
using Cephalon.Behaviors.Services;

namespace Cephalon.Behaviors.Messaging.Abstractions;

/// <summary>
/// Adapts a messaging transport to the ABT behavior dispatcher.
/// Each transport variant (InMemory, RabbitMQ, Kafka, etc.) implements this interface
/// and is registered in the <see cref="IMessagingBehaviorBindingRegistry" />.
/// </summary>
public interface IMessagingBehaviorBinding
{
    /// <summary>
    /// Gets the canonical transport identifier (e.g. <c>"rabbitmq"</c>, <c>"kafka"</c>, <c>"in-memory"</c>).
    /// </summary>
    string TransportId { get; }

    /// <summary>
    /// Starts consuming messages for the given behavior topology and dispatches them.
    /// Called once per descriptor per transport; implementations should be idempotent.
    /// </summary>
    /// <param name="descriptor">The behavior topology descriptor.</param>
    /// <param name="dispatcher">The behavior dispatcher to invoke for each received message.</param>
    /// <param name="ct">A token that signals when the host is shutting down.</param>
    /// <returns>A task that completes when the binding is fully started.</returns>
    Task StartAsync(BehaviorTopologyDescriptor descriptor, BehaviorDispatcher dispatcher, CancellationToken ct);

    /// <summary>
    /// Stops the binding and releases transport resources.
    /// </summary>
    /// <param name="ct">A token that cancels the graceful-stop wait.</param>
    /// <returns>A task that completes when the binding has fully stopped.</returns>
    Task StopAsync(CancellationToken ct);
}
