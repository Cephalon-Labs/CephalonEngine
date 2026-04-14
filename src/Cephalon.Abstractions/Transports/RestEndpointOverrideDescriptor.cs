namespace Cephalon.Abstractions.Transports;

/// <summary>
/// Describes one host-level REST endpoint override rule visible to the current runtime.
/// </summary>
public sealed class RestEndpointOverrideDescriptor
{
    /// <summary>
    /// Creates a REST endpoint override descriptor.
    /// </summary>
    /// <param name="id">The stable override identifier.</param>
    /// <param name="behaviorIds">The behavior identifiers targeted by the override rule.</param>
    /// <param name="sourceModuleIds">The source-module identifiers targeted by the override rule.</param>
    /// <param name="authoringStyles">The normalized shorthand authoring styles targeted by the override rule.</param>
    /// <param name="apiVersionMajors">The effective API major versions targeted by the override rule.</param>
    /// <param name="methods">The effective HTTP methods targeted by the override rule.</param>
    /// <param name="relativePatterns">The shorthand relative route patterns targeted by the override rule.</param>
    /// <param name="routeGroupPrefixes">The published route-group prefixes targeted by the override rule.</param>
    /// <param name="apiVersionMajor">The effective API major version applied when the rule matches.</param>
    /// <param name="method">The effective HTTP method applied when the rule matches.</param>
    /// <param name="pattern">The effective relative route pattern applied when the rule matches.</param>
    /// <param name="bindings">The effective explicit request-binding plan applied when the rule matches.</param>
    public RestEndpointOverrideDescriptor(
        string id,
        IReadOnlyList<string>? behaviorIds = null,
        IReadOnlyList<string>? sourceModuleIds = null,
        IReadOnlyList<string>? authoringStyles = null,
        IReadOnlyList<int>? apiVersionMajors = null,
        IReadOnlyList<string>? methods = null,
        IReadOnlyList<string>? relativePatterns = null,
        IReadOnlyList<string>? routeGroupPrefixes = null,
        int? apiVersionMajor = null,
        string? method = null,
        string? pattern = null,
        IReadOnlyList<RestEndpointBindingDescriptor>? bindings = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("A non-empty override id is required.", nameof(id));
        }

        if (apiVersionMajor.HasValue && apiVersionMajor <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(apiVersionMajor),
                apiVersionMajor,
                "REST endpoint override rules require a positive API major version.");
        }

        Id = id.Trim();
        BehaviorIds = NormalizeList(behaviorIds);
        SourceModuleIds = NormalizeList(sourceModuleIds);
        AuthoringStyles = NormalizeList(authoringStyles);
        ApiVersionMajors = NormalizeIntList(apiVersionMajors);
        Methods = NormalizeList(methods);
        RelativePatterns = NormalizeList(relativePatterns);
        RouteGroupPrefixes = NormalizeList(routeGroupPrefixes);
        ApiVersionMajor = apiVersionMajor;
        Method = NormalizeMethod(method);
        Pattern = NormalizePattern(pattern);
        Bindings = NormalizeBindings(bindings);

        if (!ApiVersionMajor.HasValue && Method is null && Pattern is null && Bindings.Count == 0)
        {
            throw new ArgumentException(
                "REST endpoint override descriptors require at least one override action such as ApiVersionMajor, Method, Pattern, or Bindings.",
                nameof(apiVersionMajor));
        }
    }

    /// <summary>
    /// Gets the stable override identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the behavior identifiers targeted by this override rule.
    /// </summary>
    public IReadOnlyList<string> BehaviorIds { get; }

    /// <summary>
    /// Gets the source-module identifiers targeted by this override rule.
    /// </summary>
    public IReadOnlyList<string> SourceModuleIds { get; }

    /// <summary>
    /// Gets the normalized shorthand authoring styles targeted by this override rule.
    /// </summary>
    public IReadOnlyList<string> AuthoringStyles { get; }

    /// <summary>
    /// Gets the effective API major versions targeted by this override rule.
    /// </summary>
    public IReadOnlyList<int> ApiVersionMajors { get; }

    /// <summary>
    /// Gets the effective HTTP methods targeted by this override rule.
    /// </summary>
    public IReadOnlyList<string> Methods { get; }

    /// <summary>
    /// Gets the shorthand relative route patterns targeted by this override rule.
    /// </summary>
    public IReadOnlyList<string> RelativePatterns { get; }

    /// <summary>
    /// Gets the published route-group prefixes targeted by this override rule.
    /// </summary>
    public IReadOnlyList<string> RouteGroupPrefixes { get; }

    /// <summary>
    /// Gets the effective API major version applied when this override rule matches.
    /// </summary>
    public int? ApiVersionMajor { get; }

    /// <summary>
    /// Gets the effective HTTP method applied when this override rule matches.
    /// </summary>
    public string? Method { get; }

    /// <summary>
    /// Gets the effective relative route pattern applied when this override rule matches.
    /// </summary>
    public string? Pattern { get; }

    /// <summary>
    /// Gets the effective explicit request-binding plan applied when this override rule matches.
    /// </summary>
    public IReadOnlyList<RestEndpointBindingDescriptor> Bindings { get; }

    private static string[] NormalizeList(IReadOnlyList<string>? values)
    {
        return values?
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }

    private static string? NormalizeMethod(string? method)
    {
        if (string.IsNullOrWhiteSpace(method))
        {
            return null;
        }

        return method.Trim().ToUpperInvariant() switch
        {
            "GET" => "GET",
            "POST" => "POST",
            "PUT" => "PUT",
            "PATCH" => "PATCH",
            "DELETE" => "DELETE",
            _ => throw new ArgumentException(
                $"REST endpoint override method '{method}' is not supported. Supported methods: GET, POST, PUT, PATCH, DELETE.",
                nameof(method))
        };
    }

    private static string? NormalizePattern(string? pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return null;
        }

        var normalized = pattern.Trim();
        if (!normalized.StartsWith('/'))
        {
            throw new ArgumentException(
                "REST endpoint override patterns must start with '/'.",
                nameof(pattern));
        }

        return normalized;
    }

    private static RestEndpointBindingDescriptor[] NormalizeBindings(
        IReadOnlyList<RestEndpointBindingDescriptor>? bindings)
    {
        return bindings?
            .Where(static binding => binding is not null)
            .Select(static binding => new RestEndpointBindingDescriptor(
                binding.PropertyName,
                binding.Source,
                binding.Name))
            .ToArray() ?? [];
    }

    private static int[] NormalizeIntList(IReadOnlyList<int>? values)
    {
        return values?
            .Distinct()
            .OrderBy(static value => value)
            .ToArray() ?? [];
    }
}
