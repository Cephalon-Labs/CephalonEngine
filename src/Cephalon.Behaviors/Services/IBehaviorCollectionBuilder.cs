using System.Diagnostics.CodeAnalysis;
using Cephalon.Behaviors.Builders;
using Cephalon.Abstractions.Behaviors;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Behaviors.Services;

/// <summary>
/// Provides a fluent API for registering application behaviors with their
/// topology, DI lifetime, and type registry entries.
/// </summary>
public interface IBehaviorCollectionBuilder
{
    /// <summary>
    /// Gets the underlying <see cref="IServiceCollection" /> so that transport packs
    /// and other extensions can register their own services.
    /// </summary>
    IServiceCollection Services { get; }

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
    IBehaviorCollectionBuilder Register<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        TBehavior>(
        Action<BehaviorTopologyBuilder>? configureTopology = null)
        where TBehavior : class;

    /// <summary>
    /// Registers a behavior and its closed generic execution slot with the runtime.
    /// </summary>
    /// <typeparam name="TBehavior">
    /// The concrete behavior type. Must be decorated with <see cref="AppBehaviorAttribute" />
    /// and implement <see cref="IAppBehavior{TIn,TOut}" /> for the supplied input and output types.
    /// </typeparam>
    /// <typeparam name="TInput">The behavior input contract.</typeparam>
    /// <typeparam name="TOutput">The behavior output contract.</typeparam>
    /// <param name="configureTopology">
    /// An optional callback that configures the behavior's transport topology.
    /// When <see langword="null" />, topology is resolved from configuration.
    /// </param>
    /// <returns>The same builder for fluent chaining.</returns>
    IBehaviorCollectionBuilder Register<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        TBehavior,
        TInput,
        TOutput>(
        Action<BehaviorTopologyBuilder>? configureTopology = null)
        where TBehavior : class, IAppBehavior<TInput, TOutput>
        where TInput : notnull;
}
