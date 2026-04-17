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
    /// <param name="candidateIds">The original shorthand candidate identifiers targeted by the override rule.</param>
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
    /// <param name="routeGroupPrefix">The effective published route-group prefix applied when the rule matches.</param>
    /// <param name="openApiDocumentName">The effective OpenAPI document name applied when the rule matches.</param>
    /// <param name="tagName">The effective primary OpenAPI tag name applied when the rule matches.</param>
    /// <param name="endpointName">The effective endpoint name applied when the rule matches.</param>
    /// <param name="summary">The effective OpenAPI summary applied when the rule matches.</param>
    /// <param name="description">The effective OpenAPI description applied when the rule matches.</param>
    /// <param name="requiredCapabilityKey">
    /// The required Cephalon capability key enforced at the REST boundary when the rule matches.
    /// </param>
    /// <param name="clearRequiredCapability">
    /// <see langword="true" /> when the rule removes any previously declared Cephalon capability
    /// boundary from the matched shorthand candidate.
    /// </param>
    /// <param name="bindings">The effective explicit request-binding plan applied when the rule matches.</param>
    /// <param name="removedBindingProperties">
    /// The explicit shorthand binding properties removed from the source binding plan when the rule
    /// matches.
    /// </param>
    /// <param name="bindingMode">
    /// The mode used to apply <paramref name="bindings" /> and
    /// <paramref name="removedBindingProperties" /> to the shorthand candidate's explicit binding
    /// plan.
    /// </param>
    /// <param name="clearEndpointName">
    /// <see langword="true" /> when the rule removes any previously declared shorthand endpoint
    /// name from the matched candidate.
    /// </param>
    /// <param name="clearSummary">
    /// <see langword="true" /> when the rule removes any previously declared shorthand endpoint
    /// summary from the matched candidate.
    /// </param>
    /// <param name="clearDescription">
    /// <see langword="true" /> when the rule removes any previously declared shorthand endpoint
    /// description from the matched candidate.
    /// </param>
    public RestEndpointOverrideDescriptor(
        string id,
        IReadOnlyList<string>? candidateIds = null,
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
        string? routeGroupPrefix = null,
        string? openApiDocumentName = null,
        string? tagName = null,
        string? endpointName = null,
        string? summary = null,
        string? description = null,
        string? requiredCapabilityKey = null,
        bool clearRequiredCapability = false,
        IReadOnlyList<RestEndpointBindingDescriptor>? bindings = null,
        IReadOnlyList<string>? removedBindingProperties = null,
        RestEndpointOverrideBindingMode bindingMode = RestEndpointOverrideBindingMode.Unspecified,
        bool clearEndpointName = false,
        bool clearSummary = false,
        bool clearDescription = false)
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
        CandidateIds = NormalizeList(candidateIds);
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
        RouteGroupPrefix = NormalizeRouteGroupPrefix(routeGroupPrefix);
        OpenApiDocumentName = NormalizeNonEmptyValue(openApiDocumentName);
        TagName = NormalizeNonEmptyValue(tagName);
        EndpointName = NormalizeNonEmptyValue(endpointName);
        Summary = NormalizeNonEmptyValue(summary);
        Description = NormalizeNonEmptyValue(description);
        RequiredCapabilityKey = NormalizeNonEmptyValue(requiredCapabilityKey);
        ClearRequiredCapability = clearRequiredCapability;
        ClearEndpointName = clearEndpointName;
        ClearSummary = clearSummary;
        ClearDescription = clearDescription;
        Bindings = NormalizeBindings(bindings);
        RemovedBindingProperties = NormalizeList(removedBindingProperties);
        BindingMode = NormalizeBindingMode(bindingMode, RemovedBindingProperties.Count > 0);

        if (ClearRequiredCapability && RequiredCapabilityKey is not null)
        {
            throw new ArgumentException(
                "REST endpoint override descriptors cannot both set RequiredCapabilityKey and ClearRequiredCapability in the same rule.",
                nameof(clearRequiredCapability));
        }

        if (ClearEndpointName && EndpointName is not null)
        {
            throw new ArgumentException(
                "REST endpoint override descriptors cannot both set EndpointName and ClearEndpointName in the same rule.",
                nameof(clearEndpointName));
        }

        if (ClearSummary && Summary is not null)
        {
            throw new ArgumentException(
                "REST endpoint override descriptors cannot both set Summary and ClearSummary in the same rule.",
                nameof(clearSummary));
        }

        if (ClearDescription && Description is not null)
        {
            throw new ArgumentException(
                "REST endpoint override descriptors cannot both set Description and ClearDescription in the same rule.",
                nameof(clearDescription));
        }

        if (!ApiVersionMajor.HasValue &&
            Method is null &&
            Pattern is null &&
            RouteGroupPrefix is null &&
            OpenApiDocumentName is null &&
            TagName is null &&
            EndpointName is null &&
            Summary is null &&
            Description is null &&
            !ClearEndpointName &&
            !ClearSummary &&
            !ClearDescription &&
            RequiredCapabilityKey is null &&
            !ClearRequiredCapability &&
            Bindings.Count == 0 &&
            RemovedBindingProperties.Count == 0)
        {
            throw new ArgumentException(
                "REST endpoint override descriptors require at least one override action such as ApiVersionMajor, Method, Pattern, RouteGroupPrefix, OpenApiDocumentName, TagName, EndpointName, Summary, Description, ClearEndpointName, ClearSummary, ClearDescription, RequiredCapabilityKey, ClearRequiredCapability, Bindings, or RemovedBindingProperties.",
                nameof(apiVersionMajor));
        }

        if (Bindings.Count == 0 &&
            RemovedBindingProperties.Count == 0 &&
            BindingMode == RestEndpointOverrideBindingMode.MergeExplicit)
        {
            throw new ArgumentException(
                "REST endpoint override descriptors cannot use MergeExplicit binding mode without configured Bindings or RemovedBindingProperties.",
                nameof(bindingMode));
        }

        ValidateRemovedBindingProperties(Bindings, RemovedBindingProperties);
    }

    /// <summary>
    /// Gets the stable override identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the original shorthand candidate identifiers targeted by this override rule.
    /// </summary>
    public IReadOnlyList<string> CandidateIds { get; }

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
    /// Gets the effective published route-group prefix applied when this override rule matches.
    /// </summary>
    public string? RouteGroupPrefix { get; }

    /// <summary>
    /// Gets the effective OpenAPI document name applied when this override rule matches.
    /// </summary>
    public string? OpenApiDocumentName { get; }

    /// <summary>
    /// Gets the effective primary OpenAPI tag name applied when this override rule matches.
    /// </summary>
    public string? TagName { get; }

    /// <summary>
    /// Gets the effective endpoint name applied when this override rule matches.
    /// </summary>
    public string? EndpointName { get; }

    /// <summary>
    /// Gets the effective OpenAPI summary applied when this override rule matches.
    /// </summary>
    public string? Summary { get; }

    /// <summary>
    /// Gets the effective OpenAPI description applied when this override rule matches.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets a value indicating whether this override rule clears any previously declared endpoint
    /// name from the matched shorthand candidate.
    /// </summary>
    public bool ClearEndpointName { get; }

    /// <summary>
    /// Gets a value indicating whether this override rule clears any previously declared endpoint
    /// summary from the matched shorthand candidate.
    /// </summary>
    public bool ClearSummary { get; }

    /// <summary>
    /// Gets a value indicating whether this override rule clears any previously declared endpoint
    /// description from the matched shorthand candidate.
    /// </summary>
    public bool ClearDescription { get; }

    /// <summary>
    /// Gets the required Cephalon capability key enforced at the REST boundary when this override rule matches.
    /// </summary>
    public string? RequiredCapabilityKey { get; }

    /// <summary>
    /// Gets a value indicating whether this override rule clears any previously declared Cephalon
    /// capability boundary from the matched shorthand candidate.
    /// </summary>
    public bool ClearRequiredCapability { get; }

    /// <summary>
    /// Gets the effective explicit request-binding plan applied when this override rule matches.
    /// </summary>
    public IReadOnlyList<RestEndpointBindingDescriptor> Bindings { get; }

    /// <summary>
    /// Gets the explicit shorthand binding properties removed from the source binding plan when this override rule matches.
    /// </summary>
    public IReadOnlyList<string> RemovedBindingProperties { get; }

    /// <summary>
    /// Gets how <see cref="Bindings" /> and <see cref="RemovedBindingProperties" /> apply to the shorthand candidate's explicit binding plan.
    /// </summary>
    public RestEndpointOverrideBindingMode BindingMode { get; }

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

    private static string? NormalizeRouteGroupPrefix(string? routeGroupPrefix)
    {
        if (string.IsNullOrWhiteSpace(routeGroupPrefix))
        {
            return null;
        }

        var normalized = routeGroupPrefix.Trim();
        if (!normalized.StartsWith('/'))
        {
            throw new ArgumentException(
                "REST endpoint override route-group prefixes must start with '/'.",
                nameof(routeGroupPrefix));
        }

        return normalized;
    }

    private static string? NormalizeNonEmptyValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
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

    private static void ValidateRemovedBindingProperties(
        IReadOnlyList<RestEndpointBindingDescriptor> bindings,
        IReadOnlyList<string> removedBindingProperties)
    {
        ArgumentNullException.ThrowIfNull(bindings);
        ArgumentNullException.ThrowIfNull(removedBindingProperties);

        if (bindings.Count == 0 || removedBindingProperties.Count == 0)
        {
            return;
        }

        var bindingProperties = bindings
            .Select(static binding => binding.PropertyName.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var duplicateProperty = removedBindingProperties.FirstOrDefault(bindingProperties.Contains);
        if (duplicateProperty is not null)
        {
            throw new ArgumentException(
                $"REST endpoint override descriptors cannot both remove and override explicit binding property '{duplicateProperty}' in the same rule.",
                nameof(removedBindingProperties));
        }
    }

    private static int[] NormalizeIntList(IReadOnlyList<int>? values)
    {
        return values?
            .Distinct()
            .OrderBy(static value => value)
            .ToArray() ?? [];
    }

    private static RestEndpointOverrideBindingMode NormalizeBindingMode(
        RestEndpointOverrideBindingMode bindingMode,
        bool hasRemovedBindingProperties)
    {
        return bindingMode switch
        {
            RestEndpointOverrideBindingMode.Unspecified => hasRemovedBindingProperties
                ? RestEndpointOverrideBindingMode.MergeExplicit
                : RestEndpointOverrideBindingMode.ReplaceExplicit,
            RestEndpointOverrideBindingMode.ReplaceExplicit when hasRemovedBindingProperties => throw new ArgumentException(
                "REST endpoint override descriptors cannot use ReplaceExplicit binding mode with RemovedBindingProperties. Use MergeExplicit when removing shorthand bindings.",
                nameof(bindingMode)),
            RestEndpointOverrideBindingMode.ReplaceExplicit => RestEndpointOverrideBindingMode.ReplaceExplicit,
            RestEndpointOverrideBindingMode.MergeExplicit => RestEndpointOverrideBindingMode.MergeExplicit,
            _ => throw new ArgumentOutOfRangeException(
                nameof(bindingMode),
                bindingMode,
                "REST endpoint override binding mode must be ReplaceExplicit or MergeExplicit.")
        };
    }
}
