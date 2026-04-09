using Cephalon.Abstractions.Behaviors;

namespace Cephalon.Behaviors.Http.Hosting;

/// <summary>
/// Collects behavior ownership and public REST exposure for a <see cref="RestBehaviorModuleBase" />.
/// </summary>
/// <remarks>
/// Public REST behavior routes automatically imply module ownership. Use <see cref="Own{TBehavior}()"/>
/// or <see cref="Own{TBehavior}(Action{IBehaviorTopologyBuilder})"/> when a module also owns
/// internal-only behaviors or behaviors that will be exposed through a custom/manual route path.
/// </remarks>
public interface IRestBehaviorModuleBuilder
{
    /// <summary>
    /// Declares that the current REST-capable module owns the specified behavior without exposing it
    /// through the default REST route builder.
    /// </summary>
    /// <typeparam name="TBehavior">The concrete behavior type owned by the module.</typeparam>
    /// <returns>The same builder for fluent authoring.</returns>
    IRestBehaviorModuleBuilder Own<TBehavior>()
        where TBehavior : class;

    /// <summary>
    /// Declares that the current REST-capable module owns the specified behavior and applies an
    /// explicit topology override during behavior registration.
    /// </summary>
    /// <typeparam name="TBehavior">The concrete behavior type owned by the module.</typeparam>
    /// <param name="configureTopology">The explicit topology selection callback.</param>
    /// <returns>The same builder for fluent authoring.</returns>
    IRestBehaviorModuleBuilder Own<TBehavior>(Action<IBehaviorTopologyBuilder> configureTopology)
        where TBehavior : class;

    /// <summary>
    /// Creates a REST route group that can map one or more behavior-backed endpoints.
    /// </summary>
    /// <param name="prefix">The public route prefix relative to the host REST root.</param>
    /// <returns>A builder used to describe the group's public REST endpoints.</returns>
    IRestBehaviorEndpointGroupBuilder Group(string prefix);
}
