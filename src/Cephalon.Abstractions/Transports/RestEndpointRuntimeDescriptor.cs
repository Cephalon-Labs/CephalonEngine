using System.Text.Json.Serialization;

namespace Cephalon.Abstractions.Transports;

/// <summary>
/// Describes one resolved public REST endpoint visible to the current runtime.
/// </summary>
public sealed class RestEndpointRuntimeDescriptor
{
    /// <summary>
    /// Creates a resolved REST endpoint runtime descriptor.
    /// </summary>
    /// <param name="id">The stable endpoint identifier.</param>
    /// <param name="transportId">The stable transport identifier that published the endpoint.</param>
    /// <param name="sourceKind">The source kind that produced the endpoint, such as <c>module-dsl</c> or <c>manual</c>.</param>
    /// <param name="method">The resolved HTTP method.</param>
    /// <param name="routePattern">The resolved route pattern including the host REST prefix.</param>
    /// <param name="sourceModuleId">The stable source-module identifier when one is known.</param>
    /// <param name="sourceModuleVersion">The declared source-module version when one is available.</param>
    /// <param name="sourceModuleVersionMajor">The parsed source-module major version when one is available.</param>
    /// <param name="behaviorId">The stable behavior identifier when the endpoint dispatches through a Cephalon behavior.</param>
    /// <param name="endpointName">The resolved endpoint or operation name when one is available.</param>
    /// <param name="openApiDocumentName">The resolved OpenAPI document name when one is available.</param>
    /// <param name="apiVersionMajor">The resolved public API major version when one is available.</param>
    /// <param name="tags">The resolved OpenAPI tags when any are published.</param>
    /// <param name="summary">The resolved endpoint summary when one is available.</param>
    /// <param name="description">The resolved endpoint description when one is available.</param>
    /// <param name="candidateId">
    /// The stable originating candidate identifier when this endpoint was published from the
    /// module-owned behavior projection pipeline.
    /// </param>
    /// <param name="originalProjection">
    /// The original shorthand projection shape before later host-level overrides are applied when
    /// the endpoint was published from the module-owned behavior projection pipeline.
    /// </param>
    /// <param name="bindingDescriptors">The resolved request-binding descriptors when the endpoint exposes an explicit binding plan.</param>
    /// <param name="bindingFallbackMode">
    /// The resolved request-binding fallback mode when the endpoint preserves source shorthand fallback behavior beyond
    /// the explicit binding plan.
    /// </param>
    /// <param name="metadata">Optional additive metadata.</param>
    /// <param name="authoringStyle">
    /// The normalized authoring style such as <c>behavior-module-profile</c> or <c>minimal-api</c>
    /// when the runtime can classify how the endpoint was published.
    /// </param>
    /// <param name="routeGroupPrefix">
    /// The resolved route-group prefix including the host REST prefix when the runtime can classify
    /// the grouped publication boundary that produced the endpoint.
    /// </param>
    /// <param name="relativePattern">
    /// The resolved route pattern relative to the grouped publication boundary when the runtime can
    /// classify that source shape.
    /// </param>
    /// <param name="behaviorType">
    /// The concrete behavior implementation type name when the endpoint dispatches through a
    /// Cephalon behavior and the runtime can classify that implementation identity.
    /// </param>
    /// <param name="sourceId">
    /// The stable source identity for the published endpoint when the runtime can classify the
    /// authored source shape behind that publication.
    /// </param>
    /// <param name="requiredCapabilityKey">
    /// The required Cephalon capability key enforced at the REST boundary when one is available.
    /// </param>
    /// <param name="originalRequiredCapabilityKey">
    /// The original required Cephalon capability key before later endpoint-governance rewrites when
    /// the runtime can classify that source answer.
    /// </param>
    /// <param name="appliedOverrideId">
    /// The host-level shorthand override identifier when runtime governance actually changes the
    /// published endpoint answer.
    /// </param>
    /// <param name="matchedOverrideIds">
    /// The ordered shorthand override identifiers that matched this endpoint's originating
    /// candidate before one winner was selected.
    /// </param>
    public RestEndpointRuntimeDescriptor(
        string id,
        string transportId,
        string sourceKind,
        string method,
        string routePattern,
        string? sourceModuleId = null,
        string? sourceModuleVersion = null,
        int? sourceModuleVersionMajor = null,
        string? behaviorId = null,
        string? endpointName = null,
        string? openApiDocumentName = null,
        int? apiVersionMajor = null,
        IReadOnlyList<string>? tags = null,
        string? summary = null,
        string? description = null,
        string? candidateId = null,
        RestEndpointCandidateProjectionDescriptor? originalProjection = null,
        IReadOnlyList<RestEndpointBindingDescriptor>? bindingDescriptors = null,
        RestEndpointBindingFallbackMode? bindingFallbackMode = null,
        IReadOnlyDictionary<string, string>? metadata = null,
        string? authoringStyle = null,
        string? routeGroupPrefix = null,
        string? relativePattern = null,
        string? behaviorType = null,
        string? sourceId = null,
        string? requiredCapabilityKey = null,
        string? originalRequiredCapabilityKey = null,
        string? appliedOverrideId = null,
        IReadOnlyList<string>? matchedOverrideIds = null)
    {
        Id = NormalizeRequired(id, nameof(id));
        TransportId = NormalizeRequired(transportId, nameof(transportId));
        SourceKind = NormalizeRequired(sourceKind, nameof(sourceKind));
        Method = NormalizeRequired(method, nameof(method)).ToUpperInvariant();
        RoutePattern = NormalizeRequired(routePattern, nameof(routePattern));
        SourceModuleId = NormalizeOptional(sourceModuleId);
        SourceModuleVersion = NormalizeOptional(sourceModuleVersion);
        SourceModuleVersionMajor = sourceModuleVersionMajor;
        BehaviorId = NormalizeOptional(behaviorId);
        EndpointName = NormalizeOptional(endpointName);
        OpenApiDocumentName = NormalizeOptional(openApiDocumentName);
        ApiVersionMajor = apiVersionMajor;
        Tags = NormalizeTags(tags);
        Summary = NormalizeOptional(summary);
        Description = NormalizeOptional(description);
        AuthoringStyle = NormalizeOptional(authoringStyle);
        RouteGroupPrefix = NormalizeOptional(routeGroupPrefix);
        RelativePattern = NormalizeOptional(relativePattern);
        BehaviorType = NormalizeOptional(behaviorType);
        SourceId = NormalizeOptional(sourceId);
        RequiredCapabilityKey = NormalizeOptional(requiredCapabilityKey);
        OriginalRequiredCapabilityKey = NormalizeOptional(originalRequiredCapabilityKey);
        AppliedOverrideId = NormalizeOptional(appliedOverrideId);
        MatchedOverrideIds = NormalizeOrderedList(matchedOverrideIds);
        CandidateId = NormalizeOptional(candidateId);
        OriginalProjection = NormalizeOriginalProjection(originalProjection);
        BindingDescriptors = NormalizeBindingDescriptors(bindingDescriptors);
        BindingFallbackMode = NormalizeBindingFallbackMode(bindingFallbackMode);
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the stable endpoint identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the stable transport identifier that published the endpoint.
    /// </summary>
    public string TransportId { get; }

    /// <summary>
    /// Gets the source kind that produced the endpoint.
    /// </summary>
    public string SourceKind { get; }

    /// <summary>
    /// Gets the resolved HTTP method.
    /// </summary>
    public string Method { get; }

    /// <summary>
    /// Gets the resolved route pattern including the host REST prefix.
    /// </summary>
    public string RoutePattern { get; }

    /// <summary>
    /// Gets the stable source-module identifier when one is known.
    /// </summary>
    public string? SourceModuleId { get; }

    /// <summary>
    /// Gets the declared source-module version when one is available.
    /// </summary>
    public string? SourceModuleVersion { get; }

    /// <summary>
    /// Gets the parsed source-module major version when one is available.
    /// </summary>
    public int? SourceModuleVersionMajor { get; }

    /// <summary>
    /// Gets the stable behavior identifier when the endpoint dispatches through a Cephalon behavior.
    /// </summary>
    public string? BehaviorId { get; }

    /// <summary>
    /// Gets the resolved endpoint or operation name when one is available.
    /// </summary>
    public string? EndpointName { get; }

    /// <summary>
    /// Gets the resolved OpenAPI document name when one is available.
    /// </summary>
    public string? OpenApiDocumentName { get; }

    /// <summary>
    /// Gets the resolved public API major version when one is available.
    /// </summary>
    public int? ApiVersionMajor { get; }

    /// <summary>
    /// Gets the resolved OpenAPI tags when any are published.
    /// </summary>
    public IReadOnlyList<string> Tags { get; }

    /// <summary>
    /// Gets the resolved endpoint summary when one is available.
    /// </summary>
    public string? Summary { get; }

    /// <summary>
    /// Gets the resolved endpoint description when one is available.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the normalized authoring style such as <c>behavior-module-profile</c> or
    /// <c>minimal-api</c> when the runtime can classify how the endpoint was published.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AuthoringStyle { get; }

    /// <summary>
    /// Gets the resolved route-group prefix including the host REST prefix when the runtime can
    /// classify the grouped publication boundary that produced the endpoint.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RouteGroupPrefix { get; }

    /// <summary>
    /// Gets the resolved route pattern relative to the grouped publication boundary when the
    /// runtime can classify that source shape.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RelativePattern { get; }

    /// <summary>
    /// Gets the concrete behavior implementation type name when the endpoint dispatches through a
    /// Cephalon behavior and the runtime can classify that implementation identity.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? BehaviorType { get; }

    /// <summary>
    /// Gets the stable source identity for the published endpoint when the runtime can classify the
    /// authored source shape behind that publication.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SourceId { get; }

    /// <summary>
    /// Gets the required Cephalon capability key enforced at the REST boundary when one is available.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RequiredCapabilityKey { get; }

    /// <summary>
    /// Gets the original required Cephalon capability key before later endpoint-governance rewrites
    /// when the runtime can classify that source answer.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OriginalRequiredCapabilityKey { get; }

    /// <summary>
    /// Gets the host-level shorthand override identifier when runtime governance actually changes
    /// the published endpoint answer.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AppliedOverrideId { get; }

    /// <summary>
    /// Gets the ordered shorthand override identifiers that matched this endpoint's originating
    /// candidate before one winner was selected.
    /// </summary>
    public IReadOnlyList<string> MatchedOverrideIds { get; }

    /// <summary>
    /// Gets the stable originating candidate identifier when this endpoint was published from the
    /// module-owned behavior projection pipeline.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CandidateId { get; }

    /// <summary>
    /// Gets the original shorthand projection shape before later host-level overrides are applied
    /// when the endpoint was published from the module-owned behavior projection pipeline.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RestEndpointCandidateProjectionDescriptor? OriginalProjection { get; }

    /// <summary>
    /// Gets the resolved request-binding descriptors when the endpoint exposes an explicit binding plan.
    /// </summary>
    public IReadOnlyList<RestEndpointBindingDescriptor> BindingDescriptors { get; }

    /// <summary>
    /// Gets the resolved request-binding fallback mode when the endpoint preserves source shorthand
    /// fallback behavior beyond the explicit binding plan.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RestEndpointBindingFallbackMode? BindingFallbackMode { get; }

    /// <summary>
    /// Gets optional additive metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

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

    private static string[] NormalizeTags(IReadOnlyList<string>? values)
    {
        return values?
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
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

    private static RestEndpointCandidateProjectionDescriptor? NormalizeOriginalProjection(
        RestEndpointCandidateProjectionDescriptor? value)
    {
        return value is null
            ? null
            : new RestEndpointCandidateProjectionDescriptor(
                value.Method,
                value.RoutePattern,
                value.RouteGroupPrefix,
                value.RelativePattern,
                value.ApiVersionMajor,
                value.OpenApiDocumentName,
                value.BindingDescriptors,
                value.BindingFallbackMode);
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

    private static string[] NormalizeOrderedList(IReadOnlyList<string>? values)
    {
        if (values is null || values.Count == 0)
        {
            return [];
        }

        var normalized = new List<string>(values.Count);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            var trimmed = value.Trim();
            if (seen.Add(trimmed))
            {
                normalized.Add(trimmed);
            }
        }

        return normalized.ToArray();
    }
}
