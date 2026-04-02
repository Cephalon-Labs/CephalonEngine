using Cephalon.Abstractions.Health;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Runtime;
using Cephalon.Observability.RabbitMqDependencies.Configuration;
using Cephalon.Observability.RabbitMqDependencies.Hosting;
using Cephalon.Observability.RabbitMqDependencies.Services;
using Cephalon.Worker.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;

namespace Cephalon.Tests.Hosting;

public sealed class RabbitMqDependencyHealthHostingTests
{
    [Fact]
    public async Task AddCephalonRabbitMqDependencyHealthReportsHealthyBrokerConnection()
    {
        var probeClient = new FakeRabbitMqDependencyProbeClient(dependency =>
            $"RabbitMQ endpoint '{dependency.Host}:{dependency.Port}{dependency.VirtualHost}' accepted an AMQP connection.");

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:RabbitMq:Dependencies:0:Id"] = "events-broker";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:RabbitMq:Dependencies:0:DisplayName"] = "Events Broker";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:RabbitMq:Dependencies:0:Host"] = "rabbitmq.internal.example";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:RabbitMq:Dependencies:0:Port"] = "5671";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:RabbitMq:Dependencies:0:VirtualHost"] = "/operations";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:RabbitMq:Dependencies:0:Username"] = "cephalon";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:RabbitMq:Dependencies:0:UseTls"] = "true";
        builder.AddCephalon();
        builder.Services.AddCephalonRabbitMqDependencyHealth(builder.Configuration);
        builder.Services.AddSingleton<IRabbitMqDependencyProbeClient>(probeClient);

        using var host = builder.Build();

        await host.StartAsync();

        var evaluator = host.Services.GetRequiredService<RuntimeHealthEvaluator>();
        var dependency = Assert.Single(evaluator.EvaluateDependencies());

        Assert.Equal("events-broker", dependency.Id);
        Assert.Equal("Events Broker", dependency.DisplayName);
        Assert.Equal(HealthState.Healthy, dependency.State);
        Assert.Equal("Cephalon.Observability.RabbitMqDependencies", dependency.Source);

        var captured = Assert.Single(probeClient.CapturedDependencies);
        Assert.Equal("/operations", captured.VirtualHost);
        Assert.Equal("cephalon", captured.Username);
        Assert.True(captured.UseTls);

        await host.StopAsync();
    }

    [Fact]
    public async Task AddCephalonRabbitMqDependencyHealthTreatsRequiredFailuresAsReadinessFailures()
    {
        var probeClient = new FakeRabbitMqDependencyProbeClient(_ => throw new InvalidOperationException("broker authentication failed"));

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:RabbitMq:Dependencies:0:Id"] = "required-broker";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:RabbitMq:Dependencies:0:ConnectionString"] = "amqps://runtime:secret@broker.internal.example:5671/operations";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:RabbitMq:Dependencies:0:Required"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:RabbitMq:Dependencies:0:TimeoutSeconds"] = "1";
        builder.AddCephalon();
        builder.Services.AddCephalonRabbitMqDependencyHealth(builder.Configuration);
        builder.Services.AddSingleton<IRabbitMqDependencyProbeClient>(probeClient);

        using var host = builder.Build();

        await host.StartAsync();

        var evaluator = host.Services.GetRequiredService<RuntimeHealthEvaluator>();
        var readiness = evaluator.EvaluateReadiness();
        var dependency = Assert.Single(readiness.Dependencies);

        Assert.Equal(RuntimeHealthState.Unhealthy, readiness.State);
        Assert.Equal(HealthState.Unhealthy, dependency.State);
        Assert.Contains("authentication failed", dependency.Description, StringComparison.OrdinalIgnoreCase);

        var captured = Assert.Single(probeClient.CapturedDependencies);
        Assert.Equal("amqps://runtime:secret@broker.internal.example:5671/operations", captured.ConnectionString);

        await host.StopAsync();
    }

    [Fact]
    public void CreateConnectionFactoryBuildsDiscreteConnectionSettings()
    {
        var dependency = new RabbitMqDependencyDefinition
        {
            Host = "rabbitmq.internal.example",
            Port = 5671,
            VirtualHost = "/operations",
            Username = "cephalon",
            Password = "secret",
            UseTls = true,
            TimeoutSeconds = 7
        };

        var factory = RabbitMqDependencyProbeClient.CreateConnectionFactory(dependency, timeoutSeconds: 7);

        Assert.Equal("rabbitmq.internal.example", factory.HostName);
        Assert.Equal(5671, factory.Port);
        Assert.Equal("/operations", factory.VirtualHost);
        Assert.Equal("cephalon", factory.UserName);
        Assert.Equal("secret", factory.Password);
        Assert.True(factory.Ssl.Enabled);
        Assert.Equal("rabbitmq.internal.example", factory.Ssl.ServerName);
        Assert.Equal(TimeSpan.FromSeconds(7), factory.RequestedConnectionTimeout);
        Assert.Equal(TimeSpan.FromSeconds(7), factory.HandshakeContinuationTimeout);
        Assert.Equal(TimeSpan.FromSeconds(7), factory.SocketReadTimeout);
        Assert.Equal(TimeSpan.FromSeconds(7), factory.SocketWriteTimeout);
        Assert.Equal(TimeSpan.FromSeconds(7), factory.RequestedHeartbeat);
        Assert.Equal("Cephalon.DependencyHealth.RabbitMq", factory.ClientProvidedName);
        Assert.False(factory.AutomaticRecoveryEnabled);
        Assert.False(factory.TopologyRecoveryEnabled);
    }

    [Fact]
    public void CreateConnectionFactoryUsesConnectionStringAsBase()
    {
        var dependency = new RabbitMqDependencyDefinition
        {
            ConnectionString = "amqp://runtime:secret@broker.internal.example:5672/tenant-vhost",
            UseTls = true
        };

        var factory = RabbitMqDependencyProbeClient.CreateConnectionFactory(dependency, timeoutSeconds: 4);

        Assert.Equal("broker.internal.example", factory.HostName);
        Assert.Equal(5672, factory.Port);
        Assert.Equal("tenant-vhost", factory.VirtualHost);
        Assert.Equal("runtime", factory.UserName);
        Assert.Equal("secret", factory.Password);
        Assert.True(factory.Ssl.Enabled);
        Assert.Equal(TimeSpan.FromSeconds(4), factory.RequestedConnectionTimeout);
        Assert.Equal(TimeSpan.FromSeconds(4), factory.HandshakeContinuationTimeout);
        Assert.Equal(TimeSpan.FromSeconds(4), factory.SocketReadTimeout);
        Assert.Equal(TimeSpan.FromSeconds(4), factory.SocketWriteTimeout);
        Assert.False(factory.AutomaticRecoveryEnabled);
        Assert.False(factory.TopologyRecoveryEnabled);
    }

    private sealed class FakeRabbitMqDependencyProbeClient(Func<RabbitMqDependencyDefinition, string> onProbe) : IRabbitMqDependencyProbeClient
    {
        public List<RabbitMqDependencyDefinition> CapturedDependencies { get; } = [];

        public ValueTask<string> ProbeAsync(RabbitMqDependencyDefinition dependency, CancellationToken cancellationToken)
        {
            CapturedDependencies.Add(dependency);
            return ValueTask.FromResult(onProbe(dependency));
        }
    }
}
