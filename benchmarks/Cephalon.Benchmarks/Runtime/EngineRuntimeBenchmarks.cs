using BenchmarkDotNet.Attributes;
using Cephalon.Benchmarks.Support;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Benchmarks.Runtime;

/// <summary>
/// Measures startup and shutdown costs for the baseline engine runtime lifecycle.
/// </summary>
[MemoryDiagnoser]
[Config(typeof(BenchmarkInProcessShortRunConfig))]
public class EngineRuntimeBenchmarks
{
    private const int LifecyclesPerIteration = 2048;
    private readonly Func<EngineBuilder> builderFactory = BenchmarkScenarioFactory.CreateEngineBuilder;
    private readonly Func<EngineBuilder> phase8BuilderFactory = BenchmarkScenarioFactory.CreatePhase8EngineBuilder;
    private (EngineRuntime Runtime, ServiceProvider Provider)[] scenarios = [];
    private (EngineRuntime Runtime, ServiceProvider Provider)[] phase8Scenarios = [];

    /// <summary>
    /// Prepares a fresh runtime and service provider for the current benchmark iteration.
    /// </summary>
    [IterationSetup]
    public void PrepareIteration()
    {
        scenarios = new (EngineRuntime Runtime, ServiceProvider Provider)[LifecyclesPerIteration];
        phase8Scenarios = new (EngineRuntime Runtime, ServiceProvider Provider)[LifecyclesPerIteration];
        for (var index = 0; index < scenarios.Length; index++)
        {
            var builder = builderFactory();
            scenarios[index] = (builder.Build(), builder.Services.BuildServiceProvider());

            var phase8Builder = phase8BuilderFactory();
            phase8Scenarios[index] = (phase8Builder.Build(), phase8Builder.Services.BuildServiceProvider());
        }
    }

    /// <summary>
    /// Releases the runtime resources captured for the current benchmark iteration.
    /// </summary>
    [IterationCleanup]
    public void CleanupIteration()
    {
        foreach (var scenario in scenarios)
        {
            scenario.Provider.Dispose();
            scenario.Runtime.Dispose();
        }

        scenarios = [];

        foreach (var scenario in phase8Scenarios)
        {
            scenario.Provider.Dispose();
            scenario.Runtime.Dispose();
        }

        phase8Scenarios = [];
    }

    /// <summary>
    /// Initializes, starts, and stops the prepared runtimes for the default benchmark composition.
    /// </summary>
    /// <returns>
    /// The total number of modules reported across the measured runtime lifecycles.
    /// </returns>
    [Benchmark(OperationsPerInvoke = LifecyclesPerIteration)]
    public async Task<int> InitializeStartStopRuntime()
    {
        if (scenarios.Length == 0)
        {
            throw new InvalidOperationException("Benchmark iteration was not initialized.");
        }

        var moduleCount = 0;

        foreach (var scenario in scenarios)
        {
            await scenario.Runtime.InitializeAsync(scenario.Provider);
            await scenario.Runtime.StartAsync(scenario.Provider);
            await scenario.Runtime.StopAsync();
            moduleCount += scenario.Runtime.Manifest.Modules.Count;
        }

        return moduleCount;
    }

    /// <summary>
    /// Initializes, starts, and stops the prepared runtimes for the phase-8 companion-pack composition.
    /// </summary>
    /// <returns>
    /// The total number of modules reported across the measured phase-8 runtime lifecycles.
    /// </returns>
    [Benchmark(OperationsPerInvoke = LifecyclesPerIteration)]
    public async Task<int> InitializeStartStopPhase8Runtime()
    {
        if (phase8Scenarios.Length == 0)
        {
            throw new InvalidOperationException("Benchmark iteration was not initialized.");
        }

        var moduleCount = 0;

        foreach (var scenario in phase8Scenarios)
        {
            await scenario.Runtime.InitializeAsync(scenario.Provider);
            await scenario.Runtime.StartAsync(scenario.Provider);
            await scenario.Runtime.StopAsync();
            moduleCount += scenario.Runtime.Manifest.Modules.Count;
        }

        return moduleCount;
    }
}
