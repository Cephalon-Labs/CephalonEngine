using Cephalon.Abstractions.Behaviors;

namespace Cephalon.Behaviors.Hosting;

/// <summary>Fluent builder for configuring the behavior collection during engine startup.</summary>
public interface IBehaviorCollectionBuilder
{
    /// <summary>Configures global behavior defaults applied to all behaviors that do not specify their own values.</summary>
    IBehaviorCollectionBuilder WithDefaults(Action<BehaviorDefaultsBuilder> configure);

    /// <summary>Registers a behavior type with optional fluent topology overrides.</summary>
    IBehaviorCollectionBuilder Register<TBehavior>(Action<IBehaviorTopologyBuilder>? configure = null)
        where TBehavior : class;
}

/// <summary>Fluent builder for configuring global behavior defaults.</summary>
public sealed class BehaviorDefaultsBuilder
{
    /// <summary>Gets or sets the default pattern applied when a behavior has no explicit pattern configured.</summary>
    public string Pattern { get; set; } = "direct";

    /// <summary>Gets or sets the default transport list applied when a behavior has no explicit transports configured.</summary>
    public List<string> Transports { get; set; } = [];
}
