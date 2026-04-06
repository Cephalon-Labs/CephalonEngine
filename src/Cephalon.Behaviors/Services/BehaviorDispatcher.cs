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
    private readonly FrozenDictionary<string, (BehaviorExecutionSlot Slot, Type BehaviorType, BehaviorTopologyDescriptor Descriptor)> _table;
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
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(typeRegistry);
        ArgumentNullException.ThrowIfNull(services);

        _services = services;

        var dict = new Dictionary<string, (BehaviorExecutionSlot, Type, BehaviorTopologyDescriptor)>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var descriptor in catalog.All)
        {
            if (typeRegistry.TryGetType(descriptor.Id, out var behaviorType) && behaviorType is not null)
            {
                var slot = BehaviorExecutionSlot.ForType(behaviorType);
                dict[descriptor.Id] = (slot, behaviorType, descriptor);
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

        var behavior = _services.GetRequiredService(entry.BehaviorType);
        return await entry.Slot.InvokeAsync(behavior, input, context, ct).ConfigureAwait(false);
    }
}
