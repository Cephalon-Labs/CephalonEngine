namespace Cephalon.Benchmarks.Validation;

/// <summary>
/// Represents a single benchmark measurement parsed from a BenchmarkDotNet export.
/// </summary>
/// <param name="ReportFileName">
/// The source report file that produced the measurement.
/// </param>
/// <param name="Benchmark">
/// The fully qualified benchmark name.
/// </param>
/// <param name="MeanNanoseconds">
/// The mean execution time in nanoseconds.
/// </param>
/// <param name="AllocatedBytes">
/// The managed allocations in bytes when memory metrics are present.
/// </param>
public sealed record BenchmarkMeasurement(
    string ReportFileName,
    string Benchmark,
    double MeanNanoseconds,
    double? AllocatedBytes);
