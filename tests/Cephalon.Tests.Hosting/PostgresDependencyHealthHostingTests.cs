using Cephalon.Abstractions.Health;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Runtime;
using Cephalon.Observability.PostgresDependencies.Configuration;
using Cephalon.Observability.PostgresDependencies.Hosting;
using Cephalon.Observability.PostgresDependencies.Services;
using Cephalon.Worker.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace Cephalon.Tests.Hosting;

public sealed class PostgresDependencyHealthHostingTests
{
    [Fact]
    public async Task AddCephalonPostgresDependencyHealthReportsHealthyQuery()
    {
        var probeClient = new FakePostgresDependencyProbeClient(dependency =>
            $"Postgres endpoint '{dependency.Host}:{dependency.Port}/{dependency.Database}' responded to health query.");

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Postgres:Dependencies:0:Id"] = "catalog-db";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Postgres:Dependencies:0:DisplayName"] = "Catalog Database";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Postgres:Dependencies:0:Host"] = "db.internal.example";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Postgres:Dependencies:0:Port"] = "5432";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Postgres:Dependencies:0:Database"] = "catalog";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Postgres:Dependencies:0:Username"] = "cephalon";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Postgres:Dependencies:0:SslMode"] = "Require";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Postgres:Dependencies:0:HealthQuery"] = "SELECT 1;";
        builder.AddCephalon();
        builder.Services.AddCephalonPostgresDependencyHealth(builder.Configuration);
        builder.Services.AddSingleton<IPostgresDependencyProbeClient>(probeClient);

        using var host = builder.Build();

        await host.StartAsync();

        var evaluator = host.Services.GetRequiredService<RuntimeHealthEvaluator>();
        var dependency = Assert.Single(evaluator.EvaluateDependencies());

        Assert.Equal("catalog-db", dependency.Id);
        Assert.Equal("Catalog Database", dependency.DisplayName);
        Assert.Equal(HealthState.Healthy, dependency.State);
        Assert.Equal("Cephalon.Observability.PostgresDependencies", dependency.Source);

        var captured = Assert.Single(probeClient.CapturedDependencies);
        Assert.Equal("catalog", captured.Database);
        Assert.Equal("cephalon", captured.Username);
        Assert.Equal("Require", captured.SslMode);
        Assert.Equal("SELECT 1;", captured.HealthQuery);

        await host.StopAsync();
    }

    [Fact]
    public async Task AddCephalonPostgresDependencyHealthTreatsRequiredFailuresAsReadinessFailures()
    {
        var probeClient = new FakePostgresDependencyProbeClient(_ => throw new InvalidOperationException("password authentication failed"));

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Postgres:Dependencies:0:Id"] = "required-db";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Postgres:Dependencies:0:ConnectionString"] = "Host=db.internal.example;Port=5432;Database=operations;Username=cephalon;";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Postgres:Dependencies:0:Required"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Postgres:Dependencies:0:TimeoutSeconds"] = "1";
        builder.AddCephalon();
        builder.Services.AddCephalonPostgresDependencyHealth(builder.Configuration);
        builder.Services.AddSingleton<IPostgresDependencyProbeClient>(probeClient);

        using var host = builder.Build();

        await host.StartAsync();

        var evaluator = host.Services.GetRequiredService<RuntimeHealthEvaluator>();
        var readiness = evaluator.EvaluateReadiness();
        var dependency = Assert.Single(readiness.Dependencies);

        Assert.Equal(RuntimeHealthState.Unhealthy, readiness.State);
        Assert.Equal(HealthState.Unhealthy, dependency.State);
        Assert.Contains("authentication failed", dependency.Description, StringComparison.OrdinalIgnoreCase);

        var captured = Assert.Single(probeClient.CapturedDependencies);
        Assert.Equal("Host=db.internal.example;Port=5432;Database=operations;Username=cephalon;", captured.ConnectionString);

        await host.StopAsync();
    }

    [Fact]
    public void CreateConnectionStringBuilderBuildsDiscreteConnectionSettings()
    {
        var dependency = new PostgresDependencyDefinition
        {
            Host = "postgres.internal.example",
            Port = 5433,
            Database = "operations",
            Username = "cephalon",
            Password = "secret",
            SslMode = "Require",
            TimeoutSeconds = 7
        };

        var builder = NpgsqlPostgresDependencyProbeClient.CreateConnectionStringBuilder(dependency, timeoutSeconds: 7);

        Assert.Equal("postgres.internal.example", builder.Host);
        Assert.Equal(5433, builder.Port);
        Assert.Equal("operations", builder.Database);
        Assert.Equal("cephalon", builder.Username);
        Assert.Equal("secret", builder.Password);
        Assert.Equal(SslMode.Require, builder.SslMode);
        Assert.Equal(7, builder.Timeout);
        Assert.Equal(7, builder.CommandTimeout);
        Assert.False(builder.Pooling);
        Assert.Equal("Cephalon.DependencyHealth.Postgres", builder.ApplicationName);
    }

    [Fact]
    public void CreateConnectionStringBuilderUsesConnectionStringAsBase()
    {
        var dependency = new PostgresDependencyDefinition
        {
            ConnectionString = "Host=override.internal.example;Port=5440;Database=inventory;Username=runtime;Pooling=true;Command Timeout=33;SSL Mode=Disable;",
            SslMode = "Prefer"
        };

        var builder = NpgsqlPostgresDependencyProbeClient.CreateConnectionStringBuilder(dependency, timeoutSeconds: 4);

        Assert.Equal("override.internal.example", builder.Host);
        Assert.Equal(5440, builder.Port);
        Assert.Equal("inventory", builder.Database);
        Assert.Equal("runtime", builder.Username);
        Assert.Equal(SslMode.Prefer, builder.SslMode);
        Assert.Equal(4, builder.Timeout);
        Assert.Equal(4, builder.CommandTimeout);
        Assert.False(builder.Pooling);
    }

    private sealed class FakePostgresDependencyProbeClient(Func<PostgresDependencyDefinition, string> onProbe) : IPostgresDependencyProbeClient
    {
        public List<PostgresDependencyDefinition> CapturedDependencies { get; } = [];

        public ValueTask<string> ProbeAsync(PostgresDependencyDefinition dependency, CancellationToken cancellationToken)
        {
            CapturedDependencies.Add(dependency);
            return ValueTask.FromResult(onProbe(dependency));
        }
    }
}
