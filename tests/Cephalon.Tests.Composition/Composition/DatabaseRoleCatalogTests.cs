using Cephalon.Abstractions.Data;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Runtime;
using Cephalon.Tests.Support;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Composition;

public sealed class DatabaseRoleCatalogTests
{
    [Fact]
    public void DatabaseRoleCatalogSurfacesConfiguredRolesReferencesAndConsumers()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:WriteDb"] = "Host=localhost;Database=cephalon_write",
                ["ConnectionStrings:ReadDb"] = "Host=localhost;Database=cephalon_read",
                ["ConnectionStrings:HistoryDb"] = "Host=localhost;Database=cephalon_history",
                ["Engine:Patterns:0"] = "CQRS",
                ["Engine:Patterns:1"] = "Outbox",
                ["Engine:Data:ReadWriteSplit"] = "true",
                ["Engine:Data:Outbox:Enabled"] = "true",
                ["Engine:Audit:Enabled"] = "true",
                ["Engine:Audit:History:Enabled"] = "true",
                ["Engine:Audit:History:Provider"] = "entity-framework",
                ["Engine:Audit:History:DatabaseRole"] = "history",
                ["Engine:Audit:History:Export:Enabled"] = "true",
                ["Engine:Audit:History:Retention:Enabled"] = "true",
                ["Engine:Audit:History:Retention:ApplyOnStartup"] = "true",
                ["Engine:Audit:History:Retention:MaxAgeDays"] = "90",
                ["Engine:Databases:Runtime:EnableDetailedErrors"] = "true",
                ["Engine:Databases:Write:Provider"] = "PostgreSql",
                ["Engine:Databases:Write:ConnectionStringName"] = "WriteDb",
                ["Engine:Databases:Read:Provider"] = "PostgreSql",
                ["Engine:Databases:Read:ConnectionStringName"] = "ReadDb",
                ["Engine:Databases:Outbox:UseRole"] = "write",
                ["Engine:Databases:Outbox:Schema"] = "outbox01",
                ["Engine:Databases:History:Provider"] = "PostgreSql",
                ["Engine:Databases:History:ConnectionStringName"] = "HistoryDb",
                ["Engine:Databases:Migrations:ApplyOnStartup"] = "false",
                ["Engine:Databases:Migrations:Targets:0"] = "write",
                ["Engine:Databases:Migrations:Targets:1"] = "history"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddCephalon(engine =>
        {
            engine.UseSettings(EngineSettings.FromConfiguration(configuration));
            engine.AddModule(new PlatformTestModule());
        });

        using var provider = services.BuildServiceProvider();
        var catalog = provider.GetRequiredService<IDatabaseRoleCatalog>();

        Assert.Equal(4, catalog.DatabaseRoles.Count);

        var write = Assert.Single(catalog.DatabaseRoles, role => role.Id == "write");
        Assert.Equal("write", write.RequestedRoleId);
        Assert.Equal("write", write.ResolvedRoleId);
        Assert.Equal("direct", write.ResolutionMode);
        Assert.False(write.UsesRoleReference);
        Assert.Equal("named", write.ConnectionMode);
        Assert.Equal("WriteDb", write.ConnectionStringName);
        Assert.Contains("migrations", write.Consumers);
        Assert.Contains("outbox", write.ReferencedByRoles);
        Assert.Contains("outbox", write.CoLocatedRoles);

        var outbox = Assert.Single(catalog.DatabaseRoles, role => role.Id == "outbox");
        Assert.Equal("outbox", outbox.RequestedRoleId);
        Assert.Equal("write", outbox.ResolvedRoleId);
        Assert.Equal("role-reference", outbox.ResolutionMode);
        Assert.True(outbox.UsesRoleReference);
        Assert.Equal("write", outbox.UseRole);
        Assert.Equal("WriteDb", outbox.ConnectionStringName);
        Assert.Equal("outbox01", outbox.Schema);
        Assert.Contains("outbox", outbox.Consumers);
        Assert.Contains("write", outbox.CoLocatedRoles);

        var history = Assert.Single(catalog.DatabaseRoles, role => role.Id == "history");
        Assert.Equal("history", history.ResolvedRoleId);
        Assert.Equal("HistoryDb", history.ConnectionStringName);
        Assert.Contains("audit-history", history.Consumers);
        Assert.Contains("migrations", history.Consumers);
        Assert.Equal("entity-framework", history.Metadata["auditHistoryProvider"]);
        Assert.Equal("true", history.Metadata["auditHistoryExportEnabled"]);
        Assert.Equal("true", history.Metadata["auditHistoryRetentionEnabled"]);
    }

    [Fact]
    public void RuntimeSnapshotIncludesDatabaseRoleCatalog()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:WriteDb"] = "Host=localhost;Database=cephalon_write",
                ["Engine:Databases:Write:Provider"] = "PostgreSql",
                ["Engine:Databases:Write:ConnectionStringName"] = "WriteDb"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddCephalon(engine =>
        {
            engine.UseSettings(EngineSettings.FromConfiguration(configuration));
            engine.AddModule(new PlatformTestModule());
        });

        using var provider = services.BuildServiceProvider();
        var snapshot = provider.GetRequiredService<IRuntimeIntrospectionSnapshotProvider>().CreateSnapshot();

        var write = Assert.Single(snapshot.DatabaseRoles);
        Assert.Equal("write", write.Id);
        Assert.Equal("write", write.ResolvedRoleId);
        Assert.Equal("engine-databases", write.Metadata["topologySource"]);
    }
}
