namespace Cephalon.Abstractions.Transports;

/// <summary>
/// Describes one host-level REST endpoint override rule visible to the current runtime for
/// module-owned REST candidates that participate in host governance.
/// </summary>
public sealed class RestEndpointOverrideDescriptor
{
    /// <summary>
    /// Creates a REST endpoint override descriptor.
    /// </summary>
    /// <param name="id">The stable override identifier.</param>
    /// <param name="candidateIds">The original candidate identifiers targeted by the override rule.</param>
    /// <param name="behaviorIds">The behavior identifiers targeted by the override rule.</param>
    /// <param name="sourceModuleIds">The source-module identifiers targeted by the override rule.</param>
    /// <param name="authoringStyles">
    /// The normalized authoring styles targeted by the override rule. Explicit module-DSL routes
    /// participate only when their owning route group opted into host governance.
    /// </param>
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
    /// boundary from the matched candidate.
    /// </param>
    /// <param name="bindings">The effective explicit request-binding plan applied when the rule matches.</param>
    /// <param name="removedBindingProperties">
    /// The explicit binding properties removed from the source binding plan when the rule matches.
    /// </param>
    /// <param name="clearBindings">
    /// <see langword="true" /> when the rule removes the matched candidate's entire explicit
    /// binding plan and returns publication to the implicit request-binding baseline.
    /// </param>
    /// <param name="bindingMode">
    /// The mode used to apply <paramref name="bindings" /> and
    /// <paramref name="removedBindingProperties" /> to the candidate's explicit binding plan.
    /// </param>
    /// <param name="clearEndpointName">
    /// <see langword="true" /> when the rule removes any previously declared endpoint name from
    /// the matched candidate.
    /// </param>
    /// <param name="clearSummary">
    /// <see langword="true" /> when the rule removes any previously declared endpoint summary
    /// from the matched candidate.
    /// </param>
    /// <param name="clearDescription">
    /// <see langword="true" /> when the rule removes any previously declared endpoint description
    /// from the matched candidate.
    /// </param>
    /// <param name="openApiDocumentNames">
    /// The original candidate OpenAPI document names targeted by the override rule before override
    /// actions are applied.
    /// </param>
    /// <param name="tagNames">
    /// The original candidate primary OpenAPI tag names targeted by the override rule before
    /// override actions are applied.
    /// </param>
    /// <param name="bindingFallbackModes">
    /// The original candidate request-binding fallback modes targeted by the override rule before
    /// override actions are applied.
    /// </param>
    /// <param name="targetBindings">
    /// The original candidate explicit binding descriptors targeted by the override rule before
    /// override actions are applied.
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
        bool clearBindings = false,
        RestEndpointOverrideBindingMode bindingMode = RestEndpointOverrideBindingMode.Unspecified,
        bool clearEndpointName = false,
        bool clearSummary = false,
        bool clearDescription = false,
        IReadOnlyList<string>? openApiDocumentNames = null,
        IReadOnlyList<string>? tagNames = null,
        IReadOnlyList<RestEndpointBindingFallbackMode>? bindingFallbackModes = null,
        IReadOnlyList<RestEndpointBindingDescriptor>? targetBindings = null)
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
        OpenApiDocumentNames = NormalizeList(openApiDocumentNames);
        TagNames = NormalizeList(tagNames);
        BindingFallbackModes = NormalizeBindingFallbackModes(bindingFallbackModes);
        TargetBindings = NormalizeTargetBindings(targetBindings, nameof(targetBindings));
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
        ClearBindings = clearBindings;
        BindingMode = NormalizeBindingMode(
            bindingMode,
            RemovedBindingProperties.Count > 0,
            clearBindings);

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

        if (ClearBindings && (Bindings.Count > 0 || RemovedBindingProperties.Count > 0))
        {
            throw new ArgumentException(
                "REST endpoint override descriptors cannot combine ClearBindings with Bindings or RemovedBindingProperties in the same rule.",
                nameof(clearBindings));
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
            !ClearBindings &&
            Bindings.Count == 0 &&
            RemovedBindingProperties.Count == 0)
        {
            throw new ArgumentException(
                "REST endpoint override descriptors require at least one override action such as ApiVersionMajor, Method, Pattern, RouteGroupPrefix, OpenApiDocumentName, TagName, EndpointName, Summary, Description, ClearEndpointName, ClearSummary, ClearDescription, RequiredCapabilityKey, ClearRequiredCapability, ClearBindings, Bindings, or RemovedBindingProperties.",
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
    /// Gets the original candidate identifiers targeted by this override rule.
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
    /// Gets the normalized authoring styles targeted by this override rule. Explicit module-DSL
    /// routes participate only when their owning route group opted into host governance.
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
    /// Gets the relative route patterns targeted by this override rule.
    /// </summary>
    public IReadOnlyList<string> RelativePatterns { get; }

    /// <summary>
    /// Gets the published route-group prefixes targeted by this override rule.
    /// </summary>
    public IReadOnlyList<string> RouteGroupPrefixes { get; }

    /// <summary>
    /// Gets the original candidate OpenAPI document names targeted by this override rule.
    /// </summary>
    public IReadOnlyList<string> OpenApiDocumentNames { get; }

    /// <summary>
    /// Gets the original candidate primary OpenAPI tag names targeted by this override rule.
    /// </summary>
    public IReadOnlyList<string> TagNames { get; }

    /// <summary>
    /// Gets the original candidate request-binding fallback modes targeted by this override rule.
    /// </summary>
    public IReadOnlyList<RestEndpointBindingFallbackMode> BindingFallbackModes { get; }

    /// <summary>
    /// Gets the original candidate explicit binding descriptors targeted by this override rule
    /// before override actions are applied.
    /// </summary>
    public IReadOnlyList<RestEndpointBindingDescriptor> TargetBindings { get; }

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
    /// name from the matched candidate.
    /// </summary>
    public bool ClearEndpointName { get; }

    /// <summary>
    /// Gets a value indicating whether this override rule clears any previously declared endpoint
    /// summary from the matched candidate.
    /// </summary>
    public bool ClearSummary { get; }

    /// <summary>
    /// Gets a value indicating whether this override rule clears any previously declared endpoint
    /// description from the matched candidate.
    /// </summary>
    public bool ClearDescription { get; }

    /// <summary>
    /// Gets the required Cephalon capability key enforced at the REST boundary when this override rule matches.
    /// </summary>
    public string? RequiredCapabilityKey { get; }

    /// <summary>
    /// Gets a value indicating whether this override rule clears any previously declared Cephalon
    /// capability boundary from the matched candidate.
    /// </summary>
    public bool ClearRequiredCapability { get; }

    /// <summary>
    /// Gets the effective explicit request-binding plan applied when this override rule matches.
    /// </summary>
    public IReadOnlyList<RestEndpointBindingDescriptor> Bindings { get; }

    /// <summary>
    /// Gets the explicit binding properties removed from the source binding plan when this override rule matches.
    /// </summary>
    public IReadOnlyList<string> RemovedBindingProperties { get; }

    /// <summary>
    /// Gets a value indicating whether this override rule clears the matched candidate's entire
    /// explicit binding plan.
    /// </summary>
    public bool ClearBindings { get; }

    /// <summary>
    /// Gets how <see cref="Bindings" /> and <see cref="RemovedBindingProperties" /> apply to the
    /// candidate's explicit binding plan.
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

    private static RestEndpointBindingDescriptor[] NormalizeTargetBindings(
        IReadOnlyList<RestEndpointBindingDescriptor>? bindings,
        string paramName)
    {
        if (bindings is null)
        {
            return [];
        }

        var normalized = new List<RestEndpointBindingDescriptor>(bindings.Count);
        var seenByProperty = new Dictionary<string, RestEndpointBindingDescriptor>(StringComparer.OrdinalIgnoreCase);
        foreach (var bindingValue in bindings)
        {
            if (bindingValue is null)
            {
                continue;
            }

            var binding = new RestEndpointBindingDescriptor(
                bindingValue.PropertyName,
                bindingValue.Source,
                bindingValue.Name);
            var propertyName = binding.PropertyName.Trim();
            if (seenByProperty.TryGetValue(propertyName, out var existing))
            {
                if (existing.Source != binding.Source ||
                    !string.Equals(existing.Name, binding.Name, StringComparison.Ordinal))
                {
                    throw new ArgumentException(
                        $"REST endpoint override target bindings cannot declare more than one binding selector for property '{propertyName}'.",
                        paramName);
                }

                continue;
            }

            seenByProperty[propertyName] = binding;
            normalized.Add(binding);
        }

        return normalized
            .OrderBy(static binding => binding.PropertyName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static binding => binding.Source)
            .ThenBy(static binding => binding.Name ?? string.Empty, StringComparer.Ordinal)
            .ToArray();
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
        bool hasRemovedBindingProperties,
        bool clearBindings)
    {
        if (clearBindings)
        {
            return bindingMode == RestEndpointOverrideBindingMode.Unspecified
                ? RestEndpointOverrideBindingMode.Unspecified
                : throw new ArgumentException(
                    "REST endpoint override descriptors cannot set BindingMode when ClearBindings is true.",
                    nameof(bindingMode));
        }

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

    private static RestEndpointBindingFallbackMode[] NormalizeBindingFallbackModes(
        IReadOnlyList<RestEndpointBindingFallbackMode>? values)
    {
        if (values is null)
        {
            return [];
        }

        var normalized = values
            .Distinct()
            .OrderBy(static value => value)
            .ToArray();
        if (normalized.Any(static value => !Enum.IsDefined(value)))
        {
            throw new ArgumentOutOfRangeException(
                nameof(values),
                "REST endpoint override binding fallback selectors must use supported fallback modes.");
        }

        return normalized;
    }
}
