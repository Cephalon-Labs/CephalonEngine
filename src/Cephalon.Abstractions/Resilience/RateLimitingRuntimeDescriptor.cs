using Cephalon.Abstractions.AppModel;

namespace Cephalon.Abstractions.Resilience;

/// <summary>
/// Describes one effective HTTP rate-limiting policy exposed by the current runtime.
/// </summary>
/// <param name="Id">The stable runtime policy identifier.</param>
/// <param name="DisplayName">The human-readable policy name.</param>
/// <param name="Description">The human-readable policy description.</param>
/// <param name="ExecutionMode">
/// The enforcement mode used by the active host, such as <c>aspnetcore-global-middleware</c> or <c>disabled</c>.
/// </param>
/// <param name="Scope">
/// The runtime scope covered by the policy, such as <c>public-http-endpoints</c>.
/// </param>
/// <param name="RejectionStatusCode">
/// The HTTP status code returned when the limiter rejects a request.
/// </param>
/// <param name="TransportIds">
/// The transport identifiers whose HTTP surfaces are covered by the policy.
/// </param>
/// <param name="ExcludedPathPrefixes">
/// The rooted path prefixes intentionally excluded from enforcement, such as operator or documentation routes.
/// </param>
/// <param name="Requested">
/// The requested app-model selection that asked for rate limiting.
/// </param>
/// <param name="Effective">
/// The effective policy values after host defaults and adapter-specific normalization have been applied.
/// </param>
/// <param name="Metadata">Additional host-specific metadata describing the policy.</param>
public sealed record RateLimitingRuntimeDescriptor(
    string Id,
    string DisplayName,
    string Description,
    string ExecutionMode,
    string Scope,
    int RejectionStatusCode,
    IReadOnlyList<string> TransportIds,
    IReadOnlyList<string> ExcludedPathPrefixes,
    RateLimitingSelection Requested,
    RateLimitingSelection Effective,
    IReadOnlyDictionary<string, string> Metadata);
