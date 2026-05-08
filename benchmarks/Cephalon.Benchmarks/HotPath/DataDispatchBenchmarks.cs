using BenchmarkDotNet.Attributes;
using Cephalon.Abstractions.Data;
using Cephalon.Benchmarks.Support;
using Cephalon.Data.Registration;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Benchmarks.HotPath;

/// <summary>
/// Measures the per-call dispatch overhead of the CQRS data layer.
/// <see cref="IReadStore.ExecuteAsync{TResult}" /> and <see cref="IWriteStore.ExecuteAsync(ICommand, CancellationToken)" />
/// use reflection-based handler dispatch with concurrent caching. These benchmarks measure the warm-cache
/// dispatch cost, which is the steady-state per-request overhead.
/// </summary>
[MemoryDiagnoser]
[Config(typeof(BenchmarkInProcessShortRunConfig))]
public class DataDispatchBenchmarks
{
    private const int DispatchesPerIteration = 8192;

    private ServiceProvider provider = null!;
    private IServiceScope scope = null!;
    private IReadStore readStore = null!;
    private IWriteStore writeStore = null!;
    private BenchmarkQuery query = null!;
    private BenchmarkCommand command = null!;
    private BenchmarkResultCommand resultCommand = null!;

    /// <summary>
    /// Builds the service provider with the data companion pack and registered benchmark handlers.
    /// The handler dispatch cache warms on the first call; subsequent benchmark iterations measure steady-state cost.
    /// </summary>
    [GlobalSetup]
    public async Task Setup()
    {
        var services = new ServiceCollection();
        services.AddCephalonDataQuery<BenchmarkQuery, int>();
        services.AddCephalonDataCommand<BenchmarkCommand>();
        services.AddCephalonDataCommand<BenchmarkResultCommand, int>();
        services.AddSingleton<IQueryHandler<BenchmarkQuery, int>, BenchmarkQueryHandler>();
        services.AddSingleton<ICommandHandler<BenchmarkCommand>, BenchmarkCommandHandler>();
        services.AddSingleton<ICommandHandler<BenchmarkResultCommand, int>, BenchmarkResultCommandHandler>();

        var builder = new EngineBuilder(services);
        builder.UseSettings(new EngineSettings(
            blueprint: "modular-vertical-slice",
            patterns: ["cqrs"],
            transports: ["rest-api"],
            data: new DataSettings(readWriteSplit: true)));
        builder.AddData();
        builder.AddModule(new BenchmarkClockModule());

        using var runtime = builder.Build();
        provider = services.BuildServiceProvider();
        await runtime.InitializeAsync(provider);

        scope = provider.CreateScope();
        readStore = scope.ServiceProvider.GetRequiredService<IReadStore>();
        writeStore = scope.ServiceProvider.GetRequiredService<IWriteStore>();

        query = new BenchmarkQuery(42);
        command = new BenchmarkCommand(42);
        resultCommand = new BenchmarkResultCommand(42);

        // Warm the dispatch caches
        await readStore.ExecuteAsync(query);
        await writeStore.ExecuteAsync(command);
        await writeStore.ExecuteAsync(resultCommand);
    }

    /// <summary>
    /// Releases all DI resources.
    /// </summary>
    [GlobalCleanup]
    public void Cleanup()
    {
        scope.Dispose();
        provider.Dispose();
    }

    /// <summary>
    /// Dispatches a query through the handler-dispatching read store with a warm dispatch cache.
    /// </summary>
    [Benchmark(OperationsPerInvoke = DispatchesPerIteration)]
    public async Task<int> DispatchQuery()
    {
        var sum = 0;
        for (var i = 0; i < DispatchesPerIteration; i++)
        {
            sum += await readStore.ExecuteAsync(query);
        }

        return sum;
    }

    /// <summary>
    /// Dispatches a void command through the handler-dispatching write store with a warm dispatch cache.
    /// </summary>
    [Benchmark(OperationsPerInvoke = DispatchesPerIteration)]
    public async Task DispatchCommand()
    {
        for (var i = 0; i < DispatchesPerIteration; i++)
        {
            await writeStore.ExecuteAsync(command);
        }
    }

    /// <summary>
    /// Dispatches a result-returning command through the handler-dispatching write store with a warm dispatch cache.
    /// </summary>
    [Benchmark(OperationsPerInvoke = DispatchesPerIteration)]
    public async Task<int> DispatchCommandWithResult()
    {
        var sum = 0;
        for (var i = 0; i < DispatchesPerIteration; i++)
        {
            sum += await writeStore.ExecuteAsync(resultCommand);
        }

        return sum;
    }
}
