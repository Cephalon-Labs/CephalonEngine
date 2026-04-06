namespace Cephalon.Behaviors.Messaging.Abstractions;

/// <summary>
/// Provides lookup and enumeration of all registered <see cref="IMessagingBehaviorBinding" /> instances.
/// </summary>
public interface IMessagingBehaviorBindingRegistry
{
    /// <summary>
    /// Returns the binding registered for <paramref name="transportId" />,
    /// or <see langword="null" /> if none is registered.
    /// </summary>
    /// <param name="transportId">The canonical transport identifier, e.g. <c>"rabbitmq"</c>.</param>
    /// <returns>The matching binding, or <see langword="null" />.</returns>
    IMessagingBehaviorBinding? GetBinding(string transportId);

    /// <summary>
    /// Gets all registered bindings in registration order.
    /// </summary>
    IReadOnlyList<IMessagingBehaviorBinding> All { get; }
}
