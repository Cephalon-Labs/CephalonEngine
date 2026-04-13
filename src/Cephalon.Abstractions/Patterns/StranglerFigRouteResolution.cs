namespace Cephalon.Abstractions.Patterns;

/// <summary>
/// Describes the strangler-fig routing decision made for one request.
/// </summary>
/// <param name="RouteId">The matched route identifier.</param>
/// <param name="RouteDisplayName">The operator-facing route name.</param>
/// <param name="SourceModuleId">The Cephalon module that owns the modern boundary.</param>
/// <param name="RequestedPath">The normalized request path that was evaluated.</param>
/// <param name="RequestedMethod">The normalized request method that was evaluated.</param>
/// <param name="MatchedPathPrefix">The normalized route prefix that matched the request.</param>
/// <param name="SelectedTarget">The migration boundary chosen for the request.</param>
/// <param name="SelectedEndpoint">The concrete endpoint or boundary identifier that should receive the request.</param>
/// <param name="LegacyEndpoint">The configured legacy endpoint when one exists.</param>
/// <param name="ModernEndpoint">The configured modern endpoint when one exists.</param>
/// <param name="ResolutionMode">The reason the boundary was chosen, such as <c>preferred-target</c> or <c>fallback-target</c>.</param>
/// <param name="Metadata">Additional route metadata that traveled with the decision.</param>
public sealed record StranglerFigRouteResolution(
    string RouteId,
    string RouteDisplayName,
    string SourceModuleId,
    string RequestedPath,
    string RequestedMethod,
    string MatchedPathPrefix,
    StranglerFigTarget SelectedTarget,
    string SelectedEndpoint,
    string? LegacyEndpoint,
    string? ModernEndpoint,
    string ResolutionMode,
    IReadOnlyDictionary<string, string> Metadata);
