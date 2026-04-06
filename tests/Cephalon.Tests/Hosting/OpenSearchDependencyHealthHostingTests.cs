using System.Net;
using System.Net.Sockets;
using System.Text;
using Cephalon.Abstractions.Health;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Runtime;
using Cephalon.Observability.OpenSearchDependencies.Configuration;
using Cephalon.Observability.OpenSearchDependencies.Hosting;
using Cephalon.Observability.OpenSearchDependencies.Services;
using Cephalon.Worker.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cephalon.Tests.Hosting;

public sealed class OpenSearchDependencyHealthHostingTests
{
    [Fact]
    public async Task AddCephalonOpenSearchDependencyHealthReportsHealthyCluster()
    {
        var probeClient = new FakeOpenSearchDependencyProbeClient(dependency =>
            $"OpenSearch cluster 'search-prod' at {dependency.Endpoint} reported green health across 4 nodes.");

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:OpenSearch:Dependencies:0:Id"] = "catalog-search";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:OpenSearch:Dependencies:0:DisplayName"] = "Catalog Search";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:OpenSearch:Dependencies:0:Endpoint"] = "https://search.internal.example:9200";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:OpenSearch:Dependencies:0:Index"] = "catalog-items";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:OpenSearch:Dependencies:0:BearerToken"] = "opensearch-bearer-token";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:OpenSearch:Dependencies:0:TimeoutSeconds"] = "7";
        builder.AddCephalon();
        builder.Services.AddCephalonOpenSearchDependencyHealth(builder.Configuration);
        builder.Services.AddSingleton<IOpenSearchDependencyProbeClient>(probeClient);

        using var host = builder.Build();

        await host.StartAsync();

        var evaluator = host.Services.GetRequiredService<RuntimeHealthEvaluator>();
        var dependency = Assert.Single(evaluator.EvaluateDependencies());

        Assert.Equal("catalog-search", dependency.Id);
        Assert.Equal("Catalog Search", dependency.DisplayName);
        Assert.Equal(HealthState.Healthy, dependency.State);
        Assert.Equal("Cephalon.Observability.OpenSearchDependencies", dependency.Source);

        var captured = Assert.Single(probeClient.CapturedDependencies);
        Assert.Equal("catalog-items", captured.Index);
        Assert.Equal("opensearch-bearer-token", captured.BearerToken);
        Assert.Equal(7, captured.TimeoutSeconds);

