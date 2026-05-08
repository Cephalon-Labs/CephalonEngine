using BenchmarkDotNet.Attributes;
using Cephalon.AspNetCore.Hosting;
using Cephalon.Benchmarks.Support;
using Cephalon.Worker.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cephalon.Benchmarks.Runtime;

/// <summary>
/// Measures representative Cephalon host cold-start costs for ASP.NET Core and generic-host worker adapters.
/// </summary>
[MemoryDiagnoser]
[Config(typeof(BenchmarkInProcessShortRunConfig))]
public class ColdStartBenchmarks
{
    private const int ColdStartsPerIteration = 16;
    private int invocationCount;

    /// <summary>
    /// Builds, starts, handles the first request, stops, and disposes representative ASP.NET Core hosts.
    /// </summary>
    /// <returns>The number of successful first-request probes completed during the benchmark invocation.</returns>
    [Benchmark(OperationsPerInvoke = ColdStartsPerIteration)]
    public async Task<int> BuildStartHandleFirstRequestAspNetCore()
    {
        invocationCount++;
        var successfulProbes = 0;

        for (var index = 0; index < ColdStartsPerIteration; index++)
        {
            await using var app = CreateAspNetCoreApp();
            await app.StartAsync().ConfigureAwait(false);

            using var client = app.GetTestClient();
            using var response = await client.GetAsync("/benchmark/ready").ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            await app.StopAsync().ConfigureAwait(false);
            successfulProbes++;
        }

        return successfulProbes;
    }

    /// <summary>
    /// Builds, starts, stops, and disposes representative generic-host worker hosts.
    /// </summary>
    /// <returns>The number of successful worker host startup cycles completed during the benchmark invocation.</returns>
    [Benchmark(OperationsPerInvoke = ColdStartsPerIteration)]
    public async Task<int> BuildStartWorkerHost()
    {
        invocationCount++;
        var successfulStarts = 0;

        for (var index = 0; index < ColdStartsPerIteration; index++)
        {
            using var host = CreateWorkerHost();
            await host.StartAsync().ConfigureAwait(false);
            await host.StopAsync().ConfigureAwait(false);
            successfulStarts++;
        }

        return successfulStarts;
    }

    private static WebApplication CreateAspNetCoreApp()
    {
        var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(ColdStartBenchmarks).Assembly.GetName().Name,
            ContentRootPath = Directory.GetCurrentDirectory(),
            EnvironmentName = Environments.Production
        });

        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Configuration["Engine:AspNetCore:OperatorSurface:Mode"] = "core";
        builder.AddCephalon(BenchmarkScenarioFactory.ConfigureAspNetCoreEngine);

        var app = builder.Build();
        app.MapCephalon();
        app.MapGet("/benchmark/ready", static () => Results.Ok(new { status = "ready" }));

        return app;
    }

    private static IHost CreateWorkerHost()
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            ApplicationName = typeof(ColdStartBenchmarks).Assembly.GetName().Name,
            ContentRootPath = Directory.GetCurrentDirectory(),
            EnvironmentName = Environments.Production
        });

        builder.Logging.ClearProviders();
        builder.AddCephalon(BenchmarkScenarioFactory.ConfigureEngine);

        return builder.Build();
    }
}
