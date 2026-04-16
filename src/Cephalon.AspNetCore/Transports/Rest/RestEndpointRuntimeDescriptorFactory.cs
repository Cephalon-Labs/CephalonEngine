using Cephalon.Abstractions.Transports;

namespace Cephalon.AspNetCore.Transports.Rest;

internal static class RestEndpointRuntimeDescriptorFactory
{
    internal static RestEndpointRuntimeDescriptor CreateBehaviorDescriptor(
        string sourceKind,
        string method,
        string routePattern,
        string sourceModuleId,
        string? sourceModuleVersion,
        int? sourceModuleVersionMajor,
        string behaviorId,
        string? endpointName,
        string? openApiDocumentName,
        int? apiVersionMajor,
        IReadOnlyList<string>? tags,
        string? summary,
        string? description,
        string? candidateId,
        string authoringStyle,
        string behaviorType,
        string routeGroupPrefix,
        string relativePattern,
        IReadOnlyList<RestEndpointBindingDescriptor>? bindingDescriptors = null,
        bool preserveImplicitQueryFallback = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceKind);
        ArgumentException.ThrowIfNullOrWhiteSpace(method);
        ArgumentException.ThrowIfNullOrWhiteSpace(routePattern);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceModuleId);
        ArgumentException.ThrowIfNullOrWhiteSpace(behaviorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(authoringStyle);
        ArgumentException.ThrowIfNullOrWhiteSpace(behaviorType);
        ArgumentException.ThrowIfNullOrWhiteSpace(routeGroupPrefix);
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePattern);

        var normalizedMethod = method.Trim().ToUpperInvariant();
        RestEndpointBindingFallbackMode? bindingFallbackMode = preserveImplicitQueryFallback
            ? RestEndpointBindingFallbackMode.PreserveSourceImplicitFallback
            : null;
        return new RestEndpointRuntimeDescriptor(
            id: BuildBehaviorEndpointId(sourceModuleId, behaviorId, normalizedMethod, routePattern),
            transportId: "rest-api",
            sourceKind: sourceKind,
            method: normalizedMethod,
            routePattern: routePattern,
            sourceModuleId: sourceModuleId,
            sourceModuleVersion: sourceModuleVersion,
            sourceModuleVersionMajor: sourceModuleVersionMajor,
            behaviorId: behaviorId,
            endpointName: endpointName,
            openApiDocumentName: openApiDocumentName,
            apiVersionMajor: apiVersionMajor,
            tags: tags,
            summary: summary,
            description: description,
            authoringStyle: authoringStyle,
            candidateId: candidateId,
            bindingDescriptors: bindingDescriptors,
            bindingFallbackMode: bindingFallbackMode,
            metadata: CreateBehaviorMetadata(
                normalizedMethod,
                behaviorId,
                authoringStyle,
                behaviorType,
                routeGroupPrefix,
                relativePattern,
                bindingFallbackMode),
            routeGroupPrefix: routeGroupPrefix,
            relativePattern: relativePattern,
            behaviorType: behaviorType);
    }

    internal static string BuildBehaviorEndpointId(
        string moduleId,
        string behaviorId,
        string method,
        string routePattern)
    {
        return BuildEndpointId($"{moduleId}:{behaviorId}:{method}:{routePattern}");
    }

    internal static string BuildEndpointId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var normalized = string.Concat(value.Select(static ch =>
            char.IsLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : '_'));

        return string.IsNullOrWhiteSpace(normalized)
            ? "rest_endpoint"
            : normalized;
    }

    internal static string CombinePaths(params string?[] segments)
    {
        var normalizedSegments = segments
            .Where(static segment => !string.IsNullOrWhiteSpace(segment))
            .Select(static segment => segment!.Trim())
            .Select(static segment => segment.Trim('/'))
            .Where(static segment => segment.Length > 0)
            .ToArray();

        if (normalizedSegments.Length == 0)
        {
            return "/";
        }

        return "/" + string.Join("/", normalizedSegments);
    }

    private static Dictionary<string, string> CreateBehaviorMetadata(
        string method,
        string behaviorId,
        string authoringStyle,
        string behaviorType,
        string routeGroupPrefix,
        string relativePattern,
        RestEndpointBindingFallbackMode? bindingFallbackMode)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["method"] = method,
            ["authoringStyle"] = authoringStyle,
            ["behaviorType"] = behaviorType,
            ["routeGroupPrefix"] = routeGroupPrefix,
            ["relativePattern"] = relativePattern,
            ["sourceId"] = $"{behaviorId}:{method}:{relativePattern}"
        };

        if (bindingFallbackMode == RestEndpointBindingFallbackMode.PreserveSourceImplicitFallback)
        {
            metadata[RestEndpointRuntimeMetadata.BindingFallbackModeMetadataKey] =
                RestEndpointRuntimeMetadata.PreserveSourceImplicitFallbackMode;
        }

        return metadata;
    }
}
