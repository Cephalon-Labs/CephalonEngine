using Cephalon.Abstractions.Behaviors;

namespace Cephalon.Behaviors.Hosting;

/// <summary>A no-op contributor used as a marker to carry fluent default settings. Actual defaults are applied via the resolver.</summary>
internal sealed class FluentDefaultsContributor : IBehaviorContributor
{
    /// <summary>Initializes a new instance of <see cref="FluentDefaultsContributor"/>.</summary>
    public FluentDefaultsContributor(string pattern, List<string> transports)
    {
        Pattern = pattern;
        Transports = transports;
    }

    /// <summary>Gets the default pattern.</summary>
    public string Pattern { get; }

    /// <summary>Gets the default transport list.</summary>
    public List<string> Transports { get; }

    /// <inheritdoc />
    public void RegisterBehaviors(IBehaviorRegistry registry)
    {
        // This contributor carries defaults — it does not register behaviors directly.
        // The BehaviorTopologyResolver reads defaults from IConfiguration (Engine:BehaviorDefaults).
        // Fluent defaults are provided here for integration scenarios where IConfiguration is not available.
    }
}
