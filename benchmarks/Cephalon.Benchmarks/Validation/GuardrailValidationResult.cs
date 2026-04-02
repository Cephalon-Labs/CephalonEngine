namespace Cephalon.Benchmarks.Validation;

/// <summary>
/// Describes the outcome of validating benchmark results against the guardrail catalog.
/// </summary>
/// <param name="Passed">
/// Indicates whether every enforced guardrail passed.
/// </param>
/// <param name="Messages">
/// The diagnostic messages collected while validating the benchmark results.
/// </param>
public sealed record GuardrailValidationResult(
    bool Passed,
    IReadOnlyList<string> Messages);
