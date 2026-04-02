using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Cephalon.Abstractions.Health;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Runtime;
using Cephalon.Observability.HttpDependencies.Hosting;
using Cephalon.Tests.Support;
using Cephalon.Worker.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cephalon.Tests.Hosting;

public sealed class HttpDependencyHealthHostingTests
{
    [Fact]
    public async Task AddCephalonHttpDependencyHealthReportsHealthyEndpoint()
    {
        await using var server = new DependencyHealthHttpServer(new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["/catalog/health"] = 200
        });

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Http:RefreshIntervalSeconds"] = "60";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Http:Dependencies:0:Id"] = "catalog-api";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Http:Dependencies:0:DisplayName"] = "Catalog API";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Http:Dependencies:0:Endpoint"] = server.GetUrl("/catalog/health");
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Http:Dependencies:0:Method"] = "GET";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Http:Dependencies:0:Required"] = "false";
        builder.AddCephalon(cephalon =>
        {
            cephalon.AddModule(new PlatformTestModule());
            cephalon.AddModule(new DiscoveryTestModule());
        });
        builder.Services.AddCephalonHttpDependencyHealth(builder.Configuration);

        using var host = builder.Build();

        await host.StartAsync();

        var evaluator = host.Services.GetRequiredService<RuntimeHealthEvaluator>();
        var dependencies = evaluator.EvaluateDependencies();

        var dependency = Assert.Single(dependencies);
        Assert.Equal("catalog-api", dependency.Id);
        Assert.Equal("Catalog API", dependency.DisplayName);
        Assert.Equal(HealthState.Healthy, dependency.State);
        Assert.Equal("Cephalon.Observability.HttpDependencies", dependency.Source);

        await host.StopAsync();
    }

    [Fact]
    public async Task AddCephalonHttpDependencyHealthTreatsRequiredFailuresAsReadinessFailures()
    {
        await using var server = new DependencyHealthHttpServer(new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["/payments/health"] = 503
        });

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Http:Dependencies:0:Id"] = "payments-api";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Http:Dependencies:0:DisplayName"] = "Payments API";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Http:Dependencies:0:Endpoint"] = server.GetUrl("/payments/health");
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Http:Dependencies:0:Method"] = "GET";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Http:Dependencies:0:Required"] = "true";
        builder.AddCephalon(cephalon =>
        {
            cephalon.AddModule(new PlatformTestModule());
            cephalon.AddModule(new DiscoveryTestModule());
        });
        builder.Services.AddCephalonHttpDependencyHealth(builder.Configuration);

        using var host = builder.Build();

        await host.StartAsync();

        var evaluator = host.Services.GetRequiredService<RuntimeHealthEvaluator>();
        var readiness = evaluator.EvaluateReadiness();

        Assert.Equal(RuntimeHealthState.Unhealthy, readiness.State);
        var dependency = Assert.Single(readiness.Dependencies);
        Assert.Equal(HealthState.Unhealthy, dependency.State);
        Assert.Contains("503", dependency.Description, StringComparison.Ordinal);

        await host.StopAsync();
    }

    private sealed class DependencyHealthHttpServer : IAsyncDisposable
    {
        private readonly HttpListener listener = new();
        private readonly CancellationTokenSource cancellationSource = new();
        private readonly ConcurrentDictionary<string, int> responseCodes;
        private readonly Task listenLoop;

        public DependencyHealthHttpServer(IReadOnlyDictionary<string, int> responseCodes)
        {
            ArgumentNullException.ThrowIfNull(responseCodes);

            this.responseCodes = new ConcurrentDictionary<string, int>(responseCodes, StringComparer.OrdinalIgnoreCase);

            var port = ReservePort();
            BaseUrl = $"http://127.0.0.1:{port}";
            listener.Prefixes.Add($"{BaseUrl}/");
            listener.Start();
            listenLoop = Task.Run(ListenAsync);
        }

        public string BaseUrl { get; }

        public string GetUrl(string path) => $"{BaseUrl}/{path.TrimStart('/')}";

        public async ValueTask DisposeAsync()
        {
            cancellationSource.Cancel();
            listener.Stop();
            listener.Close();

            try
            {
                await listenLoop;
            }
            catch (OperationCanceledException)
            {
            }
            catch (HttpListenerException)
            {
            }

            cancellationSource.Dispose();
        }

        private async Task ListenAsync()
        {
            while (!cancellationSource.IsCancellationRequested)
            {
                HttpListenerContext context;
                try
                {
                    context = await listener.GetContextAsync();
                }
                catch (HttpListenerException) when (cancellationSource.IsCancellationRequested)
                {
                    break;
                }
                catch (ObjectDisposedException) when (cancellationSource.IsCancellationRequested)
                {
                    break;
                }

                var rawPath = context.Request.Url?.AbsolutePath ?? "/";
                var statusCode = responseCodes.TryGetValue(rawPath, out var configuredStatusCode)
                    ? configuredStatusCode
                    : 404;

                context.Response.StatusCode = statusCode;
                context.Response.Close();
            }
        }

        private static int ReservePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();

            try
            {
                return ((IPEndPoint)listener.LocalEndpoint).Port;
            }
            finally
            {
                listener.Stop();
            }
        }
    }
}
