namespace Cephalon.AspNetCore.Transports.Rest;

internal static class RestEndpointRuntimeMetadata
{
    internal const string ManualSourceKind = "manual";
    internal const string ModuleDslSourceKind = "module-dsl";
}

internal sealed record RestModuleEndpointMetadata(
    string ModuleId,
    string DisplayName,
    string Description,
    string? Version,
    int? MajorVersion);

internal sealed record RestBehaviorEndpointMetadata(
    string SourceKind,
    string BehaviorId,
    string BehaviorType,
    string OperationName,
    string? Summary,
    string? Description,
    string TagName,
    string OpenApiDocumentName,
    int? ApiVersionMajor,
    string RouteGroupPrefix,
    string RelativePattern);
