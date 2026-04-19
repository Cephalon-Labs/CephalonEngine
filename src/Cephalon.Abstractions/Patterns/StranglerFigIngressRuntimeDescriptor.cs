namespace Cephalon.Abstractions.Patterns;

/// <summary>
/// Describes the effective strangler-fig ingress materialization answer for one route.
/// </summary>
public sealed class StranglerFigIngressRuntimeDescriptor
{
    /// <summary>
    /// Creates a strangler-fig ingress runtime descriptor.
    /// </summary>
    /// <param name="routeId">The stable route identifier.</param>
    /// <param name="sourceModuleId">The Cephalon module that owns the modern boundary for this route.</param>
    /// <param name="displayName">The operator-facing route name.</param>
    /// <param name="description">The human-readable description of the migration boundary.</param>
    /// <param name="pathPrefix">The rooted public path prefix that this route matches.</param>
    /// <param name="requestedTarget">The target requested after applying migration-policy overlays.</param>
    /// <param name="effectiveTarget">The target that will actually receive traffic after endpoint fallback is considered.</param>
    /// <param name="requestedTargetSource">The source of the requested target, such as <c>authored-route</c> or <c>migration-route</c>.</param>
    /// <param name="selectionMode">The runtime selection result, such as <c>requested-target</c> or <c>fallback-target</c>.</param>
    /// <param name="selectedEndpoint">The concrete endpoint or boundary identifier that will receive traffic.</param>
    /// <param name="selectedEndpointKind">The normalized selected-endpoint kind, such as <c>local-path</c>, <c>absolute-uri</c>, or <c>opaque</c>.</param>
    /// <param name="ingressMode">The normalized ingress follow-through mode, such as <c>pass-through</c>, <c>rewrite-local-path</c>, <c>proxy-absolute-uri</c>, or <c>opaque-endpoint</c>.</param>
    /// <param name="canMaterialize">Indicates whether a generic ingress or traffic manager can materialize this selected endpoint directly.</param>
    /// <param name="targetPathPrefix">The normalized path prefix that traffic should land on when the selected endpoint is path-shaped.</param>
    /// <param name="targetQuery">The normalized base query string that should flow with the selected endpoint when one exists.</param>
    /// <param name="targetUri">The normalized absolute URI that traffic should target when the selected endpoint is absolute.</param>
    /// <param name="methods">Optional request methods that this route matches.</param>
    /// <param name="progressState">The normalized migration-progress state for the route.</param>
    /// <param name="progressPercent">The normalized migration-progress percentage for the route.</param>
    /// <param name="metadata">The original authored route metadata.</param>
    /// <param name="runtimeMetadata">Additional runtime-only metadata such as notes or overlay provenance.</param>
    public StranglerFigIngressRuntimeDescriptor(
        string routeId,
        string sourceModuleId,
        string displayName,
        string description,
        string pathPrefix,
        StranglerFigTarget requestedTarget,
        StranglerFigTarget effectiveTarget,
        string requestedTargetSource,
        string selectionMode,
        string selectedEndpoint,
        string selectedEndpointKind,
        string ingressMode,
        bool canMaterialize,
        string? targetPathPrefix = null,
        string? targetQuery = null,
        string? targetUri = null,
        IReadOnlyList<string>? methods = null,
        string? progressState = null,
        int progressPercent = 0,
        IReadOnlyDictionary<string, string>? metadata = null,
        IReadOnlyDictionary<string, string>? runtimeMetadata = null)
    {
        if (string.IsNullOrWhiteSpace(routeId))
        {
            throw new ArgumentException("Route id is required.", nameof(routeId));
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

        if (string.IsNullOrWhiteSpace(requestedTargetSource))
        {
            throw new ArgumentException("Requested target source is required.", nameof(requestedTargetSource));
        }

        if (string.IsNullOrWhiteSpace(selectionMode))
        {
            throw new ArgumentException("Selection mode is required.", nameof(selectionMode));
        }

        if (string.IsNullOrWhiteSpace(selectedEndpoint))
        {
            throw new ArgumentException("Selected endpoint is required.", nameof(selectedEndpoint));
        }

        if (string.IsNullOrWhiteSpace(selectedEndpointKind))
        {
            throw new ArgumentException("Selected endpoint kind is required.", nameof(selectedEndpointKind));
        }

        if (string.IsNullOrWhiteSpace(ingressMode))
        {
            throw new ArgumentException("Ingress mode is required.", nameof(ingressMode));
        }

        if (progressPercent is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(progressPercent), progressPercent, "Progress percent must be between 0 and 100.");
        }

        var normalizedTargetPathPrefix = NormalizeOptionalPathPrefix(targetPathPrefix);
        var normalizedTargetQuery = NormalizeOptionalQuery(targetQuery);
        var normalizedTargetUri = NormalizeOptionalUri(targetUri);
        var normalizedSelectedEndpointKind = selectedEndpointKind.Trim();
        var normalizedIngressMode = ingressMode.Trim();

        if (string.Equals(normalizedSelectedEndpointKind, "local-path", StringComparison.OrdinalIgnoreCase) &&
            normalizedTargetPathPrefix is null)
        {
            throw new ArgumentException("A local-path ingress answer requires a target path prefix.", nameof(targetPathPrefix));
        }

        if (string.Equals(normalizedSelectedEndpointKind, "absolute-uri", StringComparison.OrdinalIgnoreCase) &&
            normalizedTargetUri is null)
        {
            throw new ArgumentException("An absolute-uri ingress answer requires a target URI.", nameof(targetUri));
        }

        if (string.Equals(normalizedIngressMode, "proxy-absolute-uri", StringComparison.OrdinalIgnoreCase) &&
            normalizedTargetUri is null)
        {
            throw new ArgumentException("A proxy-absolute-uri ingress answer requires a target URI.", nameof(targetUri));
        }

        if ((string.Equals(normalizedIngressMode, "pass-through", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(normalizedIngressMode, "rewrite-local-path", StringComparison.OrdinalIgnoreCase)) &&
            normalizedTargetPathPrefix is null)
        {
            throw new ArgumentException("A path-shaped ingress answer requires a target path prefix.", nameof(targetPathPrefix));
        }

        if (string.Equals(normalizedIngressMode, "opaque-endpoint", StringComparison.OrdinalIgnoreCase) &&
            canMaterialize)
        {
            throw new ArgumentException("An opaque-endpoint ingress answer cannot claim generic materialization support.", nameof(canMaterialize));
        }

        RouteId = routeId.Trim();
        SourceModuleId = sourceModuleId.Trim();
        DisplayName = displayName.Trim();
        Description = description.Trim();
        PathPrefix = NormalizeRequiredPathPrefix(pathPrefix);
        RequestedTarget = requestedTarget;
        EffectiveTarget = effectiveTarget;
        RequestedTargetSource = requestedTargetSource.Trim();
        SelectionMode = selectionMode.Trim();
        SelectedEndpoint = selectedEndpoint.Trim();
        SelectedEndpointKind = normalizedSelectedEndpointKind;
        IngressMode = normalizedIngressMode;
        CanMaterialize = canMaterialize;
        TargetPathPrefix = normalizedTargetPathPrefix;
        TargetQuery = normalizedTargetQuery;
        TargetUri = normalizedTargetUri;
        Methods = NormalizeMethods(methods);
        ProgressState = string.IsNullOrWhiteSpace(progressState)
            ? "not-started"
            : progressState.Trim();
        ProgressPercent = progressPercent;
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
        RuntimeMetadata = runtimeMetadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(runtimeMetadata, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the stable route identifier.
    /// </summary>
    public string RouteId { get; }

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
    /// Gets the rooted public path prefix that matches this route.
    /// </summary>
    public string PathPrefix { get; }

    /// <summary>
    /// Gets the target requested after applying migration-policy overlays.
    /// </summary>
    public StranglerFigTarget RequestedTarget { get; }

    /// <summary>
    /// Gets the target that will actually receive traffic after endpoint fallback is considered.
    /// </summary>
    public StranglerFigTarget EffectiveTarget { get; }

    /// <summary>
    /// Gets the source of the requested target, such as <c>authored-route</c>, <c>migration-default</c>, or <c>migration-route</c>.
    /// </summary>
    public string RequestedTargetSource { get; }

    /// <summary>
    /// Gets the runtime selection result, such as <c>requested-target</c> or <c>fallback-target</c>.
    /// </summary>
    public string SelectionMode { get; }

    /// <summary>
    /// Gets the concrete endpoint or boundary identifier that will receive traffic.
    /// </summary>
    public string SelectedEndpoint { get; }

    /// <summary>
    /// Gets the normalized selected-endpoint kind, such as <c>local-path</c>, <c>absolute-uri</c>, or <c>opaque</c>.
    /// </summary>
    public string SelectedEndpointKind { get; }

    /// <summary>
    /// Gets the normalized ingress follow-through mode, such as <c>pass-through</c>, <c>rewrite-local-path</c>, <c>proxy-absolute-uri</c>, or <c>opaque-endpoint</c>.
    /// </summary>
    public string IngressMode { get; }

    /// <summary>
    /// Gets a value indicating whether a generic ingress or traffic manager can materialize this selected endpoint directly.
    /// </summary>
    public bool CanMaterialize { get; }

    /// <summary>
    /// Gets the normalized path prefix that traffic should land on when the selected endpoint is path-shaped.
    /// </summary>
    public string? TargetPathPrefix { get; }

    /// <summary>
    /// Gets the normalized base query string that should flow with the selected endpoint when one exists.
    /// </summary>
    public string? TargetQuery { get; }

    /// <summary>
    /// Gets the normalized absolute URI that traffic should target when the selected endpoint is absolute.
    /// </summary>
    public string? TargetUri { get; }

    /// <summary>
    /// Gets the normalized request methods that this route matches.
    /// </summary>
    public IReadOnlyList<string> Methods { get; }

    /// <summary>
    /// Gets the normalized migration-progress state for the route.
    /// </summary>
    public string ProgressState { get; }

    /// <summary>
    /// Gets the normalized migration-progress percentage for the route.
    /// </summary>
    public int ProgressPercent { get; }

    /// <summary>
    /// Gets the original authored route metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    /// <summary>
    /// Gets runtime-only metadata such as notes or overlay provenance.
    /// </summary>
    public IReadOnlyDictionary<string, string> RuntimeMetadata { get; }

    private static string NormalizeRequiredPathPrefix(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Route path prefix is required.", nameof(value));
        }

        return NormalizePathPrefix(value)!;
    }

    private static string? NormalizeOptionalPathPrefix(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : NormalizePathPrefix(value);
    }

    private static string NormalizePathPrefix(string value)
    {
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

    private static string? NormalizeOptionalQuery(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || string.Equals(trimmed, "?", StringComparison.Ordinal))
        {
            return null;
        }

        return trimmed.StartsWith('?')
            ? trimmed
            : "?" + trimmed;
    }

    private static string? NormalizeOptionalUri(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri))
        {
            throw new ArgumentException("Target URI must be an absolute URI.", nameof(value));
        }

        return uri.AbsoluteUri;
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
