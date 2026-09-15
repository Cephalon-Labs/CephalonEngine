using Cephalon.Abstractions.Data;

namespace Cephalon.AspNetCore.Hosting;

/// <summary>
/// Describes the operator-facing runtime projection for inbox descriptors.
/// </summary>
/// <param name="Inboxes">The active inbox descriptors visible to the current runtime.</param>
/// <param name="EvaluatedAtUtc">The UTC timestamp when the runtime payload was evaluated.</param>
/// <param name="EvaluationDurationMilliseconds">The time in milliseconds spent evaluating the runtime payload.</param>
public sealed record InboxRuntimeSurface(
    IReadOnlyList<InboxDescriptor> Inboxes,
    DateTimeOffset EvaluatedAtUtc = default,
    int EvaluationDurationMilliseconds = 0);