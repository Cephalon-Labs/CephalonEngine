namespace Cephalon.AspNetCore.Documentation;

/// <summary>
/// Describes the operator-facing runtime projection for hosted Cephalon reference documentation.
/// </summary>
/// <param name="Surface">The current reference-doc hosting surface configuration and availability.</param>
/// <param name="EvaluatedAtUtc">The UTC timestamp when the runtime payload was evaluated.</param>
/// <param name="EvaluationDurationMilliseconds">The time in milliseconds spent evaluating the runtime payload.</param>
public sealed record ReferenceDocsRuntimeSurface(
    ReferenceDocsSurface Surface,
    DateTimeOffset EvaluatedAtUtc = default,
    int EvaluationDurationMilliseconds = 0);
