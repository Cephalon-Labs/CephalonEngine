using System.Text.Json.Serialization;

namespace Cephalon.Abstractions.Transports;

/// <summary>
/// Describes one module-owned REST candidate projection shape before publication precedence or host-level overrides are applied.
/// </summary>
public sealed class RestEndpointCandidateProjectionDescriptor
{
    /// <summary>
    /// Creates a REST endpoint candidate projection descriptor.
    /// </summary>
    /// <param name="method">The projected HTTP method.</param>
    /// <param name="routePattern">The projected route pattern including the host REST prefix.</param>
    /// <param name="routeGroupPrefix">The projected route-group prefix including the host REST prefix.</param>
    /// <param name="relativePattern">The projected route pattern relative to the owning route group.</param>
    /// <param name="apiVersionMajor">The projected public API major version when one is available.</param>
    /// <param name="openApiDocumentName">The projected OpenAPI document name when one is available.</param>
    /// <param name="bindingDescriptors">The projected request-binding descriptors when the projection exposes an explicit binding plan.</param>
    /// <param name="bindingFallbackMode">
    /// The projected request-binding fallback mode when the projection preserves source shorthand fallback behavior
    /// beyond the explicit binding plan.
    /// </param>
    /// <param name="tagName">The projected primary OpenAPI tag name when one is available.</param>
    public RestEndpointCandidateProjectionDescriptor(
        string method,
        string routePattern,
        string routeGroupPrefix,
        string relativePattern,
        int? apiVersionMajor = null,
        string? openApiDocumentName = null,
        IReadOnlyList<RestEndpointBindingDescriptor>? bindingDescriptors = null,
        RestEndpointBindingFallbackMode? bindingFallbackMode = null,
        string? tagName = null)
    {
        Method = NormalizeRequired(method, nameof(method)).ToUpperInvariant();
        RoutePattern = NormalizeRequired(routePattern, nameof(routePattern));
        RouteGroupPrefix = NormalizeRequired(routeGroupPrefix, nameof(routeGroupPrefix));
        RelativePattern = NormalizeRequired(relativePattern, nameof(relativePattern));
        ApiVersionMajor = apiVersionMajor;
        OpenApiDocumentName = NormalizeOptional(openApiDocumentName);
        BindingDescriptors = NormalizeBindingDescriptors(bindingDescriptors);
        BindingFallbackMode = NormalizeBindingFallbackMode(bindingFallbackMode);
        TagName = NormalizeOptional(tagName);
    }

    /// <summary>
    /// Gets the projected HTTP method.
    /// </summary>
    public string Method { get; }

    /// <summary>
    /// Gets the projected route pattern including the host REST prefix.
    /// </summary>
    public string RoutePattern { get; }

    /// <summary>
    /// Gets the projected route-group prefix including the host REST prefix.
    /// </summary>
    public string RouteGroupPrefix { get; }

    /// <summary>
    /// Gets the projected route pattern relative to the owning route group.
    /// </summary>
    public string RelativePattern { get; }

    /// <summary>
    /// Gets the projected public API major version when one is available.
    /// </summary>
    public int? ApiVersionMajor { get; }

    /// <summary>
    /// Gets the projected OpenAPI document name when one is available.
    /// </summary>
    public string? OpenApiDocumentName { get; }

    /// <summary>
    /// Gets the projected request-binding descriptors when the projection exposes an explicit binding plan.
    /// </summary>
    public IReadOnlyList<RestEndpointBindingDescriptor> BindingDescriptors { get; }

    /// <summary>
    /// Gets the projected request-binding fallback mode when the projection preserves source shorthand
    /// fallback behavior beyond the explicit binding plan.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RestEndpointBindingFallbackMode? BindingFallbackMode { get; }

    /// <summary>
    /// Gets the projected primary OpenAPI tag name when one is available.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TagName { get; }

    private static string NormalizeRequired(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A non-empty value is required.", paramName);
        }

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static RestEndpointBindingDescriptor[] NormalizeBindingDescriptors(
        IReadOnlyList<RestEndpointBindingDescriptor>? values)
    {
        return values?
            .Where(static value => value is not null)
            .Select(static value => new RestEndpointBindingDescriptor(
                value.PropertyName,
                value.Source,
                value.Name))
            .ToArray() ?? [];
    }

    private static RestEndpointBindingFallbackMode? NormalizeBindingFallbackMode(
        RestEndpointBindingFallbackMode? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        if (!Enum.IsDefined(value.Value))
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                "A supported REST endpoint binding fallback mode is required.");
        }

        return value.Value;
    }
}
