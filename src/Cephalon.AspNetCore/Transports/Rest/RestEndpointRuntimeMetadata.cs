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
    internal const string BindingFallbackModeMetadataKey = "bindingFallbackMode";
    internal const string PreserveSourceImplicitFallbackMode = "preserve-source-implicit-fallback";
    internal const string PreserveRemainingBodyFallbackMode = "preserve-remaining-body-fallback";
    internal const string RequiredCapabilityKeyMetadataKey = "requiredCapabilityKey";
    internal const int BehaviorModuleDslPrecedenceRank = 2;
    internal const int BehaviorModuleProfilePrecedenceRank = 3;
    internal const int BehaviorModuleGeneratedPrecedenceRank = 4;

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
}

internal sealed record RestEndpointCapabilityMetadata(string? CapabilityKey, bool ClearsExisting = false);

internal sealed class RestEndpointCapabilityRegistration;

internal sealed record RestEndpointSourceCapabilityMetadata(string? RequiredCapabilityKey);

internal sealed record RestEndpointSourceDocumentationMetadata(
    string? EndpointName,
    string? Summary,
    string? Description);

internal sealed record RestEndpointAppliedOverrideMetadata(string OverrideId);

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
    IReadOnlyList<string>? MatchedOverrideIds);
