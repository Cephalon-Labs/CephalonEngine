using Cephalon.Abstractions.Localization;

namespace Cephalon.AspNetCore.Localization;

/// <summary>
/// Describes the operator-facing runtime projection for localization snapshot requests.
/// </summary>
/// <param name="Snapshot">The resolved localization snapshot payload.</param>
/// <param name="RequestedCulture">The optional culture requested by the operator.</param>
/// <param name="EvaluatedAtUtc">The UTC timestamp when the runtime payload was evaluated.</param>
/// <param name="EvaluationDurationMilliseconds">The time in milliseconds spent evaluating the runtime payload.</param>
public sealed record LocalizedResourcesRuntimeSurface(
    LocalizedResourcesSnapshot Snapshot,
    string? RequestedCulture,
    DateTimeOffset EvaluatedAtUtc = default,
    int EvaluationDurationMilliseconds = 0);
