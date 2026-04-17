using Cephalon.Abstractions.Transports;
using Cephalon.AspNetCore.Transports.Rest;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Cephalon.AspNetCore.Hosting;

/// <summary>
/// Describes one host-level override rule for descriptor-backed REST shorthand candidates.
/// </summary>
public sealed class RestEndpointOverrideOptions
{
    private static readonly string[] DefaultAuthoringStyles =
    [
        RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle,
        RestEndpointRuntimeMetadata.BehaviorModuleProfileAuthoringStyle
    ];

    private static readonly HashSet<string> SupportedAuthoringStyles = new(
        DefaultAuthoringStyles,
        StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="RestEndpointOverrideOptions" /> class.
    /// </summary>
    /// <param name="id">The stable override identifier.</param>
    /// <param name="candidateIds">
    /// The original shorthand candidate identifiers targeted by the override rule.
    /// </param>
    /// <param name="behaviorIds">The behavior identifiers targeted by the override rule.</param>
    /// <param name="sourceModuleIds">The source-module identifiers targeted by the override rule.</param>
    /// <param name="authoringStyles">
    /// The shorthand authoring styles targeted by the override rule. When omitted, the rule
    /// targets both <c>behavior-module-profile</c> and <c>behavior-module-generated</c>.
    /// </param>
    /// <param name="apiVersionMajors">
    /// The effective API major versions targeted by the override rule before any override actions
    /// are applied.
    /// </param>
    /// <param name="methods">
    /// The effective HTTP methods targeted by the override rule before any override actions are
    /// applied.
    /// </param>
    /// <param name="relativePatterns">
    /// The shorthand relative route patterns targeted by the override rule before any override
    /// actions are applied.
    /// </param>
    /// <param name="routeGroupPrefixes">
    /// The published route-group prefixes targeted by the override rule before any override actions
    /// are applied.
    /// </param>
    /// <param name="apiVersionMajor">
    /// The effective API major version applied when the rule matches a shorthand candidate.
    /// </param>
    /// <param name="method">
    /// The effective HTTP method applied when the rule matches a shorthand candidate.
    /// </param>
    /// <param name="pattern">
    /// The effective relative route pattern applied when the rule matches a shorthand candidate.
    /// </param>
    /// <param name="routeGroupPrefix">
    /// The effective published route-group prefix applied when the rule matches a shorthand
    /// candidate.
    /// </param>
    /// <param name="tagName">
    /// The effective primary OpenAPI tag name applied when the rule matches a shorthand candidate.
    /// </param>
    /// <param name="endpointName">
    /// The effective endpoint name applied when the rule matches a shorthand candidate.
    /// </param>
    /// <param name="summary">
    /// The effective OpenAPI summary applied when the rule matches a shorthand candidate.
    /// </param>
    /// <param name="description">
    /// The effective OpenAPI description applied when the rule matches a shorthand candidate.
    /// </param>
    /// <param name="requiredCapabilityKey">
    /// The required Cephalon capability key enforced at the REST boundary when the rule matches a shorthand candidate.
    /// </param>
    /// <param name="clearRequiredCapability">
    /// <see langword="true" /> when the rule removes any previously declared Cephalon capability
    /// boundary from the matched shorthand candidate.
    /// </param>
    /// <param name="bindings">
    /// The effective explicit request-binding plan applied when the rule matches a shorthand
    /// candidate.
    /// </param>
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
    public RestEndpointOverrideOptions(
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
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        Id = id.Trim();
        CandidateIds = NormalizeList(candidateIds);
        BehaviorIds = NormalizeList(behaviorIds);
        SourceModuleIds = NormalizeList(sourceModuleIds);
        AuthoringStyles = NormalizeAuthoringStyles(authoringStyles);
        ApiVersionMajors = NormalizePositiveIntegers(
            apiVersionMajors,
            nameof(apiVersionMajors),
            "REST endpoint override API major selectors must be positive integers.");
        Methods = NormalizeMethods(
            methods,
            nameof(methods),
            "REST endpoint override target method");
        RelativePatterns = NormalizeRoutePatterns(
            relativePatterns,
            nameof(relativePatterns),
            "REST endpoint override target relative pattern");
        RouteGroupPrefixes = NormalizeRoutePatterns(
            routeGroupPrefixes,
            nameof(routeGroupPrefixes),
            "REST endpoint override target route-group prefix");
        Method = NormalizeMethod(method);
        Pattern = NormalizePattern(pattern);
        RouteGroupPrefix = NormalizeRouteGroupPrefix(routeGroupPrefix);
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
                "REST endpoint override rules cannot both set RequiredCapabilityKey and ClearRequiredCapability in the same rule.",
                nameof(clearRequiredCapability));
        }

