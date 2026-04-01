namespace Cephalon.Benchmarks.Validation;

public sealed record GuardrailValidationResult(
    bool Passed,
    IReadOnlyList<string> Messages);
