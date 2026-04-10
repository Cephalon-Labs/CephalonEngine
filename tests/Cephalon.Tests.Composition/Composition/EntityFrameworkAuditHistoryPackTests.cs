using Cephalon.Abstractions.Audit;
using Cephalon.Audit.EntityFramework;
using Cephalon.Audit.EntityFramework.Modeling;
using Cephalon.Audit.EntityFramework.Registration;
using Cephalon.Audit.Registration;
using Cephalon.Audit.Services;
using Cephalon.Data.EntityFramework.Configuration;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cephalon.Tests.Composition;

public sealed class EntityFrameworkAuditHistoryPackTests
{
    [Fact]
    public async Task AddEntityFrameworkAuditHistoryCanResolveTheConfiguredAuditHistoryRoleFromEngineDatabases()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:WriteDb"] = "Host=localhost;Database=cephalon_write",
                ["Engine:Audit:Enabled"] = "true",
                ["Engine:Audit:History:Enabled"] = "true",
                ["Engine:Audit:History:Provider"] = "entity-framework",
                ["Engine:Audit:History:DatabaseRole"] = "write",
                ["Engine:Audit:EnableInMemoryWriter"] = "false",
                ["Engine:Databases:Write:Provider"] = "PostgreSql",
                ["Engine:Databases:Write:ConnectionStringName"] = "WriteDb",
                ["Engine:Databases:Migrations:ApplyOnStartup"] = "true",
                ["Engine:Databases:Migrations:Targets:0"] = "write"
            })
            .Build();
        var capturedRoles = new List<EntityFrameworkDatabaseRoleContext>();
        var databaseName = $"cephalon-audit-history-{Guid.NewGuid():N}";
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddCephalon(engine =>
        {
            engine.UseSettings(EngineSettings.FromConfiguration(configuration));
            engine.AddModule(new PlatformTestModule());
            engine.AddAudit();
            engine.AddEntityFrameworkAuditHistory<TestAuditHistoryDbContext>((role, options) =>
            {
                capturedRoles.Add(role);
                options.UseInMemoryDatabase(databaseName);
            });
        });

        await using var provider = services.BuildServiceProvider();
        var hostedService = Assert.Single(
            provider.GetServices<IHostedService>(),
            service => service is global::Cephalon.Data.EntityFramework.Services.EntityFrameworkDatabaseMigrationHostedService);

        await hostedService.StartAsync(CancellationToken.None);

        var role = Assert.Single(capturedRoles);
        Assert.Equal("write", role.Role);
        Assert.Equal("write", role.ResolvedRoleId);
        Assert.Equal("WriteDb", role.ConnectionStringName);
        Assert.Equal("Host=localhost;Database=cephalon_write", role.ConnectionString);
    }

    [Fact]
    public async Task AddEntityFrameworkAuditHistoryPersistsAuditEntriesAndPublishesDurableStoreMetadata()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Engine:Audit:Enabled"] = "true",
                ["Engine:Audit:History:Enabled"] = "true",
                ["Engine:Audit:History:Provider"] = "entity-framework",
                ["Engine:Audit:History:DatabaseRole"] = "history",
                ["Engine:Audit:EnableInMemoryWriter"] = "false",
                ["Engine:Databases:History:Provider"] = "Sqlite",
                ["Engine:Databases:History:ConnectionString"] = "Data Source=ignored-for-inmemory"
            })
            .Build();
        var databaseName = $"cephalon-audit-store-{Guid.NewGuid():N}";
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddCephalon(engine =>
        {
            engine.UseSettings(EngineSettings.FromConfiguration(configuration));
            engine.AddModule(new PlatformTestModule());
            engine.AddAudit();
            engine.AddEntityFrameworkAuditHistory<TestAuditHistoryDbContext>(options =>
            {
                options.UseInMemoryDatabase(databaseName);
            });
        });

        await using var provider = services.BuildServiceProvider();
        var recorder = provider.GetRequiredService<IAuditRecorder>();
        var auditStoreCatalog = provider.GetRequiredService<IAuditStoreCatalog>();
        var snapshot = provider.GetRequiredService<global::Cephalon.Engine.Runtime.IRuntimeIntrospectionSnapshotProvider>().CreateSnapshot();

        var entry = await recorder.RecordAsync(new AuditRecordRequest(
            category: "catalog",
            action: "product-created",
            summary: "Created a product through the durable audit pipeline.",
            subjectType: "product",
            subjectId: "prod-001",
            outcome: AuditOutcome.Succeeded,
            tags: ["catalog", "audit"]));

        await using var scope = provider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TestAuditHistoryDbContext>();
        var persistedEntry = await dbContext.AuditEntries.SingleAsync();
        var auditStore = Assert.Single(auditStoreCatalog.AuditStores);
        var snapshotStore = Assert.Single(snapshot.AuditStores);

        Assert.Equal(entry.Id, persistedEntry.Id);
        Assert.Equal("catalog", persistedEntry.Category);
        Assert.Equal("product-created", persistedEntry.Action);
        Assert.Equal("entity-framework", auditStore.Provider);
        Assert.Equal("transactional-table", auditStore.Mode);
        Assert.Equal("history", auditStore.Metadata["databaseRole"]);
        Assert.Equal(typeof(TestAuditHistoryDbContext).FullName, auditStore.Metadata["dbContext"]);
        Assert.Equal(auditStore.Id, snapshotStore.Id);
    }

    [Fact]
    public async Task AddEntityFrameworkAuditHistoryAcceptsLegacyProviderAliasForSelection()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Engine:Audit:Enabled"] = "true",
                ["Engine:Audit:History:Enabled"] = "true",
                ["Engine:Audit:History:Provider"] = "EntityFramework",
                ["Engine:Audit:History:DatabaseRole"] = "history",
                ["Engine:Audit:EnableInMemoryWriter"] = "false",
                ["Engine:Databases:History:Provider"] = "Sqlite",
                ["Engine:Databases:History:ConnectionString"] = "Data Source=ignored-for-inmemory"
            })
            .Build();
        var databaseName = $"cephalon-audit-store-legacy-{Guid.NewGuid():N}";
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddCephalon(engine =>
        {
            engine.UseSettings(EngineSettings.FromConfiguration(configuration));
            engine.AddModule(new PlatformTestModule());
            engine.AddAudit();
            engine.AddEntityFrameworkAuditHistory<TestAuditHistoryDbContext>(options =>
            {
                options.UseInMemoryDatabase(databaseName);
            });
        });

        await using var provider = services.BuildServiceProvider();
        var recorder = provider.GetRequiredService<IAuditRecorder>();

        await recorder.RecordAsync(new AuditRecordRequest(
            category: "catalog",
            action: "product-created",
            summary: "Created a product through the legacy durable audit configuration.",
            subjectType: "product",
            subjectId: "prod-legacy",
            outcome: AuditOutcome.Succeeded));

        await using var scope = provider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TestAuditHistoryDbContext>();
        Assert.Equal(1, await dbContext.AuditEntries.CountAsync());
    }

    private sealed class TestAuditHistoryDbContext(DbContextOptions<TestAuditHistoryDbContext> options)
        : DbContext(options), IEntityFrameworkAuditHistoryContext
    {
        public DbSet<EntityFrameworkAuditHistoryEntry> AuditEntries => Set<EntityFrameworkAuditHistoryEntry>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ArgumentNullException.ThrowIfNull(modelBuilder);

            modelBuilder.ConfigureCephalonAuditHistory(tableName: "test_audit_history");
        }
    }
}
