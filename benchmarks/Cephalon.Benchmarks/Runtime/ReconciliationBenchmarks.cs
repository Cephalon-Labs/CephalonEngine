using BenchmarkDotNet.Attributes;
using Cephalon.Abstractions.Coordination;
using Cephalon.Benchmarks.Support;

namespace Cephalon.Benchmarks.Runtime;

/// <summary>Measures immutable reconciliation planning including the intent fingerprint.</summary>
[MemoryDiagnoser]
[Config(typeof(BenchmarkInProcessShortRunConfig))]
public class ReconciliationBenchmarks
{
    private readonly ReconciliationRequest request = new("op", "tenant", "actor", "apply", "target", "v2", "v1");
    private readonly DateTimeOffset observedAt = new(2026, 9, 16, 0, 0, 0, TimeSpan.Zero);

    /// <summary>Creates a ready plan with a canonical SHA-256 binding.</summary>
    /// <returns>The immutable plan.</returns>
    [Benchmark]
    public ReconciliationPlan PlanReady() => new(request, "v1", observedAt, observedAt.AddMinutes(1));
}
