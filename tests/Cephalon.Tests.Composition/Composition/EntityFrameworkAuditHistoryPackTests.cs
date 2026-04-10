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
    public async Task AddEntityFrameworkAuditHistoryCanResolveHistoryRoleReferencesThroughUseRole()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:WriteDb"] = "Host=localhost;Database=cephalon_write",
                ["Engine:Audit:Enabled"] = "true",
                ["Engine:Audit:History:Enabled"] = "true",
                ["Engine:Audit:History:Provider"] = "entity-framework",
                ["Engine:Audit:History:DatabaseRole"] = "history",
                ["Engine:Audit:EnableInMemoryWriter"] = "false",
                ["Engine:Databases:Write:Provider"] = "PostgreSql",
                ["Engine:Databases:Write:ConnectionStringName"] = "WriteDb",
                ["Engine:Databases:Write:Runtime:CommandTimeoutSeconds"] = "30",
                ["Engine:Databases:History:UseRole"] = "write",
                ["Engine:Databases:History:Schema"] = "audit01",
                ["Engine:Databases:History:Runtime:CommandTimeoutSeconds"] = "120",
                ["Engine:Databases:Migrations:ApplyOnStartup"] = "true",
                ["Engine:Databases:Migrations:Targets:0"] = "history"
            })
            .Build();
        var capturedRoles = new List<EntityFrameworkDatabaseRoleContext>();
        var databaseName = $"cephalon-audit-history-userole-{Guid.NewGuid():N}";
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
        var auditStore = Assert.Single(provider.GetRequiredService<IAuditStoreCatalog>().AuditStores);

        Assert.Equal("history", role.Role);
        Assert.Equal("write", role.ResolvedRoleId);
        Assert.Equal("WriteDb", role.ConnectionStringName);
        Assert.Equal("audit01", role.Schema);
        Assert.Equal(120, role.Runtime.CommandTimeoutSeconds);

        Assert.Equal("history", auditStore.Metadata["databaseRole"]);
        Assert.Equal("write", auditStore.Metadata["resolvedDatabaseRole"]);
        Assert.Equal("role-reference", auditStore.Metadata["resolutionMode"]);
        Assert.Equal("write", auditStore.Metadata["useRole"]);
        Assert.Equal("true", auditStore.Metadata["usesRoleReference"]);
        Assert.Equal("audit01", auditStore.Metadata["schema"]);
        Assert.Equal("WriteDb", auditStore.Metadata["connectionStringName"]);
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
                ["Engine:Audit:History:Export:Enabled"] = "true",
                ["Engine:Audit:History:Export:MaxEntries"] = "150",
                ["Engine:Audit:History:Retention:Enabled"] = "true",
                ["Engine:Audit:History:Retention:MaxAgeDays"] = "90",
                ["Engine:Audit:History:Retention:DeleteBatchSize"] = "250",
                ["Engine:Audit:History:Retention:ApplyOnStartup"] = "true",
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
        Assert.Equal("history", auditStore.Metadata["resolvedDatabaseRole"]);
        Assert.Equal("direct", auditStore.Metadata["resolutionMode"]);
        Assert.Equal("filtered-page-reader", auditStore.Metadata["queryMode"]);
        Assert.Equal("ndjson-stream", auditStore.Metadata["exportMode"]);
        Assert.Equal("150", auditStore.Metadata["exportMaxEntries"]);
        Assert.Equal("startup-only", auditStore.Metadata["retentionMode"]);
        Assert.Equal("90", auditStore.Metadata["retentionMaxAgeDays"]);
        Assert.Equal("250", auditStore.Metadata["retentionDeleteBatchSize"]);
        Assert.Equal("true", auditStore.Metadata["retentionApplyOnStartup"]);
        Assert.Equal(typeof(TestAuditHistoryDbContext).FullName, auditStore.Metadata["dbContext"]);
        Assert.Equal(auditStore.Id, snapshotStore.Id);
    }

    [Fact]
    public async Task AddEntityFrameworkAuditHistoryAppliesConfiguredRetentionOnStartup()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Engine:Audit:Enabled"] = "true",
                ["Engine:Audit:History:Enabled"] = "true",
                ["Engine:Audit:History:Provider"] = "entity-framework",
                ["Engine:Audit:History:DatabaseRole"] = "history",
                ["Engine:Audit:History:Retention:Enabled"] = "true",
                ["Engine:Audit:History:Retention:MaxAgeDays"] = "30",
                ["Engine:Audit:History:Retention:DeleteBatchSize"] = "2",
                ["Engine:Audit:History:Retention:ApplyOnStartup"] = "true",
                ["Engine:Audit:EnableInMemoryWriter"] = "false",
                ["Engine:Databases:History:Provider"] = "Sqlite",
                ["Engine:Databases:History:ConnectionString"] = "Data Source=ignored-for-inmemory"
            })
            .Build();
        var databaseName = $"cephalon-audit-retention-{Guid.NewGuid():N}";
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<TimeProvider>(new FakeTimeProvider(new DateTimeOffset(2026, 04, 10, 0, 0, 0, TimeSpan.Zero)));
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
        await using (var scope = provider.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<TestAuditHistoryDbContext>();
            dbContext.AuditEntries.AddRange(
                new EntityFrameworkAuditHistoryEntry
                {
                    Id = "audit-old-001",
                    Category = "catalog",
                    Action = "product-created",
                    Summary = "Old audit row.",
                    SubjectType = "product",
                    SubjectId = "prod-old",
                    OccurredAtUtc = new DateTimeOffset(2026, 01, 01, 0, 0, 0, TimeSpan.Zero),
                    PersistedAtUtc = new DateTimeOffset(2026, 01, 01, 0, 5, 0, TimeSpan.Zero),
                    ActorId = "system",
                    ActorDisplayName = "system",
                    ActorType = "system",
                    Outcome = AuditOutcome.Succeeded.ToString()
                },
                new EntityFrameworkAuditHistoryEntry
                {
                    Id = "audit-old-002",
                    Category = "catalog",
                    Action = "product-updated",
                    Summary = "Another old audit row.",
                    SubjectType = "product",
                    SubjectId = "prod-old-2",
                    OccurredAtUtc = new DateTimeOffset(2026, 02, 01, 0, 0, 0, TimeSpan.Zero),
                    PersistedAtUtc = new DateTimeOffset(2026, 02, 01, 0, 5, 0, TimeSpan.Zero),
                    ActorId = "system",
                    ActorDisplayName = "system",
                    ActorType = "system",
                    Outcome = AuditOutcome.Succeeded.ToString()
                },
                new EntityFrameworkAuditHistoryEntry
                {
                    Id = "audit-new-001",
                    Category = "orders",
                    Action = "order-created",
                    Summary = "Recent audit row.",
                    SubjectType = "order",
                    SubjectId = "ord-001",
                    OccurredAtUtc = new DateTimeOffset(2026, 04, 05, 0, 0, 0, TimeSpan.Zero),
                    PersistedAtUtc = new DateTimeOffset(2026, 04, 05, 0, 5, 0, TimeSpan.Zero),
                    ActorId = "system",
                    ActorDisplayName = "system",
                    ActorType = "system",
                    Outcome = AuditOutcome.Succeeded.ToString()
                });
            await dbContext.SaveChangesAsync();
        }

        var retentionHostedService = provider
            .GetServices<IHostedService>()
            .Single(service => service.GetType().Name.StartsWith("EntityFrameworkAuditHistoryRetentionHostedService", StringComparison.Ordinal));

        await retentionHostedService.StartAsync(CancellationToken.None);
        await retentionHostedService.StopAsync(CancellationToken.None);

        await using var verificationScope = provider.CreateAsyncScope();
        var verificationDbContext = verificationScope.ServiceProvider.GetRequiredService<TestAuditHistoryDbContext>();
        var retainedIds = await verificationDbContext.AuditEntries
            .OrderBy(entry => entry.Id)
            .Select(entry => entry.Id)
            .ToArrayAsync();

        Assert.Equal(["audit-new-001"], retainedIds);
    }

    [Fact]
    public async Task AddEntityFrameworkAuditHistoryCanQueryPersistedAuditEntries()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Engine:Audit:Enabled"] = "true",
                ["Engine:Audit:History:Enabled"] = "true",
                ["Engine:Audit:History:Provider"] = "entity-framework",
                ["Engine:Audit:History:DatabaseRole"] = "history",
                ["Engine:Audit:History:Export:Enabled"] = "true",
                ["Engine:Audit:History:Export:MaxEntries"] = "2",
                ["Engine:Audit:EnableInMemoryWriter"] = "false",
                ["Engine:Databases:History:Provider"] = "Sqlite",
                ["Engine:Databases:History:ConnectionString"] = "Data Source=ignored-for-inmemory"
            })
            .Build();
        var databaseName = $"cephalon-audit-query-{Guid.NewGuid():N}";
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
        var reader = provider.GetRequiredService<IAuditHistoryReader>();
        var exporter = provider.GetRequiredService<IAuditHistoryExporter>();

        await recorder.RecordAsync(new AuditRecordRequest(
            category: "catalog",
            action: "product-created",
            summary: "Created product prod-001.",
            subjectType: "product",
            subjectId: "prod-001",
            outcome: AuditOutcome.Succeeded,
            tenantId: "tenant-alpha",
            correlationId: "corr-001",
            tags: ["catalog", "create"]));

        var failedEntry = await recorder.RecordAsync(new AuditRecordRequest(
            category: "orders",
            action: "order-cancelled",
            summary: "Cancelled order ord-001.",
            subjectType: "order",
            subjectId: "ord-001",
            outcome: AuditOutcome.Failed,
            tenantId: "tenant-beta",
            correlationId: "corr-002",
            tags: ["orders", "cancel"]));

        await recorder.RecordAsync(new AuditRecordRequest(
            category: "catalog",
            action: "product-updated",
            summary: "Updated product prod-001.",
            subjectType: "product",
            subjectId: "prod-001",
            outcome: AuditOutcome.Succeeded,
            tenantId: "tenant-alpha",
            correlationId: "corr-003",
            tags: ["catalog", "update"]));

        var directEntry = await reader.GetByIdAsync(failedEntry.Id);
        var catalogPage = await reader.QueryAsync(new AuditHistoryQuery(
            category: "catalog",
            subjectType: "product",
            tenantId: "tenant-alpha",
            outcome: AuditOutcome.Succeeded,
            limit: 1));
        var nextCatalogPage = await reader.QueryAsync(new AuditHistoryQuery(
            category: "catalog",
            subjectType: "product",
            tenantId: "tenant-alpha",
            outcome: AuditOutcome.Succeeded,
            offset: 1,
            limit: 1));
        var exportedCatalogEntries = new List<AuditHistoryEntry>();
        await foreach (var entry in exporter.ExportAsync(new AuditHistoryExportRequest(
                           category: "catalog",
                           subjectType: "product",
                           tenantId: "tenant-alpha",
                           outcome: AuditOutcome.Succeeded,
                           maxEntries: 2)))
        {
            exportedCatalogEntries.Add(entry);
        }

        Assert.NotNull(directEntry);
        Assert.Equal(failedEntry.Id, directEntry.Id);
        Assert.Equal("orders", directEntry.Category);
        Assert.Equal(AuditOutcome.Failed, directEntry.Outcome);
        Assert.Equal("tenant-beta", directEntry.TenantId);

        Assert.Equal(2, catalogPage.TotalCount);
        Assert.Single(catalogPage.Entries);
        Assert.True(catalogPage.HasMore);
        Assert.All(catalogPage.Entries, entry =>
        {
            Assert.Equal("catalog", entry.Category);
            Assert.Equal("product", entry.SubjectType);
            Assert.Equal(AuditOutcome.Succeeded, entry.Outcome);
            Assert.Equal("tenant-alpha", entry.TenantId);
        });

        Assert.Equal(2, nextCatalogPage.TotalCount);
        Assert.Single(nextCatalogPage.Entries);
        Assert.False(nextCatalogPage.HasMore);
        Assert.Equal("catalog", nextCatalogPage.Entries[0].Category);
        Assert.Equal("product", nextCatalogPage.Entries[0].SubjectType);
        Assert.Equal("tenant-alpha", nextCatalogPage.Entries[0].TenantId);
        Assert.Equal(2, exportedCatalogEntries.Count);
        Assert.All(exportedCatalogEntries, entry =>
        {
            Assert.Equal("catalog", entry.Category);
            Assert.Equal("product", entry.SubjectType);
            Assert.Equal("tenant-alpha", entry.TenantId);
            Assert.Equal(AuditOutcome.Succeeded, entry.Outcome);
        });
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

    private sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private readonly DateTimeOffset now = now;

        public override DateTimeOffset GetUtcNow()
        {
            return now;
        }
    }
}
