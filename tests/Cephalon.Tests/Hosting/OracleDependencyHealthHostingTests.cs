using System.Globalization;
using Cephalon.Abstractions.Health;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Runtime;
using Cephalon.Observability.OracleDependencies.Configuration;
using Cephalon.Observability.OracleDependencies.Hosting;
using Cephalon.Observability.OracleDependencies.Services;
using Cephalon.Worker.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Oracle.ManagedDataAccess.Client;

namespace Cephalon.Tests.Hosting;

public sealed class OracleDependencyHealthHostingTests
{
    [Fact]
    public async Task AddCephalonOracleDependencyHealthReportsHealthyQuery()
    {
        var probeClient = new FakeOracleDependencyProbeClient(dependency =>
            $"Oracle endpoint '{dependency.Host}:{dependency.Port}/{dependency.ServiceName}' responded to health query.");

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Oracle:Dependencies:0:Id"] = "orders-oracle";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Oracle:Dependencies:0:DisplayName"] = "Orders Oracle";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Oracle:Dependencies:0:Host"] = "oracle.internal.example";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Oracle:Dependencies:0:Port"] = "1522";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Oracle:Dependencies:0:ServiceName"] = "ORDERSPDB";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Oracle:Dependencies:0:Username"] = "cephalon";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Oracle:Dependencies:0:HealthQuery"] = "SELECT 1 FROM DUAL";
        builder.AddCephalon();
        builder.Services.AddCephalonOracleDependencyHealth(builder.Configuration);
        builder.Services.AddSingleton<IOracleDependencyProbeClient>(probeClient);

        using var host = builder.Build();

        await host.StartAsync();

        var evaluator = host.Services.GetRequiredService<RuntimeHealthEvaluator>();
        var dependency = Assert.Single(evaluator.EvaluateDependencies());

        Assert.Equal("orders-oracle", dependency.Id);
        Assert.Equal("Orders Oracle", dependency.DisplayName);
        Assert.Equal(HealthState.Healthy, dependency.State);
        Assert.Equal("Cephalon.Observability.OracleDependencies", dependency.Source);

        var captured = Assert.Single(probeClient.CapturedDependencies);
        Assert.Equal("ORDERSPDB", captured.ServiceName);
        Assert.Equal("cephalon", captured.Username);
        Assert.Equal("SELECT 1 FROM DUAL", captured.HealthQuery);

        await host.StopAsync();
    }

    [Fact]
    public async Task AddCephalonOracleDependencyHealthTreatsRequiredFailuresAsReadinessFailures()
    {
        var probeClient = new FakeOracleDependencyProbeClient(_ => throw new InvalidOperationException("listener refused the session"));

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Oracle:Dependencies:0:Id"] = "required-oracle";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Oracle:Dependencies:0:ConnectionString"] = "Data Source=oracle.internal.example:1521/INVENTORYPDB;User Id=cephalon;Password=secret;Pooling=true;Connection Timeout=33;";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Oracle:Dependencies:0:Required"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:Oracle:Dependencies:0:TimeoutSeconds"] = "1";
        builder.AddCephalon();
        builder.Services.AddCephalonOracleDependencyHealth(builder.Configuration);
        builder.Services.AddSingleton<IOracleDependencyProbeClient>(probeClient);

        using var host = builder.Build();

        await host.StartAsync();

        var evaluator = host.Services.GetRequiredService<RuntimeHealthEvaluator>();
        var readiness = evaluator.EvaluateReadiness();
        var dependency = Assert.Single(readiness.Dependencies);

        Assert.Equal(RuntimeHealthState.Unhealthy, readiness.State);
        Assert.Equal(HealthState.Unhealthy, dependency.State);
        Assert.Contains("listener refused the session", dependency.Description, StringComparison.OrdinalIgnoreCase);

        var captured = Assert.Single(probeClient.CapturedDependencies);
        Assert.Equal("Data Source=oracle.internal.example:1521/INVENTORYPDB;User Id=cephalon;Password=secret;Pooling=true;Connection Timeout=33;", captured.ConnectionString);

        await host.StopAsync();
    }

    [Fact]
    public void CreateConnectionStringBuilderBuildsDiscreteConnectionSettings()
    {
        var dependency = new OracleDependencyDefinition
        {
            Host = "oracle.internal.example",
            Port = 1522,
            ServiceName = "ORDERSPDB",
            Username = "cephalon",
            Password = "secret",
            TimeoutSeconds = 7
        };

        var builder = OracleDependencyProbeClient.CreateConnectionStringBuilder(dependency, timeoutSeconds: 7);

        Assert.Equal("oracle.internal.example:1522/ORDERSPDB", builder.DataSource);
        Assert.Equal("cephalon", builder.UserID);
        Assert.Equal("secret", builder.Password);
        Assert.Equal(7, Convert.ToInt32(builder["Connection Timeout"], CultureInfo.InvariantCulture));
        Assert.False(Convert.ToBoolean(builder["Pooling"], CultureInfo.InvariantCulture));
    }

    [Fact]
    public void CreateConnectionStringBuilderUsesConnectionStringAsBase()
    {
        var dependency = new OracleDependencyDefinition
        {
            ConnectionString = "Data Source=override.internal.example:1521/FINANCEPDB;User Id=runtime;Password=secret;Pooling=true;Connection Timeout=33;",
            Username = "cephalon",
            Password = "override"
        };

        var builder = OracleDependencyProbeClient.CreateConnectionStringBuilder(dependency, timeoutSeconds: 4);

        Assert.Equal("override.internal.example:1521/FINANCEPDB", builder.DataSource);
        Assert.Equal("cephalon", builder.UserID);
        Assert.Equal("override", builder.Password);
        Assert.Equal(4, Convert.ToInt32(builder["Connection Timeout"], CultureInfo.InvariantCulture));
        Assert.False(Convert.ToBoolean(builder["Pooling"], CultureInfo.InvariantCulture));
    }

    private sealed class FakeOracleDependencyProbeClient(Func<OracleDependencyDefinition, string> onProbe) : IOracleDependencyProbeClient
    {
        public List<OracleDependencyDefinition> CapturedDependencies { get; } = [];

        public ValueTask<string> ProbeAsync(OracleDependencyDefinition dependency, CancellationToken cancellationToken)
        {
            CapturedDependencies.Add(dependency);
            return ValueTask.FromResult(onProbe(dependency));
        }
    }
}
