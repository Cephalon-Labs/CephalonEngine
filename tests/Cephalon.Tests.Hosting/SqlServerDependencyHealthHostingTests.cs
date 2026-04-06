using Cephalon.Abstractions.Health;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Runtime;
using Cephalon.Observability.SqlServerDependencies.Configuration;
using Cephalon.Observability.SqlServerDependencies.Hosting;
using Cephalon.Observability.SqlServerDependencies.Services;
using Cephalon.Worker.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cephalon.Tests.Hosting;

public sealed class SqlServerDependencyHealthHostingTests
{
    [Fact]
    public async Task AddCephalonSqlServerDependencyHealthReportsHealthyQuery()
    {
        var probeClient = new FakeSqlServerDependencyProbeClient(dependency =>
            $"SQL Server endpoint '{dependency.Host}:{dependency.Port}/{dependency.Database}' responded to health query.");

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:SqlServer:Dependencies:0:Id"] = "orders-sql";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:SqlServer:Dependencies:0:DisplayName"] = "Orders SQL";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:SqlServer:Dependencies:0:Host"] = "sql.internal.example";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:SqlServer:Dependencies:0:Port"] = "1433";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:SqlServer:Dependencies:0:Database"] = "orders";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:SqlServer:Dependencies:0:Username"] = "cephalon";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:SqlServer:Dependencies:0:Encrypt"] = "Mandatory";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:SqlServer:Dependencies:0:TrustServerCertificate"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:SqlServer:Dependencies:0:HealthQuery"] = "SELECT 1;";
        builder.AddCephalon();
        builder.Services.AddCephalonSqlServerDependencyHealth(builder.Configuration);
        builder.Services.AddSingleton<ISqlServerDependencyProbeClient>(probeClient);

        using var host = builder.Build();

        await host.StartAsync();

        var evaluator = host.Services.GetRequiredService<RuntimeHealthEvaluator>();
        var dependency = Assert.Single(evaluator.EvaluateDependencies());

        Assert.Equal("orders-sql", dependency.Id);
        Assert.Equal("Orders SQL", dependency.DisplayName);
        Assert.Equal(HealthState.Healthy, dependency.State);
        Assert.Equal("Cephalon.Observability.SqlServerDependencies", dependency.Source);

        var captured = Assert.Single(probeClient.CapturedDependencies);
        Assert.Equal("orders", captured.Database);
        Assert.Equal("cephalon", captured.Username);
        Assert.Equal("Mandatory", captured.Encrypt);
        Assert.Equal(false, captured.TrustServerCertificate);
        Assert.Equal("SELECT 1;", captured.HealthQuery);

        await host.StopAsync();
    }

    [Fact]
    public async Task AddCephalonSqlServerDependencyHealthTreatsRequiredFailuresAsReadinessFailures()
    {
        var probeClient = new FakeSqlServerDependencyProbeClient(_ => throw new InvalidOperationException("login failed for user"));

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:SqlServer:Dependencies:0:Id"] = "required-sql";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:SqlServer:Dependencies:0:ConnectionString"] = "Server=sql.internal.example,1433;Database=operations;User ID=cephalon;";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:SqlServer:Dependencies:0:Required"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:DependencyHealth:SqlServer:Dependencies:0:TimeoutSeconds"] = "1";
        builder.AddCephalon();
        builder.Services.AddCephalonSqlServerDependencyHealth(builder.Configuration);
        builder.Services.AddSingleton<ISqlServerDependencyProbeClient>(probeClient);

        using var host = builder.Build();

        await host.StartAsync();

        var evaluator = host.Services.GetRequiredService<RuntimeHealthEvaluator>();
        var readiness = evaluator.EvaluateReadiness();
        var dependency = Assert.Single(readiness.Dependencies);

        Assert.Equal(RuntimeHealthState.Unhealthy, readiness.State);
        Assert.Equal(HealthState.Unhealthy, dependency.State);
        Assert.Contains("login failed", dependency.Description, StringComparison.OrdinalIgnoreCase);

        var captured = Assert.Single(probeClient.CapturedDependencies);
        Assert.Equal("Server=sql.internal.example,1433;Database=operations;User ID=cephalon;", captured.ConnectionString);

        await host.StopAsync();
    }

    [Fact]
    public void CreateConnectionStringBuilderBuildsDiscreteConnectionSettings()
    {
        var dependency = new SqlServerDependencyDefinition
        {
            Host = "sql.internal.example",
            Port = 1444,
            Database = "operations",
            Username = "cephalon",
            Password = "secret",
            Encrypt = "Optional",
            TrustServerCertificate = true,
            TimeoutSeconds = 7
        };

        var builder = SqlServerDependencyProbeClient.CreateConnectionStringBuilder(dependency, timeoutSeconds: 7);

        Assert.Equal("sql.internal.example,1444", builder.DataSource);
        Assert.Equal("operations", builder.InitialCatalog);
        Assert.Equal("cephalon", builder.UserID);
        Assert.Equal("secret", builder.Password);
        Assert.Equal("False", builder["Encrypt"]?.ToString());
        Assert.True(builder.TrustServerCertificate);
        Assert.Equal(7, builder.ConnectTimeout);
        Assert.False(builder.Pooling);
        Assert.Equal("Cephalon.DependencyHealth.SqlServer", builder.ApplicationName);
    }

    [Fact]
    public void CreateConnectionStringBuilderUsesConnectionStringAsBase()
    {
        var dependency = new SqlServerDependencyDefinition
        {
            ConnectionString = "Server=override.internal.example,1445;Database=inventory;User ID=runtime;Password=secret;Encrypt=Mandatory;TrustServerCertificate=False;Pooling=true;Connect Timeout=33;",
            TrustServerCertificate = true
        };

        var builder = SqlServerDependencyProbeClient.CreateConnectionStringBuilder(dependency, timeoutSeconds: 4);

        Assert.Equal("override.internal.example,1445", builder.DataSource);
        Assert.Equal("inventory", builder.InitialCatalog);
        Assert.Equal("runtime", builder.UserID);
        Assert.Equal("True", builder["Encrypt"]?.ToString());
        Assert.True(builder.TrustServerCertificate);
        Assert.Equal(4, builder.ConnectTimeout);
        Assert.False(builder.Pooling);
    }

    private sealed class FakeSqlServerDependencyProbeClient(Func<SqlServerDependencyDefinition, string> onProbe) : ISqlServerDependencyProbeClient
    {
        public List<SqlServerDependencyDefinition> CapturedDependencies { get; } = [];

        public ValueTask<string> ProbeAsync(SqlServerDependencyDefinition dependency, CancellationToken cancellationToken)
        {
            CapturedDependencies.Add(dependency);
            return ValueTask.FromResult(onProbe(dependency));
        }
    }
}
