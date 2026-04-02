namespace Cephalon.Benchmarks.Validation;

/// <summary>
/// Defines the allowed performance envelope for a benchmark entry.
/// </summary>
/// <param name="ReportFileName">
/// The BenchmarkDotNet report file expected to contain the measurement.
/// </param>
/// <param name="Benchmark">
/// The fully qualified benchmark name.
/// </param>
/// <param name="MaxMeanNanoseconds">
/// The maximum accepted mean execution time in nanoseconds.
/// </param>
/// <param name="MaxAllocatedBytes">
/// The maximum accepted managed allocations in bytes when enforced.
/// </param>
/// <param name="Notes">
/// Optional human guidance that explains the guardrail.
/// </param>
public sealed record GuardrailEntry(
    string ReportFileName,
    string Benchmark,
    double MaxMeanNanoseconds,
    double? MaxAllocatedBytes,
    string? Notes);
