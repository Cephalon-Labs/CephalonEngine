namespace Cephalon.Abstractions.Transports;

/// <summary>
/// Describes one host-level REST endpoint suppression rule visible to the current runtime.
/// </summary>
public sealed class RestEndpointSuppressionDescriptor
{
    /// <summary>
    /// Creates a REST endpoint suppression descriptor.
    /// </summary>
    /// <param name="id">The stable suppression identifier.</param>
    /// <param name="candidateIds">The original shorthand candidate identifiers targeted by the suppression rule.</param>
    /// <param name="behaviorIds">The behavior identifiers targeted by the suppression rule.</param>
    /// <param name="sourceModuleIds">The source-module identifiers targeted by the suppression rule.</param>
    /// <param name="authoringStyles">The normalized shorthand authoring styles targeted by the suppression rule.</param>
    /// <param name="apiVersionMajors">The effective API major versions targeted by the suppression rule.</param>
    /// <param name="methods">The effective HTTP methods targeted by the suppression rule.</param>
    /// <param name="relativePatterns">The shorthand relative route patterns targeted by the suppression rule.</param>
    /// <param name="routeGroupPrefixes">The published route-group prefixes targeted by the suppression rule.</param>
    /// <param name="openApiDocumentNames">
    /// The original shorthand OpenAPI document names targeted by the suppression rule.
    /// </param>
    /// <param name="tagNames">
    /// The original shorthand primary OpenAPI tag names targeted by the suppression rule.
    /// </param>
    /// <param name="bindingFallbackModes">
    /// The original shorthand request-binding fallback modes targeted by the suppression rule.
    /// </param>
    public RestEndpointSuppressionDescriptor(
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
        IReadOnlyList<RestEndpointBindingFallbackMode>? bindingFallbackModes = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("A non-empty suppression id is required.", nameof(id));
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
    /// Gets the source-module identifiers targeted by this suppression rule.
    /// </summary>
    public IReadOnlyList<string> SourceModuleIds { get; }

    /// <summary>
    /// Gets the normalized shorthand authoring styles targeted by this suppression rule.
    /// </summary>
    public IReadOnlyList<string> AuthoringStyles { get; }

    /// <summary>
    /// Gets the effective API major versions targeted by this suppression rule.
    /// </summary>
    public IReadOnlyList<int> ApiVersionMajors { get; }

    /// <summary>
    /// Gets the effective HTTP methods targeted by this suppression rule.
    /// </summary>
    public IReadOnlyList<string> Methods { get; }

    /// <summary>
    /// Gets the shorthand relative route patterns targeted by this suppression rule.
    /// </summary>
    public IReadOnlyList<string> RelativePatterns { get; }

    /// <summary>
    /// Gets the published route-group prefixes targeted by this suppression rule.
    /// </summary>
    public IReadOnlyList<string> RouteGroupPrefixes { get; }

    /// <summary>
    /// Gets the original shorthand OpenAPI document names targeted by this suppression rule.
    /// </summary>
    public IReadOnlyList<string> OpenApiDocumentNames { get; }

    /// <summary>
    /// Gets the original shorthand primary OpenAPI tag names targeted by this suppression rule.
    /// </summary>
    public IReadOnlyList<string> TagNames { get; }

    /// <summary>
    /// Gets the original shorthand request-binding fallback modes targeted by this suppression rule.
    /// </summary>
    public IReadOnlyList<RestEndpointBindingFallbackMode> BindingFallbackModes { get; }

    private static string[] NormalizeList(IReadOnlyList<string>? values)
    {
        return values?
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }

    private static int[] NormalizeIntList(IReadOnlyList<int>? values)
    {
        return values?
            .Distinct()
            .OrderBy(static value => value)
            .ToArray() ?? [];
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
                "REST endpoint suppression binding fallback selectors must use supported fallback modes.");
        }

        return normalized;
    }
}
