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

        if (!items.TryAdd(capability.Key, item))
        {
            throw new InvalidOperationException(
                $"Capability '{capability.Key}' is already registered.");
        }
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
