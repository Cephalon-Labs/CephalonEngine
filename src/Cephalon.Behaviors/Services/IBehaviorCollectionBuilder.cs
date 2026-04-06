using Cephalon.Behaviors.Builders;

namespace Cephalon.Behaviors.Services;

/// <summary>
/// Provides a fluent API for registering application behaviors with their
/// topology, DI lifetime, and type registry entries.
/// </summary>
public interface IBehaviorCollectionBuilder
{
    /// <summary>
    /// Registers a behavior of type <typeparamref name="TBehavior" /> with the runtime.
    /// </summary>
    /// <typeparam name="TBehavior">
    /// The concrete behavior type. Must be decorated with <see cref="Cephalon.Abstractions.Behaviors.AppBehaviorAttribute" />
    /// and implement <see cref="Cephalon.Abstractions.Behaviors.IAppBehavior{TIn,TOut}" />.
    /// </typeparam>
    /// <param name="configureTopology">
    /// An optional callback that configures the behavior's transport topology.
    /// When <see langword="null" />, topology is resolved from configuration.
    /// </param>
    /// <returns>The same builder for fluent chaining.</returns>
    IBehaviorCollectionBuilder Register<TBehavior>(
        Action<BehaviorTopologyBuilder>? configureTopology = null)
        where TBehavior : class;
}
