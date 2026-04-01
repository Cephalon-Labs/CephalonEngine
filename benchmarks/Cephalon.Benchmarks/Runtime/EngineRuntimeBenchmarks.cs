using BenchmarkDotNet.Attributes;
using Cephalon.Benchmarks.Support;
using Cephalon.Engine.Composition;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Benchmarks.Runtime;

[MemoryDiagnoser]
[ShortRunJob]
public class EngineRuntimeBenchmarks
{
    private readonly Func<EngineBuilder> builderFactory = BenchmarkScenarioFactory.CreateEngineBuilder;

    [Benchmark]
    public async Task<int> InitializeStartStopRuntime()
    {
        var builder = builderFactory();
        using var runtime = builder.Build();
        using var provider = builder.Services.BuildServiceProvider();

        await runtime.InitializeAsync(provider);
        await runtime.StartAsync(provider);
        await runtime.StopAsync();

        return runtime.Manifest.Modules.Count;
    }
}