        await host.StopAsync();
    }

    [Fact]
    public async Task AddCephalonOpenSearchDependencyHealthTreatsRequiredFailuresAsReadinessFailures()
    {
        var probeClient = new FakeOpenSearchDependencyProbeClient(_ =>
            throw new InvalidOperationException(
                "OpenSearch cluster 'search-prod' at https://search.internal.example:9200/_cluster/health/orders reported red health across 2 nodes."));

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:OpenSearch:Dependencies:0:Id"] = "required-search";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:OpenSearch:Dependencies:0:Endpoint"] = "https://search.internal.example:9200";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:OpenSearch:Dependencies:0:Index"] = "orders";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:OpenSearch:Dependencies:0:Required"] = "true";
        builder.AddCephalon();
        builder.Services.AddCephalonOpenSearchDependencyHealth(builder.Configuration);
        builder.Services.AddSingleton<IOpenSearchDependencyProbeClient>(probeClient);

        using var host = builder.Build();

        await host.StartAsync();

        var evaluator = host.Services.GetRequiredService<RuntimeHealthEvaluator>();
        var readiness = evaluator.EvaluateReadiness();
        var dependency = Assert.Single(readiness.Dependencies);

        Assert.Equal(RuntimeHealthState.Unhealthy, readiness.State);
        Assert.Equal(HealthState.Unhealthy, dependency.State);
        Assert.Contains("red health", dependency.Description, StringComparison.OrdinalIgnoreCase);

        await host.StopAsync();
    }

    [Fact]
    public void ResolveClusterHealthEndpointAppendsDefaultPathAndIndexWhenBaseUrlIsConfigured()
    {
        var endpoint = OpenSearchDependencyProbeClient.ResolveClusterHealthEndpoint(
            new OpenSearchDependencyDefinition
            {
                Endpoint = "https://search.internal.example:9200",
                Index = "catalog-items"
            });

        Assert.Equal("https://search.internal.example:9200/_cluster/health/catalog-items", endpoint.ToString());
    }

    [Fact]
    public async Task ProbeAsyncUsesBasicAuthAndThrowsForYellowClusterHealth()
    {
        await using var server = new OpenSearchHealthHttpServer("""
            {
              "cluster_name": "search-prod",
              "status": "yellow",
              "timed_out": false,
              "number_of_nodes": 2,
              "discovered_cluster_manager": true
            }
            """);

        var services = new ServiceCollection();
        services.AddHttpClient(OpenSearchDependencyHealthServiceCollectionExtensions.HttpClientName)
            .ConfigureHttpClient(client => client.Timeout = Timeout.InfiniteTimeSpan);
        services.AddSingleton<IOpenSearchDependencyProbeClient, OpenSearchDependencyProbeClient>();

        using var provider = services.BuildServiceProvider();
        var probeClient = provider.GetRequiredService<IOpenSearchDependencyProbeClient>();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            probeClient.ProbeAsync(
                new OpenSearchDependencyDefinition
                {
                    Endpoint = server.BaseUrl,
                    Index = "catalog-items",
                    Username = "cephalon-runtime",
                    Password = "super-secret"
                },
                CancellationToken.None).AsTask());

        Assert.Contains("yellow health", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("/_cluster/health/catalog-items", server.LastPath);

        var expectedAuthorization = $"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes("cephalon-runtime:super-secret"))}";
        Assert.Equal(expectedAuthorization, server.LastAuthorizationHeader);
    }

    private sealed class FakeOpenSearchDependencyProbeClient(Func<OpenSearchDependencyDefinition, string> onProbe)
        : IOpenSearchDependencyProbeClient
    {
        public List<OpenSearchDependencyDefinition> CapturedDependencies { get; } = [];

        public ValueTask<string> ProbeAsync(
            OpenSearchDependencyDefinition dependency,
            CancellationToken cancellationToken)
        {
            CapturedDependencies.Add(dependency);
            return ValueTask.FromResult(onProbe(dependency));
        }
    }

    private sealed class OpenSearchHealthHttpServer : IAsyncDisposable
    {
        private readonly HttpListener listener = new();
        private readonly CancellationTokenSource cancellationSource = new();
        private readonly string responseBody;
        private readonly Task listenLoop;

        public OpenSearchHealthHttpServer(string responseBody)
        {
            this.responseBody = responseBody;

            var port = ReservePort();
            BaseUrl = $"http://127.0.0.1:{port}";
            listener.Prefixes.Add($"{BaseUrl}/");
            listener.Start();
            listenLoop = Task.Run(ListenAsync);
        }

        public string BaseUrl { get; }

        public string? LastAuthorizationHeader { get; private set; }

        public string? LastPath { get; private set; }

        public async ValueTask DisposeAsync()
        {
            cancellationSource.Cancel();
            listener.Stop();
            listener.Close();

            try
            {
                await listenLoop.ConfigureAwait(false);
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
                    context = await listener.GetContextAsync().ConfigureAwait(false);
                }
                catch (HttpListenerException) when (cancellationSource.IsCancellationRequested)
                {
                    break;
                }
                catch (ObjectDisposedException) when (cancellationSource.IsCancellationRequested)
                {
                    break;
                }

                LastPath = context.Request.Url?.AbsolutePath;
                LastAuthorizationHeader = context.Request.Headers["Authorization"];
                context.Response.StatusCode = 200;
                context.Response.ContentType = "application/json";

                var payload = Encoding.UTF8.GetBytes(responseBody);
                await context.Response.OutputStream.WriteAsync(payload).ConfigureAwait(false);
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
