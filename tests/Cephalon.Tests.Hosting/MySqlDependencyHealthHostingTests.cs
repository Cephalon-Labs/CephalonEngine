using Cephalon.Abstractions.Health;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Runtime;
using Cephalon.Observability.MySqlDependencies.Configuration;
using Cephalon.Observability.MySqlDependencies.Hosting;
using Cephalon.Observability.MySqlDependencies.Services;
using Cephalon.Worker.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MySqlConnector;

namespace Cephalon.Tests.Hosting;

public sealed class MySqlDependencyHealthHostingTests
{
    [Fact]
    public async Task AddCephalonMySqlDependencyHealthReportsHealthyQuery()
    {
        var probeClient = new FakeMySqlDependencyProbeClient(dependency =>
            $"MySQL endpoint '{dependency.Host}:{dependency.Port}/{dependency.Database}' responded to health query.");

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:MySql:Dependencies:0:Id"] = "orders-mysql";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:MySql:Dependencies:0:DisplayName"] = "Orders MySQL";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:MySql:Dependencies:0:Host"] = "mysql.internal.example";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:MySql:Dependencies:0:Port"] = "3307";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:MySql:Dependencies:0:Database"] = "orders";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:MySql:Dependencies:0:Username"] = "cephalon";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:MySql:Dependencies:0:SslMode"] = "Required";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:MySql:Dependencies:0:AllowPublicKeyRetrieval"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:MySql:Dependencies:0:HealthQuery"] = "SELECT 1;";
        builder.AddCephalon();
        builder.Services.AddCephalonMySqlDependencyHealth(builder.Configuration);
        builder.Services.AddSingleton<IMySqlDependencyProbeClient>(probeClient);

        using var host = builder.Build();

        await host.StartAsync();

        var evaluator = host.Services.GetRequiredService<RuntimeHealthEvaluator>();
        var dependency = Assert.Single(evaluator.EvaluateDependencies());

        Assert.Equal("orders-mysql", dependency.Id);
        Assert.Equal("Orders MySQL", dependency.DisplayName);
        Assert.Equal(HealthState.Healthy, dependency.State);
        Assert.Equal("Cephalon.Observability.MySqlDependencies", dependency.Source);

        var captured = Assert.Single(probeClient.CapturedDependencies);
        Assert.Equal("orders", captured.Database);
        Assert.Equal("cephalon", captured.Username);
        Assert.Equal("Required", captured.SslMode);
        Assert.Equal(false, captured.AllowPublicKeyRetrieval);
        Assert.Equal("SELECT 1;", captured.HealthQuery);

        await host.StopAsync();
    }

    [Fact]
    public async Task AddCephalonMySqlDependencyHealthTreatsRequiredFailuresAsReadinessFailures()
    {
        var probeClient = new FakeMySqlDependencyProbeClient(_ => throw new InvalidOperationException("authentication method failed"));

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:MySql:Dependencies:0:Id"] = "required-mysql";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:MySql:Dependencies:0:ConnectionString"] = "Server=mysql.internal.example;Port=3306;Database=operations;User ID=cephalon;";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:MySql:Dependencies:0:Required"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:MySql:Dependencies:0:TimeoutSeconds"] = "1";
        builder.AddCephalon();
        builder.Services.AddCephalonMySqlDependencyHealth(builder.Configuration);
        builder.Services.AddSingleton<IMySqlDependencyProbeClient>(probeClient);

        using var host = builder.Build();

        await host.StartAsync();

        var evaluator = host.Services.GetRequiredService<RuntimeHealthEvaluator>();
        var readiness = evaluator.EvaluateReadiness();
        var dependency = Assert.Single(readiness.Dependencies);

        Assert.Equal(RuntimeHealthState.Unhealthy, readiness.State);
        Assert.Equal(HealthState.Unhealthy, dependency.State);
        Assert.Contains("authentication method failed", dependency.Description, StringComparison.OrdinalIgnoreCase);

        var captured = Assert.Single(probeClient.CapturedDependencies);
        Assert.Equal("Server=mysql.internal.example;Port=3306;Database=operations;User ID=cephalon;", captured.ConnectionString);

        await host.StopAsync();
    }

    [Fact]
    public void CreateConnectionStringBuilderBuildsDiscreteConnectionSettings()
    {
        var dependency = new MySqlDependencyDefinition
        {
            Host = "mysql.internal.example",
            Port = 3307,
            Database = "operations",
            Username = "cephalon",
            Password = "secret",
            SslMode = "VerifyFull",
            AllowPublicKeyRetrieval = true,
            TimeoutSeconds = 7
        };

        var builder = MySqlDependencyProbeClient.CreateConnectionStringBuilder(dependency, timeoutSeconds: 7);

        Assert.Equal("mysql.internal.example", builder.Server);
        Assert.Equal((uint)3307, builder.Port);
        Assert.Equal("operations", builder.Database);
        Assert.Equal("cephalon", builder.UserID);
        Assert.Equal("secret", builder.Password);
        Assert.Equal(MySqlSslMode.VerifyFull, builder.SslMode);
        Assert.True(builder.AllowPublicKeyRetrieval);
        Assert.Equal((uint)7, builder.ConnectionTimeout);
        Assert.Equal((uint)7, builder.DefaultCommandTimeout);
        Assert.False(builder.Pooling);
        Assert.Equal("Cephalon.DependencyHealth.MySql", builder.ApplicationName);
    }

    [Fact]
    public void CreateConnectionStringBuilderUsesConnectionStringAsBase()
    {
        var dependency = new MySqlDependencyDefinition
        {
            ConnectionString = "Server=override.internal.example;Port=3308;Database=inventory;User ID=runtime;Password=secret;SslMode=Preferred;AllowPublicKeyRetrieval=false;Pooling=true;ConnectionTimeout=33;DefaultCommandTimeout=44;",
            SslMode = "Required",
            AllowPublicKeyRetrieval = true
        };

        var builder = MySqlDependencyProbeClient.CreateConnectionStringBuilder(dependency, timeoutSeconds: 4);

        Assert.Equal("override.internal.example", builder.Server);
        Assert.Equal((uint)3308, builder.Port);
        Assert.Equal("inventory", builder.Database);
        Assert.Equal("runtime", builder.UserID);
        Assert.Equal(MySqlSslMode.Required, builder.SslMode);
        Assert.True(builder.AllowPublicKeyRetrieval);
        Assert.Equal((uint)4, builder.ConnectionTimeout);
        Assert.Equal((uint)4, builder.DefaultCommandTimeout);
        Assert.False(builder.Pooling);
    }

    private sealed class FakeMySqlDependencyProbeClient(Func<MySqlDependencyDefinition, string> onProbe) : IMySqlDependencyProbeClient
    {
        public List<MySqlDependencyDefinition> CapturedDependencies { get; } = [];

        public ValueTask<string> ProbeAsync(MySqlDependencyDefinition dependency, CancellationToken cancellationToken)
        {
            CapturedDependencies.Add(dependency);
            return ValueTask.FromResult(onProbe(dependency));
        }
    }
}
