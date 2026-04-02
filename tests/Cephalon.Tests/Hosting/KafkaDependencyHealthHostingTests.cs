using Cephalon.Abstractions.Health;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Runtime;
using Cephalon.Observability.KafkaDependencies.Configuration;
using Cephalon.Observability.KafkaDependencies.Hosting;
using Cephalon.Observability.KafkaDependencies.Services;
using Cephalon.Worker.Hosting;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cephalon.Tests.Hosting;

public sealed class KafkaDependencyHealthHostingTests
{
    [Fact]
    public async Task AddCephalonKafkaDependencyHealthReportsHealthyClusterMetadata()
    {
        var probeClient = new FakeKafkaDependencyProbeClient(dependency =>
            $"Kafka cluster '{dependency.BootstrapServers}' returned metadata for topic '{dependency.Topic}' using 2 broker(s).");

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Kafka:Dependencies:0:Id"] = "events-kafka";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Kafka:Dependencies:0:DisplayName"] = "Events Kafka";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Kafka:Dependencies:0:BootstrapServers"] = "kafka-1.internal.example:9093,kafka-2.internal.example:9093";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Kafka:Dependencies:0:ClientId"] = "cephalon-runtime";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Kafka:Dependencies:0:Topic"] = "cephalon.events";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Kafka:Dependencies:0:SecurityProtocol"] = "SaslSsl";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Kafka:Dependencies:0:SaslMechanism"] = "ScramSha512";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Kafka:Dependencies:0:Username"] = "cephalon";
        builder.AddCephalon();
        builder.Services.AddCephalonKafkaDependencyHealth(builder.Configuration);
        builder.Services.AddSingleton<IKafkaDependencyProbeClient>(probeClient);

        using var host = builder.Build();

        await host.StartAsync();

        var evaluator = host.Services.GetRequiredService<RuntimeHealthEvaluator>();
        var dependency = Assert.Single(evaluator.EvaluateDependencies());

        Assert.Equal("events-kafka", dependency.Id);
        Assert.Equal("Events Kafka", dependency.DisplayName);
        Assert.Equal(HealthState.Healthy, dependency.State);
        Assert.Equal("Cephalon.Observability.KafkaDependencies", dependency.Source);

        var captured = Assert.Single(probeClient.CapturedDependencies);
        Assert.Equal("cephalon-runtime", captured.ClientId);
        Assert.Equal("cephalon.events", captured.Topic);
        Assert.Equal("SaslSsl", captured.SecurityProtocol);
        Assert.Equal("ScramSha512", captured.SaslMechanism);
        Assert.Equal("cephalon", captured.Username);

        await host.StopAsync();
    }

    [Fact]
    public async Task AddCephalonKafkaDependencyHealthTreatsRequiredFailuresAsReadinessFailures()
    {
        var probeClient = new FakeKafkaDependencyProbeClient(_ => throw new InvalidOperationException("metadata request failed"));

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Kafka:Dependencies:0:Id"] = "required-kafka";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Kafka:Dependencies:0:BootstrapServers"] = "kafka.internal.example:9092";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Kafka:Dependencies:0:Required"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Kafka:Dependencies:0:TimeoutSeconds"] = "1";
        builder.AddCephalon();
        builder.Services.AddCephalonKafkaDependencyHealth(builder.Configuration);
        builder.Services.AddSingleton<IKafkaDependencyProbeClient>(probeClient);

        using var host = builder.Build();

        await host.StartAsync();

        var evaluator = host.Services.GetRequiredService<RuntimeHealthEvaluator>();
        var readiness = evaluator.EvaluateReadiness();
        var dependency = Assert.Single(readiness.Dependencies);

        Assert.Equal(RuntimeHealthState.Unhealthy, readiness.State);
        Assert.Equal(HealthState.Unhealthy, dependency.State);
        Assert.Contains("metadata request failed", dependency.Description, StringComparison.OrdinalIgnoreCase);

        var captured = Assert.Single(probeClient.CapturedDependencies);
        Assert.Equal("kafka.internal.example:9092", captured.BootstrapServers);

        await host.StopAsync();
    }

    [Fact]
    public void CreateAdminClientConfigBuildsSecureSaslSettings()
    {
        var dependency = new KafkaDependencyDefinition
        {
            BootstrapServers = "kafka-1.internal.example:9093,kafka-2.internal.example:9093",
            ClientId = "cephalon-runtime",
            Topic = "cephalon.events",
            SecurityProtocol = "SaslSsl",
            SaslMechanism = "ScramSha512",
            Username = "cephalon",
            Password = "secret",
            TimeoutSeconds = 7
        };

        var config = KafkaDependencyProbeClient.CreateAdminClientConfig(dependency, timeoutSeconds: 7);

        Assert.Equal("kafka-1.internal.example:9093,kafka-2.internal.example:9093", config.BootstrapServers);
        Assert.Equal("cephalon-runtime", config.ClientId);
        Assert.Equal(SecurityProtocol.SaslSsl, config.SecurityProtocol);
        Assert.Equal(SaslMechanism.ScramSha512, config.SaslMechanism);
        Assert.Equal("cephalon", config.SaslUsername);
        Assert.Equal("secret", config.SaslPassword);
        Assert.Equal(7000, config.SocketTimeoutMs);
    }

    [Fact]
    public void CreateAdminClientConfigFallsBackToPlaintextDefaults()
    {
        var dependency = new KafkaDependencyDefinition
        {
            BootstrapServers = "kafka.internal.example:9092"
        };

        var config = KafkaDependencyProbeClient.CreateAdminClientConfig(dependency, timeoutSeconds: 4);

        Assert.Equal("kafka.internal.example:9092", config.BootstrapServers);
        Assert.Equal("Cephalon.DependencyHealth.Kafka", config.ClientId);
        Assert.Null(config.SecurityProtocol);
        Assert.Null(config.SaslMechanism);
        Assert.Equal(4000, config.SocketTimeoutMs);
    }

    private sealed class FakeKafkaDependencyProbeClient(Func<KafkaDependencyDefinition, string> onProbe) : IKafkaDependencyProbeClient
    {
        public List<KafkaDependencyDefinition> CapturedDependencies { get; } = [];

        public ValueTask<string> ProbeAsync(KafkaDependencyDefinition dependency, CancellationToken cancellationToken)
        {
            CapturedDependencies.Add(dependency);
            return ValueTask.FromResult(onProbe(dependency));
        }
    }
}