        if (ClearEndpointName && EndpointName is not null)
        {
            throw new ArgumentException(
                "REST endpoint override rules cannot both set EndpointName and ClearEndpointName in the same rule.",
                nameof(clearEndpointName));
        }

        if (ClearSummary && Summary is not null)
        {
            throw new ArgumentException(
                "REST endpoint override rules cannot both set Summary and ClearSummary in the same rule.",
                nameof(clearSummary));
        }

        if (ClearDescription && Description is not null)
        {
            throw new ArgumentException(
                "REST endpoint override rules cannot both set Description and ClearDescription in the same rule.",
                nameof(clearDescription));
        }

        if (CandidateIds.Count == 0 && BehaviorIds.Count == 0 && SourceModuleIds.Count == 0)
        {
            throw new ArgumentException(
                "REST endpoint override rules must target at least one candidate id, behavior id, or source module id.",
                nameof(candidateIds));
        }

        if (apiVersionMajor.HasValue && apiVersionMajor <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(apiVersionMajor),
                apiVersionMajor,
                "REST endpoint override rules must define a positive API major version.");
        }

        ApiVersionMajor = apiVersionMajor;

        if (!ApiVersionMajor.HasValue &&
            Method is null &&
            Pattern is null &&
            RouteGroupPrefix is null &&
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
                "REST endpoint override rules must define at least one override action such as ApiVersionMajor, Method, Pattern, RouteGroupPrefix, TagName, EndpointName, Summary, Description, ClearEndpointName, ClearSummary, ClearDescription, RequiredCapabilityKey, ClearRequiredCapability, Bindings, or RemovedBindingProperties.",
                nameof(apiVersionMajor));
        }

        if (Bindings.Count == 0 &&
            RemovedBindingProperties.Count == 0 &&
            BindingMode == RestEndpointOverrideBindingMode.MergeExplicit)
        {
            throw new ArgumentException(
                "REST endpoint override rules cannot use MergeExplicit binding mode without configured Bindings or RemovedBindingProperties.",
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
    /// Gets the effective API major versions targeted by this override rule before override actions are applied.
    /// </summary>
    public IReadOnlyList<int> ApiVersionMajors { get; }

    /// <summary>
    /// Gets the effective HTTP methods targeted by this override rule before override actions are applied.
    /// </summary>
    public IReadOnlyList<string> Methods { get; }

    /// <summary>
    /// Gets the shorthand relative route patterns targeted by this override rule before override actions are applied.
    /// </summary>
    public IReadOnlyList<string> RelativePatterns { get; }

    /// <summary>
    /// Gets the published route-group prefixes targeted by this override rule before override actions are applied.
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

    /// <summary>
    /// Gets a value indicating whether any targeting values or override actions were explicitly supplied.
    /// </summary>
    public bool HasValues =>
        CandidateIds.Count > 0 ||
        BehaviorIds.Count > 0 ||
        SourceModuleIds.Count > 0 ||
        ApiVersionMajors.Count > 0 ||
        Methods.Count > 0 ||
        RelativePatterns.Count > 0 ||
        RouteGroupPrefixes.Count > 0 ||
        ApiVersionMajor.HasValue ||
        Method is not null ||
        Pattern is not null ||
        RouteGroupPrefix is not null ||
        TagName is not null ||
        EndpointName is not null ||
        Summary is not null ||
        Description is not null ||
        ClearEndpointName ||
        ClearSummary ||
        ClearDescription ||
        RequiredCapabilityKey is not null ||
        ClearRequiredCapability ||
        Bindings.Count > 0 ||
        RemovedBindingProperties.Count > 0;

    private static string[] NormalizeList(IReadOnlyList<string>? values)
    {
        return values?
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }

    private static string[] NormalizeAuthoringStyles(IReadOnlyList<string>? values)
    {
        var normalized = NormalizeList(values);
        if (normalized.Length == 0)
        {
            return DefaultAuthoringStyles;
        }

        var unsupportedStyle = normalized.FirstOrDefault(style => !SupportedAuthoringStyles.Contains(style));
        if (unsupportedStyle is not null)
        {
            throw new ArgumentException(
                $"REST endpoint override style '{unsupportedStyle}' is not supported. " +
                $"Supported shorthand authoring styles: {string.Join(", ", DefaultAuthoringStyles)}.",
                nameof(values));
        }

        return normalized;
    }

    private static int[] NormalizePositiveIntegers(
        IReadOnlyList<int>? values,
        string paramName,
        string errorMessage)
    {
        if (values is null)
        {
            return [];
        }

        var normalized = values
            .Distinct()
            .OrderBy(static value => value)
            .ToArray();
        if (normalized.Any(static value => value <= 0))
        {
            throw new ArgumentOutOfRangeException(paramName, errorMessage);
        }

        return normalized;
    }

    private static string[] NormalizeMethods(
        IReadOnlyList<string>? values,
        string paramName,
        string errorPrefix)
    {
        return values?
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(value => NormalizeSupportedMethod(value, paramName, errorPrefix))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }

    private static string[] NormalizeRoutePatterns(
        IReadOnlyList<string>? values,
        string paramName,
        string errorPrefix)
    {
        return values?
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(value => NormalizeSupportedRoutePattern(value, paramName, errorPrefix))
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

        return NormalizeSupportedMethod(method, nameof(method), "REST endpoint override method");
    }

    private static string NormalizeSupportedMethod(string method, string paramName, string errorPrefix)
    {
        return method.Trim().ToUpperInvariant() switch
        {
            "GET" => "GET",
            "POST" => "POST",
            "PUT" => "PUT",
            "PATCH" => "PATCH",
            "DELETE" => "DELETE",
            _ => throw new ArgumentException(
                $"{errorPrefix} '{method}' is not supported. Supported methods: GET, POST, PUT, PATCH, DELETE.",
                paramName)
        };
    }

    private static string? NormalizePattern(string? pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return null;
        }

        return NormalizeSupportedRoutePattern(pattern, nameof(pattern), "REST endpoint override pattern");
    }

    private static string? NormalizeRouteGroupPrefix(string? routeGroupPrefix)
    {
        if (string.IsNullOrWhiteSpace(routeGroupPrefix))
        {
            return null;
        }

        return NormalizeSupportedRoutePattern(
            routeGroupPrefix,
            nameof(routeGroupPrefix),
            "REST endpoint override route-group prefix");
    }

    private static string? NormalizeNonEmptyValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static string NormalizeSupportedRoutePattern(string pattern, string paramName, string errorPrefix)
    {
        var normalized = pattern.Trim();
        if (!normalized.StartsWith('/'))
        {
            throw new ArgumentException(
                $"{errorPrefix}s must start with '/'.",
                paramName);
        }

        try
        {
            _ = RoutePatternFactory.Parse(normalized);
        }
        catch (Exception ex)
        {
            throw new ArgumentException(
                $"{errorPrefix} '{pattern}' is not a valid ASP.NET Core route pattern.",
                paramName,
                ex);
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
                $"REST endpoint override rules cannot both remove and override explicit binding property '{duplicateProperty}' in the same rule.",
                nameof(removedBindingProperties));
        }
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
                "REST endpoint override rules cannot use ReplaceExplicit binding mode with RemovedBindingProperties. Use MergeExplicit when removing shorthand bindings.",
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
