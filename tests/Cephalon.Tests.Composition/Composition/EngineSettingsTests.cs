using Cephalon.Engine.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cephalon.Tests.Composition;

public sealed class EngineSettingsTests
{
    [Fact]
    public void FromConfigurationReadsStructuredEngineSettingsIncludingDatabaseTopology()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Engine:Data:Provider"] = "EntityFramework",
                ["Engine:Data:ReadWriteSplit"] = "true",
                ["Engine:Data:Outbox:Enabled"] = "true",
                ["Engine:Data:Ids:Generator"] = "Sfid",
                ["Engine:Identity:Enabled"] = "true",
                ["Engine:Identity:AuthorizationModes:0"] = "RBAC",
                ["Engine:Identity:AuthorizationModes:1"] = "Policy",
                ["Engine:Tenancy:Enabled"] = "true",
                ["Engine:Tenancy:Mode"] = "SharedDatabase",
                ["Engine:Audit:Enabled"] = "true",
                ["Engine:Audit:History:Enabled"] = "true",
                ["Engine:Audit:History:Provider"] = "EntityFramework",
                ["Engine:Audit:History:DatabaseRole"] = "History",
                ["Engine:Audit:History:Export:Enabled"] = "true",
                ["Engine:Audit:History:Export:MaxEntries"] = "750",
                ["Engine:Audit:History:Retention:Enabled"] = "true",
                ["Engine:Audit:History:Retention:MaxAgeDays"] = "90",
                ["Engine:Audit:History:Retention:DeleteBatchSize"] = "250",
                ["Engine:Audit:History:Retention:ApplyOnStartup"] = "true",
                ["Engine:Messaging:Provider"] = "Wolverine",
                ["Engine:Resilience:Retry:Enabled"] = "true",
                ["Engine:Resilience:Retry:MaxAttempts"] = "3",
                ["Engine:Resilience:Retry:BaseDelayMilliseconds"] = "200",
                ["Engine:Resilience:Retry:MaxDelayMilliseconds"] = "5000",
                ["Engine:Resilience:Retry:Backoff"] = "Exponential",
                ["Engine:Resilience:Retry:UseJitter"] = "true",
                ["Engine:Resilience:CircuitBreaker:Enabled"] = "true",
                ["Engine:Resilience:CircuitBreaker:FailureRatio"] = "0.5",
                ["Engine:Resilience:CircuitBreaker:MinimumThroughput"] = "20",
                ["Engine:Resilience:CircuitBreaker:SamplingDurationSeconds"] = "30",
                ["Engine:Resilience:CircuitBreaker:BreakDurationSeconds"] = "15",
                ["Engine:Resilience:Timeout:Enabled"] = "true",
                ["Engine:Resilience:Timeout:TotalTimeoutSeconds"] = "10",
                ["Engine:Resilience:Timeout:AttemptTimeoutSeconds"] = "3",
                ["Engine:Resilience:Bulkhead:Enabled"] = "true",
                ["Engine:Resilience:Bulkhead:MaxConcurrentExecutions"] = "64",
                ["Engine:Resilience:Bulkhead:MaxQueuedActions"] = "32",
                ["Engine:Resilience:RateLimiting:Enabled"] = "true",
                ["Engine:Resilience:RateLimiting:Algorithm"] = "SlidingWindow",
                ["Engine:Resilience:RateLimiting:PermitLimit"] = "100",
                ["Engine:Resilience:RateLimiting:QueueLimit"] = "10",
                ["Engine:Resilience:RateLimiting:WindowSeconds"] = "60",
                ["Engine:Resilience:RateLimiting:SegmentsPerWindow"] = "4",
                ["Engine:Databases:Runtime:EnableDetailedErrors"] = "true",
                ["Engine:Databases:Runtime:EnableRetryOnFailure"] = "true",
                ["Engine:Databases:Runtime:MaxRetryCount"] = "5",
                ["Engine:Databases:Runtime:RoleProbeFreshnessSeconds"] = "30",
                ["Engine:Databases:Write:Provider"] = "PostgreSql",
                ["Engine:Databases:Write:ConnectionStringName"] = "WriteDb",
                ["Engine:Databases:Write:Runtime:EnableRetryOnFailure"] = "false",
                ["Engine:Databases:Read:Provider"] = "PostgreSql",
                ["Engine:Databases:Read:ConnectionString"] = "Host=localhost;Database=cephalon_read",
                ["Engine:Databases:Outbox:UseRole"] = "write",
                ["Engine:Databases:Outbox:Schema"] = "outbox01",
                ["Engine:Databases:History:UseRole"] = "write",
                ["Engine:Databases:History:Schema"] = "audit01",
                ["Engine:Databases:Migrations:ApplyOnStartup"] = "true",
                ["Engine:Databases:Migrations:ExitAfterApply"] = "false",
                ["Engine:Databases:Migrations:Targets:0"] = "write",
                ["Engine:Databases:Migrations:Targets:1"] = "outbox",
                ["Engine:Databases:Migrations:Targets:2"] = "history"
            })
            .Build();

        var settings = EngineSettings.FromConfiguration(configuration);

        Assert.True(settings.HasValues);
        Assert.Equal("EntityFramework", settings.Data.Provider);
        Assert.True(settings.Data.ReadWriteSplit);
        Assert.True(settings.Data.OutboxEnabled);
        Assert.Equal("Sfid", settings.Data.IdGenerator);
        Assert.True(settings.Identity.Enabled);
        Assert.Equal(["RBAC", "Policy"], settings.Identity.AuthorizationModes);
        Assert.True(settings.Tenancy.Enabled);
        Assert.Equal("SharedDatabase", settings.Tenancy.Mode);
        Assert.True(settings.Audit.Enabled);
        Assert.True(settings.Audit.History.Enabled);
        Assert.Equal("EntityFramework", settings.Audit.History.Provider);
        Assert.Equal("History", settings.Audit.History.DatabaseRole);
        Assert.True(settings.Audit.History.Export.Enabled);
        Assert.Equal(750, settings.Audit.History.Export.MaxEntries);
        Assert.True(settings.Audit.History.Retention.Enabled);
        Assert.Equal(90, settings.Audit.History.Retention.MaxAgeDays);
        Assert.Equal(250, settings.Audit.History.Retention.DeleteBatchSize);
        Assert.True(settings.Audit.History.Retention.ApplyOnStartup);
        Assert.Null(settings.Audit.History.Retention.RunIntervalMinutes);
        Assert.Equal("Wolverine", settings.Messaging.Provider);
        Assert.True(settings.Resilience.Retry.Enabled);
        Assert.Equal(3, settings.Resilience.Retry.MaxAttempts);
        Assert.Equal("Exponential", settings.Resilience.Retry.Backoff);
        Assert.Equal(200, settings.Resilience.Retry.BaseDelayMilliseconds);
        Assert.Equal(5000, settings.Resilience.Retry.MaxDelayMilliseconds);
        Assert.True(settings.Resilience.Retry.UseJitter);
        Assert.True(settings.Resilience.CircuitBreaker.Enabled);
        Assert.Equal(0.5m, settings.Resilience.CircuitBreaker.FailureRatio);
        Assert.Equal(20, settings.Resilience.CircuitBreaker.MinimumThroughput);
        Assert.Equal(30, settings.Resilience.CircuitBreaker.SamplingDurationSeconds);
        Assert.Equal(15, settings.Resilience.CircuitBreaker.BreakDurationSeconds);
        Assert.True(settings.Resilience.Timeout.Enabled);
        Assert.Equal(10, settings.Resilience.Timeout.TotalTimeoutSeconds);
        Assert.Equal(3, settings.Resilience.Timeout.AttemptTimeoutSeconds);
        Assert.True(settings.Resilience.Bulkhead.Enabled);
        Assert.Equal(64, settings.Resilience.Bulkhead.MaxConcurrentExecutions);
        Assert.Equal(32, settings.Resilience.Bulkhead.MaxQueuedActions);
        Assert.True(settings.Resilience.RateLimiting.Enabled);
        Assert.Equal("SlidingWindow", settings.Resilience.RateLimiting.Algorithm);
        Assert.Equal(100, settings.Resilience.RateLimiting.PermitLimit);
        Assert.Equal(10, settings.Resilience.RateLimiting.QueueLimit);
        Assert.Equal(60, settings.Resilience.RateLimiting.WindowSeconds);
        Assert.Equal(4, settings.Resilience.RateLimiting.SegmentsPerWindow);
        Assert.True(settings.Databases.Runtime.EnableDetailedErrors);
        Assert.True(settings.Databases.Runtime.EnableRetryOnFailure);
        Assert.Equal(5, settings.Databases.Runtime.MaxRetryCount);
        Assert.Equal(30, settings.Databases.Runtime.RoleProbeFreshnessSeconds);
        Assert.Equal("PostgreSql", settings.Databases.Write.Provider);
        Assert.Equal("WriteDb", settings.Databases.Write.ConnectionStringName);
        Assert.False(settings.Databases.Write.Runtime.EnableRetryOnFailure);
        Assert.Equal("Host=localhost;Database=cephalon_read", settings.Databases.Read.ConnectionString);
        Assert.Equal("write", settings.Databases.Outbox.UseRole);
        Assert.Equal("outbox01", settings.Databases.Outbox.Schema);
        Assert.Equal("write", settings.Databases.History.UseRole);
        Assert.Equal("audit01", settings.Databases.History.Schema);
        Assert.True(settings.Databases.Migrations.ApplyOnStartup);
        Assert.False(settings.Databases.Migrations.ExitAfterApply);
        Assert.Equal(["history", "outbox", "write"], settings.Databases.Migrations.Targets);
    }

    [Fact]
    public void FromConfigurationThrowsWhenDatabaseTargetUsesNamedAndInlineConnectionStrings()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Engine:Databases:Write:Provider"] = "PostgreSql",
                ["Engine:Databases:Write:ConnectionStringName"] = "WriteDb",
                ["Engine:Databases:Write:ConnectionString"] = "Host=localhost;Database=cephalon_write"
            })
            .Build();

        var exception = Assert.Throws<InvalidOperationException>(() => EngineSettings.FromConfiguration(configuration));

        Assert.Contains("ConnectionStringName", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ConnectionString", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FromConfigurationThrowsWhenDatabaseTargetUsesUseRoleAndDirectProviderSettings()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Engine:Databases:History:Provider"] = "PostgreSql",
                ["Engine:Databases:History:UseRole"] = "write"
            })
            .Build();

        var exception = Assert.Throws<InvalidOperationException>(() => EngineSettings.FromConfiguration(configuration));

        Assert.Contains("UseRole", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("provider", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FromConfigurationAllowsZeroRoleProbeFreshnessSeconds()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Engine:Databases:Runtime:RoleProbeFreshnessSeconds"] = "0"
            })
            .Build();

        var settings = EngineSettings.FromConfiguration(configuration);

        Assert.Equal(0, settings.Databases.Runtime.RoleProbeFreshnessSeconds);
    }
}
