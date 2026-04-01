namespace Cephalon.Benchmarks.Validation;

public sealed record GuardrailEntry(
    string ReportFileName,
    string Benchmark,
    double MaxMeanNanoseconds,
    double? MaxAllocatedBytes,
    string? Notes);
