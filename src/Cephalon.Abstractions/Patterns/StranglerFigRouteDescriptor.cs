namespace Cephalon.Abstractions.Patterns;

/// <summary>
/// Describes one strangler-fig route owned by a Cephalon module.
/// </summary>
public sealed class StranglerFigRouteDescriptor
{
    /// <summary>
    /// Creates a strangler-fig route descriptor.
    /// </summary>
    /// <param name="id">The stable route identifier.</param>
    /// <param name="sourceModuleId">The Cephalon module that owns the modern boundary for this route.</param>
    /// <param name="displayName">The operator-facing route name.</param>
    /// <param name="description">The human-readable description of the migration boundary.</param>
    /// <param name="pathPrefix">The rooted path prefix that this route should match.</param>
    /// <param name="preferredTarget">The preferred boundary for matched requests.</param>
    /// <param name="legacyEndpoint">The legacy boundary identifier or endpoint.</param>
    /// <param name="modernEndpoint">The modern Cephalon boundary identifier or endpoint.</param>
    /// <param name="methods">Optional request methods that this route should match.</param>
    /// <param name="metadata">Optional route metadata.</param>
    public StranglerFigRouteDescriptor(
        string id,
        string sourceModuleId,
        string displayName,
        string description,
        string pathPrefix,
        StranglerFigTarget preferredTarget = StranglerFigTarget.Modern,
        string? legacyEndpoint = null,
        string? modernEndpoint = null,
        IReadOnlyList<string>? methods = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Route id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(sourceModuleId))
        {
            throw new ArgumentException("Source module id is required.", nameof(sourceModuleId));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Route display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Route description is required.", nameof(description));
        }

        var normalizedLegacyEndpoint = NormalizeEndpoint(legacyEndpoint);
        var normalizedModernEndpoint = NormalizeEndpoint(modernEndpoint);
        if (normalizedLegacyEndpoint is null && normalizedModernEndpoint is null)
        {
            throw new ArgumentException(
                "At least one migration boundary endpoint must be configured.",
                nameof(legacyEndpoint));
        }

        Id = id.Trim();
        SourceModuleId = sourceModuleId.Trim();
        DisplayName = displayName.Trim();
        Description = description.Trim();
        PathPrefix = NormalizePathPrefix(pathPrefix);
        PreferredTarget = preferredTarget;
        LegacyEndpoint = normalizedLegacyEndpoint;
        ModernEndpoint = normalizedModernEndpoint;
        Methods = NormalizeMethods(methods);
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the stable route identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the module that owns the modern Cephalon boundary for this route.
    /// </summary>
    public string SourceModuleId { get; }

    /// <summary>
    /// Gets the operator-facing route name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the human-readable description of the migration boundary.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the rooted path prefix that should match this route.
    /// </summary>
    public string PathPrefix { get; }

    /// <summary>
    /// Gets the preferred boundary for matched requests.
    /// </summary>
    public StranglerFigTarget PreferredTarget { get; }

    /// <summary>
    /// Gets the legacy boundary identifier or endpoint when one is configured.
    /// </summary>
    public string? LegacyEndpoint { get; }

    /// <summary>
    /// Gets the modern Cephalon boundary identifier or endpoint when one is configured.
    /// </summary>
    public string? ModernEndpoint { get; }

    /// <summary>
    /// Gets the normalized request methods that this route should match.
    /// </summary>
    public IReadOnlyList<string> Methods { get; }

    /// <summary>
    /// Gets optional route metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    private static string? NormalizeEndpoint(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static string NormalizePathPrefix(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Route path prefix is required.", nameof(value));
        }

        var normalized = value.Trim();
        if (Uri.TryCreate(normalized, UriKind.Absolute, out var absoluteUri))
        {
            normalized = absoluteUri.AbsolutePath;
        }

        var separatorIndex = normalized.IndexOfAny(['?', '#']);
        if (separatorIndex >= 0)
        {
            normalized = normalized[..separatorIndex];
        }

        if (!normalized.StartsWith('/'))
        {
            normalized = "/" + normalized.TrimStart('/');
        }

        normalized = normalized.Length > 1
            ? normalized.TrimEnd('/')
            : normalized;

        return string.IsNullOrWhiteSpace(normalized)
            ? "/"
            : normalized;
    }

    private static string[] NormalizeMethods(IReadOnlyList<string>? methods)
    {
        return methods?
            .Where(static method => !string.IsNullOrWhiteSpace(method))
            .Select(static method => method.Trim().ToUpperInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static method => method, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }
}
