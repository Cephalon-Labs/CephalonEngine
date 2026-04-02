using Cephalon.Abstractions.Health;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Runtime;
using Cephalon.Observability.ClickHouseDependencies.Configuration;
using Cephalon.Observability.ClickHouseDependencies.Hosting;
using Cephalon.Observability.ClickHouseDependencies.Services;
using Cephalon.Worker.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cephalon.Tests.Hosting;

public sealed class ClickHouseDependencyHealthHostingTests
{
    [Fact]
    public async Task AddCephalonClickHouseDependencyHealthReportsHealthyQuery()
    {
        var probeClient = new FakeClickHouseDependencyProbeClient(dependency =>
            $"ClickHouse endpoint '{dependency.Protocol}://{dependency.Host}:{dependency.Port}/{dependency.Database}' responded to health query.");

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:ClickHouse:Dependencies:0:Id"] = "analytics-clickhouse";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:ClickHouse:Dependencies:0:DisplayName"] = "Analytics ClickHouse";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:ClickHouse:Dependencies:0:Host"] = "analytics.internal.example";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:ClickHouse:Dependencies:0:Protocol"] = "https";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:ClickHouse:Dependencies:0:Port"] = "8443";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:ClickHouse:Dependencies:0:Database"] = "analytics";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:ClickHouse:Dependencies:0:Username"] = "cephalon";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:ClickHouse:Dependencies:0:HealthQuery"] = "SELECT 1";
        builder.AddCephalon();
        builder.Services.AddCephalonClickHouseDependencyHealth(builder.Configuration);
        builder.Services.AddSingleton<IClickHouseDependencyProbeClient>(probeClient);

        using var host = builder.Build();

        await host.StartAsync();

        var evaluator = host.Services.GetRequiredService<RuntimeHealthEvaluator>();
        var dependency = Assert.Single(evaluator.EvaluateDependencies());

        Assert.Equal("analytics-clickhouse", dependency.Id);
        Assert.Equal("Analytics ClickHouse", dependency.DisplayName);
        Assert.Equal(HealthState.Healthy, dependency.State);
        Assert.Equal("Cephalon.Observability.ClickHouseDependencies", dependency.Source);

        var captured = Assert.Single(probeClient.CapturedDependencies);
        Assert.Equal("analytics", captured.Database);
        Assert.Equal("cephalon", captured.Username);
        Assert.Equal("SELECT 1", captured.HealthQuery);

        await host.StopAsync();
    }

    [Fact]
    public async Task AddCephalonClickHouseDependencyHealthTreatsRequiredFailuresAsReadinessFailures()
    {
        var probeClient = new FakeClickHouseDependencyProbeClient(_ => throw new InvalidOperationException("authentication failed"));

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:ClickHouse:Dependencies:0:Id"] = "required-clickhouse";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:ClickHouse:Dependencies:0:ConnectionString"] = "Host=analytics.internal.example;Protocol=https;Port=8443;Database=warehouse;Username=default;Password=secret";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:ClickHouse:Dependencies:0:Required"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:ClickHouse:Dependencies:0:TimeoutSeconds"] = "1";
        builder.AddCephalon();
        builder.Services.AddCephalonClickHouseDependencyHealth(builder.Configuration);
        builder.Services.AddSingleton<IClickHouseDependencyProbeClient>(probeClient);

        using var host = builder.Build();

        await host.StartAsync();

        var evaluator = host.Services.GetRequiredService<RuntimeHealthEvaluator>();
        var readiness = evaluator.EvaluateReadiness();
        var dependency = Assert.Single(readiness.Dependencies);

        Assert.Equal(RuntimeHealthState.Unhealthy, readiness.State);
        Assert.Equal(HealthState.Unhealthy, dependency.State);
        Assert.Contains("authentication failed", dependency.Description, StringComparison.OrdinalIgnoreCase);

        var captured = Assert.Single(probeClient.CapturedDependencies);
        Assert.Equal("Host=analytics.internal.example;Protocol=https;Port=8443;Database=warehouse;Username=default;Password=secret", captured.ConnectionString);

        await host.StopAsync();
    }

    [Fact]
    public void CreateConnectionStringBuildsDiscreteConnectionSettings()
    {
        var connectionString = ClickHouseDependencyProbeClient.CreateConnectionString(
            new ClickHouseDependencyDefinition
            {
                Host = "analytics.internal.example",
                Protocol = "https",
                Port = 8443,
                Database = "analytics",
                Username = "cephalon",
                Password = "secret"
            });

        Assert.Equal(
            "Host=analytics.internal.example;Protocol=https;Port=8443;Database=analytics;Username=cephalon;Password=secret",
            connectionString);
    }

    [Fact]
    public void CreateConnectionStringUsesConnectionStringAsBase()
    {
        var connectionString = ClickHouseDependencyProbeClient.CreateConnectionString(
            new ClickHouseDependencyDefinition
            {
                ConnectionString = "Host=warehouse.internal.example;Protocol=http;Port=8123;Database=warehouse;Username=default;Compression=lz4",
                Protocol = "https",
                Port = 8443,
                Username = "cephalon",
                Password = "override"
            });

        Assert.Equal(
            "Host=warehouse.internal.example;Protocol=https;Port=8443;Database=warehouse;Username=cephalon;Password=override;Compression=lz4",
            connectionString);
    }

    private sealed class FakeClickHouseDependencyProbeClient(Func<ClickHouseDependencyDefinition, string> onProbe) : IClickHouseDependencyProbeClient
    {
        public List<ClickHouseDependencyDefinition> CapturedDependencies { get; } = [];

        public ValueTask<string> ProbeAsync(ClickHouseDependencyDefinition dependency, CancellationToken cancellationToken)
        {
            CapturedDependencies.Add(dependency);
            return ValueTask.FromResult(onProbe(dependency));
        }
    }
}
