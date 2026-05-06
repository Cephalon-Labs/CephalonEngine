using System;

namespace Cephalon.Abstractions.Behaviors;

/// <summary>
/// Collects behavior ownership declarations contributed by a Cephalon module.
/// </summary>
/// <remarks>
/// This builder is host-agnostic and only declares which behaviors a module owns.
/// Public REST exposure stays in host adapters such as ASP.NET Core.
/// </remarks>
public interface IBehaviorModuleBuilder
{
    /// <summary>
    /// Declares that the current module owns the specified behavior type.
    /// </summary>
    /// <param name="behaviorType">The concrete behavior type owned by the module.</param>
    /// <returns>The same builder for fluent ownership registration.</returns>
    IBehaviorModuleBuilder Add(Type behaviorType);

    /// <summary>
    /// Declares that the current module owns the specified behavior type and supplies an explicit topology override.
    /// </summary>
    /// <param name="behaviorType">The concrete behavior type owned by the module.</param>
    /// <param name="configureTopology">
    /// The callback that selects the resolved behavior topology when attribute-only synthesis is not enough.
    /// </param>
    /// <returns>The same builder for fluent ownership registration.</returns>
    IBehaviorModuleBuilder Add(Type behaviorType, Action<IBehaviorTopologyBuilder> configureTopology);

    /// <summary>
    /// Declares that the current module owns the specified behavior.
    /// </summary>
    /// <typeparam name="TBehavior">The concrete behavior type owned by the module.</typeparam>
    /// <returns>The same builder for fluent ownership registration.</returns>
    IBehaviorModuleBuilder Add<TBehavior>()
        where TBehavior : class;

    /// <summary>
    /// Declares that the current module owns the specified behavior and supplies a closed execution slot.
    /// </summary>
    /// <typeparam name="TBehavior">The concrete behavior type owned by the module.</typeparam>
    /// <typeparam name="TInput">The behavior input contract.</typeparam>
    /// <typeparam name="TOutput">The behavior output contract.</typeparam>
    /// <returns>The same builder for fluent ownership registration.</returns>
    IBehaviorModuleBuilder Add<TBehavior, TInput, TOutput>()
        where TBehavior : class, IAppBehavior<TInput, TOutput>
        where TInput : notnull;

    /// <summary>
    /// Declares that the current module owns the specified behavior and supplies an explicit topology override.
    /// </summary>
    /// <typeparam name="TBehavior">The concrete behavior type owned by the module.</typeparam>
    /// <param name="configureTopology">
    /// The callback that selects the resolved behavior topology when attribute-only synthesis is not enough.
    /// </param>
    /// <returns>The same builder for fluent ownership registration.</returns>
    IBehaviorModuleBuilder Add<TBehavior>(Action<IBehaviorTopologyBuilder> configureTopology)
        where TBehavior : class;

    /// <summary>
    /// Declares that the current module owns the specified behavior, supplies a closed execution slot,
    /// and applies an explicit topology override.
    /// </summary>
    /// <typeparam name="TBehavior">The concrete behavior type owned by the module.</typeparam>
    /// <typeparam name="TInput">The behavior input contract.</typeparam>
    /// <typeparam name="TOutput">The behavior output contract.</typeparam>
    /// <param name="configureTopology">
    /// The callback that selects the resolved behavior topology when attribute-only synthesis is not enough.
    /// </param>
    /// <returns>The same builder for fluent ownership registration.</returns>
    IBehaviorModuleBuilder Add<TBehavior, TInput, TOutput>(Action<IBehaviorTopologyBuilder> configureTopology)
        where TBehavior : class, IAppBehavior<TInput, TOutput>
        where TInput : notnull;
}
