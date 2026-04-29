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
        string? originalEndpointName,
        string? originalSummary,
        string? originalDescription,
        string? candidateId,
        RestEndpointCandidateProjectionDescriptor? originalProjection,
        string authoringStyle,
        string behaviorType,
        string routeGroupPrefix,
        string relativePattern,
        IReadOnlyList<RestEndpointBindingDescriptor>? bindingDescriptors = null,
        RestEndpointBindingFallbackMode? bindingFallbackMode = null,
        string? requiredCapabilityKey = null,
        string? originalRequiredCapabilityKey = null,
        IReadOnlyList<string>? requiredFeatureFlagIds = null,
        IReadOnlyList<string>? originalRequiredFeatureFlagIds = null,
        string? appliedOverrideId = null,
        IReadOnlyList<string>? matchedOverrideIds = null,
        string? selectedOverrideId = null,
        IReadOnlyList<RestEndpointOverrideActionKind>? selectedOverrideActionKinds = null,
        IReadOnlyList<RestEndpointOverrideActionKind>? appliedOverrideActionKinds = null,
        RestEndpointGovernanceRuleSelectionBasis? overrideSelectionBasis = null,
        IReadOnlyList<string>? skippedSuppressionIds = null,
        IReadOnlyList<string>? skippedOverrideIds = null)
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
        var sourceId = $"{behaviorId}:{normalizedMethod}:{relativePattern}";
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
            originalEndpointName: originalEndpointName,
            originalSummary: originalSummary,
            originalDescription: originalDescription,
            authoringStyle: authoringStyle,
            candidateId: candidateId,
            originalProjection: originalProjection,
            bindingDescriptors: bindingDescriptors,
            bindingFallbackMode: bindingFallbackMode,
            metadata: CreateBehaviorMetadata(
                normalizedMethod,
                behaviorId,
                authoringStyle,
                behaviorType,
                routeGroupPrefix,
                relativePattern,
                bindingFallbackMode,
                requiredCapabilityKey,
                requiredFeatureFlagIds),
            routeGroupPrefix: routeGroupPrefix,
            relativePattern: relativePattern,
            behaviorType: behaviorType,
            sourceId: sourceId,
            requiredCapabilityKey: requiredCapabilityKey,
            originalRequiredCapabilityKey: originalRequiredCapabilityKey,
            requiredFeatureFlagIds: requiredFeatureFlagIds,
            originalRequiredFeatureFlagIds: originalRequiredFeatureFlagIds,
            appliedOverrideId: appliedOverrideId,
            matchedOverrideIds: matchedOverrideIds,
            selectedOverrideId: selectedOverrideId,
            selectedOverrideActionKinds: selectedOverrideActionKinds,
            appliedOverrideActionKinds: appliedOverrideActionKinds,
            overrideSelectionBasis: overrideSelectionBasis,
            skippedSuppressionIds: skippedSuppressionIds,
            skippedOverrideIds: skippedOverrideIds);
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
        RestEndpointBindingFallbackMode? bindingFallbackMode,
        string? requiredCapabilityKey,
        IReadOnlyList<string>? requiredFeatureFlagIds)
    {
        var normalizedRequiredFeatureFlagIds = RestEndpointRuntimeMetadata.NormalizeFeatureFlagIds(requiredFeatureFlagIds);
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [RestEndpointRuntimeMetadataKeys.Method] = method,
            [RestEndpointRuntimeMetadataKeys.AuthoringStyle] = authoringStyle,
            [RestEndpointRuntimeMetadataKeys.BehaviorType] = behaviorType,
            [RestEndpointRuntimeMetadataKeys.RouteGroupPrefix] = routeGroupPrefix,
            [RestEndpointRuntimeMetadataKeys.RelativePattern] = relativePattern,
            [RestEndpointRuntimeMetadataKeys.SourceId] = $"{behaviorId}:{method}:{relativePattern}",
            [RestEndpointRuntimeMetadataKeys.RestPublicationActivationOwnership] =
                RestEndpointRuntimeMetadata.ApplicationManagedOwnership,
            [RestEndpointRuntimeMetadataKeys.RestMaterializationOwnership] =
                RestEndpointRuntimeMetadata.CephalonManagedOwnership,
            [RestEndpointRuntimeMetadataKeys.RestPublicationActivationMode] =
                RestEndpointRuntimeMetadata.ResolvePublicationActivationMode(authoringStyle)
        };

        if (RestEndpointRuntimeMetadata.IsShorthandAuthoringStyle(authoringStyle))
        {
            metadata[RestEndpointRuntimeMetadataKeys.RestProfileMetadataOwnership] =
                RestEndpointRuntimeMetadata.ApplicationManagedOwnership;
        }

        if (bindingFallbackMode is { } fallbackMode)
        {
            metadata[RestEndpointRuntimeMetadata.BindingFallbackModeMetadataKey] =
                fallbackMode.GetWireName();
        }

        if (!string.IsNullOrWhiteSpace(requiredCapabilityKey))
        {
            metadata[RestEndpointRuntimeMetadata.RequiredCapabilityKeyMetadataKey] = requiredCapabilityKey.Trim();
        }

        if (normalizedRequiredFeatureFlagIds.Length > 0)
        {
            metadata[RestEndpointRuntimeMetadata.RequiredFeatureFlagIdsMetadataKey] =
                string.Join(",", normalizedRequiredFeatureFlagIds);
        }

        return metadata;
    }
}
