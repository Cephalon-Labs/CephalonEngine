using Cephalon.Abstractions.Transports;

namespace Cephalon.AspNetCore.Transports.Rest;

internal static class RestEndpointRuntimeMetadata
{
    internal const string ManualSourceKind = "manual";
    internal const string ModuleDslSourceKind = "module-dsl";
    internal const string MinimalApiAuthoringStyle = "minimal-api";
    internal const string BehaviorHelperAuthoringStyle = "behavior-helper";
    internal const string BehaviorModuleDslAuthoringStyle = "behavior-module-dsl";
    internal const string BehaviorModuleGeneratedAuthoringStyle = "behavior-module-generated";
    internal const string BehaviorModuleProfileAuthoringStyle = "behavior-module-profile";
    internal const string BindingFallbackModeMetadataKey = RestEndpointRuntimeMetadataKeys.BindingFallbackMode;
    internal const string RequiredCapabilityKeyMetadataKey = RestEndpointRuntimeMetadataKeys.RequiredCapabilityKey;
    internal const string RequiredFeatureFlagIdsMetadataKey = RestEndpointRuntimeMetadataKeys.RequiredFeatureFlagIds;
    internal const string ApplicationManagedOwnership = "application-managed";
    internal const string CephalonManagedOwnership = "cephalon-managed";
    internal const string ExplicitBehaviorHelperActivationMode = "explicit-behavior-helper";
    internal const string ExplicitMapGeneratedProfilesActivationMode = "explicit-map-generated-profiles";
    internal const string ExplicitMapProfileActivationMode = "explicit-map-profile";
    internal const string ExplicitModuleDslActivationMode = "explicit-module-dsl";
    internal const int BehaviorModuleDslPrecedenceRank = 2;
    internal const int BehaviorModuleProfilePrecedenceRank = 3;
    internal const int BehaviorModuleGeneratedPrecedenceRank = 4;

    internal static bool IsShorthandAuthoringStyle(string? authoringStyle)
    {
        return string.Equals(authoringStyle, BehaviorModuleGeneratedAuthoringStyle, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(authoringStyle, BehaviorModuleProfileAuthoringStyle, StringComparison.OrdinalIgnoreCase);
    }

    internal static string ResolvePublicationActivationMode(string authoringStyle)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(authoringStyle);

        return authoringStyle.Trim() switch
        {
            BehaviorHelperAuthoringStyle => ExplicitBehaviorHelperActivationMode,
            BehaviorModuleDslAuthoringStyle => ExplicitModuleDslActivationMode,
            BehaviorModuleGeneratedAuthoringStyle => ExplicitMapGeneratedProfilesActivationMode,
            BehaviorModuleProfileAuthoringStyle => ExplicitMapProfileActivationMode,
            _ => authoringStyle.Trim()
        };
    }

    internal static int ResolvePrecedenceRank(string authoringStyle)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(authoringStyle);

        return authoringStyle.Trim() switch
        {
            BehaviorModuleDslAuthoringStyle => BehaviorModuleDslPrecedenceRank,
            BehaviorModuleProfileAuthoringStyle => BehaviorModuleProfilePrecedenceRank,
            BehaviorModuleGeneratedAuthoringStyle => BehaviorModuleGeneratedPrecedenceRank,
            _ => int.MaxValue
        };
    }

    internal static string? ResolveEffectiveRequiredCapabilityKey(
        IEnumerable<RestEndpointCapabilityMetadata> metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        var requiredCapabilityMetadata = metadata.LastOrDefault();
        return requiredCapabilityMetadata?.ClearsExisting == true
            ? null
            : requiredCapabilityMetadata?.CapabilityKey;
    }

    internal static string? ResolveLastDeclaredRequiredCapabilityKey(
        IEnumerable<RestEndpointCapabilityMetadata> metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        return metadata
            .LastOrDefault(static item =>
                !item.ClearsExisting &&
                !string.IsNullOrWhiteSpace(item.CapabilityKey))
            ?.CapabilityKey;
    }

    internal static string[] NormalizeFeatureFlagIds(IReadOnlyList<string>? featureFlagIds)
    {
        return featureFlagIds?
            .Where(static item => !string.IsNullOrWhiteSpace(item))
            .Select(static item => item.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static item => item, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }

    internal static string[] ResolveEffectiveRequiredFeatureFlagIds(
        IEnumerable<RestEndpointFeatureFlagMetadata> metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        var requiredFeatureMetadata = metadata.LastOrDefault();
        return requiredFeatureMetadata?.ClearsExisting == true
            ? []
            : NormalizeFeatureFlagIds(requiredFeatureMetadata?.FeatureFlagIds);
    }

    internal static string[] ResolveLastDeclaredRequiredFeatureFlagIds(
        IEnumerable<RestEndpointFeatureFlagMetadata> metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        return NormalizeFeatureFlagIds(
            metadata
                .LastOrDefault(static item =>
                    !item.ClearsExisting &&
                    item.FeatureFlagIds.Count > 0)
                ?.FeatureFlagIds);
    }
}

internal sealed record RestEndpointCapabilityMetadata(string? CapabilityKey, bool ClearsExisting = false);

internal sealed class RestEndpointCapabilityRegistration;

internal sealed record RestEndpointSourceCapabilityMetadata(string? RequiredCapabilityKey);

internal sealed record RestEndpointFeatureFlagMetadata(
    IReadOnlyList<string> FeatureFlagIds,
    bool ClearsExisting = false);

internal sealed class RestEndpointFeatureFlagRegistration;

internal sealed record RestEndpointSourceFeatureFlagMetadata(
    IReadOnlyList<string> RequiredFeatureFlagIds);

internal sealed record RestEndpointSourceDocumentationMetadata(
    string? EndpointName,
    string? Summary,
    string? Description);

internal sealed record RestEndpointAppliedOverrideMetadata(
    string OverrideId,
    IReadOnlyList<RestEndpointOverrideActionKind>? ActionKinds);

internal sealed record RestEndpointClearedMetadataState(
    bool ClearEndpointName,
    bool ClearSummary,
    bool ClearDescription);

internal sealed record RestModuleEndpointMetadata(
    string ModuleId,
    string DisplayName,
    string Description,
    string? Version,
    int? MajorVersion);

internal sealed record RestBehaviorEndpointMetadata(
    string SourceKind,
    string AuthoringStyle,
    string BehaviorId,
    string BehaviorType,
    string OperationName,
    string? Summary,
    string? Description,
    string? OriginalEndpointName,
    string? OriginalSummary,
    string? OriginalDescription,
    string TagName,
    string OpenApiDocumentName,
    int? ApiVersionMajor,
    string RouteGroupPrefix,
    string RelativePattern,
    string? CandidateId,
    RestEndpointCandidateProjectionDescriptor? OriginalProjection,
    IReadOnlyList<RestEndpointBindingDescriptor>? BindingDescriptors,
    RestEndpointBindingFallbackMode? BindingFallbackMode,
    string? SelectedOverrideId,
    IReadOnlyList<string>? MatchedOverrideIds,
    IReadOnlyList<RestEndpointOverrideActionKind>? SelectedOverrideActionKinds,
    RestEndpointGovernanceRuleSelectionBasis? OverrideSelectionBasis,
    IReadOnlyList<string>? SkippedSuppressionIds,
    IReadOnlyList<string>? SkippedOverrideIds);
