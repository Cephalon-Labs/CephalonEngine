using System.Net;
using System.Net.Sockets;
using System.Text;
using Cephalon.Abstractions.Health;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Runtime;
using Cephalon.Observability.ConsulDependencies.Configuration;
using Cephalon.Observability.ConsulDependencies.Hosting;
using Cephalon.Observability.ConsulDependencies.Services;
using Cephalon.Worker.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cephalon.Tests.Hosting;

public sealed class ConsulDependencyHealthHostingTests
{
    [Fact]
    public async Task AddCephalonConsulDependencyHealthReportsLeader()
    {
        var probeClient = new FakeConsulDependencyProbeClient(dependency =>
            new ConsulProbeResult(
                HealthState.Healthy,
                $"Consul endpoint '{dependency.Endpoint}' reported leader '10.0.0.5:8300' for datacenter 'ops-dc'."));

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Consul:Dependencies:0:Id"] = "service-discovery";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Consul:Dependencies:0:DisplayName"] = "Service Discovery";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Consul:Dependencies:0:Endpoint"] = "https://consul.internal.example:8501";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Consul:Dependencies:0:AclToken"] = "consul-acl-token";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Consul:Dependencies:0:Datacenter"] = "ops-dc";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Consul:Dependencies:0:TimeoutSeconds"] = "7";
        builder.AddCephalon();
        builder.Services.AddCephalonConsulDependencyHealth(builder.Configuration);
        builder.Services.AddSingleton<IConsulDependencyProbeClient>(probeClient);

        using var host = builder.Build();

        await host.StartAsync();

        var evaluator = host.Services.GetRequiredService<RuntimeHealthEvaluator>();
        var dependency = Assert.Single(evaluator.EvaluateDependencies());

        Assert.Equal("service-discovery", dependency.Id);
        Assert.Equal("Service Discovery", dependency.DisplayName);
        Assert.Equal(HealthState.Healthy, dependency.State);
        Assert.Equal("Cephalon.Observability.ConsulDependencies", dependency.Source);

        var captured = Assert.Single(probeClient.CapturedDependencies);
        Assert.Equal("https://consul.internal.example:8501", captured.Endpoint);
        Assert.Equal("consul-acl-token", captured.AclToken);
        Assert.Equal("ops-dc", captured.Datacenter);
        Assert.Equal(7, captured.TimeoutSeconds);

        await host.StopAsync();
    }

    [Fact]
    public async Task AddCephalonConsulDependencyHealthTreatsRequiredMissingLeaderAsReadinessFailure()
    {
        var probeClient = new FakeConsulDependencyProbeClient(_ =>
            new ConsulProbeResult(
                HealthState.Unhealthy,
                "Consul endpoint 'https://consul.internal.example:8501/v1/status/leader' reported no active leader."));

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Consul:Dependencies:0:Id"] = "required-consul";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Consul:Dependencies:0:Endpoint"] = "https://consul.internal.example:8501";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Consul:Dependencies:0:Required"] = "true";
        builder.AddCephalon();
        builder.Services.AddCephalonConsulDependencyHealth(builder.Configuration);
        builder.Services.AddSingleton<IConsulDependencyProbeClient>(probeClient);

        using var host = builder.Build();

        await host.StartAsync();

        var evaluator = host.Services.GetRequiredService<RuntimeHealthEvaluator>();
        var readiness = evaluator.EvaluateReadiness();
        var dependency = Assert.Single(readiness.Dependencies);

        Assert.Equal(RuntimeHealthState.Unhealthy, readiness.State);
        Assert.Equal(HealthState.Unhealthy, dependency.State);
        Assert.Contains("no active leader", dependency.Description, StringComparison.OrdinalIgnoreCase);

        await host.StopAsync();
    }

    [Fact]
    public void ResolveLeaderEndpointAppendsDefaultPathAndDatacenter()
    {
        var endpoint = ConsulDependencyProbeClient.ResolveLeaderEndpoint(
            "https://consul.internal.example:8501",
            "ops-dc");

        Assert.Equal("https://consul.internal.example:8501/v1/status/leader?dc=ops-dc", endpoint.ToString());
    }

    [Fact]
    public async Task ProbeAsyncUsesAclTokenAndParsesLeaderResponse()
    {
        await using var server = new ConsulLeaderHttpServer("\"10.0.0.5:8300\"");

        var services = new ServiceCollection();
        services.AddHttpClient(ConsulDependencyHealthServiceCollectionExtensions.HttpClientName)
            .ConfigureHttpClient(client => client.Timeout = Timeout.InfiniteTimeSpan);
        services.AddSingleton<IConsulDependencyProbeClient, ConsulDependencyProbeClient>();

        using var provider = services.BuildServiceProvider();
        var probeClient = provider.GetRequiredService<IConsulDependencyProbeClient>();

        var result = await probeClient.ProbeAsync(
            new ConsulDependencyDefinition
            {
                Endpoint = server.BaseUrl,
                AclToken = "consul-acl-token",
                Datacenter = "ops-dc"
            },
            CancellationToken.None);

        Assert.Equal(HealthState.Healthy, result.State);
        Assert.Contains("10.0.0.5:8300", result.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("/v1/status/leader", server.LastPath);
        Assert.Equal("dc=ops-dc", server.LastQueryString);
        Assert.Equal("consul-acl-token", server.LastAclToken);
    }

    private sealed class FakeConsulDependencyProbeClient(Func<ConsulDependencyDefinition, ConsulProbeResult> onProbe)
        : IConsulDependencyProbeClient
    {
        public List<ConsulDependencyDefinition> CapturedDependencies { get; } = [];

        public ValueTask<ConsulProbeResult> ProbeAsync(
            ConsulDependencyDefinition dependency,
            CancellationToken cancellationToken)
        {
            CapturedDependencies.Add(dependency);
            return ValueTask.FromResult(onProbe(dependency));
        }
    }

    private sealed class ConsulLeaderHttpServer : IAsyncDisposable
    {
        private readonly HttpListener listener = new();
        private readonly CancellationTokenSource cancellationSource = new();
        private readonly string responseBody;
        private readonly Task listenLoop;

        public ConsulLeaderHttpServer(string responseBody)
        {
            this.responseBody = responseBody;

            var port = ReservePort();
            BaseUrl = $"http://127.0.0.1:{port}";
            listener.Prefixes.Add($"{BaseUrl}/");
            listener.Start();
            listenLoop = Task.Run(ListenAsync);
        }

        public string BaseUrl { get; }

        public string? LastAclToken { get; private set; }

        public string? LastPath { get; private set; }

        public string? LastQueryString { get; private set; }

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
                LastQueryString = context.Request.Url?.Query.TrimStart('?');
                LastAclToken = context.Request.Headers["X-Consul-Token"];
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
