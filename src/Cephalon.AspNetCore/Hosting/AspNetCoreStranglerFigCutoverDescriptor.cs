using Cephalon.Abstractions.Patterns;

namespace Cephalon.AspNetCore.Hosting;

internal sealed record AspNetCoreStranglerFigCutoverDescriptor(
    string RouteId,
    string SourceModuleId,
    string DisplayName,
    string PathPrefix,
    StranglerFigTarget RequestedTarget,
    StranglerFigTarget EffectiveTarget,
    string RequestedTargetSource,
    string SelectionMode,
    string SelectedEndpoint,
    string SelectedEndpointKind,
    string HandlingMode,
    bool CutoverEnabled,
    bool InterceptsRequest,
    int? RedirectStatusCode,
    string ProgressState,
    int ProgressPercent,
    IReadOnlyDictionary<string, string> Metadata,
    IReadOnlyDictionary<string, string> RuntimeMetadata);
