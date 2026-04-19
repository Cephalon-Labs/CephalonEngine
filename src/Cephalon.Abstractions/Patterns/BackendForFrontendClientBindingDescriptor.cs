namespace Cephalon.Abstractions.Patterns;

/// <summary>
/// Describes one client-specific transport binding owned by a Cephalon module.
/// </summary>
public sealed class BackendForFrontendClientBindingDescriptor
{
    /// <summary>
    /// Creates a backend-for-frontend client binding descriptor.
    /// </summary>
    /// <param name="id">The stable binding identifier.</param>
    /// <param name="clientId">The stable client identifier, such as <c>mobile</c> or <c>storefront</c>.</param>
    /// <param name="sourceModuleId">The Cephalon module that owns this binding.</param>
    /// <param name="displayName">The operator-facing binding name.</param>
    /// <param name="description">The human-readable description of the client-specific surface.</param>
    /// <param name="transportId">The transport identifier used by this client surface.</param>
    /// <param name="entryPoint">The transport-specific entry point, route prefix, or endpoint handle when one is known.</param>
    /// <param name="behaviorFilter">The behavior, capability, and tag-selection hints attached to this client binding.</param>
    /// <param name="metadata">Optional binding metadata.</param>
    public BackendForFrontendClientBindingDescriptor(
        string id,
        string clientId,
        string sourceModuleId,
        string displayName,
        string description,
        string transportId,
        string? entryPoint = null,
        BackendForFrontendBehaviorFilterDescriptor? behaviorFilter = null,
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
        EntryPoint = NormalizeOptional(entryPoint);
        BehaviorFilter = behaviorFilter ?? BackendForFrontendBehaviorFilterDescriptor.Empty;
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
    public BackendForFrontendBehaviorFilterDescriptor BehaviorFilter { get; }

    /// <summary>
    /// Gets optional binding metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
