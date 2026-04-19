using System.Text.Json.Serialization;

namespace Cephalon.Abstractions.Transports;

/// <summary>
/// Describes one configured or materially applied REST override action dimension.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<RestEndpointOverrideActionKind>))]
public enum RestEndpointOverrideActionKind
{
    /// <summary>
    /// The action kind was not classified.
    /// </summary>
    Unspecified = 0,

    /// <summary>
    /// The rule changes the effective public API major version.
    /// </summary>
    [JsonStringEnumMemberName("api-version-major")]
    ApiVersionMajor = 1,

    /// <summary>
    /// The rule changes the effective HTTP method.
    /// </summary>
    [JsonStringEnumMemberName("method")]
    Method = 2,

    /// <summary>
    /// The rule changes the effective relative route pattern.
    /// </summary>
    [JsonStringEnumMemberName("pattern")]
    Pattern = 3,

    /// <summary>
    /// The rule changes the effective published route-group prefix.
    /// </summary>
    [JsonStringEnumMemberName("route-group-prefix")]
    RouteGroupPrefix = 4,

    /// <summary>
    /// The rule changes the effective OpenAPI document name.
    /// </summary>
    [JsonStringEnumMemberName("openapi-document-name")]
    OpenApiDocumentName = 5,

    /// <summary>
    /// The rule changes the effective primary OpenAPI tag name.
    /// </summary>
    [JsonStringEnumMemberName("tag-name")]
    TagName = 6,

    /// <summary>
    /// The rule changes the effective endpoint name.
    /// </summary>
    [JsonStringEnumMemberName("endpoint-name")]
    EndpointName = 7,

    /// <summary>
    /// The rule changes the effective endpoint summary.
    /// </summary>
    [JsonStringEnumMemberName("summary")]
    Summary = 8,

    /// <summary>
    /// The rule changes the effective endpoint description.
    /// </summary>
    [JsonStringEnumMemberName("description")]
    Description = 9,

    /// <summary>
    /// The rule clears any previously declared endpoint name.
    /// </summary>
    [JsonStringEnumMemberName("clear-endpoint-name")]
    ClearEndpointName = 10,

    /// <summary>
    /// The rule clears any previously declared endpoint summary.
    /// </summary>
    [JsonStringEnumMemberName("clear-summary")]
    ClearSummary = 11,

    /// <summary>
    /// The rule clears any previously declared endpoint description.
    /// </summary>
    [JsonStringEnumMemberName("clear-description")]
    ClearDescription = 12,

    /// <summary>
    /// The rule changes the required Cephalon capability key.
    /// </summary>
    [JsonStringEnumMemberName("required-capability-key")]
    RequiredCapabilityKey = 13,

    /// <summary>
    /// The rule clears any previously declared required Cephalon capability key.
    /// </summary>
    [JsonStringEnumMemberName("clear-required-capability")]
    ClearRequiredCapability = 14,

    /// <summary>
    /// The rule replaces the explicit request-binding plan.
    /// </summary>
    [JsonStringEnumMemberName("replace-bindings")]
    ReplaceBindings = 15,

    /// <summary>
    /// The rule merges changes into the explicit request-binding plan.
    /// </summary>
    [JsonStringEnumMemberName("merge-bindings")]
    MergeBindings = 16,

    /// <summary>
    /// The rule removes explicit request-binding properties from the source plan.
    /// </summary>
    [JsonStringEnumMemberName("remove-binding-properties")]
    RemoveBindingProperties = 17,

    /// <summary>
    /// The rule clears the explicit request-binding plan and returns to the implicit baseline.
    /// </summary>
    [JsonStringEnumMemberName("clear-bindings")]
    ClearBindings = 18,

    /// <summary>
    /// The rule opts the matched explicit-binding shorthand candidate into preserved implicit-query fallback.
    /// </summary>
    [JsonStringEnumMemberName("preserve-implicit-query-fallback")]
    PreserveImplicitQueryFallback = 19,

    /// <summary>
    /// The rule changes the required Cephalon feature-flag identifiers.
    /// </summary>
    [JsonStringEnumMemberName("required-feature-flag-ids")]
    RequiredFeatureFlagIds = 20,

    /// <summary>
    /// The rule clears any previously declared required Cephalon feature-flag identifiers.
    /// </summary>
    [JsonStringEnumMemberName("clear-required-feature-flags")]
    ClearRequiredFeatureFlags = 21
}
