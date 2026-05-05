namespace Cephalon.Abstractions.Transports;

/// <summary>
/// Provides canonical wire-name helpers for <see cref="RestEndpointOverrideActionKind" />.
/// </summary>
public static class RestEndpointOverrideActionKindExtensions
{
    /// <summary>
    /// Gets the stable wire name used by JSON serialization for the override action kind.
    /// </summary>
    /// <param name="actionKind">The override action kind.</param>
    /// <returns>The stable wire name.</returns>
    public static string GetWireName(this RestEndpointOverrideActionKind actionKind)
    {
        return actionKind switch
        {
            RestEndpointOverrideActionKind.Unspecified => "Unspecified",
            RestEndpointOverrideActionKind.ApiVersionMajor => "api-version-major",
            RestEndpointOverrideActionKind.Method => "method",
            RestEndpointOverrideActionKind.Pattern => "pattern",
            RestEndpointOverrideActionKind.RouteGroupPrefix => "route-group-prefix",
            RestEndpointOverrideActionKind.OpenApiDocumentName => "openapi-document-name",
            RestEndpointOverrideActionKind.TagName => "tag-name",
            RestEndpointOverrideActionKind.EndpointName => "endpoint-name",
            RestEndpointOverrideActionKind.Summary => "summary",
            RestEndpointOverrideActionKind.Description => "description",
            RestEndpointOverrideActionKind.ClearEndpointName => "clear-endpoint-name",
            RestEndpointOverrideActionKind.ClearSummary => "clear-summary",
            RestEndpointOverrideActionKind.ClearDescription => "clear-description",
            RestEndpointOverrideActionKind.RequiredCapabilityKey => "required-capability-key",
            RestEndpointOverrideActionKind.ClearRequiredCapability => "clear-required-capability",
            RestEndpointOverrideActionKind.ReplaceBindings => "replace-bindings",
            RestEndpointOverrideActionKind.MergeBindings => "merge-bindings",
            RestEndpointOverrideActionKind.RemoveBindingProperties => "remove-binding-properties",
            RestEndpointOverrideActionKind.ClearBindings => "clear-bindings",
            RestEndpointOverrideActionKind.PreserveImplicitQueryFallback => "preserve-implicit-query-fallback",
            RestEndpointOverrideActionKind.RequiredFeatureFlagIds => "required-feature-flag-ids",
            RestEndpointOverrideActionKind.ClearRequiredFeatureFlags => "clear-required-feature-flags",
            _ => throw new ArgumentOutOfRangeException(
                nameof(actionKind),
                actionKind,
                "A supported REST endpoint override action kind is required.")
        };
    }

    /// <summary>
    /// Tries to parse the stable wire name used by JSON serialization into an override action kind.
    /// </summary>
    /// <param name="value">The wire name to parse.</param>
    /// <param name="actionKind">The parsed override action kind when the wire name is recognized.</param>
    /// <returns>
    /// <see langword="true" /> when the wire name maps to a supported override action kind;
    /// otherwise, <see langword="false" />.
    /// </returns>
    public static bool TryParseWireName(string? value, out RestEndpointOverrideActionKind actionKind)
    {
        switch (value?.Trim())
        {
            case "Unspecified":
                actionKind = RestEndpointOverrideActionKind.Unspecified;
                return true;
            case "api-version-major":
                actionKind = RestEndpointOverrideActionKind.ApiVersionMajor;
                return true;
            case "method":
                actionKind = RestEndpointOverrideActionKind.Method;
                return true;
            case "pattern":
                actionKind = RestEndpointOverrideActionKind.Pattern;
                return true;
            case "route-group-prefix":
                actionKind = RestEndpointOverrideActionKind.RouteGroupPrefix;
                return true;
            case "openapi-document-name":
                actionKind = RestEndpointOverrideActionKind.OpenApiDocumentName;
                return true;
            case "tag-name":
                actionKind = RestEndpointOverrideActionKind.TagName;
                return true;
            case "endpoint-name":
                actionKind = RestEndpointOverrideActionKind.EndpointName;
                return true;
            case "summary":
                actionKind = RestEndpointOverrideActionKind.Summary;
                return true;
            case "description":
                actionKind = RestEndpointOverrideActionKind.Description;
                return true;
            case "clear-endpoint-name":
                actionKind = RestEndpointOverrideActionKind.ClearEndpointName;
                return true;
            case "clear-summary":
                actionKind = RestEndpointOverrideActionKind.ClearSummary;
                return true;
            case "clear-description":
                actionKind = RestEndpointOverrideActionKind.ClearDescription;
                return true;
            case "required-capability-key":
                actionKind = RestEndpointOverrideActionKind.RequiredCapabilityKey;
                return true;
            case "clear-required-capability":
                actionKind = RestEndpointOverrideActionKind.ClearRequiredCapability;
                return true;
            case "replace-bindings":
                actionKind = RestEndpointOverrideActionKind.ReplaceBindings;
                return true;
            case "merge-bindings":
                actionKind = RestEndpointOverrideActionKind.MergeBindings;
                return true;
            case "remove-binding-properties":
                actionKind = RestEndpointOverrideActionKind.RemoveBindingProperties;
                return true;
            case "clear-bindings":
                actionKind = RestEndpointOverrideActionKind.ClearBindings;
                return true;
            case "preserve-implicit-query-fallback":
                actionKind = RestEndpointOverrideActionKind.PreserveImplicitQueryFallback;
                return true;
            case "required-feature-flag-ids":
                actionKind = RestEndpointOverrideActionKind.RequiredFeatureFlagIds;
                return true;
            case "clear-required-feature-flags":
                actionKind = RestEndpointOverrideActionKind.ClearRequiredFeatureFlags;
                return true;
            default:
                actionKind = default;
                return false;
        }
    }
}
