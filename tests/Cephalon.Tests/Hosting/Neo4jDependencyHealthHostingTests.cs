using Cephalon.Abstractions.Health;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Runtime;
using Cephalon.Observability.Neo4jDependencies.Configuration;
using Cephalon.Observability.Neo4jDependencies.Hosting;
using Cephalon.Observability.Neo4jDependencies.Services;
using Cephalon.Worker.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cephalon.Tests.Hosting;

public sealed class Neo4jDependencyHealthHostingTests
{
    [Fact]
    public async Task AddCephalonNeo4jDependencyHealthReportsHealthyQuery()
    {
        var probeClient = new FakeNeo4jDependencyProbeClient(dependency =>
            $"Neo4j endpoint '{dependency.Uri}/{dependency.Database}' responded to health query.");

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Neo4j:Dependencies:0:Id"] = "orders-neo4j";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Neo4j:Dependencies:0:DisplayName"] = "Orders Neo4j";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Neo4j:Dependencies:0:Uri"] = "neo4j://graph.internal.example:7687";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Neo4j:Dependencies:0:Database"] = "orders";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Neo4j:Dependencies:0:Username"] = "cephalon";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Neo4j:Dependencies:0:HealthQuery"] = "RETURN 1 AS health";
        builder.AddCephalon();
        builder.Services.AddCephalonNeo4jDependencyHealth(builder.Configuration);
        builder.Services.AddSingleton<INeo4jDependencyProbeClient>(probeClient);

        using var host = builder.Build();

        await host.StartAsync();

        var evaluator = host.Services.GetRequiredService<RuntimeHealthEvaluator>();
        var dependency = Assert.Single(evaluator.EvaluateDependencies());

        Assert.Equal("orders-neo4j", dependency.Id);
        Assert.Equal("Orders Neo4j", dependency.DisplayName);
        Assert.Equal(HealthState.Healthy, dependency.State);
        Assert.Equal("Cephalon.Observability.Neo4jDependencies", dependency.Source);

        var captured = Assert.Single(probeClient.CapturedDependencies);
        Assert.Equal("neo4j://graph.internal.example:7687", captured.Uri);
        Assert.Equal("orders", captured.Database);
        Assert.Equal("cephalon", captured.Username);
        Assert.Equal("RETURN 1 AS health", captured.HealthQuery);

        await host.StopAsync();
    }

    [Fact]
    public async Task AddCephalonNeo4jDependencyHealthTreatsRequiredFailuresAsReadinessFailures()
    {
        var probeClient = new FakeNeo4jDependencyProbeClient(_ => throw new InvalidOperationException("routing table could not be resolved"));

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Neo4j:Dependencies:0:Id"] = "required-neo4j";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Neo4j:Dependencies:0:Host"] = "graph.internal.example";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Neo4j:Dependencies:0:Port"] = "8687";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Neo4j:Dependencies:0:Scheme"] = "bolt+s";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Neo4j:Dependencies:0:Required"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Neo4j:Dependencies:0:TimeoutSeconds"] = "1";
        builder.AddCephalon();
        builder.Services.AddCephalonNeo4jDependencyHealth(builder.Configuration);
        builder.Services.AddSingleton<INeo4jDependencyProbeClient>(probeClient);

        using var host = builder.Build();

        await host.StartAsync();

        var evaluator = host.Services.GetRequiredService<RuntimeHealthEvaluator>();
        var readiness = evaluator.EvaluateReadiness();
        var dependency = Assert.Single(readiness.Dependencies);

        Assert.Equal(RuntimeHealthState.Unhealthy, readiness.State);
        Assert.Equal(HealthState.Unhealthy, dependency.State);
        Assert.Contains("routing table could not be resolved", dependency.Description, StringComparison.OrdinalIgnoreCase);

        var captured = Assert.Single(probeClient.CapturedDependencies);
        Assert.Equal("graph.internal.example", captured.Host);
        Assert.Equal(8687, captured.Port);
        Assert.Equal("bolt+s", captured.Scheme);

        await host.StopAsync();
    }

    [Fact]
    public void CreateEndpointUriBuildsDiscreteSettings()
    {
        var dependency = new Neo4jDependencyDefinition
        {
            Host = "graph.internal.example",
            Port = 8687,
            Scheme = "bolt+s"
        };

        var endpoint = Neo4jDependencyProbeClient.CreateEndpointUri(dependency);

        Assert.Equal("bolt+s://graph.internal.example:8687/", endpoint.AbsoluteUri);
    }

    [Fact]
    public void CreateEndpointUriUsesExplicitUri()
    {
        var dependency = new Neo4jDependencyDefinition
        {
            Uri = "neo4j://graph.internal.example:7687"
        };

        var endpoint = Neo4jDependencyProbeClient.CreateEndpointUri(dependency);

        Assert.Equal("neo4j://graph.internal.example:7687/", endpoint.AbsoluteUri);
    }

    private sealed class FakeNeo4jDependencyProbeClient(Func<Neo4jDependencyDefinition, string> onProbe) : INeo4jDependencyProbeClient
    {
        public List<Neo4jDependencyDefinition> CapturedDependencies { get; } = [];

        public ValueTask<string> ProbeAsync(Neo4jDependencyDefinition dependency, CancellationToken cancellationToken)
        {
            CapturedDependencies.Add(dependency);
            return ValueTask.FromResult(onProbe(dependency));
        }
    }
}
