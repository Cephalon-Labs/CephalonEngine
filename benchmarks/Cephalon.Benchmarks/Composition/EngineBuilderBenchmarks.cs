using BenchmarkDotNet.Attributes;
using Cephalon.Benchmarks.Support;
using Cephalon.Engine.Composition;

namespace Cephalon.Benchmarks.Composition;

/// <summary>
/// Measures the cost of composing the baseline engine runtime and manifest surface.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class EngineBuilderBenchmarks
{
    private readonly Func<EngineBuilder> builderFactory = BenchmarkScenarioFactory.CreateEngineBuilder;

    /// <summary>
    /// Builds the runtime manifest for the baseline benchmark scenario.
    /// </summary>
    /// <returns>
    /// The number of modules and capabilities surfaced by the composed runtime.
    /// </returns>
    [Benchmark]
    public (int Modules, int Capabilities) BuildRuntimeManifest()
    {
        using var runtime = builderFactory().Build();
        return (runtime.Manifest.Modules.Count, runtime.Manifest.Capabilities.Count);
    }
}
