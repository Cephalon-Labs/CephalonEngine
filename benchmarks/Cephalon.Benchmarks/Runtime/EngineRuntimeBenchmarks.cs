using BenchmarkDotNet.Attributes;
using Cephalon.Benchmarks.Support;
using Cephalon.Engine.Composition;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Benchmarks.Runtime;

/// <summary>
/// Measures startup and shutdown costs for the baseline engine runtime lifecycle.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class EngineRuntimeBenchmarks
{
    private readonly Func<EngineBuilder> builderFactory = BenchmarkScenarioFactory.CreateEngineBuilder;

    /// <summary>
    /// Initializes, starts, and stops the runtime for the default benchmark composition.
    /// </summary>
    /// <returns>
    /// The number of modules reported by the runtime manifest after the lifecycle completes.
    /// </returns>
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
