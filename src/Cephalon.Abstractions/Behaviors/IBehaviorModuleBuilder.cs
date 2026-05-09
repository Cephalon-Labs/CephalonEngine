using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;

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
    IBehaviorModuleBuilder Add(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        Type behaviorType);

    /// <summary>
    /// Declares that the current module owns the specified behavior and supplies the closed input/output contract.
    /// </summary>
    /// <remarks>
    /// This overload is intended for compatibility with descriptor-driven callers that only have <see cref="Type" />
    /// values. Trim-ready hosts should prefer the generic overload or generated descriptors so the behavior contract
    /// can be preserved statically.
    /// </remarks>
    /// <param name="behaviorType">The concrete behavior type owned by the module.</param>
    /// <param name="inputType">The closed behavior input contract type.</param>
    /// <param name="outputType">The closed behavior output contract type.</param>
    /// <returns>The same builder for fluent ownership registration.</returns>
    [RequiresDynamicCode("Type-based behavior ownership registration closes generic execution delegates at runtime. Prefer Add<TBehavior, TInput, TOutput>() or source-generated behavior descriptors for trim-ready hosts.")]
    [RequiresUnreferencedCode("Type-based behavior ownership registration uses runtime type and JSON contract information that cannot be statically preserved by the trimmer. Prefer Add<TBehavior, TInput, TOutput>() or source-generated behavior descriptors for trim-ready hosts.")]
    IBehaviorModuleBuilder Add(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        Type behaviorType,
        Type inputType,
        Type outputType);

    /// <summary>
    /// Declares that the current module owns the specified behavior type and supplies an explicit topology override.
    /// </summary>
    /// <param name="behaviorType">The concrete behavior type owned by the module.</param>
    /// <param name="configureTopology">
    /// The callback that selects the resolved behavior topology when attribute-only synthesis is not enough.
    /// </param>
    /// <returns>The same builder for fluent ownership registration.</returns>
    IBehaviorModuleBuilder Add(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        Type behaviorType,
        Action<IBehaviorTopologyBuilder> configureTopology);

    /// <summary>
    /// Declares that the current module owns the specified behavior, supplies the closed input/output contract,
    /// and applies an explicit topology override.
    /// </summary>
    /// <remarks>
    /// This overload is intended for compatibility with descriptor-driven callers that only have <see cref="Type" />
    /// values. Trim-ready hosts should prefer the generic overload or generated descriptors so the behavior contract
    /// can be preserved statically.
    /// </remarks>
    /// <param name="behaviorType">The concrete behavior type owned by the module.</param>
    /// <param name="inputType">The closed behavior input contract type.</param>
    /// <param name="outputType">The closed behavior output contract type.</param>
    /// <param name="configureTopology">
    /// The callback that selects the resolved behavior topology when attribute-only synthesis is not enough.
    /// </param>
    /// <returns>The same builder for fluent ownership registration.</returns>
    [RequiresDynamicCode("Type-based behavior ownership registration closes generic execution delegates at runtime. Prefer Add<TBehavior, TInput, TOutput>() or source-generated behavior descriptors for trim-ready hosts.")]
    [RequiresUnreferencedCode("Type-based behavior ownership registration uses runtime type and JSON contract information that cannot be statically preserved by the trimmer. Prefer Add<TBehavior, TInput, TOutput>() or source-generated behavior descriptors for trim-ready hosts.")]
    IBehaviorModuleBuilder Add(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        Type behaviorType,
        Type inputType,
        Type outputType,
        Action<IBehaviorTopologyBuilder> configureTopology);

    /// <summary>
    /// Declares that the current module owns the specified behavior.
    /// </summary>
    /// <typeparam name="TBehavior">The concrete behavior type owned by the module.</typeparam>
    /// <returns>The same builder for fluent ownership registration.</returns>
    IBehaviorModuleBuilder Add<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        TBehavior>()
        where TBehavior : class;

    /// <summary>
    /// Declares that the current module owns the specified behavior and supplies a closed execution slot.
    /// </summary>
    /// <remarks>
    /// This overload uses the default reflection-based JSON contract path when dispatch receives a
    /// <see cref="System.Text.Json.JsonElement" /> input. Trim- and Native AOT-ready modules should
    /// use the overload that supplies a <see cref="JsonTypeInfo{T}" /> for <typeparamref name="TInput" />.
    /// </remarks>
    /// <typeparam name="TBehavior">The concrete behavior type owned by the module.</typeparam>
    /// <typeparam name="TInput">The behavior input contract.</typeparam>
    /// <typeparam name="TOutput">The behavior output contract.</typeparam>
    /// <returns>The same builder for fluent ownership registration.</returns>
    [RequiresDynamicCode("Behavior ownership registration without JsonTypeInfo uses reflection-based System.Text.Json input materialization. Prefer Add<TBehavior, TInput, TOutput>(JsonTypeInfo<TInput>) for Native AOT-ready hosts.")]
    [RequiresUnreferencedCode("Behavior ownership registration without JsonTypeInfo uses JSON contract information that cannot be statically preserved by the trimmer. Prefer Add<TBehavior, TInput, TOutput>(JsonTypeInfo<TInput>) for trim-ready hosts.")]
    IBehaviorModuleBuilder Add<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        TBehavior,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
        TInput,
        TOutput>()
        where TBehavior : class, IAppBehavior<TInput, TOutput>
        where TInput : notnull;

    /// <summary>
    /// Declares that the current module owns the specified behavior and supplies a closed execution slot
    /// with source-generated JSON input metadata.
    /// </summary>
    /// <typeparam name="TBehavior">The concrete behavior type owned by the module.</typeparam>
    /// <typeparam name="TInput">The behavior input contract.</typeparam>
    /// <typeparam name="TOutput">The behavior output contract.</typeparam>
    /// <param name="inputJsonTypeInfo">The source-generated JSON contract used to materialize <typeparamref name="TInput" /> inputs.</param>
    /// <returns>The same builder for fluent ownership registration.</returns>
    IBehaviorModuleBuilder Add<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        TBehavior,
        TInput,
        TOutput>(JsonTypeInfo<TInput> inputJsonTypeInfo)
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
    IBehaviorModuleBuilder Add<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        TBehavior>(Action<IBehaviorTopologyBuilder> configureTopology)
        where TBehavior : class;

    /// <summary>
    /// Declares that the current module owns the specified behavior, supplies a closed execution slot,
    /// and applies an explicit topology override.
    /// </summary>
    /// <remarks>
    /// This overload uses the default reflection-based JSON contract path when dispatch receives a
    /// <see cref="System.Text.Json.JsonElement" /> input. Trim- and Native AOT-ready modules should
    /// use the overload that supplies a <see cref="JsonTypeInfo{T}" /> for <typeparamref name="TInput" />.
    /// </remarks>
    /// <typeparam name="TBehavior">The concrete behavior type owned by the module.</typeparam>
    /// <typeparam name="TInput">The behavior input contract.</typeparam>
    /// <typeparam name="TOutput">The behavior output contract.</typeparam>
    /// <param name="configureTopology">
    /// The callback that selects the resolved behavior topology when attribute-only synthesis is not enough.
    /// </param>
    /// <returns>The same builder for fluent ownership registration.</returns>
    [RequiresDynamicCode("Behavior ownership registration without JsonTypeInfo uses reflection-based System.Text.Json input materialization. Prefer Add<TBehavior, TInput, TOutput>(JsonTypeInfo<TInput>, Action<IBehaviorTopologyBuilder>) for Native AOT-ready hosts.")]
    [RequiresUnreferencedCode("Behavior ownership registration without JsonTypeInfo uses JSON contract information that cannot be statically preserved by the trimmer. Prefer Add<TBehavior, TInput, TOutput>(JsonTypeInfo<TInput>, Action<IBehaviorTopologyBuilder>) for trim-ready hosts.")]
    IBehaviorModuleBuilder Add<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        TBehavior,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
        TInput,
        TOutput>(Action<IBehaviorTopologyBuilder> configureTopology)
        where TBehavior : class, IAppBehavior<TInput, TOutput>
        where TInput : notnull;

    /// <summary>
    /// Declares that the current module owns the specified behavior, supplies a closed execution slot
    /// with source-generated JSON input metadata, and applies an explicit topology override.
    /// </summary>
    /// <typeparam name="TBehavior">The concrete behavior type owned by the module.</typeparam>
    /// <typeparam name="TInput">The behavior input contract.</typeparam>
    /// <typeparam name="TOutput">The behavior output contract.</typeparam>
    /// <param name="inputJsonTypeInfo">The source-generated JSON contract used to materialize <typeparamref name="TInput" /> inputs.</param>
    /// <param name="configureTopology">
    /// The callback that selects the resolved behavior topology when attribute-only synthesis is not enough.
    /// </param>
    /// <returns>The same builder for fluent ownership registration.</returns>
    IBehaviorModuleBuilder Add<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        TBehavior,
        TInput,
        TOutput>(
        JsonTypeInfo<TInput> inputJsonTypeInfo,
        Action<IBehaviorTopologyBuilder> configureTopology)
        where TBehavior : class, IAppBehavior<TInput, TOutput>
        where TInput : notnull;
}
