using Cephalon.Abstractions.Behaviors;
using Cephalon.Behaviors.Configuration;

namespace Cephalon.Behaviors.Services;

/// <summary>
/// Resolves the final <see cref="BehaviorTopologyDescriptor" /> for a given behavior identifier
/// by merging four priority layers from lowest to highest:
/// <list type="number">
///   <item><description>Layer 1 (lowest): compiled defaults — pattern=<c>direct</c>, transport=[].</description></item>
///   <item><description>Layer 2: engine-level defaults from the <c>BehaviorDefaults</c> configuration.</description></item>
///   <item><description>Layer 3: per-behavior entry from the <c>Behaviors</c> configuration (transport override replaces, not merges).</description></item>
///   <item><description>Layer 4 (highest): fluent DI registration via <c>FluentBehaviorContributor</c>.</description></item>
/// </list>
/// </summary>
public sealed class BehaviorTopologyResolver
{
    private readonly BehaviorOptions _options;
    private readonly IReadOnlyDictionary<string, BehaviorTopologyDescriptor> _fluentOverrides;

    /// <summary>
    /// Initializes the resolver with the active behavior options and any fluent overrides.
    /// </summary>
    /// <param name="options">The behavior topology options read from configuration.</param>
    /// <param name="fluentOverrides">
    /// Per-behavior topology descriptors contributed via fluent DI registration.
    /// These override all configuration-layer entries.
    /// </param>
    public BehaviorTopologyResolver(
        BehaviorOptions options,
        IReadOnlyDictionary<string, BehaviorTopologyDescriptor> fluentOverrides)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(fluentOverrides);
        _options = options;
        _fluentOverrides = fluentOverrides;
    }

    /// <summary>
    /// Resolves the topology descriptor for the given behavior identifier by applying all four layers.
    /// </summary>
    /// <param name="behaviorId">The stable behavior identifier.</param>
    /// <returns>The fully resolved <see cref="BehaviorTopologyDescriptor" />.</returns>
    public BehaviorTopologyDescriptor Resolve(string behaviorId)
    {
        ArgumentNullException.ThrowIfNull(behaviorId);

        // Layer 4 (highest): fluent DI registration wins entirely
        if (_fluentOverrides.TryGetValue(behaviorId, out var fluentDescriptor))
        {
            return fluentDescriptor;
        }

        // Layer 1: compiled defaults
        var pattern = "direct";
        IReadOnlyList<string> transport = [];

        // Layer 2: engine defaults from config
        var defaults = _options.BehaviorDefaults;
        if (!string.IsNullOrWhiteSpace(defaults.Pattern))
        {
            pattern = defaults.Pattern;
        }

        if (defaults.Transport is { Count: > 0 })
        {
            transport = defaults.Transport;
        }

        // Layer 3: per-behavior config entry (transport override = replace, not merge)
        if (_options.Behaviors.TryGetValue(behaviorId, out var entry))
        {
            if (!string.IsNullOrWhiteSpace(entry.Pattern))
            {
                pattern = entry.Pattern;
            }

            if (entry.Transport is { Count: > 0 })
            {
                transport = entry.Transport;
            }
        }

        return new BehaviorTopologyDescriptor(behaviorId, pattern, transport);
    }
}
