using Cephalon.Abstractions.Health;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Runtime;
using Cephalon.Observability.MongoDbDependencies.Configuration;
using Cephalon.Observability.MongoDbDependencies.Hosting;
using Cephalon.Observability.MongoDbDependencies.Services;
using Cephalon.Worker.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;

namespace Cephalon.Tests.Hosting;

public sealed class MongoDbDependencyHealthHostingTests
{
    [Fact]
    public async Task AddCephalonMongoDbDependencyHealthReportsHealthyCommand()
    {
        var probeClient = new FakeMongoDbDependencyProbeClient(dependency =>
            $"MongoDB endpoint '{dependency.Host}:{dependency.Port}/{dependency.Database}' responded to health command '{dependency.HealthCommand}'.");

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:MongoDb:Dependencies:0:Id"] = "catalog-mongodb";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:MongoDb:Dependencies:0:DisplayName"] = "Catalog MongoDB";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:MongoDb:Dependencies:0:Host"] = "mongo.internal.example";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:MongoDb:Dependencies:0:Port"] = "27018";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:MongoDb:Dependencies:0:Database"] = "catalog";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:MongoDb:Dependencies:0:Username"] = "cephalon";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:MongoDb:Dependencies:0:AuthSource"] = "admin";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:MongoDb:Dependencies:0:UseTls"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:MongoDb:Dependencies:0:AllowInsecureTls"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:MongoDb:Dependencies:0:DirectConnection"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:MongoDb:Dependencies:0:HealthCommand"] = "ping";
        builder.AddCephalon();
        builder.Services.AddCephalonMongoDbDependencyHealth(builder.Configuration);
        builder.Services.AddSingleton<IMongoDbDependencyProbeClient>(probeClient);

        using var host = builder.Build();

        await host.StartAsync();

        var evaluator = host.Services.GetRequiredService<RuntimeHealthEvaluator>();
        var dependency = Assert.Single(evaluator.EvaluateDependencies());

        Assert.Equal("catalog-mongodb", dependency.Id);
        Assert.Equal("Catalog MongoDB", dependency.DisplayName);
        Assert.Equal(HealthState.Healthy, dependency.State);
        Assert.Equal("Cephalon.Observability.MongoDbDependencies", dependency.Source);

        var captured = Assert.Single(probeClient.CapturedDependencies);
        Assert.Equal("catalog", captured.Database);
        Assert.Equal("cephalon", captured.Username);
        Assert.Equal("admin", captured.AuthSource);
        Assert.Equal(true, captured.UseTls);
        Assert.Equal(false, captured.AllowInsecureTls);
        Assert.Equal(true, captured.DirectConnection);
        Assert.Equal("ping", captured.HealthCommand);

        await host.StopAsync();
    }

    [Fact]
    public async Task AddCephalonMongoDbDependencyHealthTreatsRequiredFailuresAsReadinessFailures()
    {
        var probeClient = new FakeMongoDbDependencyProbeClient(_ => throw new InvalidOperationException("server selection timed out"));

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:MongoDb:Dependencies:0:Id"] = "required-mongodb";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:MongoDb:Dependencies:0:ConnectionString"] = "mongodb://cephalon@mongo.internal.example:27017/operations";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:MongoDb:Dependencies:0:Required"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:MongoDb:Dependencies:0:TimeoutSeconds"] = "1";
        builder.AddCephalon();
        builder.Services.AddCephalonMongoDbDependencyHealth(builder.Configuration);
        builder.Services.AddSingleton<IMongoDbDependencyProbeClient>(probeClient);

        using var host = builder.Build();

        await host.StartAsync();

        var evaluator = host.Services.GetRequiredService<RuntimeHealthEvaluator>();
        var readiness = evaluator.EvaluateReadiness();
        var dependency = Assert.Single(readiness.Dependencies);

        Assert.Equal(RuntimeHealthState.Unhealthy, readiness.State);
        Assert.Equal(HealthState.Unhealthy, dependency.State);
        Assert.Contains("server selection timed out", dependency.Description, StringComparison.OrdinalIgnoreCase);

        var captured = Assert.Single(probeClient.CapturedDependencies);
        Assert.Equal("mongodb://cephalon@mongo.internal.example:27017/operations", captured.ConnectionString);

        await host.StopAsync();
    }

    [Fact]
    public void CreateClientSettingsBuildsDiscreteConnectionSettings()
    {
        var dependency = new MongoDbDependencyDefinition
        {
            Host = "mongo.internal.example",
            Port = 27018,
            Database = "operations",
            Username = "cephalon",
            Password = "secret",
            AuthSource = "admin",
            UseTls = true,
            AllowInsecureTls = true,
            DirectConnection = false,
            TimeoutSeconds = 7
        };

        var settings = MongoDbDependencyProbeClient.CreateClientSettings(dependency, timeoutSeconds: 7);

        Assert.Equal("mongo.internal.example", settings.Server.Host);
        Assert.Equal(27018, settings.Server.Port);
        Assert.NotNull(settings.Credential);
        Assert.Equal("cephalon", settings.Credential.Username);
        Assert.Equal("admin", settings.Credential.Source);
        Assert.True(settings.UseTls);
        Assert.True(settings.AllowInsecureTls);
        Assert.False(settings.DirectConnection);
        Assert.Equal(TimeSpan.FromSeconds(7), settings.ConnectTimeout);
        Assert.Equal(TimeSpan.FromSeconds(7), settings.ServerSelectionTimeout);
        Assert.Equal("Cephalon.DependencyHealth.MongoDb", settings.ApplicationName);
    }

    [Fact]
    public void CreateClientSettingsUsesConnectionStringAsBase()
    {
        var dependency = new MongoDbDependencyDefinition
        {
            ConnectionString = "mongodb://runtime:secret@override.internal.example:27019/inventory?directConnection=false&tls=false",
            UseTls = true,
            AllowInsecureTls = true,
            DirectConnection = true
        };

        var settings = MongoDbDependencyProbeClient.CreateClientSettings(dependency, timeoutSeconds: 4);

        Assert.Equal("override.internal.example", settings.Server.Host);
        Assert.Equal(27019, settings.Server.Port);
        Assert.NotNull(settings.Credential);
        Assert.Equal("runtime", settings.Credential.Username);
        Assert.True(settings.UseTls);
        Assert.True(settings.AllowInsecureTls);
        Assert.True(settings.DirectConnection);
        Assert.Equal(TimeSpan.FromSeconds(4), settings.ConnectTimeout);
        Assert.Equal(TimeSpan.FromSeconds(4), settings.ServerSelectionTimeout);
    }

    private sealed class FakeMongoDbDependencyProbeClient(Func<MongoDbDependencyDefinition, string> onProbe) : IMongoDbDependencyProbeClient
    {
        public List<MongoDbDependencyDefinition> CapturedDependencies { get; } = [];

        public ValueTask<string> ProbeAsync(MongoDbDependencyDefinition dependency, CancellationToken cancellationToken)
        {
            CapturedDependencies.Add(dependency);
            return ValueTask.FromResult(onProbe(dependency));
        }
    }
}
