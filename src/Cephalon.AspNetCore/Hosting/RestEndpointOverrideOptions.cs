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
        [
            .. DefaultAuthoringStyles,
            RestEndpointRuntimeMetadata.BehaviorModuleDslAuthoringStyle
        ],
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
    /// The module-owned REST authoring styles targeted by the override rule. When omitted, the
    /// rule targets only shorthand styles <c>behavior-module-profile</c> and
    /// <c>behavior-module-generated</c>. Explicit <c>behavior-module-dsl</c> routes participate
    /// only when the owning route group opted into host governance.
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
    /// <param name="openApiDocumentName">
    /// The effective OpenAPI document name applied when the rule matches a shorthand candidate.
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
    /// <param name="requiredFeatureFlagIds">
    /// The required Cephalon feature-flag identifiers enforced at the REST boundary when the rule
    /// matches a shorthand candidate.
    /// </param>
    /// <param name="clearRequiredFeatureFlags">
    /// <see langword="true" /> when the rule removes any previously declared Cephalon
    /// feature-flag requirements from the matched shorthand candidate.
    /// </param>
    /// <param name="bindings">
    /// The effective explicit request-binding plan applied when the rule matches a shorthand
    /// candidate.
    /// </param>
    /// <param name="removedBindingProperties">
    /// The explicit shorthand binding properties removed from the source binding plan when the rule
    /// matches.
    /// </param>
    /// <param name="clearBindings">
    /// <see langword="true" /> when the rule removes the matched shorthand candidate's entire
    /// explicit binding plan and returns publication to the implicit request-binding baseline.
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
    /// <param name="openApiDocumentNames">
    /// The original shorthand OpenAPI document names targeted by the override rule before any
    /// override actions are applied.
    /// </param>
    /// <param name="tagNames">
    /// The original shorthand primary OpenAPI tag names targeted by the override rule before any
    /// override actions are applied.
    /// </param>
    /// <param name="endpointNames">
    /// The original shorthand endpoint names targeted by the override rule before any override
    /// actions are applied.
    /// </param>
    /// <param name="hostGovernanceScopes">
    /// The original shorthand host-governance scopes targeted by the override rule before any
    /// override actions are applied. This selector can also serve as the rule's primary target
    /// when candidate, behavior, and source-module identifiers are intentionally omitted.
    /// </param>
    /// <param name="bindingFallbackModes">
    /// The original shorthand request-binding fallback modes targeted by the override rule before
    /// any override actions are applied.
    /// </param>
    /// <param name="targetBindings">
    /// The original shorthand explicit binding descriptors targeted by the override rule before
    /// any override actions are applied.
    /// </param>
    /// <param name="behaviorIdPrefixes">
    /// The behavior-id prefixes targeted by the override rule. Prefix matches use the stable
    /// dot-separated behavior-id hierarchy, so a prefix targets the exact behavior id and any
    /// descendant behavior ids beneath that prefix.
    /// </param>
    /// <param name="preserveImplicitQueryFallback">
    /// <see langword="true" /> when the rule opts the matched explicit-binding shorthand candidate
    /// into preserved implicit-query fallback for any remaining unbound query properties.
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
        string? openApiDocumentName = null,
        string? tagName = null,
        string? endpointName = null,
        string? summary = null,
        string? description = null,
        string? requiredCapabilityKey = null,
        bool clearRequiredCapability = false,
        IReadOnlyList<string>? requiredFeatureFlagIds = null,
        bool clearRequiredFeatureFlags = false,
        IReadOnlyList<RestEndpointBindingDescriptor>? bindings = null,
        IReadOnlyList<string>? removedBindingProperties = null,
        bool clearBindings = false,
        RestEndpointOverrideBindingMode bindingMode = RestEndpointOverrideBindingMode.Unspecified,
        bool clearEndpointName = false,
        bool clearSummary = false,
        bool clearDescription = false,
        IReadOnlyList<string>? openApiDocumentNames = null,
        IReadOnlyList<string>? tagNames = null,
        IReadOnlyList<string>? endpointNames = null,
        IReadOnlyList<RestEndpointBindingFallbackMode>? bindingFallbackModes = null,
        IReadOnlyList<RestEndpointBindingDescriptor>? targetBindings = null,
        IReadOnlyList<string>? hostGovernanceScopes = null,
        IReadOnlyList<string>? behaviorIdPrefixes = null,
        bool preserveImplicitQueryFallback = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        Id = id.Trim();
        CandidateIds = NormalizeList(candidateIds);
        BehaviorIds = NormalizeList(behaviorIds);
        BehaviorIdPrefixes = NormalizeList(behaviorIdPrefixes);
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
        OpenApiDocumentNames = NormalizeList(openApiDocumentNames);
        TagNames = NormalizeList(tagNames);
        EndpointNames = NormalizeList(endpointNames);
        HostGovernanceScopes = NormalizeList(hostGovernanceScopes);
        BindingFallbackModes = NormalizeBindingFallbackModes(
            bindingFallbackModes,
            nameof(bindingFallbackModes));
        TargetBindings = NormalizeTargetBindings(targetBindings, nameof(targetBindings));
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
        RequiredFeatureFlagIds = NormalizeList(requiredFeatureFlagIds);
        ClearRequiredFeatureFlags = clearRequiredFeatureFlags;
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
        PreserveImplicitQueryFallback = preserveImplicitQueryFallback;

        if (ClearRequiredCapability && RequiredCapabilityKey is not null)
        {
            throw new ArgumentException(
                "REST endpoint override rules cannot both set RequiredCapabilityKey and ClearRequiredCapability in the same rule.",
                nameof(clearRequiredCapability));
        }

        if (ClearRequiredFeatureFlags && RequiredFeatureFlagIds.Count > 0)
        {
            throw new ArgumentException(
                "REST endpoint override rules cannot both set RequiredFeatureFlagIds and ClearRequiredFeatureFlags in the same rule.",
                nameof(clearRequiredFeatureFlags));
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

        if (ClearBindings && (Bindings.Count > 0 || RemovedBindingProperties.Count > 0))
        {
            throw new ArgumentException(
                "REST endpoint override rules cannot combine ClearBindings with Bindings or RemovedBindingProperties in the same rule.",
                nameof(clearBindings));
        }

        if (ClearBindings && PreserveImplicitQueryFallback)
        {
            throw new ArgumentException(
                "REST endpoint override rules cannot combine ClearBindings with PreserveImplicitQueryFallback in the same rule.",
                nameof(preserveImplicitQueryFallback));
        }

        if (CandidateIds.Count == 0 &&
            BehaviorIds.Count == 0 &&
            BehaviorIdPrefixes.Count == 0 &&
            SourceModuleIds.Count == 0 &&
            HostGovernanceScopes.Count == 0)
        {
            throw new ArgumentException(
                "REST endpoint override rules must target at least one candidate id, behavior id, behavior-id prefix, source module id, or host-governance scope.",
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
        ActionKinds = ResolveActionKinds(
            ApiVersionMajor,
            Method,
            Pattern,
            RouteGroupPrefix,
            OpenApiDocumentName,
            TagName,
            EndpointName,
            Summary,
            Description,
            ClearEndpointName,
            ClearSummary,
            ClearDescription,
            RequiredCapabilityKey,
            ClearRequiredCapability,
            RequiredFeatureFlagIds,
            ClearRequiredFeatureFlags,
            Bindings,
            RemovedBindingProperties,
            ClearBindings,
            BindingMode,
            PreserveImplicitQueryFallback);

        if (ActionKinds.Count == 0)
        {
            throw new ArgumentException(
                "REST endpoint override rules must define at least one override action such as ApiVersionMajor, Method, Pattern, RouteGroupPrefix, OpenApiDocumentName, TagName, EndpointName, Summary, Description, ClearEndpointName, ClearSummary, ClearDescription, RequiredCapabilityKey, ClearRequiredCapability, RequiredFeatureFlagIds, ClearRequiredFeatureFlags, ClearBindings, Bindings, or RemovedBindingProperties.",
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
    /// Gets the behavior-id prefixes targeted by this override rule.
    /// </summary>
    public IReadOnlyList<string> BehaviorIdPrefixes { get; }

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
    /// Gets the original shorthand OpenAPI document names targeted by this override rule before any
    /// override actions are applied.
    /// </summary>
    public IReadOnlyList<string> OpenApiDocumentNames { get; }

    /// <summary>
    /// Gets the original shorthand primary OpenAPI tag names targeted by this override rule before
    /// any override actions are applied.
    /// </summary>
    public IReadOnlyList<string> TagNames { get; }

    /// <summary>
    /// Gets the original shorthand endpoint names targeted by this override rule before any
    /// override actions are applied.
    /// </summary>
    public IReadOnlyList<string> EndpointNames { get; }

    /// <summary>
    /// Gets the original shorthand host-governance scopes targeted by this override rule before
    /// any override actions are applied. These scopes can also act as the rule's primary target.
    /// </summary>
    public IReadOnlyList<string> HostGovernanceScopes { get; }

    /// <summary>
    /// Gets the original shorthand request-binding fallback modes targeted by this override rule
    /// before any override actions are applied.
    /// </summary>
    public IReadOnlyList<RestEndpointBindingFallbackMode> BindingFallbackModes { get; }

    /// <summary>
    /// Gets the original shorthand explicit binding descriptors targeted by this override rule
    /// before any override actions are applied.
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
    /// Gets the required Cephalon feature-flag identifiers enforced at the REST boundary when this
    /// override rule matches.
    /// </summary>
    public IReadOnlyList<string> RequiredFeatureFlagIds { get; }

    /// <summary>
    /// Gets a value indicating whether this override rule clears any previously declared
    /// Cephalon feature-flag requirements from the matched shorthand candidate.
    /// </summary>
    public bool ClearRequiredFeatureFlags { get; }

    /// <summary>
    /// Gets the effective explicit request-binding plan applied when this override rule matches.
    /// </summary>
    public IReadOnlyList<RestEndpointBindingDescriptor> Bindings { get; }

    /// <summary>
    /// Gets the explicit shorthand binding properties removed from the source binding plan when this override rule matches.
    /// </summary>
    public IReadOnlyList<string> RemovedBindingProperties { get; }

    /// <summary>
    /// Gets a value indicating whether this override rule clears the matched shorthand candidate's
    /// entire explicit binding plan.
    /// </summary>
    public bool ClearBindings { get; }

    /// <summary>
    /// Gets how <see cref="Bindings" /> and <see cref="RemovedBindingProperties" /> apply to the shorthand candidate's explicit binding plan.
    /// </summary>
    public RestEndpointOverrideBindingMode BindingMode { get; }

    /// <summary>
    /// Gets a value indicating whether this override rule opts the matched explicit-binding
    /// shorthand candidate into preserved implicit-query fallback for remaining unbound query
    /// properties.
    /// </summary>
    public bool PreserveImplicitQueryFallback { get; }

    /// <summary>
    /// Gets the normalized action dimensions declared by this override rule.
    /// </summary>
    public IReadOnlyList<RestEndpointOverrideActionKind> ActionKinds { get; }

    /// <summary>
    /// Gets a value indicating whether any targeting values or override actions were explicitly supplied.
    /// </summary>
    public bool HasValues =>
        CandidateIds.Count > 0 ||
        BehaviorIds.Count > 0 ||
        BehaviorIdPrefixes.Count > 0 ||
        SourceModuleIds.Count > 0 ||
        ApiVersionMajors.Count > 0 ||
        Methods.Count > 0 ||
        RelativePatterns.Count > 0 ||
        RouteGroupPrefixes.Count > 0 ||
        OpenApiDocumentNames.Count > 0 ||
        TagNames.Count > 0 ||
        EndpointNames.Count > 0 ||
        HostGovernanceScopes.Count > 0 ||
        BindingFallbackModes.Count > 0 ||
        TargetBindings.Count > 0 ||
        ApiVersionMajor.HasValue ||
        Method is not null ||
        Pattern is not null ||
        RouteGroupPrefix is not null ||
        OpenApiDocumentName is not null ||
        TagName is not null ||
        EndpointName is not null ||
        Summary is not null ||
        Description is not null ||
        ClearEndpointName ||
        ClearSummary ||
        ClearDescription ||
        RequiredCapabilityKey is not null ||
        ClearRequiredCapability ||
        RequiredFeatureFlagIds.Count > 0 ||
        ClearRequiredFeatureFlags ||
        ClearBindings ||
        Bindings.Count > 0 ||
        RemovedBindingProperties.Count > 0 ||
        PreserveImplicitQueryFallback;

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
                $"Supported authoring styles: {string.Join(", ", SupportedAuthoringStyles.OrderBy(static value => value, StringComparer.OrdinalIgnoreCase))}. " +
                "When omitted, only shorthand styles are targeted by default.",
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
                $"REST endpoint override rules cannot both remove and override explicit binding property '{duplicateProperty}' in the same rule.",
                nameof(removedBindingProperties));
        }
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
                    "REST endpoint override rules cannot set BindingMode when ClearBindings is true.",
                    nameof(bindingMode));
        }

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

    private static RestEndpointBindingFallbackMode[] NormalizeBindingFallbackModes(
        IReadOnlyList<RestEndpointBindingFallbackMode>? values,
        string paramName)
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
                paramName,
                "REST endpoint override binding fallback selectors must use supported fallback modes.");
        }

        return normalized;
    }

    private static RestEndpointOverrideActionKind[] ResolveActionKinds(
        int? apiVersionMajor,
        string? method,
        string? pattern,
        string? routeGroupPrefix,
        string? openApiDocumentName,
        string? tagName,
        string? endpointName,
        string? summary,
        string? description,
        bool clearEndpointName,
        bool clearSummary,
        bool clearDescription,
        string? requiredCapabilityKey,
        bool clearRequiredCapability,
        IReadOnlyList<string> requiredFeatureFlagIds,
        bool clearRequiredFeatureFlags,
        IReadOnlyList<RestEndpointBindingDescriptor> bindings,
        IReadOnlyList<string> removedBindingProperties,
        bool clearBindings,
        RestEndpointOverrideBindingMode bindingMode,
        bool preserveImplicitQueryFallback)
    {
        ArgumentNullException.ThrowIfNull(bindings);
        ArgumentNullException.ThrowIfNull(removedBindingProperties);
        ArgumentNullException.ThrowIfNull(requiredFeatureFlagIds);

        var actionKinds = new List<RestEndpointOverrideActionKind>(16);
        if (apiVersionMajor.HasValue)
        {
            actionKinds.Add(RestEndpointOverrideActionKind.ApiVersionMajor);
        }

        if (method is not null)
        {
            actionKinds.Add(RestEndpointOverrideActionKind.Method);
        }

        if (pattern is not null)
        {
            actionKinds.Add(RestEndpointOverrideActionKind.Pattern);
        }

        if (routeGroupPrefix is not null)
        {
            actionKinds.Add(RestEndpointOverrideActionKind.RouteGroupPrefix);
        }

        if (openApiDocumentName is not null)
        {
            actionKinds.Add(RestEndpointOverrideActionKind.OpenApiDocumentName);
        }

        if (tagName is not null)
        {
            actionKinds.Add(RestEndpointOverrideActionKind.TagName);
        }

        if (endpointName is not null)
        {
            actionKinds.Add(RestEndpointOverrideActionKind.EndpointName);
        }

        if (summary is not null)
        {
            actionKinds.Add(RestEndpointOverrideActionKind.Summary);
        }

        if (description is not null)
        {
            actionKinds.Add(RestEndpointOverrideActionKind.Description);
        }

        if (clearEndpointName)
        {
            actionKinds.Add(RestEndpointOverrideActionKind.ClearEndpointName);
        }

        if (clearSummary)
        {
            actionKinds.Add(RestEndpointOverrideActionKind.ClearSummary);
        }

        if (clearDescription)
        {
            actionKinds.Add(RestEndpointOverrideActionKind.ClearDescription);
        }

        if (requiredCapabilityKey is not null)
        {
            actionKinds.Add(RestEndpointOverrideActionKind.RequiredCapabilityKey);
        }

        if (clearRequiredCapability)
        {
            actionKinds.Add(RestEndpointOverrideActionKind.ClearRequiredCapability);
        }

        if (requiredFeatureFlagIds.Count > 0)
        {
            actionKinds.Add(RestEndpointOverrideActionKind.RequiredFeatureFlagIds);
        }

        if (clearRequiredFeatureFlags)
        {
            actionKinds.Add(RestEndpointOverrideActionKind.ClearRequiredFeatureFlags);
        }

        if (clearBindings)
        {
            actionKinds.Add(RestEndpointOverrideActionKind.ClearBindings);
        }
        else
        {
            if (bindings.Count > 0)
            {
                actionKinds.Add(bindingMode == RestEndpointOverrideBindingMode.MergeExplicit
                    ? RestEndpointOverrideActionKind.MergeBindings
                    : RestEndpointOverrideActionKind.ReplaceBindings);
            }

            if (removedBindingProperties.Count > 0)
            {
                actionKinds.Add(RestEndpointOverrideActionKind.RemoveBindingProperties);
            }
        }

        if (preserveImplicitQueryFallback)
        {
            actionKinds.Add(RestEndpointOverrideActionKind.PreserveImplicitQueryFallback);
        }

        return actionKinds
            .Distinct()
            .OrderBy(static actionKind => actionKind)
            .ToArray();
    }
}
