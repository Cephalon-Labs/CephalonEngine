namespace Cephalon.Benchmarks.Validation;

public sealed record BenchmarkMeasurement(
    string ReportFileName,
    string Benchmark,
    double MeanNanoseconds,
    double? AllocatedBytes);
