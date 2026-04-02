namespace Cephalon.Engine.Manifest;

/// <summary>
/// Describes a capability exposed by a module in the runtime manifest.
/// </summary>
public sealed class CapabilityManifest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CapabilityManifest" /> class.
    /// </summary>
    /// <param name="key">The stable capability key.</param>
    /// <param name="displayName">The operator-facing capability name.</param>
    /// <param name="description">A description of the capability behavior.</param>
    /// <param name="sourceModuleId">The identifier of the module that contributed the capability.</param>
    /// <param name="metadata">Additional capability metadata.</param>
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

    /// <summary>
    /// Gets the stable capability key.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Gets the operator-facing capability name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the capability description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the identifier of the module that contributed the capability.
    /// </summary>
    public string SourceModuleId { get; }

    /// <summary>
    /// Gets additional capability metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }
}
