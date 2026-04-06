namespace Cephalon.Behaviors.Http.Abstractions;

/// <summary>
/// Provides lookup and enumeration of all registered <see cref="IHttpBehaviorBinding" /> instances.
/// </summary>
public interface IHttpBehaviorBindingRegistry
{
    /// <summary>
    /// Returns the binding registered for <paramref name="transportId" />,
    /// or <see langword="null" /> if none is registered.
    /// </summary>
    /// <param name="transportId">The canonical transport identifier, e.g. <c>http.rest</c>.</param>
    /// <returns>The matching binding, or <see langword="null" />.</returns>
    IHttpBehaviorBinding? GetBinding(string transportId);

    /// <summary>
    /// Gets all registered bindings in registration order.
    /// </summary>
    IReadOnlyList<IHttpBehaviorBinding> All { get; }
}
