using System.Net;
using System.Net.Sockets;
using System.Text;
using Cephalon.Abstractions.Health;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Runtime;
using Cephalon.Observability.ElasticsearchDependencies.Configuration;
using Cephalon.Observability.ElasticsearchDependencies.Hosting;
using Cephalon.Observability.ElasticsearchDependencies.Services;
using Cephalon.Worker.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cephalon.Tests.Hosting;

public sealed class ElasticsearchDependencyHealthHostingTests
{
    [Fact]
    public async Task AddCephalonElasticsearchDependencyHealthReportsGreenCluster()
    {
        var probeClient = new FakeElasticsearchDependencyProbeClient(dependency =>
            $"Elasticsearch cluster 'search-prod' at {dependency.Endpoint} reported green health across 3 nodes.");

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Elasticsearch:Dependencies:0:Id"] = "search-cluster";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Elasticsearch:Dependencies:0:DisplayName"] = "Search Cluster";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Elasticsearch:Dependencies:0:Endpoint"] = "https://search.internal.example:9200";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Elasticsearch:Dependencies:0:ApiKey"] = "elastic-api-key";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Elasticsearch:Dependencies:0:TimeoutSeconds"] = "7";
        builder.AddCephalon();
        builder.Services.AddCephalonElasticsearchDependencyHealth(builder.Configuration);
        builder.Services.AddSingleton<IElasticsearchDependencyProbeClient>(probeClient);

        using var host = builder.Build();

        await host.StartAsync();

        var evaluator = host.Services.GetRequiredService<RuntimeHealthEvaluator>();
        var dependency = Assert.Single(evaluator.EvaluateDependencies());

        Assert.Equal("search-cluster", dependency.Id);
        Assert.Equal("Search Cluster", dependency.DisplayName);
        Assert.Equal(HealthState.Healthy, dependency.State);
        Assert.Equal("Cephalon.Observability.ElasticsearchDependencies", dependency.Source);

        var captured = Assert.Single(probeClient.CapturedDependencies);
        Assert.Equal("https://search.internal.example:9200", captured.Endpoint);
        Assert.Equal("elastic-api-key", captured.ApiKey);
        Assert.Equal(7, captured.TimeoutSeconds);

        await host.StopAsync();
    }

    [Fact]
    public async Task AddCephalonElasticsearchDependencyHealthTreatsRequiredRedClusterAsReadinessFailure()
    {
        var probeClient = new FakeElasticsearchDependencyProbeClient(_ =>
            throw new InvalidOperationException(
                "Elasticsearch cluster 'search-prod' at https://search.internal.example:9200/_cluster/health reported red health across 2 nodes."));

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Elasticsearch:Dependencies:0:Id"] = "required-search";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Elasticsearch:Dependencies:0:Endpoint"] = "https://search.internal.example:9200";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Elasticsearch:Dependencies:0:Required"] = "true";
        builder.AddCephalon();
        builder.Services.AddCephalonElasticsearchDependencyHealth(builder.Configuration);
        builder.Services.AddSingleton<IElasticsearchDependencyProbeClient>(probeClient);

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
    public void ResolveClusterHealthEndpointAppendsDefaultPathWhenOnlyBaseUrlIsConfigured()
    {
        var endpoint = ElasticsearchDependencyProbeClient.ResolveClusterHealthEndpoint("https://search.internal.example:9200");

        Assert.Equal("https://search.internal.example:9200/_cluster/health", endpoint.ToString());
    }

    [Fact]
    public async Task ProbeAsyncUsesApiKeyAuthAndThrowsForYellowClusterHealth()
    {
        await using var server = new ElasticsearchHealthHttpServer("""
            {
              "cluster_name": "search-prod",
              "status": "yellow",
              "timed_out": false,
              "number_of_nodes": 1
            }
            """);

        var services = new ServiceCollection();
        services.AddHttpClient(ElasticsearchDependencyHealthServiceCollectionExtensions.HttpClientName)
            .ConfigureHttpClient(client => client.Timeout = Timeout.InfiniteTimeSpan);
        services.AddSingleton<IElasticsearchDependencyProbeClient, ElasticsearchDependencyProbeClient>();

        using var provider = services.BuildServiceProvider();
        var probeClient = provider.GetRequiredService<IElasticsearchDependencyProbeClient>();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            probeClient.ProbeAsync(
                new ElasticsearchDependencyDefinition
                {
                    Endpoint = server.BaseUrl,
                    ApiKey = "elastic-api-key"
                },
                CancellationToken.None).AsTask());

        Assert.Contains("yellow health", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("/_cluster/health", server.LastPath);
        Assert.Equal("ApiKey elastic-api-key", server.LastAuthorizationHeader);
    }

    private sealed class FakeElasticsearchDependencyProbeClient(Func<ElasticsearchDependencyDefinition, string> onProbe)
        : IElasticsearchDependencyProbeClient
    {
        public List<ElasticsearchDependencyDefinition> CapturedDependencies { get; } = [];

        public ValueTask<string> ProbeAsync(
            ElasticsearchDependencyDefinition dependency,
            CancellationToken cancellationToken)
        {
            CapturedDependencies.Add(dependency);
            return ValueTask.FromResult(onProbe(dependency));
        }
    }

    private sealed class ElasticsearchHealthHttpServer : IAsyncDisposable
    {
        private readonly HttpListener listener = new();
        private readonly CancellationTokenSource cancellationSource = new();
        private readonly string responseBody;
        private readonly Task listenLoop;

        public ElasticsearchHealthHttpServer(string responseBody)
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
