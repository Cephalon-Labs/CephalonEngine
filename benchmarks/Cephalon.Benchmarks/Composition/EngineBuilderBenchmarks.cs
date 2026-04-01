using BenchmarkDotNet.Attributes;
using Cephalon.Benchmarks.Support;
using Cephalon.Engine.Composition;

namespace Cephalon.Benchmarks.Composition;

[MemoryDiagnoser]
[ShortRunJob]
public class EngineBuilderBenchmarks
{
    private readonly Func<EngineBuilder> builderFactory = BenchmarkScenarioFactory.CreateEngineBuilder;

    [Benchmark]
    public (int Modules, int Capabilities) BuildRuntimeManifest()
    {
        using var runtime = builderFactory().Build();
        return (runtime.Manifest.Modules.Count, runtime.Manifest.Capabilities.Count);
    }
}
