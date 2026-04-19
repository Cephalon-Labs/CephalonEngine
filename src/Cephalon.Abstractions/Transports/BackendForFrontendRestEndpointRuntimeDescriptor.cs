using Cephalon.Abstractions.Patterns;

namespace Cephalon.Abstractions.Transports;

/// <summary>
/// Describes one backend-for-frontend client binding matched to one published REST endpoint.
/// </summary>
/// <remarks>
/// This runtime surface keeps the host-agnostic backend-for-frontend binding contract separate
/// from the host-owned REST runtime catalog while still letting operator tooling answer which
/// published REST endpoints are currently visible to a specific client binding.
/// </remarks>
public sealed class BackendForFrontendRestEndpointRuntimeDescriptor
{
    /// <summary>
    /// Creates a backend-for-frontend REST endpoint runtime descriptor.
    /// </summary>
    /// <param name="id">The stable binding-plus-endpoint identifier.</param>
    /// <param name="binding">The backend-for-frontend client binding that selected the endpoint.</param>
    /// <param name="endpoint">The published REST endpoint selected for that binding.</param>
    /// <param name="matchedByDefault">
    /// <see langword="true" /> when the endpoint stayed visible because the binding declared no
    /// positive include filters and the endpoint was not excluded.
    /// </param>
    /// <param name="matchedBehaviorIds">
    /// The included behavior identifiers that matched the published endpoint when explicit
    /// behavior-id filters were part of the binding.
    /// </param>
    /// <param name="matchedCapabilityKeys">
    /// The included required-capability keys that matched the published endpoint when explicit
    /// capability filters were part of the binding.
    /// </param>
    /// <param name="matchedTags">
    /// The included tags that matched the published endpoint when explicit tag filters were part
    /// of the binding.
    /// </param>
    public BackendForFrontendRestEndpointRuntimeDescriptor(
        string id,
        BackendForFrontendClientBindingDescriptor binding,
        RestEndpointRuntimeDescriptor endpoint,
        bool matchedByDefault = false,
        IReadOnlyList<string>? matchedBehaviorIds = null,
        IReadOnlyList<string>? matchedCapabilityKeys = null,
        IReadOnlyList<string>? matchedTags = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("A non-empty runtime descriptor id is required.", nameof(id));
        }

        ArgumentNullException.ThrowIfNull(binding);
        ArgumentNullException.ThrowIfNull(endpoint);

        var normalizedMatchedBehaviorIds = NormalizeOrderedList(matchedBehaviorIds);
        var normalizedMatchedCapabilityKeys = NormalizeOrderedList(matchedCapabilityKeys);
        var normalizedMatchedTags = NormalizeOrderedList(matchedTags);

        if (!IsRestTransport(binding.TransportId))
        {
            throw new ArgumentException(
                "Backend-for-frontend REST endpoint runtime descriptors require a REST client binding.",
                nameof(binding));
        }

        if (!IsRestTransport(endpoint.TransportId))
        {
            throw new ArgumentException(
                "Backend-for-frontend REST endpoint runtime descriptors require a published REST endpoint.",
                nameof(endpoint));
        }

        if (matchedByDefault &&
            (normalizedMatchedBehaviorIds.Length > 0 ||
             normalizedMatchedCapabilityKeys.Length > 0 ||
             normalizedMatchedTags.Length > 0))
        {
            throw new ArgumentException(
                "Default-matched runtime descriptors cannot also declare explicit match dimensions.",
                nameof(matchedByDefault));
        }

        if (!matchedByDefault &&
            normalizedMatchedBehaviorIds.Length == 0 &&
            normalizedMatchedCapabilityKeys.Length == 0 &&
            normalizedMatchedTags.Length == 0)
        {
            throw new ArgumentException(
                "At least one explicit match dimension is required when the runtime descriptor is not matched by default.",
                nameof(matchedBehaviorIds));
        }

        Id = id.Trim();
        Binding = binding;
        Endpoint = endpoint;
        MatchedByDefault = matchedByDefault;
        MatchedBehaviorIds = normalizedMatchedBehaviorIds;
        MatchedCapabilityKeys = normalizedMatchedCapabilityKeys;
        MatchedTags = normalizedMatchedTags;
    }

    /// <summary>
    /// Gets the stable binding-plus-endpoint identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the backend-for-frontend client binding that selected the endpoint.
    /// </summary>
    public BackendForFrontendClientBindingDescriptor Binding { get; }

    /// <summary>
    /// Gets the published REST endpoint selected for that binding.
    /// </summary>
    public RestEndpointRuntimeDescriptor Endpoint { get; }

    /// <summary>
    /// Gets the stable backend-for-frontend binding identifier.
    /// </summary>
    public string BindingId => Binding.Id;

    /// <summary>
    /// Gets the stable client identifier.
    /// </summary>
    public string ClientId => Binding.ClientId;

    /// <summary>
    /// Gets the binding-owner module identifier.
    /// </summary>
    public string SourceModuleId => Binding.SourceModuleId;

    /// <summary>
    /// Gets the stable published REST endpoint identifier.
    /// </summary>
    public string RestEndpointId => Endpoint.Id;

    /// <summary>
    /// Gets a value indicating whether the endpoint stayed visible because the binding declared no
    /// positive include filters and the endpoint was not excluded.
    /// </summary>
    public bool MatchedByDefault { get; }

    /// <summary>
    /// Gets the included behavior identifiers that matched the published endpoint.
    /// </summary>
    public IReadOnlyList<string> MatchedBehaviorIds { get; }

    /// <summary>
    /// Gets the included required-capability keys that matched the published endpoint.
    /// </summary>
    public IReadOnlyList<string> MatchedCapabilityKeys { get; }

    /// <summary>
    /// Gets the included tags that matched the published endpoint.
    /// </summary>
    public IReadOnlyList<string> MatchedTags { get; }

    private static string[] NormalizeOrderedList(IReadOnlyList<string>? values)
    {
        return values?
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }

    private static bool IsRestTransport(string transportId)
    {
        return transportId.Trim().ToLowerInvariant() switch
        {
            "rest-api" => true,
            "rest" => true,
            "http.rest" => true,
            _ => false
        };
    }
}
