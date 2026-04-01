namespace Cephalon.Engine.Manifest;

public sealed class CapabilityManifest
{
    public CapabilityManifest(
        string key,
        string displayName,
        string description,
        string sourceModuleId,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        Key = key ?? throw new ArgumentNullException(nameof(key));
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        SourceModuleId = sourceModuleId ?? throw new ArgumentNullException(nameof(sourceModuleId));
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    public string Key { get; }

    public string DisplayName { get; }

    public string Description { get; }

    public string SourceModuleId { get; }

    public IReadOnlyDictionary<string, string> Metadata { get; }
}
