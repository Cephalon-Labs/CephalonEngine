using Cephalon.Abstractions.Health;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Runtime;
using Cephalon.Observability.CassandraDependencies.Configuration;
using Cephalon.Observability.CassandraDependencies.Hosting;
using Cephalon.Observability.CassandraDependencies.Services;
using Cephalon.Worker.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cephalon.Tests.Hosting;

public sealed class CassandraDependencyHealthHostingTests
{
    [Fact]
    public async Task AddCephalonCassandraDependencyHealthReportsHealthyQuery()
    {
        var probeClient = new FakeCassandraDependencyProbeClient(dependency =>
            $"Cassandra endpoints '{string.Join(", ", dependency.ContactPoints)}:{dependency.Port}/{dependency.Keyspace}' responded to health query.");

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Cassandra:Dependencies:0:Id"] = "orders-cassandra";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Cassandra:Dependencies:0:DisplayName"] = "Orders Cassandra";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Cassandra:Dependencies:0:ContactPoints:0"] = "cass-a.internal.example";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Cassandra:Dependencies:0:ContactPoints:1"] = "cass-b.internal.example";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Cassandra:Dependencies:0:Port"] = "9142";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Cassandra:Dependencies:0:Keyspace"] = "orders";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Cassandra:Dependencies:0:Username"] = "cephalon";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Cassandra:Dependencies:0:HealthQuery"] = "SELECT release_version FROM system.local;";
        builder.AddCephalon();
        builder.Services.AddCephalonCassandraDependencyHealth(builder.Configuration);
        builder.Services.AddSingleton<ICassandraDependencyProbeClient>(probeClient);

        using var host = builder.Build();

        await host.StartAsync();

        var evaluator = host.Services.GetRequiredService<RuntimeHealthEvaluator>();
        var dependency = Assert.Single(evaluator.EvaluateDependencies());

        Assert.Equal("orders-cassandra", dependency.Id);
        Assert.Equal("Orders Cassandra", dependency.DisplayName);
        Assert.Equal(HealthState.Healthy, dependency.State);
        Assert.Equal("Cephalon.Observability.CassandraDependencies", dependency.Source);

        var captured = Assert.Single(probeClient.CapturedDependencies);
        Assert.Equal(["cass-a.internal.example", "cass-b.internal.example"], captured.ContactPoints);
        Assert.Equal(9142, captured.Port);
        Assert.Equal("orders", captured.Keyspace);
        Assert.Equal("cephalon", captured.Username);
        Assert.Equal("SELECT release_version FROM system.local;", captured.HealthQuery);

        await host.StopAsync();
    }

    [Fact]
    public async Task AddCephalonCassandraDependencyHealthTreatsRequiredFailuresAsReadinessFailures()
    {
        var probeClient = new FakeCassandraDependencyProbeClient(_ => throw new InvalidOperationException("coordinator did not respond"));

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Cassandra:Dependencies:0:Id"] = "required-cassandra";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Cassandra:Dependencies:0:ContactPoints"] = "cass-a.internal.example,cass-b.internal.example";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Cassandra:Dependencies:0:Required"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Cassandra:Dependencies:0:TimeoutSeconds"] = "1";
        builder.AddCephalon();
        builder.Services.AddCephalonCassandraDependencyHealth(builder.Configuration);
        builder.Services.AddSingleton<ICassandraDependencyProbeClient>(probeClient);

        using var host = builder.Build();

        await host.StartAsync();

        var evaluator = host.Services.GetRequiredService<RuntimeHealthEvaluator>();
        var readiness = evaluator.EvaluateReadiness();
        var dependency = Assert.Single(readiness.Dependencies);

        Assert.Equal(RuntimeHealthState.Unhealthy, readiness.State);
        Assert.Equal(HealthState.Unhealthy, dependency.State);
        Assert.Contains("coordinator did not respond", dependency.Description, StringComparison.OrdinalIgnoreCase);

        var captured = Assert.Single(probeClient.CapturedDependencies);
        Assert.Equal(["cass-a.internal.example", "cass-b.internal.example"], captured.ContactPoints);

        await host.StopAsync();
    }

    [Fact]
    public void NormalizeContactPointsTrimsAndFiltersBlankEntries()
    {
        var contactPoints = CassandraDependencyProbeClient.NormalizeContactPoints(
            [" cass-a.internal.example ", string.Empty, "cass-b.internal.example", "   "]);

        Assert.Equal(["cass-a.internal.example", "cass-b.internal.example"], contactPoints);
    }

    private sealed class FakeCassandraDependencyProbeClient(Func<CassandraDependencyDefinition, string> onProbe) : ICassandraDependencyProbeClient
    {
        public List<CassandraDependencyDefinition> CapturedDependencies { get; } = [];

        public ValueTask<string> ProbeAsync(CassandraDependencyDefinition dependency, CancellationToken cancellationToken)
        {
            CapturedDependencies.Add(dependency);
            return ValueTask.FromResult(onProbe(dependency));
        }
    }
}
