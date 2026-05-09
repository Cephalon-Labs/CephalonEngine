using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;
using Cephalon.Abstractions.Behaviors;
using Cephalon.Behaviors.Builders;
using Cephalon.Behaviors.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cephalon.Behaviors.Services;

/// <summary>
/// Default implementation of <see cref="IBehaviorCollectionBuilder" />.
/// Registers behavior types in DI, contributes implementation descriptors,
/// and optionally contributes a fluent topology descriptor at Layer 4.
/// </summary>
public sealed class BehaviorCollectionBuilder : IBehaviorCollectionBuilder
{
    /// <summary>
    /// Initializes the builder with the target service collection.
    /// </summary>
    /// <param name="services">The service collection to register behaviors into.</param>
    public BehaviorCollectionBuilder(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        Services = services;
    }

    /// <inheritdoc />
    public IServiceCollection Services { get; }

    /// <summary>
    /// Registers a behavior of type <typeparamref name="TBehavior" /> with the runtime.
    /// <list type="number">
    ///   <item><description>Resolves the behavior id from <see cref="AppBehaviorAttribute" />.</description></item>
    ///   <item><description>Registers <typeparamref name="TBehavior" /> as a transient service in DI.</description></item>
    ///   <item><description>Contributes a descriptor for the behavior implementation.</description></item>
    ///   <item><description>When <paramref name="configureTopology" /> is provided, adds a <see cref="FluentBehaviorContributor" /> at Layer 4.</description></item>
    /// </list>
    /// </summary>
    /// <typeparam name="TBehavior">
    /// The concrete behavior type. Must be decorated with <see cref="AppBehaviorAttribute" />
    /// and implement <see cref="IAppBehavior{TIn,TOut}" />.
    /// </typeparam>
    /// <param name="configureTopology">
    /// An optional callback that configures the behavior's transport topology at Layer 4 (highest priority).
    /// When <see langword="null" />, topology is resolved from configuration layers only.
    /// </param>
    /// <returns>The same builder for fluent chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <typeparamref name="TBehavior" /> is not decorated with <see cref="AppBehaviorAttribute" />.
    /// </exception>
    public IBehaviorCollectionBuilder Register<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        TBehavior>(
        Action<BehaviorTopologyBuilder>? configureTopology = null)
        where TBehavior : class
    {
        Action<IBehaviorTopologyBuilder>? configureTopologyAdapter = configureTopology is null
            ? null
            : builder => configureTopology((BehaviorTopologyBuilder)builder);

        RegisterCore<TBehavior>(configureTopologyAdapter);
        return this;
    }

    /// <summary>
    /// Registers a behavior of type <typeparamref name="TBehavior" /> with a closed generic execution slot.
    /// </summary>
    /// <typeparam name="TBehavior">
    /// The concrete behavior type. Must be decorated with <see cref="AppBehaviorAttribute" />
    /// and implement <see cref="IAppBehavior{TIn,TOut}" />.
    /// </typeparam>
    /// <typeparam name="TInput">The behavior input contract.</typeparam>
    /// <typeparam name="TOutput">The behavior output contract.</typeparam>
    /// <param name="configureTopology">
    /// An optional callback that configures the behavior's transport topology at Layer 4 (highest priority).
    /// When <see langword="null" />, topology is resolved from configuration layers only.
    /// </param>
    /// <returns>The same builder for fluent chaining.</returns>
    public IBehaviorCollectionBuilder Register<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        TBehavior,
        TInput,
        TOutput>(
        Action<BehaviorTopologyBuilder>? configureTopology = null)
        where TBehavior : class, IAppBehavior<TInput, TOutput>
        where TInput : notnull
    {
        Action<IBehaviorTopologyBuilder>? configureTopologyAdapter = configureTopology is null
            ? null
            : builder => configureTopology((BehaviorTopologyBuilder)builder);

        var behaviorId = RegisterCore<TBehavior>(configureTopologyAdapter);
        RegisterExecutionSlot(
            behaviorId,
            typeof(TBehavior),
            BehaviorExecutionSlot.For<TBehavior, TInput, TOutput>());
        return this;
    }

