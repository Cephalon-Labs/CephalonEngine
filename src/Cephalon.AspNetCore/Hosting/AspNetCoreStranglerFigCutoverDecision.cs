using Cephalon.Abstractions.Patterns;

namespace Cephalon.AspNetCore.Hosting;

internal sealed record AspNetCoreStranglerFigCutoverDecision(
    StranglerFigRouteResolution Resolution,
    string SelectedEndpointKind,
    string HandlingMode,
    bool CutoverEnabled,
    bool InterceptsRequest,
    string? DestinationPath,
    string? DestinationQuery,
    string? DestinationUri,
    int? ResponseStatusCode,
    string? FailureReason);
