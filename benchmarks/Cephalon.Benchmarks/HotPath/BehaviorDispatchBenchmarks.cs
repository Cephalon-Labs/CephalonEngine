using BenchmarkDotNet.Attributes;
using Cephalon.Abstractions.Behaviors;
using Cephalon.Behaviors.Services;
using Cephalon.Benchmarks.Support;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Benchmarks.HotPath;

/// <summary>
/// Measures the per-call dispatch overhead of the <see cref="BehaviorDispatcher" />.
/// The dispatcher uses a frozen dictionary for O(1) lookup and a compiled delegate for type-safe
/// invocation. This benchmark measures the steady-state cost after the dispatch table is built.
/// </summary>
[MemoryDiagnoser]
[Config(typeof(BenchmarkInProcessShortRunConfig))]
public class BehaviorDispatchBenchmarks
{
    private const int DispatchesPerIteration = 8192;

    private ServiceProvider provider = null!;
    private BehaviorDispatcher dispatcher = null!;
    private StubBehaviorContext context = null!;

    /// <summary>
    /// Builds the behavior dispatcher with a single echo behavior registered in the dispatch table.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddTransient<EchoBenchmarkBehavior>();

        provider = services.BuildServiceProvider();

        var descriptor = new BehaviorTopologyDescriptor(
            id: "echo",
            pattern: "direct",
            transportIds: ["rest-api"]);

        var catalog = new StubBehaviorCatalog(descriptor);
        var registry = new StubBehaviorTypeRegistry();
        registry.Register("echo", typeof(EchoBenchmarkBehavior));

        dispatcher = new BehaviorDispatcher(catalog, registry, provider);
        context = new StubBehaviorContext("echo");
    }

    /// <summary>
    /// Releases all DI resources.
    /// </summary>
    [GlobalCleanup]
    public void Cleanup()
    {
        provider.Dispose();
    }

    /// <summary>
    /// Dispatches a string input to the echo behavior through the frozen dispatch table and compiled delegate.
    /// </summary>
    [Benchmark(OperationsPerInvoke = DispatchesPerIteration)]
    public async Task<int> DispatchBehavior()
    {
        var length = 0;
        for (var i = 0; i < DispatchesPerIteration; i++)
        {
            var result = await dispatcher.DispatchAsync("echo", "benchmark-input", context);
            length += ((string?)result)?.Length ?? 0;
        }

        return length;
    }
}