    /// <summary>
    /// Registers a behavior of type <typeparamref name="TBehavior" /> with a closed generic
    /// execution slot that uses explicit JSON input metadata.
    /// </summary>
    /// <typeparam name="TBehavior">
    /// The concrete behavior type. Must be decorated with <see cref="AppBehaviorAttribute" />
    /// and implement <see cref="IAppBehavior{TIn,TOut}" />.
    /// </typeparam>
    /// <typeparam name="TInput">The behavior input contract.</typeparam>
    /// <typeparam name="TOutput">The behavior output contract.</typeparam>
    /// <param name="inputJsonTypeInfo">
    /// The source-generated JSON contract used to materialize <typeparamref name="TInput" /> inputs.
    /// </param>
    /// <param name="configureTopology">
    /// An optional callback that configures the behavior's transport topology at Layer 4 (highest priority).
    /// When <see langword="null" />, topology is resolved from configuration layers only.
    /// </param>
    /// <returns>The same builder for fluent chaining.</returns>
    public IBehaviorCollectionBuilder Register<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        TBehavior,
        TInput,
        TOutput>(
        JsonTypeInfo<TInput> inputJsonTypeInfo,
        Action<BehaviorTopologyBuilder>? configureTopology)
        where TBehavior : class, IAppBehavior<TInput, TOutput>
        where TInput : notnull
    {
        ArgumentNullException.ThrowIfNull(inputJsonTypeInfo);

        Action<IBehaviorTopologyBuilder>? configureTopologyAdapter = configureTopology is null
            ? null
            : builder => configureTopology((BehaviorTopologyBuilder)builder);

        var behaviorId = RegisterCore<TBehavior>(configureTopologyAdapter);
        RegisterExecutionSlot(
            behaviorId,
            typeof(TBehavior),
            BehaviorExecutionSlot.For<TBehavior, TInput, TOutput>(inputJsonTypeInfo));
        return this;
    }

