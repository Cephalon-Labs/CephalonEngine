using Cephalon.Abstractions.Resilience;

namespace Cephalon.AspNetCore.Resilience;

/// <summary>
/// Describes the operator-facing runtime projection for rate-limiting policies.
/// </summary>
/// <param name="Policies">The active rate-limiting policies visible to the current runtime.</param>
/// <param name="EvaluatedAtUtc">The UTC timestamp when the runtime payload was evaluated.</param>
/// <param name="EvaluationDurationMilliseconds">The time in milliseconds spent evaluating the runtime payload.</param>
public sealed record RateLimitingRuntimeSurface(
    IReadOnlyList<RateLimitingRuntimeDescriptor> Policies,
    DateTimeOffset EvaluatedAtUtc = default,
    int EvaluationDurationMilliseconds = 0);