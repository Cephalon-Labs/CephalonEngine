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
    private const int BuildsPerIteration = 4096;
    private readonly Func<EngineBuilder> builderFactory = BenchmarkScenarioFactory.CreateEngineBuilder;
    private EngineBuilder[] builders = [];

    /// <summary>
    /// Prepares a fresh configured engine builder for the current benchmark iteration.
    /// </summary>
    [IterationSetup]
    public void PrepareIteration()
    {
        builders = new EngineBuilder[BuildsPerIteration];
        for (var index = 0; index < builders.Length; index++)
        {
            builders[index] = builderFactory();
        }
    }

    /// <summary>
    /// Clears the configured builder captured for the current benchmark iteration.
    /// </summary>
    [IterationCleanup]
    public void CleanupIteration()
    {
        builders = [];
    }

    /// <summary>
    /// Builds the runtime manifest for the prepared baseline benchmark scenarios.
    /// </summary>
    /// <returns>
    /// The total number of modules and capabilities surfaced across the measured runtime builds.
    /// </returns>
    [Benchmark(OperationsPerInvoke = BuildsPerIteration)]
    public (int Modules, int Capabilities) BuildRuntimeManifest()
    {
        if (builders.Length == 0)
        {
            throw new InvalidOperationException("Benchmark iteration was not initialized.");
        }

        var moduleCount = 0;
        var capabilityCount = 0;

        foreach (var builder in builders)
        {
            using var runtime = builder.Build();
            moduleCount += runtime.Manifest.Modules.Count;
            capabilityCount += runtime.Manifest.Capabilities.Count;
        }

        return (moduleCount, capabilityCount);
    }
}
