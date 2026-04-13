using Cephalon.Abstractions.Transports;

namespace Cephalon.AspNetCore.Transports.Rest;

internal static class RestEndpointRuntimeMetadata
{
    internal const string ManualSourceKind = "manual";
    internal const string ModuleDslSourceKind = "module-dsl";
    internal const string MinimalApiAuthoringStyle = "minimal-api";
    internal const string BehaviorHelperAuthoringStyle = "behavior-helper";
    internal const string BehaviorModuleDslAuthoringStyle = "behavior-module-dsl";
    internal const string BehaviorModuleProfileAuthoringStyle = "behavior-module-profile";
}

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
    string TagName,
    string OpenApiDocumentName,
    int? ApiVersionMajor,
    string RouteGroupPrefix,
    string RelativePattern,
    IReadOnlyList<RestEndpointBindingDescriptor>? BindingDescriptors);
