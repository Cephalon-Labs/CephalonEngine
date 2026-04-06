using Cephalon.Abstractions.Behaviors;
using Cephalon.Abstractions.Technologies;
using System.Globalization;

namespace Cephalon.Behaviors.Runtime;

/// <summary>
/// Contributes the active behavior topology surface to the engine runtime snapshot.
/// Reports total behavior count, pattern distribution, and transport distribution.
/// </summary>
internal sealed class BehaviorRuntimeContributor(IBehaviorCatalog catalog) : ITechnologyRuntimeContributor
{
    /// <inheritdoc />
    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        var all = catalog.All;

        var patternCounts = all
            .GroupBy(b => b.Pattern, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        var transportCounts = all
            .SelectMany(b => b.TransportIds)
            .GroupBy(t => t, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["behaviorCount"] = all.Count.ToString(CultureInfo.InvariantCulture)
        };

        foreach (var (pattern, count) in patternCounts)
            metadata[$"pattern.{pattern}"] = count.ToString(CultureInfo.InvariantCulture);

        foreach (var (transport, count) in transportCounts)
            metadata[$"transport.{transport}"] = count.ToString(CultureInfo.InvariantCulture);

        return new TechnologyRuntimeSurface(
            technologyId: "behaviors",
            surfaceId: "behaviors",
            displayName: "Behavior Topology",
            description: "Summarizes the active Adaptive Behavior Topology (ABT) registered with the engine.",
            entries:
            [
                new TechnologyRuntimeEntry(
                    id: "behaviors-runtime",
                    displayName: "Behavior Runtime",
                    description: "Reports the active behavior count, pattern distribution, and transport distribution.",
                    metadata: metadata)
            ]);
    }
}
