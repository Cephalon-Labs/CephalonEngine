using Cephalon.Abstractions.Behaviors;

namespace Cephalon.Behaviors.Hosting;

/// <summary>A no-op contributor used as a marker to carry fluent default settings.</summary>
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
    public IReadOnlyList<BehaviorTopologyDescriptor> Contribute()
    {
        // This contributor carries defaults — it does not register behaviors directly.
        return [];
    }
}
