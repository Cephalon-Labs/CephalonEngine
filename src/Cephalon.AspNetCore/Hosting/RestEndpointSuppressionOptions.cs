using Cephalon.AspNetCore.Transports.Rest;
using Microsoft.AspNetCore.Routing.Patterns;
using Cephalon.Abstractions.Transports;

namespace Cephalon.AspNetCore.Hosting;

/// <summary>
/// Describes one host-level suppression rule for descriptor-backed REST shorthand candidates.
/// </summary>
public sealed class RestEndpointSuppressionOptions
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
    /// Initializes a new instance of the <see cref="RestEndpointSuppressionOptions" /> class.
    /// </summary>
    /// <param name="id">The stable suppression identifier.</param>
    /// <param name="candidateIds">
    /// The original shorthand candidate identifiers targeted by the suppression rule.
    /// </param>
    /// <param name="behaviorIds">The behavior identifiers targeted by the suppression rule.</param>
    /// <param name="sourceModuleIds">The source-module identifiers targeted by the suppression rule.</param>
    /// <param name="authoringStyles">
    /// The module-owned REST authoring styles targeted by the suppression rule. When omitted, the
    /// rule targets only shorthand styles <c>behavior-module-profile</c> and
    /// <c>behavior-module-generated</c>. Explicit <c>behavior-module-dsl</c> routes participate
    /// only when the owning route group opted into host governance.
    /// </param>
    /// <param name="apiVersionMajors">
    /// The effective API major versions targeted by the suppression rule before any override
    /// actions are applied.
    /// </param>
    /// <param name="methods">
    /// The effective HTTP methods targeted by the suppression rule before any override actions are
    /// applied.
    /// </param>
    /// <param name="relativePatterns">
    /// The shorthand relative route patterns targeted by the suppression rule before any override
    /// actions are applied.
    /// </param>
    /// <param name="routeGroupPrefixes">
    /// The published route-group prefixes targeted by the suppression rule before any override
    /// actions are applied.
    /// </param>
    /// <param name="openApiDocumentNames">
    /// The original shorthand OpenAPI document names targeted by the suppression rule before any
    /// override actions are applied.
    /// </param>
    /// <param name="tagNames">
    /// The original shorthand primary OpenAPI tag names targeted by the suppression rule before
    /// any override actions are applied.
    /// </param>
    /// <param name="endpointNames">
    /// The original shorthand endpoint names targeted by the suppression rule before any override
    /// actions are applied.
    /// </param>
    /// <param name="hostGovernanceScopes">
    /// The original shorthand host-governance scopes targeted by the suppression rule before any
    /// override actions are applied. This selector can also serve as the rule's primary target
    /// when candidate, behavior, and source-module identifiers are intentionally omitted.
    /// </param>
    /// <param name="bindingFallbackModes">
    /// The original shorthand request-binding fallback modes targeted by the suppression rule
    /// before any override actions are applied.
    /// </param>
    /// <param name="targetBindings">
    /// The original shorthand explicit binding descriptors targeted by the suppression rule before
    /// any override actions are applied.
    /// </param>
    /// <param name="behaviorIdPrefixes">
    /// The behavior-id prefixes targeted by the suppression rule. Prefix matches use the stable
    /// dot-separated behavior-id hierarchy, so a prefix targets the exact behavior id and any
    /// descendant behavior ids beneath that prefix.
    /// </param>
    public RestEndpointSuppressionOptions(
        string id,
        IReadOnlyList<string>? candidateIds = null,
        IReadOnlyList<string>? behaviorIds = null,
        IReadOnlyList<string>? sourceModuleIds = null,
        IReadOnlyList<string>? authoringStyles = null,
        IReadOnlyList<int>? apiVersionMajors = null,
        IReadOnlyList<string>? methods = null,
        IReadOnlyList<string>? relativePatterns = null,
        IReadOnlyList<string>? routeGroupPrefixes = null,
        IReadOnlyList<string>? openApiDocumentNames = null,
        IReadOnlyList<string>? tagNames = null,
        IReadOnlyList<string>? endpointNames = null,
        IReadOnlyList<RestEndpointBindingFallbackMode>? bindingFallbackModes = null,
        IReadOnlyList<RestEndpointBindingDescriptor>? targetBindings = null,
        IReadOnlyList<string>? hostGovernanceScopes = null,
        IReadOnlyList<string>? behaviorIdPrefixes = null)
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
            "REST endpoint suppression API major versions must be positive integers.");
        Methods = NormalizeMethods(
            methods,
            nameof(methods),
            "REST endpoint suppression method");
        RelativePatterns = NormalizeRoutePatterns(
            relativePatterns,
            nameof(relativePatterns),
            "REST endpoint suppression relative pattern");
        RouteGroupPrefixes = NormalizeRoutePatterns(
            routeGroupPrefixes,
            nameof(routeGroupPrefixes),
            "REST endpoint suppression route-group prefix");
        OpenApiDocumentNames = NormalizeList(openApiDocumentNames);
        TagNames = NormalizeList(tagNames);
        EndpointNames = NormalizeList(endpointNames);
        HostGovernanceScopes = NormalizeList(hostGovernanceScopes);
        BindingFallbackModes = NormalizeBindingFallbackModes(
            bindingFallbackModes,
            nameof(bindingFallbackModes));
        TargetBindings = NormalizeTargetBindings(targetBindings, nameof(targetBindings));

        if (CandidateIds.Count == 0 &&
            BehaviorIds.Count == 0 &&
            BehaviorIdPrefixes.Count == 0 &&
            SourceModuleIds.Count == 0 &&
            HostGovernanceScopes.Count == 0)
        {
            throw new ArgumentException(
                "REST endpoint suppression rules must target at least one candidate id, behavior id, behavior-id prefix, source module id, or host-governance scope.",
                nameof(candidateIds));
        }
    }

    /// <summary>
    /// Gets the stable suppression identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the original shorthand candidate identifiers targeted by this suppression rule.
    /// </summary>
    public IReadOnlyList<string> CandidateIds { get; }

    /// <summary>
    /// Gets the behavior identifiers targeted by this suppression rule.
    /// </summary>
    public IReadOnlyList<string> BehaviorIds { get; }

    /// <summary>
    /// Gets the behavior-id prefixes targeted by this suppression rule.
    /// </summary>
    public IReadOnlyList<string> BehaviorIdPrefixes { get; }

    /// <summary>
    /// Gets the source-module identifiers targeted by this suppression rule.
    /// </summary>
    public IReadOnlyList<string> SourceModuleIds { get; }

    /// <summary>
    /// Gets the normalized shorthand authoring styles targeted by this suppression rule.
    /// </summary>
    public IReadOnlyList<string> AuthoringStyles { get; }

    /// <summary>
    /// Gets the effective API major versions targeted by this suppression rule before override actions are applied.
    /// </summary>
    public IReadOnlyList<int> ApiVersionMajors { get; }

    /// <summary>
    /// Gets the effective HTTP methods targeted by this suppression rule before override actions are applied.
    /// </summary>
    public IReadOnlyList<string> Methods { get; }

    /// <summary>
    /// Gets the shorthand relative route patterns targeted by this suppression rule before override actions are applied.
    /// </summary>
    public IReadOnlyList<string> RelativePatterns { get; }

    /// <summary>
    /// Gets the published route-group prefixes targeted by this suppression rule before override actions are applied.
    /// </summary>
    public IReadOnlyList<string> RouteGroupPrefixes { get; }

    /// <summary>
    /// Gets the original shorthand OpenAPI document names targeted by this suppression rule before
    /// override actions are applied.
    /// </summary>
    public IReadOnlyList<string> OpenApiDocumentNames { get; }

    /// <summary>
    /// Gets the original shorthand primary OpenAPI tag names targeted by this suppression rule
    /// before override actions are applied.
    /// </summary>
    public IReadOnlyList<string> TagNames { get; }

    /// <summary>
    /// Gets the original shorthand endpoint names targeted by this suppression rule before
    /// override actions are applied.
    /// </summary>
    public IReadOnlyList<string> EndpointNames { get; }

    /// <summary>
    /// Gets the original shorthand host-governance scopes targeted by this suppression rule
    /// before override actions are applied. These scopes can also act as the rule's primary target.
    /// </summary>
    public IReadOnlyList<string> HostGovernanceScopes { get; }

    /// <summary>
    /// Gets the original shorthand request-binding fallback modes targeted by this suppression rule
    /// before override actions are applied.
    /// </summary>
    public IReadOnlyList<RestEndpointBindingFallbackMode> BindingFallbackModes { get; }

    /// <summary>
    /// Gets the original shorthand explicit binding descriptors targeted by this suppression rule
    /// before override actions are applied.
    /// </summary>
    public IReadOnlyList<RestEndpointBindingDescriptor> TargetBindings { get; }

    /// <summary>
    /// Gets a value indicating whether any targeting values were explicitly supplied.
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
        TargetBindings.Count > 0;

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
                $"REST endpoint suppression style '{unsupportedStyle}' is not supported. " +
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
            .Select(value => NormalizeMethod(value, paramName, errorPrefix))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }

    private static string NormalizeMethod(string value, string paramName, string errorPrefix)
    {
        return value.Trim().ToUpperInvariant() switch
        {
            "GET" => "GET",
            "POST" => "POST",
            "PUT" => "PUT",
            "PATCH" => "PATCH",
            "DELETE" => "DELETE",
            _ => throw new ArgumentException(
                $"{errorPrefix} '{value}' is not supported. Supported methods: GET, POST, PUT, PATCH, DELETE.",
                paramName)
        };
    }

    private static string[] NormalizeRoutePatterns(
        IReadOnlyList<string>? values,
        string paramName,
        string errorPrefix)
    {
        return values?
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(value => NormalizeRoutePattern(value, paramName, errorPrefix))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }

    private static string NormalizeRoutePattern(string value, string paramName, string errorPrefix)
    {
        var normalized = value.Trim();
        if (!normalized.StartsWith('/'))
        {
            throw new ArgumentException($"{errorPrefix}s must start with '/'.", paramName);
        }

        try
        {
            _ = RoutePatternFactory.Parse(normalized);
        }
        catch (Exception ex)
        {
            throw new ArgumentException(
                $"{errorPrefix} '{value}' is not a valid ASP.NET Core route pattern.",
                paramName,
                ex);
        }

        return normalized;
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
                "REST endpoint suppression binding fallback selectors must use supported fallback modes.");
        }

        return normalized;
    }

    private static RestEndpointBindingDescriptor[] NormalizeTargetBindings(
        IReadOnlyList<RestEndpointBindingDescriptor>? values,
        string paramName)
    {
        if (values is null)
        {
            return [];
        }

        var normalized = new List<RestEndpointBindingDescriptor>(values.Count);
        var seenByProperty = new Dictionary<string, RestEndpointBindingDescriptor>(StringComparer.OrdinalIgnoreCase);
        foreach (var value in values)
        {
            if (value is null)
            {
                continue;
            }

            var binding = new RestEndpointBindingDescriptor(
                value.PropertyName,
                value.Source,
                value.Name);
            var propertyName = binding.PropertyName.Trim();
            if (seenByProperty.TryGetValue(propertyName, out var existing))
            {
                if (existing.Source != binding.Source ||
                    !string.Equals(existing.Name, binding.Name, StringComparison.Ordinal))
                {
                    throw new ArgumentException(
                        $"REST endpoint suppression target bindings cannot declare more than one binding selector for property '{propertyName}'.",
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
}
