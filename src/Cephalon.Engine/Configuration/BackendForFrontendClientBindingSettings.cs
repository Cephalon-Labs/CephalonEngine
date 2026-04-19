using Microsoft.Extensions.Configuration;

namespace Cephalon.Engine.Configuration;

/// <summary>
/// Describes one configuration-driven backend-for-frontend client binding.
/// </summary>
public sealed class BackendForFrontendClientBindingSettings
{
    /// <summary>
    /// Creates backend-for-frontend client binding settings.
    /// </summary>
    /// <param name="id">The stable binding identifier.</param>
    /// <param name="clientId">The stable client identifier.</param>
    /// <param name="sourceModuleId">The Cephalon module that owns this binding.</param>
    /// <param name="displayName">The operator-facing binding name.</param>
    /// <param name="description">The human-readable description of the client-specific surface.</param>
    /// <param name="transportId">The transport identifier used by this client surface.</param>
    /// <param name="entryPoint">The transport-specific entry point, route prefix, or endpoint handle when one is known.</param>
    /// <param name="behaviorFilter">The behavior, capability, and tag-selection hints attached to this binding.</param>
    /// <param name="metadata">Optional binding metadata.</param>
    public BackendForFrontendClientBindingSettings(
        string id,
        string clientId,
        string sourceModuleId,
        string displayName,
        string description,
        string transportId,
        string? entryPoint = null,
        BackendForFrontendBehaviorFilterSettings? behaviorFilter = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Binding id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(clientId))
        {
            throw new ArgumentException("Client id is required.", nameof(clientId));
        }

        if (string.IsNullOrWhiteSpace(sourceModuleId))
        {
            throw new ArgumentException("Source module id is required.", nameof(sourceModuleId));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Binding display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Binding description is required.", nameof(description));
        }

        if (string.IsNullOrWhiteSpace(transportId))
        {
            throw new ArgumentException("Transport id is required.", nameof(transportId));
        }

        Id = id.Trim();
        ClientId = clientId.Trim();
        SourceModuleId = sourceModuleId.Trim();
        DisplayName = displayName.Trim();
        Description = description.Trim();
        TransportId = transportId.Trim();
        EntryPoint = string.IsNullOrWhiteSpace(entryPoint)
            ? null
            : entryPoint.Trim();
        BehaviorFilter = behaviorFilter ?? BackendForFrontendBehaviorFilterSettings.Empty;
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the stable binding identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the stable client identifier.
    /// </summary>
    public string ClientId { get; }

    /// <summary>
    /// Gets the module that owns this client-specific binding.
    /// </summary>
    public string SourceModuleId { get; }

    /// <summary>
    /// Gets the operator-facing binding name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the human-readable description of the client-specific surface.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the transport identifier used by this client-specific surface.
    /// </summary>
    public string TransportId { get; }

    /// <summary>
    /// Gets the transport-specific entry point, route prefix, or endpoint handle when one is known.
    /// </summary>
    public string? EntryPoint { get; }

    /// <summary>
    /// Gets the behavior, capability, and tag-selection hints attached to this client binding.
    /// </summary>
    public BackendForFrontendBehaviorFilterSettings BehaviorFilter { get; }

    /// <summary>
    /// Gets optional binding metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    /// <summary>
    /// Reads one backend-for-frontend client binding from configuration.
    /// </summary>
    /// <param name="section">The configuration section that contains the client binding.</param>
    /// <returns>The parsed client-binding settings.</returns>
    public static BackendForFrontendClientBindingSettings FromSection(IConfigurationSection section)
    {
        ArgumentNullException.ThrowIfNull(section);

        return new BackendForFrontendClientBindingSettings(
            id: section["Id"]
                ?? throw new InvalidOperationException("Backend-for-frontend binding id is required."),
            clientId: section["ClientId"]
                ?? throw new InvalidOperationException("Backend-for-frontend client id is required."),
            sourceModuleId: section["SourceModuleId"]
                ?? throw new InvalidOperationException("Backend-for-frontend source module id is required."),
            displayName: section["DisplayName"]
                ?? throw new InvalidOperationException("Backend-for-frontend display name is required."),
            description: section["Description"]
                ?? throw new InvalidOperationException("Backend-for-frontend description is required."),
            transportId: section["TransportId"]
                ?? throw new InvalidOperationException("Backend-for-frontend transport id is required."),
            entryPoint: section["EntryPoint"],
            behaviorFilter: BackendForFrontendBehaviorFilterSettings.FromSection(section.GetSection("BehaviorFilter")),
            metadata: ReadMetadata(section.GetSection("Metadata")));
    }

    private static Dictionary<string, string> ReadMetadata(IConfigurationSection section)
    {
        if (!section.Exists())
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        return section
            .GetChildren()
            .Where(static child => !string.IsNullOrWhiteSpace(child.Key) && !string.IsNullOrWhiteSpace(child.Value))
            .ToDictionary(
                static child => child.Key.Trim(),
                static child => child.Value!.Trim(),
                StringComparer.OrdinalIgnoreCase);
    }
}
