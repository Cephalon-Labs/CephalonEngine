using Cephalon.Abstractions.Data;

namespace Cephalon.AspNetCore.Hosting;

/// <summary>
/// Describes the operator-facing runtime projection for outbox descriptors.
/// </summary>
/// <param name="Outboxes">The active outbox descriptors visible to the current runtime.</param>
/// <param name="EvaluatedAtUtc">The UTC timestamp when the runtime payload was evaluated.</param>
/// <param name="EvaluationDurationMilliseconds">The time in milliseconds spent evaluating the runtime payload.</param>
public sealed record OutboxRuntimeSurface(
    IReadOnlyList<OutboxDescriptor> Outboxes,
    DateTimeOffset EvaluatedAtUtc = default,
    int EvaluationDurationMilliseconds = 0);