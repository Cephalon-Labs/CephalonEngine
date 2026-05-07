using Cephalon.Abstractions.Capabilities;
using Cephalon.Engine.Manifest;

namespace Cephalon.Engine.Composition;

internal sealed class CapabilityManifestCollector
{
    private readonly Dictionary<string, CapabilityManifest> items =
        new(StringComparer.OrdinalIgnoreCase);

    public ICapabilityRegistry ForModule(string moduleId)
    {
        return new ModuleCapabilityRegistry(moduleId, this);
    }

    public CapabilityManifest[] Build()
    {
        return items.Values
            .OrderBy(capability => capability.Key, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private void Add(string sourceModuleId, Capability capability)
    {
        ArgumentNullException.ThrowIfNull(capability);

        var item = new CapabilityManifest(
            key: capability.Key,
            displayName: capability.DisplayName,
            description: capability.Description,
            sourceModuleId: sourceModuleId,
            metadata: capability.Metadata);

        if (items.TryGetValue(capability.Key, out var existing))
        {
            if (string.Equals(existing.SourceModuleId, sourceModuleId, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Capability '{capability.Key}' is already registered by module '{sourceModuleId}'.");
            }

            items[capability.Key] = Merge(existing, item);
            return;
        }

        items.Add(capability.Key, item);
    }

    private static CapabilityManifest Merge(CapabilityManifest existing, CapabilityManifest incoming)
    {
        var sourceModuleIds = MergeCsv(existing.Metadata, "sourceModuleIds", existing.SourceModuleId, incoming.SourceModuleId);
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        CopyStableMetadata(existing.Metadata, incoming.Metadata, metadata);
        AddMergedMetadata(metadata, "sourceModuleIds", sourceModuleIds);
        AddMergedMetadata(metadata, "providers", MergeCsv(existing.Metadata, "providers", GetMetadataValue(existing.Metadata, "provider"), GetMetadataValue(incoming.Metadata, "provider")));
        AddMergedMetadata(metadata, "packs", MergeCsv(existing.Metadata, "packs", GetMetadataValue(existing.Metadata, "pack"), GetMetadataValue(incoming.Metadata, "pack")));
        metadata["contributorCount"] = sourceModuleIds.Length.ToString(System.Globalization.CultureInfo.InvariantCulture);

        return new CapabilityManifest(
            key: existing.Key,
            displayName: existing.DisplayName,
            description: string.Equals(existing.Description, incoming.Description, StringComparison.Ordinal)
                ? existing.Description
                : $"{existing.Description} Additional provider contributors also expose this shared capability family.",
            sourceModuleId: existing.SourceModuleId,
            metadata: metadata);
    }

    private static void CopyStableMetadata(
        IReadOnlyDictionary<string, string> existing,
        IReadOnlyDictionary<string, string> incoming,
        Dictionary<string, string> metadata)
    {
        foreach (var pair in existing)
        {
            if (IsAggregateMetadataKey(pair.Key))
            {
                continue;
            }

            if (incoming.TryGetValue(pair.Key, out var incomingValue) &&
                string.Equals(pair.Value, incomingValue, StringComparison.Ordinal))
            {
                metadata[pair.Key] = pair.Value;
            }
        }
    }

    private static string[] MergeCsv(
        IReadOnlyDictionary<string, string> existing,
        string existingKey,
        params string?[] values)
    {
        return existing.TryGetValue(existingKey, out var rawExisting)
            ? rawExisting
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Concat(values.Where(static value => !string.IsNullOrWhiteSpace(value)).Select(static value => value!.Trim()))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
                .ToArray()
            : values
                .Where(static value => !string.IsNullOrWhiteSpace(value))
                .Select(static value => value!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
                .ToArray();
    }

    private static string? GetMetadataValue(IReadOnlyDictionary<string, string> metadata, string key)
    {
        return metadata.TryGetValue(key, out var value)
            ? value
            : null;
    }

    private static void AddMergedMetadata(Dictionary<string, string> metadata, string key, string[] values)
    {
        if (values.Length > 0)
        {
            metadata[key] = string.Join(",", values);
        }
    }

    private static bool IsAggregateMetadataKey(string key)
    {
        return string.Equals(key, "pack", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(key, "provider", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(key, "packs", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(key, "providers", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(key, "sourceModuleIds", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(key, "contributorCount", StringComparison.OrdinalIgnoreCase);
    }

    private sealed class ModuleCapabilityRegistry : ICapabilityRegistry
    {
        private readonly string moduleId;
        private readonly CapabilityManifestCollector collector;

        public ModuleCapabilityRegistry(string moduleId, CapabilityManifestCollector collector)
        {
            this.moduleId = moduleId ?? throw new ArgumentNullException(nameof(moduleId));
            this.collector = collector ?? throw new ArgumentNullException(nameof(collector));
        }

        public void Add(Capability capability)
        {
            collector.Add(moduleId, capability);
        }
    }
}
