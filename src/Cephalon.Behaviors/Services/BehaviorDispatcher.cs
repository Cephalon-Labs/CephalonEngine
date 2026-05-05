using System.Collections.Frozen;
using Cephalon.Abstractions.Behaviors;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Behaviors.Services;

/// <summary>
/// Resolves behavior instances from the service provider and dispatches calls
/// to the appropriate <see cref="BehaviorExecutionSlot" /> by behavior identifier.
/// The dispatch table is built once at construction time from a frozen dictionary
/// for O(1) lock-free lookup on every call.
/// </summary>
public sealed class BehaviorDispatcher
{
    private readonly FrozenDictionary<string, (BehaviorExecutionDelegate Pipeline, Type BehaviorType, BehaviorTopologyDescriptor Descriptor)> _table;
    private readonly IServiceProvider _services;

    /// <summary>
    /// Initializes the dispatcher by building its frozen dispatch table from the catalog and type registry.
    /// Only behaviors that appear in both the catalog and the type registry are dispatchable.
    /// </summary>
    /// <param name="catalog">The behavior catalog that exposes topology descriptors.</param>
    /// <param name="typeRegistry">The registry that maps behavior identifiers to concrete types.</param>
    /// <param name="services">The service provider used to resolve behavior instances at dispatch time.</param>
    public BehaviorDispatcher(
        IBehaviorCatalog catalog,
        IBehaviorTypeRegistry typeRegistry,
        IServiceProvider services)
        : this(
            catalog,
            typeRegistry,
            services,
            services.GetServices<IBehaviorExecutionMiddleware>())
    {
    }

    private BehaviorDispatcher(
        IBehaviorCatalog catalog,
        IBehaviorTypeRegistry typeRegistry,
        IServiceProvider services,
        IEnumerable<IBehaviorExecutionMiddleware>? middlewares)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(typeRegistry);
        ArgumentNullException.ThrowIfNull(services);

        _services = services;
        var executionMiddlewares = middlewares?.ToArray() ?? [];
        var slotRegistry = services.GetService<BehaviorExecutionSlotRegistry>();

        var dict = new Dictionary<string, (BehaviorExecutionDelegate, Type, BehaviorTopologyDescriptor)>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var descriptor in catalog.All)
        {
            if (typeRegistry.TryGetType(descriptor.Id, out var behaviorType) && behaviorType is not null)
            {
                var slot = slotRegistry is not null &&
                    slotRegistry.TryGetSlot(descriptor.Id, behaviorType, out var generatedSlot) &&
                    generatedSlot is not null
                        ? generatedSlot
                        : BehaviorExecutionSlot.ForType(behaviorType);
                var pipeline = BuildExecutionPipeline(
                    descriptor.Id,
                    behaviorType,
                    descriptor,
                    slot,
                    executionMiddlewares);
                dict[descriptor.Id] = (pipeline, behaviorType, descriptor);
            }
        }

        _table = dict.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Dispatches a call to the behavior identified by <paramref name="behaviorId" />.
    /// The behavior instance is resolved from the service provider on every call.
    /// </summary>
    /// <param name="behaviorId">The stable identifier of the behavior to invoke.</param>
    /// <param name="input">The input message object.</param>
    /// <param name="context">The behavior execution context.</param>
    /// <param name="ct">A token that cancels the dispatch.</param>
    /// <returns>A task that resolves to the behavior output, boxed as <see cref="object" />.</returns>
    /// <exception cref="BehaviorNotFoundException">
    /// Thrown when <paramref name="behaviorId" /> is not present in the dispatch table.
    /// </exception>
    public async Task<object?> DispatchAsync(
        string behaviorId,
        object input,
        IBehaviorContext context,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(behaviorId);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);

        if (!_table.TryGetValue(behaviorId, out var entry))
        {
            throw new BehaviorNotFoundException(behaviorId);
        }

        var invocation = new BehaviorExecutionInvocation(
            entry.Descriptor.Id,
            entry.BehaviorType,
            entry.Descriptor,
            input,
            context);
        return await entry.Pipeline(invocation, ct).ConfigureAwait(false);
    }

    private BehaviorExecutionDelegate BuildExecutionPipeline(
        string behaviorId,
        Type behaviorType,
        BehaviorTopologyDescriptor descriptor,
        BehaviorExecutionSlot slot,
        IBehaviorExecutionMiddleware[] middlewares)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(behaviorId);
        ArgumentNullException.ThrowIfNull(behaviorType);
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(slot);
        ArgumentNullException.ThrowIfNull(middlewares);

        var terminal = CreateTerminalDelegate(behaviorType, slot);
        return middlewares.Length == 0
            ? terminal
            : BehaviorExecutionMiddlewarePipeline.Compose(middlewares, terminal);
    }

    private BehaviorExecutionDelegate CreateTerminalDelegate(
        Type behaviorType,
        BehaviorExecutionSlot slot)
    {
        ArgumentNullException.ThrowIfNull(behaviorType);
        ArgumentNullException.ThrowIfNull(slot);

        return async (invocation, cancellationToken) =>
        {
            var behavior = _services.GetRequiredService(behaviorType);
            return await slot.InvokeAsync(
                    behavior,
                    invocation.Input,
                    invocation.Context,
                    cancellationToken)
                .ConfigureAwait(false);
        };
    }
}
