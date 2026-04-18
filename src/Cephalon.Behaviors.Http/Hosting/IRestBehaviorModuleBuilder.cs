using Cephalon.Abstractions.Behaviors;

namespace Cephalon.Behaviors.Http.Hosting;

/// <summary>
/// Collects behavior ownership and public REST exposure for a <see cref="RestBehaviorModuleBase" />.
/// </summary>
/// <remarks>
/// Public REST behavior routes automatically imply module ownership. Use
/// <see cref="Internal{TBehavior}()"/> or
/// <see cref="Internal{TBehavior}(Action{IBehaviorTopologyBuilder})"/> when a module also owns
/// internal-only behaviors or behaviors that will be exposed through a custom/manual route path.
/// </remarks>
public interface IRestBehaviorModuleBuilder
{
    /// <summary>
    /// Declares that the current REST-capable module owns the specified behavior as an internal or
    /// custom/manual-route behavior without exposing it through the default REST route builder.
    /// </summary>
    /// <typeparam name="TBehavior">The concrete behavior type owned by the module.</typeparam>
    /// <returns>The same builder for fluent authoring.</returns>
    IRestBehaviorModuleBuilder Internal<TBehavior>()
        where TBehavior : class;

    /// <summary>
    /// Declares that the current REST-capable module owns the specified behavior as an internal or
    /// custom/manual-route behavior and applies an explicit topology override during registration.
    /// </summary>
    /// <typeparam name="TBehavior">The concrete behavior type owned by the module.</typeparam>
    /// <param name="configureTopology">The explicit topology selection callback.</param>
    /// <returns>The same builder for fluent authoring.</returns>
    IRestBehaviorModuleBuilder Internal<TBehavior>(Action<IBehaviorTopologyBuilder> configureTopology)
        where TBehavior : class;

    /// <summary>
    /// Creates a REST route group that can map one or more behavior-backed endpoints.
    /// </summary>
    /// <param name="prefix">The public route prefix relative to the host REST root.</param>
    /// <returns>A builder used to describe the group's public REST endpoints.</returns>
    IRestBehaviorEndpointGroupBuilder Group(string prefix);

    /// <summary>
    /// Creates a REST route group whose public path is derived from a dot-separated behavior-id prefix.
    /// </summary>
    /// <param name="behaviorIdPrefix">
    /// The behavior-id prefix whose segments become the route-group path, such as
    /// <c>showcase.cart</c> becoming <c>/showcase/cart</c>.
    /// </param>
    /// <returns>A builder used to describe the group's public REST endpoints.</returns>
    /// <remarks>
    /// Use this helper for the common generated-profile path where the public route group should
    /// mirror the owning behavior-id prefix while remaining an explicit module-owned REST surface.
    /// </remarks>
    IRestBehaviorEndpointGroupBuilder GroupFromBehaviorIdPrefix(string behaviorIdPrefix);

    /// <summary>
    /// Maps matching generated REST profiles from the owning module assembly into one or more
    /// derived route groups.
    /// </summary>
    /// <param name="behaviorIdPrefix">
    /// The root behavior-id prefix used to select generated REST profiles from the owning module
    /// assembly.
    /// </param>
    /// <returns>The same builder for fluent authoring.</returns>
    /// <remarks>
    /// This broader low-code opt-in remains explicit and module-owned. Cephalon groups matching
    /// behavior ids by their parent prefix, so behaviors such as <c>showcase.orders.lookup</c> and
    /// <c>showcase.orders.create</c> share one derived route group while
    /// <c>showcase.inventory.lookup</c> lands in another. Use
    /// <see cref="GroupFromBehaviorIdPrefix(string)" /> plus
    /// <see cref="IRestBehaviorEndpointGroupBuilder.MapGeneratedProfiles(string)" /> when each
    /// generated route group should still be declared manually.
    /// </remarks>
    IRestBehaviorModuleBuilder MapGeneratedProfileGroups(string behaviorIdPrefix);

    /// <summary>
    /// Maps matching generated REST profiles from the owning module assembly into one or more
    /// derived route groups while applying shared group-level conventions.
    /// </summary>
    /// <param name="behaviorIdPrefix">
    /// The root behavior-id prefix used to select generated REST profiles from the owning module
    /// assembly.
    /// </param>
    /// <param name="configureGroup">
    /// The optional callback applied to each derived route group before the generated profiles are
    /// mapped.
    /// </param>
    /// <returns>The same builder for fluent authoring.</returns>
    IRestBehaviorModuleBuilder MapGeneratedProfileGroups(
        string behaviorIdPrefix,
        Action<IRestBehaviorEndpointGroupBuilder> configureGroup);
}