    internal IBehaviorCollectionBuilder Register(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        Type behaviorType,
        Action<IBehaviorTopologyBuilder>? configureTopology = null,
        string? sourceModuleId = null)
    {
        RegisterCore(behaviorType, configureTopology, sourceModuleId);
        return this;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Owned behavior registrations are produced by Engine APIs that either preserve public constructors on typed behavior registrations or mark type-based registrations as trim-unsafe before they reach Cephalon.Behaviors.")]
    internal IBehaviorCollectionBuilder Register(OwnedBehaviorRegistration registration)
    {
        ArgumentNullException.ThrowIfNull(registration);

        var behaviorId = RegisterCore(
            registration.BehaviorType,
            registration.ConfigureTopology,
            registration.SourceModuleId);

        if (registration.ExecutionDelegate is not null)
        {
            RegisterExecutionSlot(
                behaviorId,
                registration.BehaviorType,
                BehaviorExecutionSlot.FromDelegate(registration.ExecutionDelegate));
        }

        return this;
    }

    private string RegisterCore<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        TBehavior>(
        Action<IBehaviorTopologyBuilder>? configureTopology = null,
        string? sourceModuleId = null)
        where TBehavior : class
    {
        var behaviorType = typeof(TBehavior);
        var behaviorId = RegisterCoreDescriptor(behaviorType, configureTopology, sourceModuleId);

        Services.TryAddTransient<TBehavior>();

        return behaviorId;
    }

    private string RegisterCore(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        Type behaviorType,
        Action<IBehaviorTopologyBuilder>? configureTopology = null,
        string? sourceModuleId = null)
    {
        var behaviorId = RegisterCoreDescriptor(behaviorType, configureTopology, sourceModuleId);

        Services.TryAddTransient(behaviorType);

        return behaviorId;
    }

    private string RegisterCoreDescriptor(
        Type behaviorType,
        Action<IBehaviorTopologyBuilder>? configureTopology = null,
        string? sourceModuleId = null)
    {
        ArgumentNullException.ThrowIfNull(behaviorType);

        var attr = (AppBehaviorAttribute?)Attribute.GetCustomAttribute(
            behaviorType, typeof(AppBehaviorAttribute));

        if (attr is null)
        {
            throw new InvalidOperationException(
                $"Cannot register '{behaviorType.Name}': it is not decorated with [AppBehavior(id)].");
        }

        var behaviorId = attr.Id;

        // 1. Contribute the implementation descriptor
        BehaviorImplementationRegistration.TryRegister(
            Services,
            behaviorId,
            behaviorType,
            ResolveIdempotencyMode(behaviorType));

        // 2. If fluent topology provided, add a Layer-4 contributor
        BehaviorTopologyDescriptor? descriptor = null;
        if (configureTopology is not null)
        {
            var topologyBuilder = new BehaviorTopologyBuilder();
            configureTopology(topologyBuilder);
            descriptor = topologyBuilder.Build(behaviorId);
        }

        descriptor = BehaviorAttributeTopologyResolver.Resolve(
            behaviorId,
            behaviorType,
            descriptor,
            defaultToDirect: true);
        if (descriptor is not null)
        {
            descriptor = ApplySourceModuleId(descriptor, sourceModuleId);
            Services.AddSingleton<IBehaviorContributor>(new FluentBehaviorContributor(descriptor));
        }

        return behaviorId;
    }

    private void RegisterExecutionSlot(
        string behaviorId,
        Type behaviorType,
        BehaviorExecutionSlot slot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(behaviorId);
        ArgumentNullException.ThrowIfNull(behaviorType);
        ArgumentNullException.ThrowIfNull(slot);

        ResolveOrCreateSlotRegistry(Services).Register(behaviorId, behaviorType, slot);
    }

    private static BehaviorExecutionSlotRegistry ResolveOrCreateSlotRegistry(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        for (var index = services.Count - 1; index >= 0; index--)
        {
            var descriptor = services[index];
            if (descriptor.ServiceType == typeof(BehaviorExecutionSlotRegistry) &&
                descriptor.ImplementationInstance is BehaviorExecutionSlotRegistry registry)
            {
                return registry;
            }
        }

        var createdRegistry = new BehaviorExecutionSlotRegistry();
        services.TryAddSingleton(createdRegistry);
        return createdRegistry;
    }

    private static BehaviorTopologyDescriptor ApplySourceModuleId(
        BehaviorTopologyDescriptor descriptor,
        string? sourceModuleId)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        if (string.IsNullOrWhiteSpace(sourceModuleId) ||
            string.Equals(descriptor.SourceModuleId, sourceModuleId, StringComparison.OrdinalIgnoreCase))
        {
            return descriptor;
        }

        return new BehaviorTopologyDescriptor(
            descriptor.Id,
            descriptor.Pattern,
            descriptor.TransportIds,
            inboxEnabled: descriptor.InboxEnabled,
            outboxEnabled: descriptor.OutboxEnabled,
            eventSourcingEnabled: descriptor.EventSourcingEnabled,
            apiSurface: descriptor.ApiSurface,
            displayName: descriptor.DisplayName,
            description: descriptor.Description,
            requiredFeatureFlagIds: descriptor.RequiredFeatureFlagIds,
            sourceModuleId: sourceModuleId,
            metadata: descriptor.Metadata);
    }

    private static BehaviorIdempotencyMode ResolveIdempotencyMode(Type behaviorType)
    {
        ArgumentNullException.ThrowIfNull(behaviorType);

        return ((BehaviorIdempotencyAttribute?)Attribute.GetCustomAttribute(
                behaviorType,
                typeof(BehaviorIdempotencyAttribute)))
            ?.Mode ?? BehaviorIdempotencyMode.Unknown;
    }
}
