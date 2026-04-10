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
                ["Engine:Messaging:Provider"] = "Wolverine",
                ["Engine:Databases:Runtime:EnableDetailedErrors"] = "true",
                ["Engine:Databases:Runtime:EnableRetryOnFailure"] = "true",
                ["Engine:Databases:Runtime:MaxRetryCount"] = "5",
                ["Engine:Databases:Write:Provider"] = "PostgreSql",
                ["Engine:Databases:Write:ConnectionStringName"] = "WriteDb",
                ["Engine:Databases:Write:Runtime:EnableRetryOnFailure"] = "false",
                ["Engine:Databases:Read:Provider"] = "PostgreSql",
                ["Engine:Databases:Read:ConnectionString"] = "Host=localhost;Database=cephalon_read",
                ["Engine:Databases:Outbox:Provider"] = "PostgreSql",
                ["Engine:Databases:Outbox:ConnectionStringName"] = "WriteDb",
                ["Engine:Databases:Outbox:Schema"] = "outbox01",
                ["Engine:Databases:History:Provider"] = "PostgreSql",
                ["Engine:Databases:History:ConnectionStringName"] = "HistoryDb",
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
        Assert.Equal("Wolverine", settings.Messaging.Provider);
        Assert.True(settings.Databases.Runtime.EnableDetailedErrors);
        Assert.True(settings.Databases.Runtime.EnableRetryOnFailure);
        Assert.Equal(5, settings.Databases.Runtime.MaxRetryCount);
        Assert.Equal("PostgreSql", settings.Databases.Write.Provider);
        Assert.Equal("WriteDb", settings.Databases.Write.ConnectionStringName);
        Assert.False(settings.Databases.Write.Runtime.EnableRetryOnFailure);
        Assert.Equal("Host=localhost;Database=cephalon_read", settings.Databases.Read.ConnectionString);
        Assert.Equal("outbox01", settings.Databases.Outbox.Schema);
        Assert.Equal("HistoryDb", settings.Databases.History.ConnectionStringName);
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
}
