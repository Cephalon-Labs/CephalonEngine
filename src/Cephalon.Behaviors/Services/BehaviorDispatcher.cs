using System.Collections.Frozen;
using Cephalon.Abstractions.Behaviors;
using Cephalon.Behaviors.Exceptions;

namespace Cephalon.Behaviors.Services;

/// <summary>Dispatches behavior invocations to the appropriate registered handler slot.</summary>
public sealed class BehaviorDispatcher
{
    private readonly FrozenDictionary<string, (BehaviorExecutionSlot Slot, BehaviorTopologyDescriptor Descriptor)> _table;
    private readonly IServiceProvider _services;

    /// <summary>Initializes a new instance of <see cref="BehaviorDispatcher"/>.</summary>
    public BehaviorDispatcher(IBehaviorCatalog catalog, IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(services);
        _services = services;
        _table = catalog.All
            .ToFrozenDictionary(
                d => d.Id,
                d => (BuildSlot(d), d),
                StringComparer.OrdinalIgnoreCase);
    }

    private static BehaviorExecutionSlot BuildSlot(BehaviorTopologyDescriptor d)
    {
        // For now: runtime slot building (SourceGen replaces this in M5)
        // Return a placeholder; real slot built per registered type
        throw new NotImplementedException("BehaviorExecutionSlot must be registered with the behavior type via DI.");
    }

    /// <summary>Dispatches a behavior invocation to the registered handler for the given behavior identifier.</summary>
    public async Task<object?> DispatchAsync(string behaviorId, object input, IBehaviorContext context, CancellationToken ct = default)
    {
        if (!_table.TryGetValue(behaviorId, out var entry))
            throw new BehaviorNotFoundException(behaviorId);
        // Execute via registered slot — slot building deferred to DI registration
        throw new NotImplementedException("Full dispatch implemented after slot DI integration.");
    }
}
