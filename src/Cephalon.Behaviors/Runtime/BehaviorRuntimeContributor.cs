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
            ["behaviorCount"] = all.Count.ToString(CultureInfo.InvariantCulture),
            ["featureGatedBehaviorCount"] = all
                .Count(static behavior => behavior.RequiredFeatureFlagIds.Count > 0)
                .ToString(CultureInfo.InvariantCulture)
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
            entries: CreateEntries(all, metadata));
    }

    private static List<TechnologyRuntimeEntry> CreateEntries(
        IReadOnlyList<BehaviorTopologyDescriptor> behaviors,
        IReadOnlyDictionary<string, string> summaryMetadata)
    {
        ArgumentNullException.ThrowIfNull(behaviors);
        ArgumentNullException.ThrowIfNull(summaryMetadata);

        var entries = new List<TechnologyRuntimeEntry>(behaviors.Count + 1)
        {
            new(
                id: "behaviors-runtime",
                displayName: "Behavior Runtime",
                description: "Reports the active behavior count, pattern distribution, transport distribution, and feature-gated behavior count.",
                metadata: summaryMetadata)
        };

        entries.AddRange(behaviors.Select(CreateBehaviorEntry));
        return entries;
    }

    private static TechnologyRuntimeEntry CreateBehaviorEntry(BehaviorTopologyDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["pattern"] = descriptor.Pattern,
            ["transportIds"] = string.Join(",", descriptor.TransportIds),
            ["inboxEnabled"] = descriptor.InboxEnabled.ToString(),
            ["outboxEnabled"] = descriptor.OutboxEnabled.ToString(),
            ["eventSourcingEnabled"] = descriptor.EventSourcingEnabled.ToString(),
            ["requiredFeatureFlagCount"] = descriptor.RequiredFeatureFlagIds.Count.ToString(CultureInfo.InvariantCulture)
        };

        if (!string.IsNullOrWhiteSpace(descriptor.SourceModuleId))
        {
            metadata["sourceModuleId"] = descriptor.SourceModuleId;
        }

        if (descriptor.RequiredFeatureFlagIds.Count > 0)
        {
            metadata["requiredFeatureFlagIds"] = string.Join(",", descriptor.RequiredFeatureFlagIds);
        }

        return new TechnologyRuntimeEntry(
            id: descriptor.Id,
            displayName: descriptor.DisplayName ?? descriptor.Id,
            description: descriptor.Description ?? $"Behavior topology details for '{descriptor.Id}'.",
            metadata: metadata);
    }
}
