using Cephalon.Abstractions.Behaviors;
using Cephalon.Behaviors.Configuration;

namespace Cephalon.Behaviors.Services;

/// <summary>Resolves final topology per behavior by merging compiled defaults, config defaults, per-behavior config, and fluent overrides.</summary>
internal sealed class BehaviorTopologyResolver
{
    private readonly BehaviorOptions _defaults;

    /// <summary>Initializes a new instance of <see cref="BehaviorTopologyResolver"/>.</summary>
    public BehaviorTopologyResolver(BehaviorOptions defaults)
    {
        ArgumentNullException.ThrowIfNull(defaults);
        _defaults = defaults;
    }

    /// <summary>
    /// Resolves a <see cref="BehaviorTopologyDescriptor"/> for the given behavior id by merging:
    /// 1. Compiled defaults (pattern="direct", transport=[])
    /// 2. BehaviorOptions defaults (from Engine:BehaviorDefaults)
    /// 3. Per-behavior BehaviorConfigEntry (from Engine:Behaviors)
    /// 4. Fluent DI registration overrides (via the builder callback)
    /// </summary>
    public BehaviorTopologyDescriptor Resolve(
        string behaviorId,
        BehaviorConfigEntry? entry,
        Action<IBehaviorTopologyBuilder>? fluentOverride = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(behaviorId);

        // Layer 1: compiled defaults
        var pattern = "direct";
        var transports = new List<string>();

        // Layer 2: global defaults from configuration
        if (!string.IsNullOrWhiteSpace(_defaults.Pattern))
            pattern = _defaults.Pattern;
        if (_defaults.Transport.Count > 0)
            transports = [.._defaults.Transport];

        // Layer 3: per-behavior config entry
        var inboxEnabled = false;
        var outboxEnabled = false;
        var eventSourcingEnabled = false;

        if (entry is not null)
        {
            if (!string.IsNullOrWhiteSpace(entry.Pattern))
                pattern = entry.Pattern;
            if (entry.Transport.Count > 0)
                transports = [..entry.Transport];
            inboxEnabled = entry.InboxEnabled;
            outboxEnabled = entry.OutboxEnabled;
            eventSourcingEnabled = entry.EventSourcingEnabled;
        }

        // Layer 4: fluent DI registration overrides
        if (fluentOverride is not null)
        {
            var builder = new BehaviorTopologyBuilder();
            // Pre-seed with resolved values so fluent can selectively override
            fluentOverride(builder);
            var fluentDesc = builder.Build(behaviorId);
            // Fluent always wins if it explicitly set values
            pattern = fluentDesc.Pattern;
            if (fluentDesc.TransportIds.Count > 0)
                transports = [..fluentDesc.TransportIds];
            inboxEnabled = fluentDesc.InboxEnabled || inboxEnabled;
            outboxEnabled = fluentDesc.OutboxEnabled || outboxEnabled;
            eventSourcingEnabled = fluentDesc.EventSourcingEnabled || eventSourcingEnabled;
        }

        return new BehaviorTopologyDescriptor(
            id: behaviorId,
            pattern: pattern,
            transportIds: transports.ToArray(),
            inboxEnabled: inboxEnabled,
            outboxEnabled: outboxEnabled,
            eventSourcingEnabled: eventSourcingEnabled);
    }
}
