using Cephalon.Abstractions.Transports;

namespace Cephalon.AspNetCore.Transports;

/// <summary>
/// Describes the operator-facing runtime projection for active transport selections.
/// </summary>
/// <param name="Transports">The active transport descriptors visible to the current runtime.</param>
/// <param name="EvaluatedAtUtc">The UTC timestamp when the runtime payload was evaluated.</param>
/// <param name="EvaluationDurationMilliseconds">The time in milliseconds spent evaluating the runtime payload.</param>
public sealed record TransportRuntimeSurface(
    IReadOnlyList<TransportDescriptor> Transports,
    DateTimeOffset EvaluatedAtUtc = default,
    int EvaluationDurationMilliseconds = 0);
