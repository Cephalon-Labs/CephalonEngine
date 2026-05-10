using System.Text.Json;
using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Health;
using Cephalon.Abstractions.Technologies;
using Cephalon.Behaviors.Hosting;
using Cephalon.Behaviors.Patterns.Abstractions;
using Cephalon.Behaviors.Patterns.Hosting;
using Cephalon.Data.EntityFramework.Configuration;
using Cephalon.Data.EntityFramework.Registration;
using Cephalon.Data.Registration;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Diagnostics;
using Cephalon.Engine.Runtime;
using Cephalon.Eventing.Behaviors.Registration;
using Cephalon.Eventing.Registration;
using Cephalon.Eventing.Services;
using Cephalon.Ids.Sfid.Registration;
using Cephalon.Tests.Support;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SfidNet;

namespace Cephalon.Tests.Composition;

public sealed class EntityFrameworkDataPackTests
{
    [Fact]
    public async Task AddEntityFrameworkDataCanConsumeSharedEngineDatabaseTopology()
    {
        var databaseName = $"cephalon-data-ef-topology-shared-{Guid.NewGuid():N}";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:WriteDb"] = "Host=localhost;Database=cephalon_write",
                ["Engine:Databases:Runtime:EnableDetailedErrors"] = "true",
                ["Engine:Databases:Runtime:EnableSensitiveDataLogging"] = "false",
                ["Engine:Databases:Runtime:EnableRetryOnFailure"] = "true",
                ["Engine:Databases:Runtime:MaxRetryCount"] = "5",
                ["Engine:Databases:Write:Provider"] = "PostgreSql",
                ["Engine:Databases:Write:ConnectionStringName"] = "WriteDb",
                ["Engine:Databases:Write:Runtime:EnableRetryOnFailure"] = "false",
                ["Engine:Databases:Write:Runtime:CommandTimeoutSeconds"] = "30",
                ["Engine:Databases:Migrations:ApplyOnStartup"] = "false",
                ["Engine:Databases:Migrations:Targets:0"] = "write"
            })
            .Build();
        var capturedRoles = new List<EntityFrameworkDatabaseRoleContext>();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddCephalon(engine =>
        {
            engine.UseSettings(EngineSettings.FromConfiguration(configuration));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new EntityFrameworkSingleContextTestModule());
            engine.AddData();
            engine.AddEntityFrameworkData<SingleCatalogDbContext>(
                configureDbContext: (role, options) =>
                {
                    capturedRoles.Add(role);
                    options.UseInMemoryDatabase(databaseName);
                });
        });

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var writeStore = scope.ServiceProvider.GetRequiredService<IWriteStore>();
        var readStore = scope.ServiceProvider.GetRequiredService<IReadStore>();
        var technologyCatalog = provider.GetRequiredService<global::Cephalon.Abstractions.Technologies.ITechnologyRuntimeCatalog>();
        var runtime = provider.GetRequiredService<Cephalon.Engine.Runtime.IRuntime>();

        await writeStore.ExecuteAsync(new CreateSingleCatalogItemCommand("item-topology-001", "Ada"));
        var count = await readStore.ExecuteAsync(new CountSingleCatalogItemsQuery());

        Assert.Equal(1, count);
        var role = Assert.Single(capturedRoles);
        Assert.Equal("write", role.Role);
        Assert.Equal("PostgreSql", role.Provider);
        Assert.Equal("WriteDb", role.ConnectionStringName);
        Assert.Equal("Host=localhost;Database=cephalon_write", role.ConnectionString);
        Assert.True(role.Runtime.EnableDetailedErrors);
        Assert.False(role.Runtime.EnableRetryOnFailure);
        Assert.Equal(5, role.Runtime.MaxRetryCount);
        Assert.Equal(30, role.Runtime.CommandTimeoutSeconds);

        var providerCapability = Assert.Single(runtime.Manifest.Capabilities, capability => capability.Key == "data.entity-framework");
        Assert.Equal("engine-databases", providerCapability.Metadata["topologySource"]);

        var dataManagementSurfaces = technologyCatalog.GetByTechnology("data-management");
        var databaseRoles = Assert.Single(dataManagementSurfaces, surface => surface.SurfaceId == "database-roles");
        var writeEntry = Assert.Single(databaseRoles.Entries, entry => entry.Id == "write");
        var migrations = Assert.Single(databaseRoles.Entries, entry => entry.Id == "migrations");
        Assert.Equal("write", writeEntry.Metadata["requestedRole"]);
        Assert.Equal("write", writeEntry.Metadata["resolvedRole"]);
        Assert.Equal("direct", writeEntry.Metadata["resolutionMode"]);
        Assert.Equal("write", migrations.Metadata["configuredTargets"]);
        Assert.Equal("write", migrations.Metadata["supportedTargets"]);
    }

    [Fact]
    public async Task AddEntityFrameworkDataCanConsumeSplitEngineDatabaseTopology()
    {
        var readDatabaseName = $"cephalon-data-ef-topology-read-{Guid.NewGuid():N}";
        var writeDatabaseName = $"cephalon-data-ef-topology-write-{Guid.NewGuid():N}";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:WriteDb"] = "Host=localhost;Database=cephalon_write",
                ["Engine:Data:ReadWriteSplit"] = "true",
                ["Engine:Databases:Runtime:EnableDetailedErrors"] = "true",
                ["Engine:Databases:Write:Provider"] = "PostgreSql",
                ["Engine:Databases:Write:ConnectionStringName"] = "WriteDb",
                ["Engine:Databases:Read:Provider"] = "PostgreSql",
                ["Engine:Databases:Read:ConnectionString"] = "Host=localhost;Database=cephalon_read",
                ["Engine:Databases:Migrations:ApplyOnStartup"] = "false",
                ["Engine:Databases:Migrations:Targets:0"] = "write",
                ["Engine:Databases:Migrations:Targets:1"] = "read"
            })
            .Build();
        var capturedRoles = new List<EntityFrameworkDatabaseRoleContext>();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "Microservice",
                patterns: ["CQRS"],
                data: DataSettings.FromConfiguration(configuration),
                databases: DatabaseTopologySettings.FromConfiguration(configuration)));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new EntityFrameworkSplitContextTestModule());
            engine.AddData();
            engine.AddEntityFrameworkData<SplitCatalogReadDbContext, SplitCatalogWriteDbContext>(
                configureDbContext: (role, options) =>
                {
                    capturedRoles.Add(role);
                    options.UseInMemoryDatabase(role.Role == "read" ? readDatabaseName : writeDatabaseName);
                });
        });

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var writeStore = scope.ServiceProvider.GetRequiredService<IWriteStore>();
        var readStore = scope.ServiceProvider.GetRequiredService<IReadStore>();

        await writeStore.ExecuteAsync(new CreateSplitWriteCatalogItemCommand("write-topology-001", "Grace"));
        scope.ServiceProvider.GetRequiredService<SplitCatalogReadDbContext>().CatalogItems.Add(new SplitCatalogReadItem
        {
            Id = "read-topology-001",
            Name = "Grace"
        });
        await scope.ServiceProvider.GetRequiredService<SplitCatalogReadDbContext>().SaveChangesAsync();

        var readCount = await readStore.ExecuteAsync(new CountSplitReadCatalogItemsQuery());

        Assert.Equal(1, readCount);
        Assert.Equal(2, capturedRoles.Count);
        var readRole = Assert.Single(capturedRoles, role => role.Role == "read");
        var writeRole = Assert.Single(capturedRoles, role => role.Role == "write");
        Assert.Equal("Host=localhost;Database=cephalon_read", readRole.ConnectionString);
        Assert.Equal("WriteDb", writeRole.ConnectionStringName);
        Assert.True(readRole.Runtime.EnableDetailedErrors);
        Assert.True(writeRole.Runtime.EnableDetailedErrors);
    }

    [Fact]
    public void TopologyBackedEntityFrameworkOutboxCanReferenceTheWriteRoleThroughUseRole()
    {
        var databaseName = $"cephalon-data-ef-outbox-userole-{Guid.NewGuid():N}";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:WriteDb"] = "Host=localhost;Database=cephalon_write",
                ["Engine:Patterns:0"] = "CQRS",
                ["Engine:Patterns:1"] = "Outbox",
                ["Engine:Data:Outbox:Enabled"] = "true",
                ["Engine:Databases:Write:Provider"] = "PostgreSql",
                ["Engine:Databases:Write:ConnectionStringName"] = "WriteDb",
                ["Engine:Databases:Write:Runtime:CommandTimeoutSeconds"] = "30",
                ["Engine:Databases:Outbox:UseRole"] = "write",
                ["Engine:Databases:Outbox:Schema"] = "outbox01",
                ["Engine:Databases:Outbox:Runtime:CommandTimeoutSeconds"] = "90"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddCephalon(engine =>
        {
            engine.UseSettings(EngineSettings.FromConfiguration(configuration));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new EntityFrameworkOutboxTestModule());
            engine.AddData();
            engine.AddEntityFrameworkData<OutboxCatalogDbContext>(
                configureDbContext: (_, options) => options.UseInMemoryDatabase(databaseName),
                configure: options => options.RegisterOutbox = true);
        });

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var outboxRole = EntityFrameworkDatabaseRoleResolver.ResolveOutbox(scope.ServiceProvider);
        var databaseRoleCatalog = provider.GetRequiredService<IDatabaseRoleCatalog>();
        var technologyCatalog = provider.GetRequiredService<global::Cephalon.Abstractions.Technologies.ITechnologyRuntimeCatalog>();
        var databaseRoles = Assert.Single(
            technologyCatalog.GetByTechnology("data-management"),
            surface => surface.SurfaceId == "database-roles");
        var outboxEntry = Assert.Single(databaseRoles.Entries, entry => entry.Id == "entity-framework-outbox");
        var writeRoleDescriptor = Assert.Single(databaseRoleCatalog.DatabaseRoles, role => role.Id == "write");
        var outboxRoleDescriptor = Assert.Single(databaseRoleCatalog.DatabaseRoles, role => role.Id == "outbox");

        Assert.Equal("outbox", outboxRole.Role);
        Assert.Equal("write", outboxRole.ResolvedRoleId);
        Assert.Equal("PostgreSql", outboxRole.Provider);
        Assert.Equal("WriteDb", outboxRole.ConnectionStringName);
        Assert.Equal("outbox01", outboxRole.Schema);
        Assert.Equal(90, outboxRole.Runtime.CommandTimeoutSeconds);

        Assert.Equal("outbox", outboxEntry.Metadata["requestedRole"]);
        Assert.Equal("write", outboxEntry.Metadata["resolvedRole"]);
        Assert.Equal("role-reference", outboxEntry.Metadata["resolutionMode"]);
        Assert.Equal("role-reference", outboxEntry.Metadata["routingMode"]);
        Assert.Equal("write", outboxEntry.Metadata["useRole"]);
        Assert.Equal("true", outboxEntry.Metadata["usesRoleReference"]);
        Assert.Equal("WriteDb", outboxEntry.Metadata["connectionStringName"]);
        Assert.Equal("outbox01", outboxEntry.Metadata["schema"]);
        Assert.Equal("true", outboxRoleDescriptor.Metadata["inheritsResolvedRoleRuntime"]);
        Assert.Equal(writeRoleDescriptor.HealthState, outboxRoleDescriptor.HealthState);
        Assert.Equal(writeRoleDescriptor.MigrationState, outboxRoleDescriptor.MigrationState);
        Assert.Equal("succeeded", outboxRoleDescriptor.RuntimeMetadata["probeOutcome"]);
    }

    [Fact]
    public async Task TopologyBackedEntityFrameworkSurfacesMigrationPolicyWhenApplyOnStartupIsRequested()
    {
        var databaseName = $"cephalon-data-ef-migrations-policy-{Guid.NewGuid():N}";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:WriteDb"] = "Host=localhost;Database=cephalon_write",
                ["Engine:Databases:Write:Provider"] = "PostgreSql",
                ["Engine:Databases:Write:ConnectionStringName"] = "WriteDb",
                ["Engine:Databases:Migrations:ApplyOnStartup"] = "true",
                ["Engine:Databases:Migrations:Targets:0"] = "write"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddCephalon(engine =>
        {
            engine.UseSettings(EngineSettings.FromConfiguration(configuration));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new EntityFrameworkSingleContextTestModule());
            engine.AddData();
            engine.AddEntityFrameworkData<SingleCatalogDbContext>(
                configureDbContext: (_, options) => options.UseInMemoryDatabase(databaseName));
        });

        using var provider = services.BuildServiceProvider();
        var hostedService = Assert.Single(
            provider.GetServices<IHostedService>(),
            service => string.Equals(
                service.GetType().Name,
                "EntityFrameworkDatabaseMigrationHostedService",
                StringComparison.Ordinal));
        await hostedService.StartAsync(CancellationToken.None);

        var technologyCatalog = provider.GetRequiredService<global::Cephalon.Abstractions.Technologies.ITechnologyRuntimeCatalog>();
        var dataManagementSurfaces = technologyCatalog.GetByTechnology("data-management");
        var databaseRoles = Assert.Single(dataManagementSurfaces, surface => surface.SurfaceId == "database-roles");
        var migrations = Assert.Single(databaseRoles.Entries, entry => entry.Id == "migrations");

        Assert.Equal("true", migrations.Metadata["applyOnStartup"]);
        Assert.Equal("write", migrations.Metadata["configuredTargets"]);
        Assert.Equal("write", migrations.Metadata["supportedTargets"]);
        Assert.Equal("startup-hosted-service", migrations.Metadata["executionMode"]);
        Assert.Equal("generic-host/ihostedservice", migrations.Metadata["startupMechanism"]);
        Assert.Equal("bundle-or-script", migrations.Metadata["productionRecommendation"]);
    }

    [Fact]
    public async Task TopologyBackedEntityFrameworkProjectsLiveDatabaseRoleRuntimeDiagnostics()
    {
        var databaseName = $"cephalon-data-ef-role-runtime-{Guid.NewGuid():N}";
        var timeProvider = new MutableTimeProvider(new DateTimeOffset(2026, 04, 13, 12, 0, 0, TimeSpan.Zero));
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:WriteDb"] = "Host=localhost;Database=cephalon_write",
                ["Engine:Databases:Runtime:RoleProbeFreshnessSeconds"] = "30",
                ["Engine:Databases:Write:Provider"] = "PostgreSql",
                ["Engine:Databases:Write:ConnectionStringName"] = "WriteDb",
                ["Engine:Databases:Migrations:ApplyOnStartup"] = "true",
                ["Engine:Databases:Migrations:Targets:0"] = "write"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<TimeProvider>(timeProvider);
        services.AddCephalon(engine =>
        {
            engine.UseSettings(EngineSettings.FromConfiguration(configuration));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new EntityFrameworkSingleContextTestModule());
            engine.AddData();
            engine.AddEntityFrameworkData<SingleCatalogDbContext>(
                configureDbContext: (_, options) => options.UseInMemoryDatabase(databaseName));
        });

        using var provider = services.BuildServiceProvider();
        var catalog = provider.GetRequiredService<IDatabaseRoleCatalog>();
        var hostedService = Assert.Single(
            provider.GetServices<IHostedService>(),
            service => string.Equals(
                service.GetType().Name,
                "EntityFrameworkDatabaseMigrationHostedService",
                StringComparison.Ordinal));

        var pendingWrite = Assert.Single(catalog.DatabaseRoles, role => role.Id == "write");
        Assert.Equal(HealthState.Healthy, pendingWrite.HealthState);
        Assert.Equal("pending-startup-apply", pendingWrite.MigrationState);
        Assert.Equal("entity-framework", pendingWrite.RuntimeMetadata["providerPack"]);
        Assert.Equal("true", pendingWrite.RuntimeMetadata["startupApplyEnabled"]);
        Assert.Equal("true", pendingWrite.RuntimeMetadata["migrationTargeted"]);
        Assert.Equal("startup-hosted-service", pendingWrite.RuntimeMetadata["executionMode"]);
        Assert.Equal("succeeded", pendingWrite.RuntimeMetadata["probeOutcome"]);
        Assert.Equal("true", pendingWrite.RuntimeMetadata["probeCacheEnabled"]);
        Assert.Equal("30", pendingWrite.RuntimeMetadata["probeFreshnessSeconds"]);
        Assert.Equal("configured", pendingWrite.RuntimeMetadata["probeFreshnessOrigin"]);
        Assert.Equal("live", pendingWrite.RuntimeMetadata["probeSource"]);
        Assert.NotNull(pendingWrite.Probe);
        Assert.True(pendingWrite.Probe.CacheEnabled);
        Assert.Equal(30, pendingWrite.Probe.FreshnessSeconds);
        Assert.Equal("configured", pendingWrite.Probe.FreshnessOrigin);
        Assert.Equal("live", pendingWrite.Probe.Source);
        Assert.Contains("InMemory", pendingWrite.RuntimeMetadata["providerNames"], StringComparison.Ordinal);
        Assert.Equal("0", pendingWrite.RuntimeMetadata["pendingMigrationCount"]);
        Assert.True(pendingWrite.ObservedAtUtc.HasValue);
        Assert.Equal("2026-04-13T12:00:00.0000000+00:00", pendingWrite.RuntimeMetadata["lastProbeAtUtc"]);
        Assert.Equal("0", pendingWrite.RuntimeMetadata["probeAgeSeconds"]);
        Assert.Equal("2026-04-13T12:00:30.0000000+00:00", pendingWrite.RuntimeMetadata["probeFreshUntilUtc"]);
        Assert.Equal(0, pendingWrite.Probe.AgeSeconds);
        Assert.Equal(new DateTimeOffset(2026, 04, 13, 12, 0, 30, TimeSpan.Zero), pendingWrite.Probe.FreshUntilUtc);

        timeProvider.Advance(TimeSpan.FromSeconds(10));

        var cachedWrite = Assert.Single(catalog.DatabaseRoles, role => role.Id == "write");
        Assert.Equal("cache", cachedWrite.RuntimeMetadata["probeSource"]);
        Assert.Equal("2026-04-13T12:00:00.0000000+00:00", cachedWrite.RuntimeMetadata["lastProbeAtUtc"]);
        Assert.Equal("10", cachedWrite.RuntimeMetadata["probeAgeSeconds"]);
        Assert.Equal(pendingWrite.ObservedAtUtc, cachedWrite.ObservedAtUtc);
        Assert.NotNull(cachedWrite.Probe);
        Assert.Equal("cache", cachedWrite.Probe.Source);
        Assert.Equal(10, cachedWrite.Probe.AgeSeconds);

        await hostedService.StartAsync(CancellationToken.None);

        timeProvider.Advance(TimeSpan.FromSeconds(5));

        var appliedWrite = Assert.Single(catalog.DatabaseRoles, role => role.Id == "write");
        Assert.Equal(HealthState.Healthy, appliedWrite.HealthState);
        Assert.Equal("succeeded", appliedWrite.MigrationState);
        Assert.Equal("ensure-created", appliedWrite.RuntimeMetadata["lastExecutionMode"]);
        Assert.Equal("succeeded", appliedWrite.RuntimeMetadata["lastOutcome"]);
        Assert.True(appliedWrite.ObservedAtUtc.HasValue);
        Assert.Contains("succeeded", appliedWrite.MigrationDescription ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("succeeded", appliedWrite.RuntimeMetadata["probeOutcome"]);
        Assert.Equal("0", appliedWrite.RuntimeMetadata["pendingMigrationCount"]);
        Assert.Equal("live", appliedWrite.RuntimeMetadata["probeSource"]);
        Assert.Equal("2026-04-13T12:00:15.0000000+00:00", appliedWrite.RuntimeMetadata["lastProbeAtUtc"]);
        Assert.Equal("0", appliedWrite.RuntimeMetadata["probeAgeSeconds"]);
        Assert.NotNull(appliedWrite.Probe);
        Assert.Equal("live", appliedWrite.Probe.Source);
        Assert.Equal(0, appliedWrite.Probe.AgeSeconds);
    }

    [Fact]
    public async Task TopologyBackedEntityFrameworkProjectsDatabaseMigrationCatalogState()
    {
        var databaseName = $"cephalon-data-ef-migration-catalog-{Guid.NewGuid():N}";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:WriteDb"] = "Host=localhost;Database=cephalon_write",
                ["Engine:Databases:Write:Provider"] = "PostgreSql",
                ["Engine:Databases:Write:ConnectionStringName"] = "WriteDb",
                ["Engine:Databases:Migrations:ApplyOnStartup"] = "true",
                ["Engine:Databases:Migrations:Targets:0"] = "write"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddCephalon(engine =>
        {
            engine.UseSettings(EngineSettings.FromConfiguration(configuration));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new EntityFrameworkSingleContextTestModule());
            engine.AddData();
            engine.AddEntityFrameworkData<SingleCatalogDbContext>(
                configureDbContext: (_, options) => options.UseInMemoryDatabase(databaseName));
        });

        using var provider = services.BuildServiceProvider();
        var migrationCatalog = provider.GetRequiredService<IDatabaseMigrationCatalog>();
        var hostedService = Assert.Single(
            provider.GetServices<IHostedService>(),
            service => string.Equals(
                service.GetType().Name,
                "EntityFrameworkDatabaseMigrationHostedService",
                StringComparison.Ordinal));

        var pendingWrite = Assert.Single(migrationCatalog.DatabaseMigrations);
        Assert.Equal("write", pendingWrite.Id);
        Assert.Equal(DatabaseMigrationStatus.Planned, pendingWrite.Status);
        Assert.Equal("startup-hosted-service", pendingWrite.ExecutionMode);
        Assert.True(pendingWrite.ApplyOnStartup);
        Assert.False(pendingWrite.ExitAfterApply);
        Assert.Equal("entity-framework", pendingWrite.Metadata["runtimeProvider"]);
        Assert.Equal("engine-databases", pendingWrite.Metadata["topologySource"]);
        Assert.Equal(1, pendingWrite.RecommendedExecutionOrder);
        Assert.Equal("1", pendingWrite.Metadata["recommendedExecutionOrder"]);
        Assert.Equal("bundle-or-script", pendingWrite.Metadata["recommendedExecutionMode"]);
        Assert.Equal("bundle,script,update", pendingWrite.Metadata["commandIds"]);
        Assert.Equal(HealthState.Healthy, pendingWrite.RoleHealthState);
        Assert.Contains("Entity Framework", pendingWrite.RoleHealthDescription ?? string.Empty, StringComparison.Ordinal);
        Assert.Equal("pending-startup-apply", pendingWrite.RoleMigrationState);
        Assert.Contains("startup schema apply", pendingWrite.RoleMigrationDescription ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.True(pendingWrite.RoleObservedAtUtc.HasValue);
        Assert.Equal("healthy", pendingWrite.Metadata["roleHealthState"]);
        Assert.Equal("pending-startup-apply", pendingWrite.Metadata["roleMigrationState"]);
        Assert.Equal("succeeded", pendingWrite.Metadata["roleRuntime.probeOutcome"]);
        Assert.Equal(typeof(SingleCatalogDbContext).FullName, pendingWrite.DbContextType);
        Assert.Collection(
            pendingWrite.Commands,
            bundle =>
            {
                Assert.Equal("bundle", bundle.Id);
                Assert.True(bundle.RecommendedForProduction);
                Assert.Equal("dotnet ef migrations bundle --context SingleCatalogDbContext", bundle.CommandTemplate);
                Assert.Equal("dotnet-ef", bundle.ToolId);
                Assert.Equal("deploy-time", bundle.ExecutionCategory);
                Assert.Equal("startup-project", bundle.WorkingDirectoryHint);
                Assert.Equal("dotnet-ef", bundle.Metadata["tool"]);
            },
            script =>
            {
                Assert.Equal("script", script.Id);
                Assert.True(script.RecommendedForProduction);
                Assert.Equal("dotnet ef migrations script --context SingleCatalogDbContext --idempotent", script.CommandTemplate);
                Assert.Equal("dotnet-ef", script.ToolId);
                Assert.Equal("deploy-time", script.ExecutionCategory);
                Assert.Equal("startup-project", script.WorkingDirectoryHint);
            },
            update =>
            {
                Assert.Equal("update", update.Id);
                Assert.False(update.RecommendedForProduction);
                Assert.Equal("dotnet ef database update --context SingleCatalogDbContext", update.CommandTemplate);
                Assert.Equal("dotnet-ef", update.ToolId);
                Assert.Equal("manual", update.ExecutionCategory);
                Assert.Equal("startup-project", update.WorkingDirectoryHint);
            });

        await hostedService.StartAsync(CancellationToken.None);

        var appliedWrite = Assert.Single(migrationCatalog.DatabaseMigrations);
        Assert.Equal(DatabaseMigrationStatus.Succeeded, appliedWrite.Status);
        Assert.Equal("ensure-created", appliedWrite.Mechanism);
        Assert.True(appliedWrite.StartedAtUtc.HasValue);
        Assert.True(appliedWrite.CompletedAtUtc.HasValue);
        Assert.Null(appliedWrite.LastError);
        Assert.Equal(1, appliedWrite.RecommendedExecutionOrder);
        Assert.Equal(HealthState.Healthy, appliedWrite.RoleHealthState);
        Assert.Equal("succeeded", appliedWrite.RoleMigrationState);
        Assert.Contains("succeeded", appliedWrite.RoleMigrationDescription ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.True(appliedWrite.RoleObservedAtUtc.HasValue);
        Assert.Equal("healthy", appliedWrite.Metadata["roleHealthState"]);
        Assert.Equal("succeeded", appliedWrite.Metadata["roleMigrationState"]);
        Assert.Equal("succeeded", appliedWrite.Metadata["roleRuntime.probeOutcome"]);

        var snapshot = provider.GetRequiredService<global::Cephalon.Engine.Runtime.IRuntimeIntrospectionSnapshotProvider>().CreateSnapshot();
        var snapshotMigration = Assert.Single(snapshot.DatabaseMigrations);
        Assert.Equal("write", snapshotMigration.Id);
        Assert.Equal(1, snapshotMigration.RecommendedExecutionOrder);
        Assert.Equal(DatabaseMigrationStatus.Succeeded, snapshotMigration.Status);
        Assert.Equal(3, snapshotMigration.Commands.Count);
        Assert.Equal(HealthState.Healthy, snapshotMigration.RoleHealthState);
        Assert.Equal("succeeded", snapshotMigration.RoleMigrationState);
        Assert.True(snapshotMigration.RoleObservedAtUtc.HasValue);
        Assert.Equal("healthy", snapshotMigration.Metadata["roleHealthState"]);
        Assert.Equal("succeeded", snapshotMigration.Metadata["roleRuntime.probeOutcome"]);
    }

    [Fact]
    public void TopologyBackedEntityFrameworkMarksRoleUnhealthyWhenConnectivityProbeFails()
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
            engine.AddModule(new EntityFrameworkSingleContextTestModule());
            engine.AddData();
            engine.AddEntityFrameworkData<SingleCatalogDbContext>(
                configureDbContext: static (_, _) => { });
        });

        using var provider = services.BuildServiceProvider();
        var catalog = provider.GetRequiredService<IDatabaseRoleCatalog>();

        var writeRole = Assert.Single(catalog.DatabaseRoles, role => role.Id == "write");

        Assert.Equal(HealthState.Unhealthy, writeRole.HealthState);
        Assert.Equal("failed", writeRole.RuntimeMetadata["probeOutcome"]);
        Assert.Contains("connectivity probe failed", writeRole.HealthDescription ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No database provider has been configured", writeRole.RuntimeMetadata["lastProbeError"], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TopologyBackedEntityFrameworkStartupMigrationsRejectUnsupportedOutboxTargets()
    {
        var databaseName = $"cephalon-data-ef-migrations-outbox-{Guid.NewGuid():N}";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:WriteDb"] = "Host=localhost;Database=cephalon_write",
                ["Engine:Patterns:0"] = "CQRS",
                ["Engine:Patterns:1"] = "Outbox",
                ["Engine:Data:Outbox:Enabled"] = "true",
                ["Engine:Databases:Write:Provider"] = "PostgreSql",
                ["Engine:Databases:Write:ConnectionStringName"] = "WriteDb",
                ["Engine:Databases:Outbox:Provider"] = "PostgreSql",
                ["Engine:Databases:Outbox:ConnectionStringName"] = "WriteDb",
                ["Engine:Databases:Migrations:ApplyOnStartup"] = "true",
                ["Engine:Databases:Migrations:Targets:0"] = "outbox"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddCephalon(engine =>
        {
            engine.UseSettings(EngineSettings.FromConfiguration(configuration));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new EntityFrameworkOutboxTestModule());
            engine.AddData();
            engine.AddEntityFrameworkData<OutboxCatalogDbContext>(
                configureDbContext: (_, options) => options.UseInMemoryDatabase(databaseName),
                configure: options => options.RegisterOutbox = true);
        });

        using var provider = services.BuildServiceProvider();
        var hostedService = Assert.Single(
            provider.GetServices<IHostedService>(),
            service => string.Equals(
                service.GetType().Name,
                "EntityFrameworkDatabaseMigrationHostedService",
                StringComparison.Ordinal));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            hostedService.StartAsync(CancellationToken.None));

        Assert.Contains("outbox", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("write", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AddEntityFrameworkDataRegistersSharedDbContextForReadAndWriteHandlers()
    {
        var databaseName = $"cephalon-data-ef-single-{Guid.NewGuid():N}";
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS"],
                data: new DataSettings(provider: "EntityFramework")));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new EntityFrameworkSingleContextTestModule());
            engine.AddData();
            engine.AddEntityFrameworkData<SingleCatalogDbContext>(
                options => options.UseInMemoryDatabase(databaseName));
        });

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var writeStore = scope.ServiceProvider.GetRequiredService<IWriteStore>();
        var readStore = scope.ServiceProvider.GetRequiredService<IReadStore>();

        await writeStore.ExecuteAsync(new CreateSingleCatalogItemCommand("item-001", "Ada"));
        var count = await readStore.ExecuteAsync(new CountSingleCatalogItemsQuery());
        var runtime = provider.GetRequiredService<Cephalon.Engine.Runtime.IRuntime>();

        Assert.Equal(1, count);
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "data.entity-framework");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "data.dbcontext.read");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "data.dbcontext.write");
    }

    [Fact]
    public async Task AddEntityFrameworkDataSupportsSeparateReadAndWriteDbContexts()
    {
        var readDatabaseName = $"cephalon-data-ef-read-{Guid.NewGuid():N}";
        var writeDatabaseName = $"cephalon-data-ef-write-{Guid.NewGuid():N}";
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "Microservice",
                patterns: ["CQRS"],
                data: new DataSettings(
                    provider: "EntityFramework",
                    readWriteSplit: true)));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new EntityFrameworkSplitContextTestModule());
            engine.AddData();
            engine.AddEntityFrameworkData<SplitCatalogReadDbContext, SplitCatalogWriteDbContext>(
                configureReadDbContext: options => options.UseInMemoryDatabase(readDatabaseName),
                configureWriteDbContext: options => options.UseInMemoryDatabase(writeDatabaseName));
        });

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var writeStore = scope.ServiceProvider.GetRequiredService<IWriteStore>();
        var readStore = scope.ServiceProvider.GetRequiredService<IReadStore>();
        var readDbContext = scope.ServiceProvider.GetRequiredService<SplitCatalogReadDbContext>();
        var writeDbContext = scope.ServiceProvider.GetRequiredService<SplitCatalogWriteDbContext>();

        await writeStore.ExecuteAsync(new CreateSplitWriteCatalogItemCommand("write-001", "Grace"));
        readDbContext.CatalogItems.Add(new SplitCatalogReadItem
        {
            Id = "read-001",
            Name = "Grace"
        });
        await readDbContext.SaveChangesAsync();

        var readCount = await readStore.ExecuteAsync(new CountSplitReadCatalogItemsQuery());
        var writeCount = await writeDbContext.CatalogItems.CountAsync();

        Assert.Equal(1, readCount);
        Assert.Equal(1, writeCount);
        Assert.NotEqual(readDbContext.GetType(), writeDbContext.GetType());
    }

    [Fact]
    public void AddEntityFrameworkDataPublishesProviderAndDbContextMetadata()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "Microservice",
                patterns: ["CQRS"],
                data: new DataSettings(
                    provider: "EntityFramework",
                    readWriteSplit: true)));
            engine.AddModule(new PlatformTestModule());
            engine.AddEntityFrameworkData<SplitCatalogReadDbContext, SplitCatalogWriteDbContext>(
                configureReadDbContext: options => options.UseInMemoryDatabase($"cephalon-data-ef-read-meta-{Guid.NewGuid():N}"),
                configureWriteDbContext: options => options.UseInMemoryDatabase($"cephalon-data-ef-write-meta-{Guid.NewGuid():N}"));
        });

        using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<Cephalon.Engine.Runtime.IRuntime>();
        var providerCapability = Assert.Single(runtime.Manifest.Capabilities, capability => capability.Key == "data.entity-framework");
        var readCapability = Assert.Single(runtime.Manifest.Capabilities, capability => capability.Key == "data.dbcontext.read");
        var writeCapability = Assert.Single(runtime.Manifest.Capabilities, capability => capability.Key == "data.dbcontext.write");

        Assert.Equal(EntityFrameworkDataOptions.ProviderId, providerCapability.Metadata["provider"]);
        Assert.Equal("true", providerCapability.Metadata["readWriteSplit"]);
        Assert.Equal(typeof(SplitCatalogReadDbContext).FullName, providerCapability.Metadata["readDbContext"]);
        Assert.Equal(typeof(SplitCatalogWriteDbContext).FullName, providerCapability.Metadata["writeDbContext"]);
        Assert.Equal("read", readCapability.Metadata["role"]);
        Assert.Equal(typeof(SplitCatalogReadDbContext).FullName, readCapability.Metadata["dbContext"]);
        Assert.Equal("write", writeCapability.Metadata["role"]);
        Assert.Equal(typeof(SplitCatalogWriteDbContext).FullName, writeCapability.Metadata["dbContext"]);
    }

    [Fact]
    public async Task AddEntityFrameworkDataCanRegisterAnEfBackedOutbox()
    {
        var databaseName = $"cephalon-data-ef-outbox-{Guid.NewGuid():N}";
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS", "Outbox"],
                data: new DataSettings(
                    provider: "EntityFramework",
                    outboxEnabled: true)));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new EntityFrameworkOutboxTestModule());
            engine.AddData();
            engine.AddEntityFrameworkData<OutboxCatalogDbContext>(
                options => options.UseInMemoryDatabase(databaseName),
                configure: options => options.RegisterOutbox = true);
        });

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var writeStore = scope.ServiceProvider.GetRequiredService<IWriteStore>();
        var dbContext = scope.ServiceProvider.GetRequiredService<OutboxCatalogDbContext>();
        var outboxCatalog = provider.GetRequiredService<IOutboxCatalog>();
        var runtime = provider.GetRequiredService<Cephalon.Engine.Runtime.IRuntime>();

        await writeStore.ExecuteAsync(new CreateOutboxCatalogItemCommand("item-100", "Lin"));

        var catalogItem = await dbContext.CatalogItems.SingleAsync();
        var outboxEntry = await dbContext.OutboxMessages.SingleAsync();
        var outboxDescriptor = Assert.Single(outboxCatalog.Outboxes);

        Assert.Equal("item-100", catalogItem.Id);
        Assert.Equal("catalog-events", outboxEntry.ChannelId);
        Assert.Equal("catalog.item.created", outboxEntry.MessageType);
        Assert.Equal("entity-framework-outbox", outboxDescriptor.Id);
        Assert.Equal("entity-framework", outboxDescriptor.Provider);
        Assert.Equal("transactional-table", outboxDescriptor.Mode);
        Assert.Equal("not-configured", outboxDescriptor.Metadata["dispatchRuntime"]);
        Assert.Equal("disabled", outboxDescriptor.DispatchPolicy.PolicyId);
        Assert.Equal("disabled", outboxDescriptor.DispatchPolicy.ExecutionMode);
        Assert.Equal("not-configured", outboxDescriptor.Metadata["dispatchStore"]);
        Assert.Equal("false", outboxDescriptor.Metadata["eventingLinked"]);
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "data.outbox");
    }

    [Fact]
    public async Task AddEntityFrameworkDataCanRegisterAnEfBackedInbox()
    {
        var databaseName = $"cephalon-data-ef-inbox-{Guid.NewGuid():N}";
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS"],
                data: new DataSettings(provider: "EntityFramework")));
            engine.AddModule(new PlatformTestModule());
            engine.AddData();
            engine.AddEntityFrameworkData<InboxCatalogDbContext>(
                options => options.UseInMemoryDatabase(databaseName),
                configure: options => options.RegisterInbox = true);
        });

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var inbox = scope.ServiceProvider.GetRequiredService<IInbox>();
        var dbContext = scope.ServiceProvider.GetRequiredService<InboxCatalogDbContext>();
        var inboxCatalog = provider.GetRequiredService<IInboxCatalog>();
        var runtime = provider.GetRequiredService<Cephalon.Engine.Runtime.IRuntime>();
        var message = new InboxMessage(
            id: "evt-100",
            channelId: "catalog-events",
            messageType: "catalog.item.created",
            payload: "{\"id\":\"item-100\"}",
            receivedAtUtc: DateTimeOffset.UtcNow,
            contentType: "application/json",
            correlationId: "corr-100",
            tenantId: "tenant-100");

        Assert.False(await inbox.HasProcessedAsync("evt-100"));

        await inbox.MarkProcessedAsync(message);
        await inbox.MarkProcessedAsync(message);

        var inboxEntry = await dbContext.InboxMessages.SingleAsync();
        var inboxDescriptor = Assert.Single(inboxCatalog.Inboxes);

        Assert.True(await inbox.HasProcessedAsync("evt-100"));
        Assert.Equal("evt-100", inboxEntry.Id);
        Assert.Equal("catalog-events", inboxEntry.ChannelId);
        Assert.Equal("catalog.item.created", inboxEntry.MessageType);
        Assert.Equal("corr-100", inboxEntry.CorrelationId);
        Assert.Equal("entity-framework-inbox", inboxDescriptor.Id);
        Assert.Equal("entity-framework", inboxDescriptor.Provider);
        Assert.Equal("processed-message-table", inboxDescriptor.Mode);
        Assert.Equal("message-id", inboxDescriptor.Metadata["idempotency"]);
        Assert.Equal("dynamic", inboxDescriptor.Metadata["channelMode"]);
        Assert.Equal("not-configured", inboxDescriptor.Metadata["subscriptionRuntime"]);
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "data.inbox");
    }

    [Fact]
    public void AddEntityFrameworkDataThrowsWhenOutboxIsEnabledWithoutOutboxContext()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddCephalon(engine =>
            {
                engine.UseSettings(new EngineSettings(
                    blueprint: "ModularMonolith",
                    patterns: ["Outbox"],
                    data: new DataSettings(
                        provider: "EntityFramework",
                        outboxEnabled: true)));
                engine.AddModule(new PlatformTestModule());
                engine.AddEntityFrameworkData<SingleCatalogDbContext>(
                    options => options.UseInMemoryDatabase($"cephalon-data-ef-invalid-outbox-{Guid.NewGuid():N}"),
                    configure: options => options.RegisterOutbox = true);
            }));

        Assert.Contains(nameof(Cephalon.Data.EntityFramework.Modeling.IEntityFrameworkOutboxContext), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddEntityFrameworkDataThrowsWhenInboxIsEnabledWithoutInboxContext()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddCephalon(engine =>
            {
                engine.UseSettings(new EngineSettings(
                    blueprint: "ModularMonolith",
                    patterns: ["CQRS"],
                    data: new DataSettings(provider: "EntityFramework")));
                engine.AddModule(new PlatformTestModule());
                engine.AddEntityFrameworkData<SingleCatalogDbContext>(
                    options => options.UseInMemoryDatabase($"cephalon-data-ef-invalid-inbox-{Guid.NewGuid():N}"),
                    configure: options => options.RegisterInbox = true);
            }));

        Assert.Contains(nameof(Cephalon.Data.EntityFramework.Modeling.IEntityFrameworkInboxContext), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AddEntityFrameworkDataCanUseOfficialSfidEntityFrameworkIntegration()
    {
        var databaseName = $"cephalon-data-ef-sfid-{Guid.NewGuid():N}";
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "Microservice",
                patterns: ["CQRS"],
                data: new DataSettings(
                    provider: "EntityFramework",
                    idGenerator: "Sfid")));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new EntityFrameworkSfidTestModule());
            engine.AddData();
            engine.AddSfidIds(options =>
            {
                options.DatacenterId = 4;
                options.WorkerId = 12;
            });
            engine.AddEntityFrameworkData<SfidCatalogDbContext>(
                options => options.UseInMemoryDatabase(databaseName),
                configure: options => options.EnableSfidIdentifiers = true);
        });

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var writeStore = scope.ServiceProvider.GetRequiredService<IWriteStore>();
        var dbContext = scope.ServiceProvider.GetRequiredService<SfidCatalogDbContext>();
        var runtime = provider.GetRequiredService<Cephalon.Engine.Runtime.IRuntime>();

        var id = await writeStore.ExecuteAsync(new CreateSfidCatalogItemCommand("Turing"));
        var entity = await dbContext.CatalogItems.SingleAsync();
        var providerCapability = Assert.Single(runtime.Manifest.Capabilities, capability => capability.Key == "data.entity-framework");

        Assert.True(id.Value > 0);
        Assert.Equal(id, entity.Id);
        Assert.Equal("sfid", providerCapability.Metadata["idStrategy"]);
    }

    [Fact]
    public void AddEntityFrameworkDataRejectsSfidIntegrationWhenConfiguredIdStrategyDoesNotMatch()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "Microservice",
                patterns: ["CQRS"],
                data: new DataSettings(
                    provider: "EntityFramework",
                    idGenerator: "Ulid")));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new EntityFrameworkSfidTestModule());
            engine.AddSfidIds(options =>
            {
                options.DatacenterId = 4;
                options.WorkerId = 12;
            });
            engine.AddEntityFrameworkData<SfidCatalogDbContext>(
                options => options.UseInMemoryDatabase($"cephalon-data-ef-sfid-mismatch-{Guid.NewGuid():N}"),
                configure: options => options.EnableSfidIdentifiers = true);
        });

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            scope.ServiceProvider.GetRequiredService<SfidCatalogDbContext>());

        Assert.Contains("Ulid", exception.Message, StringComparison.Ordinal);
        Assert.Contains("EnableSfidIdentifiers", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AddEntityFrameworkDataProjectsItsOutboxThroughEventDrivenTechnologySurfaces()
    {
        var databaseName = $"cephalon-data-ef-outbox-surface-{Guid.NewGuid():N}";
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS", "Outbox"],
                technologies: ["EventDrivenIntegration"],
                transports: ["RestApi"],
                data: new DataSettings(
                    provider: "EntityFramework",
                    outboxEnabled: true),
                messaging: new MessagingSettings(provider: "InMemoryChannels")));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new EntityFrameworkOutboxTestModule());
            engine.AddEventing(options =>
            {
                options.Channels.Add(new EventChannelDescriptor(
                    id: "catalog-events",
                    displayName: "Catalog Events",
                    description: "Catalog integration events."));
            });
            engine.AddEntityFrameworkData<OutboxCatalogDbContext>(
                options => options.UseInMemoryDatabase(databaseName),
                configure: options => options.RegisterOutbox = true);
        });

        await using var provider = services.BuildServiceProvider();
        var technologyCatalog = provider.GetRequiredService<global::Cephalon.Abstractions.Technologies.ITechnologyRuntimeCatalog>();
        var runtime = provider.GetRequiredService<Cephalon.Engine.Runtime.IRuntime>();
        var eventingSurfaces = technologyCatalog.GetByTechnology("event-driven-integration");

        Assert.Equal(6, eventingSurfaces.Count);
        var outboxSurface = Assert.Single(eventingSurfaces, surface => surface.SurfaceId == "outbox-producers");
        var outboxEntry = Assert.Single(outboxSurface.Entries);
        var publishSurface = Assert.Single(eventingSurfaces, surface => surface.SurfaceId == "event-publishers");
        var publisherEntry = Assert.Single(publishSurface.Entries);
        var dispatchSurface = Assert.Single(eventingSurfaces, surface => surface.SurfaceId == "event-dispatches");
        var dispatchEntry = Assert.Single(dispatchSurface.Entries);
        var remediationCommandSurface = Assert.Single(eventingSurfaces, surface => surface.SurfaceId == "event-dispatch-remediation-commands");
        var remediationCommandEntry = Assert.Single(remediationCommandSurface.Entries);
        var profileSurface = Assert.Single(eventingSurfaces, surface => surface.SurfaceId == "eventing-superiority-profile");
        var durableAuditEntry = Assert.Single(profileSurface.Entries, entry => entry.Id == "durable-remediation-command-audit");
        Assert.Equal("entity-framework-outbox", outboxEntry.Id);
        Assert.Equal("entity-framework", outboxEntry.Metadata["provider"]);
        Assert.Equal("transactional-table", outboxEntry.Metadata["mode"]);
        Assert.Equal("entity-framework-data", outboxEntry.Metadata["sourceModuleId"]);
        Assert.Equal("dynamic", outboxEntry.Metadata["channelMode"]);
        Assert.Equal("consumer-managed", outboxEntry.Metadata["dispatchPolicyId"]);
        Assert.Equal("consumer-managed", outboxEntry.Metadata["dispatchExecutionMode"]);
        Assert.Equal("outbox-backed-publisher", publisherEntry.Id);
        Assert.Equal("outbox", publisherEntry.Metadata["handoff"]);
        Assert.Equal("not-configured", publisherEntry.Metadata["dispatchRuntime"]);
        Assert.Equal("available", publisherEntry.Metadata["dispatchStore"]);
        Assert.Equal("1", publisherEntry.Metadata["consumerManagedOutboxCount"]);
        Assert.Equal("0", publisherEntry.Metadata["managedOutboxCount"]);
        Assert.Equal("catalog-events", publisherEntry.Metadata["channelIds"]);
        Assert.Equal("entity-framework-outbox", dispatchEntry.Id);
        Assert.Equal("available", dispatchEntry.Metadata["dispatchStore"]);
        Assert.Equal("not-reported", dispatchEntry.Metadata["runtimeState"]);
        Assert.Equal("entity-framework", dispatchEntry.Metadata["provider"]);
        Assert.Equal("consumer-managed", dispatchEntry.Metadata["dispatchPolicyId"]);
        Assert.Equal("consumer-managed", dispatchEntry.Metadata["dispatchExecutionMode"]);
        Assert.Equal("event-dispatch-remediation-commands", remediationCommandEntry.Id);
        Assert.Equal("durable", remediationCommandEntry.Metadata["commandJournalDurability"]);
        Assert.Equal("cross-node", remediationCommandEntry.Metadata["commandJournalScope"]);
        Assert.Equal("Cephalon.Data.EntityFramework", remediationCommandEntry.Metadata["commandJournalProvider"]);
        Assert.Equal("entity-framework-table", remediationCommandEntry.Metadata["commandJournalStorage"]);
        Assert.Equal("true", remediationCommandEntry.Metadata["commandCrossNodeCommandAudit"]);
        Assert.Equal("durable", remediationCommandEntry.Metadata["commandJournalReplayCursor"]);
        Assert.Equal("oldest-first-observed-utc-command-id", remediationCommandEntry.Metadata["commandJournalReplayCursorOrder"]);
        Assert.Equal("command-journal", remediationCommandEntry.Metadata["commandJournalReplayCursorScope"]);
        Assert.Equal("claimed", durableAuditEntry.Metadata["status"]);
        Assert.Contains("provider=Cephalon.Data.EntityFramework", durableAuditEntry.Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("durability=durable", durableAuditEntry.Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("scope=cross-node", durableAuditEntry.Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("crossNodeCommandAudit=true", durableAuditEntry.Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("replayCursor=durable", durableAuditEntry.Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.DoesNotContain(eventingSurfaces, surface => surface.SurfaceId == "event-subscriptions");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "eventing.publish" && capability.Metadata["runtimeState"] == "available");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "eventing.publish" && capability.Metadata["dispatchStore"] == "available");
        Assert.Contains(
            runtime.Manifest.Capabilities,
            capability => capability.Key == "data.entity-framework.event-dispatch-remediation-command-journal" &&
                capability.Metadata["journalDurability"] == "durable" &&
                capability.Metadata["journalScope"] == "cross-node" &&
                capability.Metadata["crossNodeCommandAudit"] == "true" &&
                capability.Metadata["journalReplayCursor"] == "durable" &&
                capability.Metadata["journalReplayCursorOrder"] == "oldest-first-observed-utc-command-id" &&
                capability.Metadata["journalReplayCursorScope"] == "command-journal");
    }

    [Fact]
    public void AddEntityFrameworkDataProjectsItsInboxThroughEventDrivenTechnologySurfaces()
    {
        var databaseName = $"cephalon-data-ef-inbox-surface-{Guid.NewGuid():N}";
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS"],
                technologies: ["EventDrivenIntegration"],
                transports: ["RestApi"],
                data: new DataSettings(provider: "EntityFramework"),
                messaging: new MessagingSettings(provider: "InMemoryChannels")));
            engine.AddModule(new PlatformTestModule());
            engine.AddEventing(options =>
            {
                options.Channels.Add(new EventChannelDescriptor(
                    id: "catalog-events",
                    displayName: "Catalog Events",
                    description: "Catalog integration events."));
                options.Subscriptions.Add(new EventSubscriptionDescriptor(
                    id: "catalog-projector",
                    displayName: "Catalog Projector",
                    description: "Projects catalog integration events into read models.",
                    channelId: "catalog-events",
                    handlerId: "catalog-read-model-projector",
                    deliveryMode: "background-service"));
            });
            engine.AddEntityFrameworkData<InboxCatalogDbContext>(
                options => options.UseInMemoryDatabase(databaseName),
                configure: options => options.RegisterInbox = true);
        });

        using var provider = services.BuildServiceProvider();
        var technologyCatalog = provider.GetRequiredService<global::Cephalon.Abstractions.Technologies.ITechnologyRuntimeCatalog>();
        var runtime = provider.GetRequiredService<Cephalon.Engine.Runtime.IRuntime>();
        var eventingSurfaces = technologyCatalog.GetByTechnology("event-driven-integration");

        Assert.Equal(4, eventingSurfaces.Count);
        var inboxSurface = Assert.Single(eventingSurfaces, surface => surface.SurfaceId == "inbox-stores");
        var inboxEntry = Assert.Single(inboxSurface.Entries);
        var subscriptionSurface = Assert.Single(eventingSurfaces, surface => surface.SurfaceId == "event-subscriptions");
        var subscriptionEntry = Assert.Single(subscriptionSurface.Entries);
        Assert.Equal("entity-framework-inbox", inboxEntry.Id);
        Assert.Equal("entity-framework", inboxEntry.Metadata["provider"]);
        Assert.Equal("processed-message-table", inboxEntry.Metadata["mode"]);
        Assert.Equal("entity-framework-data", inboxEntry.Metadata["sourceModuleId"]);
        Assert.Equal("dynamic", inboxEntry.Metadata["channelMode"]);
        Assert.Equal("message-id", inboxEntry.Metadata["idempotency"]);
        Assert.Equal("not-configured", inboxEntry.Metadata["subscriptionRuntime"]);
        Assert.Equal("available", subscriptionEntry.Metadata["inbox"]);
        Assert.Equal("application-managed", subscriptionEntry.Metadata["inboxLink"]);
        Assert.Equal("entity-framework-inbox", subscriptionEntry.Metadata["inboxIds"]);
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "data.inbox");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "eventing.subscriptions" && capability.Metadata["inbox"] == "available");
        Assert.DoesNotContain(runtime.Manifest.Capabilities, capability => capability.Key == "eventing.publish");
    }

    [Fact]
    public void AddEntityFrameworkDataProjectsItsProjectionsThroughDataManagementTechnologySurfaces()
    {
        var databaseName = $"cephalon-data-ef-projection-surface-{Guid.NewGuid():N}";
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS"],
                transports: ["RestApi"],
                data: new DataSettings(provider: "EntityFramework")));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new EntityFrameworkSingleContextTestModule());
            engine.AddEntityFrameworkData<SingleCatalogDbContext>(
                options => options.UseInMemoryDatabase(databaseName),
                configure: options => options.RegisterProjections = true);
        });

        using var provider = services.BuildServiceProvider();
        var projectionCatalog = provider.GetRequiredService<IProjectionCatalog>();
        var technologyCatalog = provider.GetRequiredService<global::Cephalon.Abstractions.Technologies.ITechnologyRuntimeCatalog>();
        var runtime = provider.GetRequiredService<Cephalon.Engine.Runtime.IRuntime>();
        var dataManagementSurfaces = technologyCatalog.GetByTechnology("data-management");

        var projection = Assert.Single(projectionCatalog.GetBySourceModule("entity-framework-data"));
        Assert.Equal("entity-framework-projections", projection.Id);
        Assert.Equal("entity-framework-read-store", projection.TargetStoreId);
        Assert.Equal("application-managed", projection.Mode);
        Assert.Equal("application-managed", projection.Metadata["projectionRuntime"]);

        var projectionsSurface = Assert.Single(dataManagementSurfaces, surface => surface.SurfaceId == "projections");
        var projectionEntry = Assert.Single(projectionsSurface.Entries);
        Assert.Equal(projection.Id, projectionEntry.Id);
        Assert.Equal("entity-framework-read-store", projectionEntry.Metadata["targetStoreId"]);
        Assert.Equal("entity-framework-data", projectionEntry.Metadata["sourceModuleId"]);
        Assert.Equal("application-managed", projectionEntry.Metadata["mode"]);
        Assert.Equal("application-managed", projectionEntry.Metadata["projectionRuntime"]);
        Assert.Equal("entity-framework", projectionEntry.Metadata["provider"]);
        Assert.Equal("Cephalon.Data.EntityFramework", projectionEntry.Metadata["pack"]);
        Assert.Contains("cqrs", projectionEntry.Metadata["tags"], StringComparison.Ordinal);
        Assert.Contains(
            runtime.Manifest.Capabilities,
            capability => capability.Key == "data.projections.entity-framework" &&
                capability.Metadata["projectionRuntime"] == "application-managed");
    }

    [Fact]
    public async Task AddEventingCanStagePublicationsThroughOutboxBackedPublisher()
    {
        var databaseName = $"cephalon-data-ef-event-publisher-{Guid.NewGuid():N}";
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS", "Outbox"],
                technologies: ["EventDrivenIntegration"],
                transports: ["RestApi"],
                data: new DataSettings(
                    provider: "EntityFramework",
                    outboxEnabled: true),
                messaging: new MessagingSettings(provider: "InMemoryChannels")));
            engine.AddModule(new PlatformTestModule());
            engine.AddEventing(options =>
            {
                options.Channels.Add(new EventChannelDescriptor(
                    id: "catalog-events",
                    displayName: "Catalog Events",
                    description: "Catalog integration events."));
            });
            engine.AddEntityFrameworkData<OutboxCatalogDbContext>(
                options => options.UseInMemoryDatabase(databaseName),
                configure: options => options.RegisterOutbox = true);
        });

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();
        var dbContext = scope.ServiceProvider.GetRequiredService<OutboxCatalogDbContext>();

        await publisher.PublishAsync(new EventPublication(
            id: "evt-001",
            channelId: "catalog-events",
            eventType: "catalog.item.created",
            payload: "{\"id\":\"item-001\"}",
            occurredAtUtc: DateTimeOffset.UtcNow,
            correlationId: "corr-001"));

        var outboxEntry = await dbContext.OutboxMessages.SingleAsync();

        Assert.Equal("evt-001", outboxEntry.Id);
        Assert.Equal("catalog-events", outboxEntry.ChannelId);
        Assert.Equal("catalog.item.created", outboxEntry.MessageType);
        Assert.Equal("corr-001", outboxEntry.CorrelationId);
    }

    [Fact]
    public async Task AddEventingReportsEventDispatchRemediationCommandRetentionTruncation()
    {
        var databaseName = $"cephalon-data-ef-event-dispatch-command-retention-{Guid.NewGuid():N}";
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS", "Outbox"],
                technologies: ["EventDrivenIntegration"],
                transports: ["RestApi"],
                data: new DataSettings(
                    provider: "EntityFramework",
                    outboxEnabled: true),
                messaging: new MessagingSettings(provider: "InMemoryChannels")));
            engine.AddModule(new PlatformTestModule());
            engine.AddEventing(options =>
            {
                options.RemediationCommandHistoryLimit = 2;
                options.Channels.Add(new EventChannelDescriptor(
                    id: "catalog-events",
                    displayName: "Catalog Events",
                    description: "Catalog integration events."));
            });
            engine.AddEntityFrameworkData<OutboxCatalogDbContext>(
                options => options.UseInMemoryDatabase(databaseName),
                configure: options => options.RegisterOutbox = true);
        });

        await using var provider = services.BuildServiceProvider();
        using (var publicationScope = provider.CreateScope())
        {
            var publisher = publicationScope.ServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(new EventPublication(
                id: "evt-retention-001",
                channelId: "catalog-events",
                eventType: "catalog.item.retention",
                payload: "{\"id\":\"item-retention-001\"}",
                occurredAtUtc: new DateTimeOffset(2026, 04, 13, 10, 0, 0, TimeSpan.Zero),
                correlationId: "corr-retention-001"));
        }

        using (var commandScope = provider.CreateScope())
        {
            var dispatcher = commandScope.ServiceProvider.GetRequiredService<IEventDispatchRemediationDispatcher>();
            var retryResult = await dispatcher.DispatchAsync(new EventDispatchRemediationRequest(
                outboxId: "entity-framework-outbox",
                messageId: "evt-retention-001",
                channelId: "catalog-events",
                operationId: EventDispatchRemediationOperationIds.RetryNow,
                commandId: "cmd-retention-001-retry",
                requestedAtUtc: new DateTimeOffset(2026, 04, 13, 10, 1, 0, TimeSpan.Zero),
                reason: "First retained candidate.",
                actorId: "operator-retention",
                correlationId: "corr-retention-command-001"));
            var deadLetterResult = await dispatcher.DispatchAsync(new EventDispatchRemediationRequest(
                outboxId: "entity-framework-outbox",
                messageId: "evt-retention-001",
                channelId: "catalog-events",
                operationId: EventDispatchRemediationOperationIds.DeadLetter,
                commandId: "cmd-retention-002-dead-letter",
                requestedAtUtc: new DateTimeOffset(2026, 04, 13, 10, 2, 0, TimeSpan.Zero),
                reason: "Retained oldest after truncation.",
                actorId: "operator-retention",
                correlationId: "corr-retention-command-002"));
            var rejectedResult = await dispatcher.DispatchAsync(new EventDispatchRemediationRequest(
                outboxId: "entity-framework-outbox",
                messageId: "evt-retention-001",
                channelId: "catalog-events",
                operationId: EventDispatchRemediationOperationIds.RetryLater,
                commandId: "cmd-retention-003-retry-later-rejected",
                requestedAtUtc: new DateTimeOffset(2026, 04, 13, 10, 3, 0, TimeSpan.Zero),
                reason: "Missing next attempt should still be retained.",
                actorId: "operator-retention",
                correlationId: "corr-retention-command-003"));

            Assert.Equal(EventDispatchRemediationOutcomes.Accepted, retryResult.Outcome);
            Assert.Equal(EventDispatchRemediationOutcomes.Accepted, deadLetterResult.Outcome);
            Assert.Equal(EventDispatchRemediationOutcomes.Rejected, rejectedResult.Outcome);
        }

        var remediationCommandCatalog = provider.GetRequiredService<IEventDispatchRemediationRuntimeCatalog>();
        var retention = remediationCommandCatalog.Retention;

        Assert.Equal(2, retention.HistoryLimit);
        Assert.Equal(2, retention.RetainedCommandCount);
        Assert.Equal(3, retention.TotalRecordedCommandCount);
        Assert.Equal(1, retention.DroppedCommandCount);
        Assert.True(retention.Truncated);
        Assert.Equal("cmd-retention-002-dead-letter", retention.OldestRetainedCommandId);
        Assert.Equal(new DateTimeOffset(2026, 04, 13, 10, 2, 0, TimeSpan.Zero), retention.OldestRetainedObservedAtUtc);
        Assert.Equal("cmd-retention-003-retry-later-rejected", retention.LatestRetainedCommandId);
        Assert.Equal(new DateTimeOffset(2026, 04, 13, 10, 3, 0, TimeSpan.Zero), retention.LatestRetainedObservedAtUtc);
        Assert.Null(remediationCommandCatalog.GetByCommandId("cmd-retention-001-retry"));
        Assert.Equal(
            ["cmd-retention-003-retry-later-rejected", "cmd-retention-002-dead-letter"],
            remediationCommandCatalog.States.Select(state => state.CommandId).ToArray());
        Assert.Equal(
            ["cmd-retention-003-retry-later-rejected", "cmd-retention-002-dead-letter"],
            remediationCommandCatalog.GetByObservedAt(
                    new DateTimeOffset(2026, 04, 13, 10, 2, 0, TimeSpan.Zero),
                    new DateTimeOffset(2026, 04, 13, 10, 3, 0, TimeSpan.Zero))
                .Select(state => state.CommandId)
                .ToArray());
        var observationSummary = remediationCommandCatalog.GetSummaryByObservedAt(
            new DateTimeOffset(2026, 04, 13, 10, 2, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 04, 13, 10, 3, 0, TimeSpan.Zero));
        Assert.Equal(2, observationSummary.TotalCommandCount);
        Assert.Equal(1, observationSummary.AcceptedCount);
        Assert.Equal(1, observationSummary.RejectedCount);
        Assert.Equal(1, observationSummary.ErrorCount);
        Assert.Equal(0, observationSummary.ReservedCount);
        Assert.Equal("cmd-retention-003-retry-later-rejected", observationSummary.LastCommandId);
        Assert.Equal(1, observationSummary.DroppedCommandCount);
        Assert.True(observationSummary.RetentionTruncated);
        Assert.False(observationSummary.SummaryMayBeIncomplete);
        Assert.Equal("cmd-retention-002-dead-letter", observationSummary.OldestRetainedCommandId);
        Assert.Equal(new DateTimeOffset(2026, 04, 13, 10, 2, 0, TimeSpan.Zero), observationSummary.OldestRetainedObservedAtUtc);
        Assert.Null(observationSummary.OldestReservedCommandId);
        Assert.Null(observationSummary.OldestReservedObservedAtUtc);
        Assert.True(observationSummary.HasCommands);
        Assert.True(observationSummary.HasFailures);
        Assert.False(observationSummary.HasInDoubtCommands);
        Assert.Equal(
            ["cmd-retention-002-dead-letter"],
            remediationCommandCatalog.GetByObservedAt(
                    null,
                    new DateTimeOffset(2026, 04, 13, 10, 2, 30, TimeSpan.Zero))
                .Select(state => state.CommandId)
                .ToArray());
        Assert.Empty(remediationCommandCatalog.GetByObservedAt(
            null,
            new DateTimeOffset(2026, 04, 13, 10, 1, 30, TimeSpan.Zero)));
        var emptyObservationSummary = remediationCommandCatalog.GetSummaryByObservedAt(
            null,
            new DateTimeOffset(2026, 04, 13, 10, 1, 30, TimeSpan.Zero));
        Assert.Equal(0, emptyObservationSummary.TotalCommandCount);
        Assert.Equal(1, emptyObservationSummary.DroppedCommandCount);
        Assert.True(emptyObservationSummary.RetentionTruncated);
        Assert.True(emptyObservationSummary.SummaryMayBeIncomplete);
        Assert.Equal("cmd-retention-002-dead-letter", emptyObservationSummary.OldestRetainedCommandId);
        Assert.Equal(new DateTimeOffset(2026, 04, 13, 10, 2, 0, TimeSpan.Zero), emptyObservationSummary.OldestRetainedObservedAtUtc);
        Assert.Null(emptyObservationSummary.OldestReservedCommandId);
        Assert.Null(emptyObservationSummary.OldestReservedObservedAtUtc);
        Assert.False(emptyObservationSummary.HasCommands);
        Assert.False(emptyObservationSummary.HasFailures);
        Assert.False(emptyObservationSummary.HasInDoubtCommands);
        Assert.Throws<ArgumentException>(() => remediationCommandCatalog.GetByObservedAt(
            new DateTimeOffset(2026, 04, 13, 10, 3, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 04, 13, 10, 2, 0, TimeSpan.Zero)));
        Assert.Throws<ArgumentException>(() => remediationCommandCatalog.GetSummaryByObservedAt(
            new DateTimeOffset(2026, 04, 13, 10, 3, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 04, 13, 10, 2, 0, TimeSpan.Zero)));
        var retainedSummary = remediationCommandCatalog.Summary;
        Assert.Equal(2, retainedSummary.TotalCommandCount);
        Assert.Equal(1, retainedSummary.AcceptedCount);
        Assert.Equal(1, retainedSummary.RejectedCount);
        Assert.Equal(1, retainedSummary.ErrorCount);
        Assert.Equal(0, retainedSummary.ReservedCount);
        Assert.Equal(1, retainedSummary.DroppedCommandCount);
        Assert.True(retainedSummary.RetentionTruncated);
        Assert.True(retainedSummary.SummaryMayBeIncomplete);
        Assert.Equal("cmd-retention-002-dead-letter", retainedSummary.OldestRetainedCommandId);
        Assert.Equal(new DateTimeOffset(2026, 04, 13, 10, 2, 0, TimeSpan.Zero), retainedSummary.OldestRetainedObservedAtUtc);
        Assert.Null(retainedSummary.OldestReservedCommandId);
        Assert.Null(retainedSummary.OldestReservedObservedAtUtc);
        Assert.False(retainedSummary.HasInDoubtCommands);
    }

    [Fact]
    public async Task AddEntityFrameworkDataPersistsEventDispatchRemediationCommandJournalAcrossProviderRebuilds()
    {
        var databaseName = $"cephalon-data-ef-event-dispatch-command-journal-{Guid.NewGuid():N}";
        var databaseRoot = new Microsoft.EntityFrameworkCore.Storage.InMemoryDatabaseRoot();

        ServiceProvider BuildProvider()
        {
            var services = new ServiceCollection();
            services.AddCephalon(engine =>
            {
                engine.UseSettings(new EngineSettings(
                    blueprint: "ModularVerticalSlice",
                    patterns: ["CQRS", "Outbox"],
                    technologies: ["EventDrivenIntegration"],
                    transports: ["RestApi"],
                    data: new DataSettings(
                        provider: "EntityFramework",
                        outboxEnabled: true),
                    messaging: new MessagingSettings(provider: "InMemoryChannels")));
                engine.AddModule(new PlatformTestModule());
                engine.AddEventing(options =>
                {
                    options.Channels.Add(new EventChannelDescriptor(
                        id: "catalog-events",
                        displayName: "Catalog Events",
                        description: "Catalog integration events."));
                });
                engine.AddEntityFrameworkData<OutboxCatalogDbContext>(
                    options => options.UseInMemoryDatabase(databaseName, databaseRoot),
                    configure: options => options.RegisterOutbox = true);
            });

            return services.BuildServiceProvider();
        }

        await using (var provider = BuildProvider())
        {
            using (var publicationScope = provider.CreateScope())
            {
                var publisher = publicationScope.ServiceProvider.GetRequiredService<IEventPublisher>();
                await publisher.PublishAsync(new EventPublication(
                    id: "evt-journal-001",
                    channelId: "catalog-events",
                    eventType: "catalog.item.journaled",
                    payload: "{\"id\":\"item-journal-001\"}",
                    occurredAtUtc: new DateTimeOffset(2026, 04, 14, 10, 0, 0, TimeSpan.Zero),
                    correlationId: "corr-journal-001"));
            }

            using var commandScope = provider.CreateScope();
            var dispatcher = commandScope.ServiceProvider.GetRequiredService<IEventDispatchRemediationDispatcher>();
            var journal = commandScope.ServiceProvider.GetRequiredService<IEventDispatchRemediationCommandJournal>();
            var result = await dispatcher.DispatchAsync(new EventDispatchRemediationRequest(
                outboxId: "entity-framework-outbox",
                messageId: "evt-journal-001",
                channelId: "catalog-events",
                operationId: EventDispatchRemediationOperationIds.RetryNow,
                commandId: "cmd-journal-001-retry",
                requestedAtUtc: new DateTimeOffset(2026, 04, 14, 10, 1, 0, TimeSpan.Zero),
                reason: "Persist command journal.",
                actorId: "operator-journal",
                correlationId: "corr-journal-command-001"));

            Assert.Equal(EventDispatchRemediationOutcomes.Accepted, result.Outcome);

            var reserved = await journal.ReserveAsync(new EventDispatchRemediationRequest(
                outboxId: "entity-framework-outbox",
                messageId: "evt-journal-001",
                channelId: "catalog-events",
                operationId: EventDispatchRemediationOperationIds.Skip,
                commandId: "cmd-journal-002-reserved",
                requestedAtUtc: new DateTimeOffset(2026, 04, 14, 10, 1, 30, TimeSpan.Zero),
                reason: "Reserve before mutation.",
                actorId: "operator-journal",
                correlationId: "corr-journal-command-002"));

            Assert.True(reserved.Reserved);
            Assert.Null(reserved.ExistingCommand);
            Assert.Equal("reserve-before-mutation", reserved.Metadata[EventDispatchRemediationMetadataKeys.CommandReservationPolicy]);
            Assert.Equal("reserved", reserved.Metadata[EventDispatchRemediationMetadataKeys.CommandReservationState]);
            Assert.Equal(EventDispatchRemediationOutcomes.Reserved, journal.GetByCommandId("cmd-journal-002-reserved")?.Outcome);
            Assert.Equal("cmd-journal-002-reserved", Assert.Single(journal.GetInDoubt()).CommandId);
            Assert.Equal("cmd-journal-002-reserved", journal.GetOldestInDoubtBefore(null)?.CommandId);
            Assert.Equal("cmd-journal-002-reserved", Assert.Single(journal.GetInDoubtBefore(new DateTimeOffset(2026, 04, 14, 10, 1, 30, TimeSpan.Zero))).CommandId);
            Assert.Equal("cmd-journal-002-reserved", journal.GetOldestInDoubtBefore(new DateTimeOffset(2026, 04, 14, 10, 1, 30, TimeSpan.Zero))?.CommandId);
            Assert.Equal("cmd-journal-002-reserved", journal.GetInDoubtSummaryBefore(null).OldestReservedCommandId);
            Assert.Equal(1, journal.GetInDoubtSummaryBefore(new DateTimeOffset(2026, 04, 14, 10, 1, 30, TimeSpan.Zero)).ReservedCount);
            Assert.Empty(journal.GetInDoubtBefore(new DateTimeOffset(2026, 04, 14, 10, 1, 29, TimeSpan.Zero)));
            Assert.Null(journal.GetOldestInDoubtBefore(new DateTimeOffset(2026, 04, 14, 10, 1, 29, TimeSpan.Zero)));
            Assert.False(journal.GetInDoubtSummaryBefore(new DateTimeOffset(2026, 04, 14, 10, 1, 29, TimeSpan.Zero)).HasInDoubtCommands);
            Assert.Equal(1, journal.Summary.ReservedCount);
            Assert.True(journal.Summary.HasInDoubtCommands);
            Assert.Equal("cmd-journal-002-reserved", journal.Summary.OldestReservedCommandId);
            Assert.Equal(new DateTimeOffset(2026, 04, 14, 10, 1, 30, TimeSpan.Zero), journal.Summary.OldestReservedObservedAtUtc);
        }

        await using (var provider = BuildProvider())
        {
            using var commandScope = provider.CreateScope();
            var journal = commandScope.ServiceProvider.GetRequiredService<IEventDispatchRemediationCommandJournal>();
            var processLocalCatalog = provider.GetRequiredService<IEventDispatchRemediationRuntimeCatalog>();
            var dispatcher = commandScope.ServiceProvider.GetRequiredService<IEventDispatchRemediationDispatcher>();

            Assert.Equal("durable", journal.Descriptor.Durability);
            Assert.Equal("cross-node", journal.Descriptor.Scope);
            Assert.True(journal.Descriptor.CrossNodeCommandAudit);
            Assert.True(journal.Descriptor.DurableReplayCursor);
            Assert.Empty(processLocalCatalog.States);

            var replayCatalog = Assert.IsAssignableFrom<IEventDispatchRemediationCommandReplayCursorCatalog>(journal);
            var persistedState = journal.GetByCommandId("cmd-journal-001-retry");
            Assert.NotNull(persistedState);
            Assert.Equal(EventDispatchRemediationOutcomes.Accepted, persistedState.Outcome);
            Assert.Equal("operator-journal", persistedState.Metadata[EventDispatchRemediationMetadataKeys.OperatorActorId]);

            var duplicate = await dispatcher.DispatchAsync(new EventDispatchRemediationRequest(
                outboxId: "entity-framework-outbox",
                messageId: "evt-journal-001",
                channelId: "catalog-events",
                operationId: EventDispatchRemediationOperationIds.Skip,
                commandId: "cmd-journal-001-retry",
                requestedAtUtc: new DateTimeOffset(2026, 04, 14, 10, 2, 0, TimeSpan.Zero),
                reason: "Duplicate after provider rebuild.",
                actorId: "operator-journal-duplicate",
                correlationId: "corr-journal-command-duplicate"));

            Assert.Equal(EventDispatchRemediationOutcomes.Rejected, duplicate.Outcome);
            Assert.Equal("true", duplicate.Metadata[EventDispatchRemediationMetadataKeys.DuplicateCommand]);
            Assert.Equal(EventDispatchRemediationOutcomes.Accepted, duplicate.Metadata[EventDispatchRemediationMetadataKeys.ExistingCommandOutcome]);
            Assert.Equal("retry-now", duplicate.Metadata[EventDispatchRemediationMetadataKeys.ExistingCommandOperationId]);

            var firstReplayPage = replayCatalog.GetAfterReplayCursor(cursor: null, maxCount: 1);
            var firstReplayState = Assert.Single(firstReplayPage);
            Assert.Equal("cmd-journal-001-retry", firstReplayState.CommandId);
            var replayCursor = new EventDispatchRemediationCommandReplayCursor(
                ObservedAtUtc: firstReplayState.ObservedAtUtc,
                CommandId: firstReplayState.CommandId);

            var inDoubtDuplicate = await dispatcher.DispatchAsync(new EventDispatchRemediationRequest(
                outboxId: "entity-framework-outbox",
                messageId: "evt-journal-001",
                channelId: "catalog-events",
                operationId: EventDispatchRemediationOperationIds.RetryNow,
                commandId: "cmd-journal-002-reserved",
                requestedAtUtc: new DateTimeOffset(2026, 04, 14, 10, 3, 0, TimeSpan.Zero),
                reason: "Duplicate while first command is reserved.",
                actorId: "operator-journal-duplicate",
                correlationId: "corr-journal-command-reserved-duplicate"));

            Assert.Equal(EventDispatchRemediationOutcomes.Rejected, inDoubtDuplicate.Outcome);
            Assert.Equal("true", inDoubtDuplicate.Metadata[EventDispatchRemediationMetadataKeys.DuplicateCommand]);
            Assert.Equal(EventDispatchRemediationOutcomes.Reserved, inDoubtDuplicate.Metadata[EventDispatchRemediationMetadataKeys.ExistingCommandOutcome]);
            Assert.Equal("duplicate", inDoubtDuplicate.Metadata[EventDispatchRemediationMetadataKeys.CommandReservationState]);
            Assert.Equal("reserve-before-mutation", inDoubtDuplicate.Metadata[EventDispatchRemediationMetadataKeys.CommandReservationPolicy]);

            var secondReplayPage = replayCatalog.GetAfterReplayCursor(replayCursor, maxCount: 10);
            var secondReplayState = Assert.Single(secondReplayPage);
            Assert.Equal("cmd-journal-002-reserved", secondReplayState.CommandId);
            var latestReplayCursor = replayCatalog.LatestReplayCursor;
            Assert.NotNull(latestReplayCursor);
            Assert.Equal("cmd-journal-002-reserved", latestReplayCursor.CommandId);
            Assert.Empty(replayCatalog.GetAfterReplayCursor(latestReplayCursor, maxCount: 10));
            Assert.Throws<ArgumentOutOfRangeException>(() => replayCatalog.GetAfterReplayCursor(cursor: null, maxCount: 0));

            var states = journal.States;
            Assert.Equal(2, states.Count);
            Assert.Contains(states, state =>
                state.CommandId == "cmd-journal-001-retry" &&
                state.Outcome == EventDispatchRemediationOutcomes.Accepted);
            var reservedState = Assert.Single(states, state => state.CommandId == "cmd-journal-002-reserved");
            Assert.Equal(EventDispatchRemediationOutcomes.Reserved, reservedState.Outcome);
            Assert.Equal("reserved", reservedState.Metadata[EventDispatchRemediationMetadataKeys.CommandReservationState]);
            Assert.Equal("cmd-journal-002-reserved", Assert.Single(journal.GetInDoubt()).CommandId);
            Assert.Equal("cmd-journal-002-reserved", journal.GetOldestInDoubtBefore(null)?.CommandId);
            Assert.Equal("cmd-journal-002-reserved", Assert.Single(journal.GetInDoubtBefore(new DateTimeOffset(2026, 04, 14, 10, 1, 30, TimeSpan.Zero))).CommandId);
            Assert.Equal("cmd-journal-002-reserved", journal.GetOldestInDoubtBefore(new DateTimeOffset(2026, 04, 14, 10, 1, 30, TimeSpan.Zero))?.CommandId);
            Assert.Equal("cmd-journal-002-reserved", journal.GetInDoubtSummaryBefore(null).OldestReservedCommandId);
            Assert.Equal(1, journal.GetInDoubtSummaryBefore(new DateTimeOffset(2026, 04, 14, 10, 1, 30, TimeSpan.Zero)).ReservedCount);
            Assert.Empty(journal.GetInDoubtBefore(new DateTimeOffset(2026, 04, 14, 10, 1, 29, TimeSpan.Zero)));
            Assert.Null(journal.GetOldestInDoubtBefore(new DateTimeOffset(2026, 04, 14, 10, 1, 29, TimeSpan.Zero)));
            Assert.False(journal.GetInDoubtSummaryBefore(new DateTimeOffset(2026, 04, 14, 10, 1, 29, TimeSpan.Zero)).HasInDoubtCommands);
            Assert.Empty(processLocalCatalog.States);

            var summary = journal.Summary;
            Assert.Equal(2, summary.TotalCommandCount);
            Assert.Equal(1, summary.AcceptedCount);
            Assert.Equal(0, summary.RejectedCount);
            Assert.Equal(0, summary.ErrorCount);
            Assert.Equal(0, summary.DuplicateCommandCount);
            Assert.Equal(1, summary.ReservedCount);
            Assert.True(summary.HasCommands);
            Assert.False(summary.HasFailures);
            Assert.True(summary.HasInDoubtCommands);
            Assert.Equal("cmd-journal-002-reserved", summary.LastCommandId);
            Assert.Equal(EventDispatchRemediationOutcomes.Reserved, summary.LastOutcome);
            Assert.Equal("cmd-journal-002-reserved", summary.OldestReservedCommandId);
            Assert.Equal(new DateTimeOffset(2026, 04, 14, 10, 1, 30, TimeSpan.Zero), summary.OldestReservedObservedAtUtc);
            Assert.Equal(2, journal.GetSummaryByOutboxId("entity-framework-outbox").TotalCommandCount);
            Assert.Equal(2, journal.GetSummaryByMessageId("evt-journal-001").TotalCommandCount);
            Assert.Equal(2, journal.GetSummaryByChannelId("catalog-events").TotalCommandCount);
            Assert.Equal(1, journal.GetSummaryByOperationId(EventDispatchRemediationOperationIds.RetryNow).AcceptedCount);
            Assert.Equal(2, journal.GetSummaryByActorId("operator-journal").TotalCommandCount);
            Assert.Equal(1, journal.GetSummaryByCorrelationId("corr-journal-command-002").ReservedCount);
            Assert.Equal(1, journal.GetSummaryByReason("Reserve before mutation.").ReservedCount);
            Assert.Equal(1, journal.GetSummaryByOutcome(EventDispatchRemediationOutcomes.Accepted).AcceptedCount);
            Assert.Equal(1, journal.GetSummaryByDispatchOutcome("pending").ReservedCount);
            Assert.False(journal.GetSummaryByMessageId("missing-message").HasCommands);
            Assert.Equal("cmd-journal-002-reserved", journal.GetLatestByOutboxId("entity-framework-outbox")?.CommandId);
            Assert.Equal("cmd-journal-002-reserved", journal.GetLatestByMessageId("evt-journal-001")?.CommandId);
            Assert.Equal("cmd-journal-002-reserved", journal.GetLatestByChannelId("catalog-events")?.CommandId);
            Assert.Equal("cmd-journal-001-retry", journal.GetLatestByOperationId(EventDispatchRemediationOperationIds.RetryNow)?.CommandId);
            Assert.Equal("cmd-journal-002-reserved", journal.GetLatestByActorId("operator-journal")?.CommandId);
            Assert.Equal("cmd-journal-002-reserved", journal.GetLatestByCorrelationId("corr-journal-command-002")?.CommandId);
            Assert.Equal("cmd-journal-002-reserved", journal.GetLatestByReason("Reserve before mutation.")?.CommandId);
            Assert.Equal("cmd-journal-001-retry", journal.GetLatestByOutcome(EventDispatchRemediationOutcomes.Accepted)?.CommandId);
            Assert.Equal("cmd-journal-002-reserved", journal.GetLatestByDispatchOutcome("pending")?.CommandId);
            Assert.Null(journal.GetLatestByMessageId("missing-message"));
            Assert.Equal("cmd-journal-001-retry", journal.GetOldestByOutboxId("entity-framework-outbox")?.CommandId);
            Assert.Equal("cmd-journal-001-retry", journal.GetOldestByMessageId("evt-journal-001")?.CommandId);
            Assert.Equal("cmd-journal-001-retry", journal.GetOldestByChannelId("catalog-events")?.CommandId);
            Assert.Equal("cmd-journal-001-retry", journal.GetOldestByOperationId(EventDispatchRemediationOperationIds.RetryNow)?.CommandId);
            Assert.Equal("cmd-journal-001-retry", journal.GetOldestByActorId("operator-journal")?.CommandId);
            Assert.Equal("cmd-journal-002-reserved", journal.GetOldestByCorrelationId("corr-journal-command-002")?.CommandId);
            Assert.Equal("cmd-journal-002-reserved", journal.GetOldestByReason("Reserve before mutation.")?.CommandId);
            Assert.Equal("cmd-journal-001-retry", journal.GetOldestByOutcome(EventDispatchRemediationOutcomes.Accepted)?.CommandId);
            Assert.Equal("cmd-journal-002-reserved", journal.GetOldestByDispatchOutcome("pending")?.CommandId);
            Assert.Null(journal.GetOldestByMessageId("missing-message"));
            AssertRetention(journal.GetRetentionByOutboxId("entity-framework-outbox"), 2, "cmd-journal-001-retry", "cmd-journal-002-reserved", expectedHistoryLimit: 0);
            AssertRetention(journal.GetRetentionByMessageId("evt-journal-001"), 2, "cmd-journal-001-retry", "cmd-journal-002-reserved", expectedHistoryLimit: 0);
            AssertRetention(journal.GetRetentionByChannelId("catalog-events"), 2, "cmd-journal-001-retry", "cmd-journal-002-reserved", expectedHistoryLimit: 0);
            AssertRetention(journal.GetRetentionByOperationId(EventDispatchRemediationOperationIds.RetryNow), 1, "cmd-journal-001-retry", "cmd-journal-001-retry", expectedHistoryLimit: 0);
            AssertRetention(journal.GetRetentionByActorId("operator-journal"), 2, "cmd-journal-001-retry", "cmd-journal-002-reserved", expectedHistoryLimit: 0);
            AssertRetention(journal.GetRetentionByCorrelationId("corr-journal-command-002"), 1, "cmd-journal-002-reserved", "cmd-journal-002-reserved", expectedHistoryLimit: 0);
            AssertRetention(journal.GetRetentionByReason("Reserve before mutation."), 1, "cmd-journal-002-reserved", "cmd-journal-002-reserved", expectedHistoryLimit: 0);
            AssertRetention(journal.GetRetentionByOutcome(EventDispatchRemediationOutcomes.Accepted), 1, "cmd-journal-001-retry", "cmd-journal-001-retry", expectedHistoryLimit: 0);
            AssertRetention(journal.GetRetentionByDispatchOutcome("pending"), 1, "cmd-journal-002-reserved", "cmd-journal-002-reserved", expectedHistoryLimit: 0);
            AssertRetention(journal.GetRetentionByMessageId("missing-message"), 0, null, null, expectedHistoryLimit: 0);

            var observationSummary = journal.GetSummaryByObservedAt(
                new DateTimeOffset(2026, 04, 14, 10, 1, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 04, 14, 10, 1, 30, TimeSpan.Zero));
            Assert.Equal(2, observationSummary.TotalCommandCount);
            Assert.Equal(1, observationSummary.AcceptedCount);
            Assert.Equal(1, observationSummary.ReservedCount);
            Assert.True(observationSummary.HasInDoubtCommands);
            Assert.Equal("cmd-journal-002-reserved", observationSummary.OldestReservedCommandId);
            Assert.Equal(new DateTimeOffset(2026, 04, 14, 10, 1, 30, TimeSpan.Zero), observationSummary.OldestReservedObservedAtUtc);

            var technologySurfaces = provider.GetRequiredService<ITechnologyRuntimeCatalog>();
            var commandSurface = Assert.Single(
                technologySurfaces.GetByTechnology("event-driven-integration"),
                surface => surface.SurfaceId == "event-dispatch-remediation-commands");
            var commandCatalogEntry = Assert.Single(commandSurface.Entries, entry => entry.Id == "event-dispatch-remediation-commands");
            Assert.Equal("1", commandCatalogEntry.Metadata["summaryReservedCount"]);
            Assert.Equal("true", commandCatalogEntry.Metadata["summaryHasInDoubtCommands"]);
            Assert.Equal("cmd-journal-002-reserved", commandCatalogEntry.Metadata["summaryOldestReservedCommandId"]);
            Assert.Equal("2026-04-14T10:01:30.0000000+00:00", commandCatalogEntry.Metadata["summaryOldestReservedObservedAtUtc"]);
            Assert.Equal("/engine/event-dispatch-remediation-commands/in-doubt", commandCatalogEntry.Metadata["commandInDoubtRoute"]);
            Assert.Equal("/engine/event-dispatch-remediation-commands/in-doubt/summary", commandCatalogEntry.Metadata["commandInDoubtSummaryRoute"]);
            Assert.Equal("/engine/event-dispatch-remediation-commands/in-doubt/oldest", commandCatalogEntry.Metadata["commandOldestInDoubtRoute"]);
            Assert.Equal("beforeUtc", commandCatalogEntry.Metadata["commandInDoubtSummaryQuery"]);
            Assert.Equal("retained-reserved-summary-observed-utc-before-or-equal", commandCatalogEntry.Metadata["commandInDoubtSummaryPolicy"]);
            Assert.Equal("beforeUtc", commandCatalogEntry.Metadata["commandOldestInDoubtQuery"]);
            Assert.Equal("oldest-retained-reserved-observed-utc-before-or-equal", commandCatalogEntry.Metadata["commandOldestInDoubtPolicy"]);
            Assert.Equal("/engine/event-dispatch-remediation-commands/messages/{messageId}/summary", commandCatalogEntry.Metadata["commandMessageSummaryRoute"]);
            Assert.Equal("/engine/event-dispatch-remediation-commands/outcomes/{outcome}/summary", commandCatalogEntry.Metadata["commandOutcomeSummaryRoute"]);
            Assert.Equal("retained-filter-server-side-aggregate", commandCatalogEntry.Metadata["commandFilterSummaryPolicy"]);
            Assert.Equal("outboxes,messages,channels,operations,actors,correlations,reasons,dispatch-outcomes,outcomes", commandCatalogEntry.Metadata["commandFilterSummaryRoutes"]);
            Assert.Equal("/engine/event-dispatch-remediation-commands/outboxes/{outboxId}/retention", commandCatalogEntry.Metadata["commandOutboxRetentionRoute"]);
            Assert.Equal("/engine/event-dispatch-remediation-commands/messages/{messageId}/retention", commandCatalogEntry.Metadata["commandMessageRetentionRoute"]);
            Assert.Equal("retained-filter-server-side-retention", commandCatalogEntry.Metadata["commandFilterRetentionPolicy"]);
            Assert.Equal("outboxes,messages,channels,operations,actors,correlations,reasons,dispatch-outcomes,outcomes", commandCatalogEntry.Metadata["commandFilterRetentionRoutes"]);
            Assert.Equal(nameof(EventDispatchRemediationRuntimeRetention), commandCatalogEntry.Metadata["commandFilterRetentionResponse"]);
            Assert.Equal("not-required", commandCatalogEntry.Metadata["commandFilterRetentionMaterialization"]);
            Assert.Equal("/engine/event-dispatch-remediation-commands/outboxes/{outboxId}/latest", commandCatalogEntry.Metadata["commandOutboxLatestRoute"]);
            Assert.Equal("/engine/event-dispatch-remediation-commands/messages/{messageId}/latest", commandCatalogEntry.Metadata["commandMessageLatestRoute"]);
            Assert.Equal("retained-filter-server-side-latest", commandCatalogEntry.Metadata["commandFilterLatestPolicy"]);
            Assert.Equal("outboxes,messages,channels,operations,actors,correlations,reasons,dispatch-outcomes,outcomes", commandCatalogEntry.Metadata["commandFilterLatestRoutes"]);
            Assert.Equal(nameof(EventDispatchRemediationRuntimeState), commandCatalogEntry.Metadata["commandFilterLatestResponse"]);
            Assert.Equal("not-found", commandCatalogEntry.Metadata["commandFilterLatestMissing"]);
            Assert.Equal("/engine/event-dispatch-remediation-commands/outboxes/{outboxId}/oldest", commandCatalogEntry.Metadata["commandOutboxOldestRoute"]);
            Assert.Equal("/engine/event-dispatch-remediation-commands/messages/{messageId}/oldest", commandCatalogEntry.Metadata["commandMessageOldestRoute"]);
            Assert.Equal("retained-filter-server-side-oldest", commandCatalogEntry.Metadata["commandFilterOldestPolicy"]);
            Assert.Equal("outboxes,messages,channels,operations,actors,correlations,reasons,dispatch-outcomes,outcomes", commandCatalogEntry.Metadata["commandFilterOldestRoutes"]);
            Assert.Equal(nameof(EventDispatchRemediationRuntimeState), commandCatalogEntry.Metadata["commandFilterOldestResponse"]);
            Assert.Equal("not-found", commandCatalogEntry.Metadata["commandFilterOldestMissing"]);

            var processLocalJournal = Assert.IsAssignableFrom<IEventDispatchRemediationCommandJournal>(processLocalCatalog);
            Assert.False(processLocalCatalog is IEventDispatchRemediationCommandReplayCursorCatalog);
            var processLocalReservation = await processLocalJournal.ReserveAsync(new EventDispatchRemediationRequest(
                outboxId: "entity-framework-outbox",
                messageId: "evt-journal-001",
                channelId: "catalog-events",
                operationId: EventDispatchRemediationOperationIds.Skip,
                commandId: "cmd-process-local-reserved",
                requestedAtUtc: new DateTimeOffset(2026, 04, 14, 10, 4, 0, TimeSpan.Zero),
                reason: "Process-local fallback summary proof.",
                actorId: "operator-journal",
                correlationId: "corr-process-local-reserved"));

            var processLocalSummary = processLocalCatalog.Summary;
            Assert.True(processLocalReservation.Reserved);
            Assert.Equal(1, processLocalSummary.TotalCommandCount);
            Assert.Equal(1, processLocalSummary.ReservedCount);
            Assert.True(processLocalSummary.HasInDoubtCommands);
            Assert.Equal(1, processLocalCatalog.GetSummaryByOutboxId("entity-framework-outbox").ReservedCount);
            Assert.Equal(1, processLocalCatalog.GetSummaryByActorId("operator-journal").ReservedCount);
            Assert.Equal(1, processLocalCatalog.GetSummaryByOutcome(EventDispatchRemediationOutcomes.Reserved).ReservedCount);
            Assert.Equal(1, processLocalCatalog.GetSummaryByDispatchOutcome("pending").ReservedCount);
            Assert.Equal("cmd-process-local-reserved", processLocalCatalog.GetLatestByOutboxId("entity-framework-outbox")?.CommandId);
            Assert.Equal("cmd-process-local-reserved", processLocalCatalog.GetLatestByMessageId("evt-journal-001")?.CommandId);
            Assert.Equal("cmd-process-local-reserved", processLocalCatalog.GetLatestByChannelId("catalog-events")?.CommandId);
            Assert.Equal("cmd-process-local-reserved", processLocalCatalog.GetLatestByOperationId(EventDispatchRemediationOperationIds.Skip)?.CommandId);
            Assert.Equal("cmd-process-local-reserved", processLocalCatalog.GetLatestByActorId("operator-journal")?.CommandId);
            Assert.Equal("cmd-process-local-reserved", processLocalCatalog.GetLatestByCorrelationId("corr-process-local-reserved")?.CommandId);
            Assert.Equal("cmd-process-local-reserved", processLocalCatalog.GetLatestByReason("Process-local fallback summary proof.")?.CommandId);
            Assert.Equal("cmd-process-local-reserved", processLocalCatalog.GetLatestByOutcome(EventDispatchRemediationOutcomes.Reserved)?.CommandId);
            Assert.Equal("cmd-process-local-reserved", processLocalCatalog.GetLatestByDispatchOutcome("pending")?.CommandId);
            Assert.Null(processLocalCatalog.GetLatestByActorId("missing-operator"));
            Assert.Equal("cmd-process-local-reserved", processLocalCatalog.GetOldestByOutboxId("entity-framework-outbox")?.CommandId);
            Assert.Equal("cmd-process-local-reserved", processLocalCatalog.GetOldestByMessageId("evt-journal-001")?.CommandId);
            Assert.Equal("cmd-process-local-reserved", processLocalCatalog.GetOldestByChannelId("catalog-events")?.CommandId);
            Assert.Equal("cmd-process-local-reserved", processLocalCatalog.GetOldestByOperationId(EventDispatchRemediationOperationIds.Skip)?.CommandId);
            Assert.Equal("cmd-process-local-reserved", processLocalCatalog.GetOldestByActorId("operator-journal")?.CommandId);
            Assert.Equal("cmd-process-local-reserved", processLocalCatalog.GetOldestByCorrelationId("corr-process-local-reserved")?.CommandId);
            Assert.Equal("cmd-process-local-reserved", processLocalCatalog.GetOldestByReason("Process-local fallback summary proof.")?.CommandId);
            Assert.Equal("cmd-process-local-reserved", processLocalCatalog.GetOldestByOutcome(EventDispatchRemediationOutcomes.Reserved)?.CommandId);
            Assert.Equal("cmd-process-local-reserved", processLocalCatalog.GetOldestByDispatchOutcome("pending")?.CommandId);
            Assert.Null(processLocalCatalog.GetOldestByActorId("missing-operator"));
            AssertRetention(processLocalCatalog.GetRetentionByOutboxId("entity-framework-outbox"), 1, "cmd-process-local-reserved", "cmd-process-local-reserved");
            AssertRetention(processLocalCatalog.GetRetentionByMessageId("evt-journal-001"), 1, "cmd-process-local-reserved", "cmd-process-local-reserved");
            AssertRetention(processLocalCatalog.GetRetentionByChannelId("catalog-events"), 1, "cmd-process-local-reserved", "cmd-process-local-reserved");
            AssertRetention(processLocalCatalog.GetRetentionByOperationId(EventDispatchRemediationOperationIds.Skip), 1, "cmd-process-local-reserved", "cmd-process-local-reserved");
            AssertRetention(processLocalCatalog.GetRetentionByActorId("operator-journal"), 1, "cmd-process-local-reserved", "cmd-process-local-reserved");
            AssertRetention(processLocalCatalog.GetRetentionByCorrelationId("corr-process-local-reserved"), 1, "cmd-process-local-reserved", "cmd-process-local-reserved");
            AssertRetention(processLocalCatalog.GetRetentionByReason("Process-local fallback summary proof."), 1, "cmd-process-local-reserved", "cmd-process-local-reserved");
            AssertRetention(processLocalCatalog.GetRetentionByOutcome(EventDispatchRemediationOutcomes.Reserved), 1, "cmd-process-local-reserved", "cmd-process-local-reserved");
            AssertRetention(processLocalCatalog.GetRetentionByDispatchOutcome("pending"), 1, "cmd-process-local-reserved", "cmd-process-local-reserved");
            AssertRetention(processLocalCatalog.GetRetentionByActorId("missing-operator"), 0, null, null);
            Assert.Equal("cmd-process-local-reserved", Assert.Single(processLocalCatalog.GetInDoubt()).CommandId);
            Assert.Equal("cmd-process-local-reserved", processLocalCatalog.GetOldestInDoubtBefore(null)?.CommandId);
            Assert.Equal("cmd-process-local-reserved", Assert.Single(processLocalCatalog.GetInDoubtBefore(new DateTimeOffset(2026, 04, 14, 10, 4, 0, TimeSpan.Zero))).CommandId);
            Assert.Equal("cmd-process-local-reserved", processLocalCatalog.GetOldestInDoubtBefore(new DateTimeOffset(2026, 04, 14, 10, 4, 0, TimeSpan.Zero))?.CommandId);
            Assert.Equal("cmd-process-local-reserved", processLocalCatalog.GetInDoubtSummaryBefore(null).OldestReservedCommandId);
            Assert.Equal(1, processLocalCatalog.GetInDoubtSummaryBefore(new DateTimeOffset(2026, 04, 14, 10, 4, 0, TimeSpan.Zero)).ReservedCount);
            Assert.Empty(processLocalCatalog.GetInDoubtBefore(new DateTimeOffset(2026, 04, 14, 10, 3, 59, TimeSpan.Zero)));
            Assert.Null(processLocalCatalog.GetOldestInDoubtBefore(new DateTimeOffset(2026, 04, 14, 10, 3, 59, TimeSpan.Zero)));
            Assert.False(processLocalCatalog.GetInDoubtSummaryBefore(new DateTimeOffset(2026, 04, 14, 10, 3, 59, TimeSpan.Zero)).HasInDoubtCommands);
            Assert.Equal("cmd-process-local-reserved", processLocalSummary.OldestReservedCommandId);
            Assert.Equal(new DateTimeOffset(2026, 04, 14, 10, 4, 0, TimeSpan.Zero), processLocalSummary.OldestReservedObservedAtUtc);
            Assert.Equal(EventDispatchRemediationOutcomes.Reserved, processLocalSummary.LastOutcome);
        }
    }

    [Fact]
    public async Task AddEventingCanReportDispatchRuntimeStateThroughEventingTechnologySurfaces()
    {
        var databaseName = $"cephalon-data-ef-event-dispatch-runtime-{Guid.NewGuid():N}";
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS", "Outbox"],
                technologies: ["EventDrivenIntegration"],
                transports: ["RestApi"],
                data: new DataSettings(
                    provider: "EntityFramework",
                    outboxEnabled: true),
                messaging: new MessagingSettings(provider: "InMemoryChannels")));
            engine.AddModule(new PlatformTestModule());
            engine.AddEventing(options =>
            {
                options.Channels.Add(new EventChannelDescriptor(
                    id: "catalog-events",
                    displayName: "Catalog Events",
                    description: "Catalog integration events."));
            });
            engine.AddEntityFrameworkData<OutboxCatalogDbContext>(
                options => options.UseInMemoryDatabase(databaseName),
                configure: options => options.RegisterOutbox = true);
        });

        await using var provider = services.BuildServiceProvider();
        using (var publicationScope = provider.CreateScope())
        {
            var publisher = publicationScope.ServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(new EventPublication(
                id: "evt-020",
                channelId: "catalog-events",
                eventType: "catalog.item.updated",
                payload: "{\"id\":\"item-020\"}",
                occurredAtUtc: new DateTimeOffset(2026, 04, 04, 11, 59, 0, TimeSpan.Zero),
                correlationId: "corr-020"));
        }

        var diagnosticsCatalog = provider.GetRequiredService<IRuntimeDiagnosticsCatalog>();
        var reporter = provider.GetRequiredService<IEventDispatchRuntimeReporter>();
        var runtimeCatalog = provider.GetRequiredService<IEventDispatchRuntimeCatalog>();
        var remediationCommandCatalog = provider.GetRequiredService<IEventDispatchRemediationRuntimeCatalog>();
        var technologyCatalog = provider.GetRequiredService<global::Cephalon.Abstractions.Technologies.ITechnologyRuntimeCatalog>();

        await reporter.ReportAsync(new EventDispatchExecutionReport(
            outboxId: "entity-framework-outbox",
            channelId: "catalog-events",
            outcome: EventDispatchExecutionOutcomes.Started,
            observedAtUtc: new DateTimeOffset(2026, 04, 04, 12, 0, 0, TimeSpan.Zero),
            messageId: "evt-020",
            attempt: 1,
            metadata: new Dictionary<string, string>
            {
                ["publisherId"] = "outbox-backed-publisher"
            }));
        await reporter.ReportAsync(new EventDispatchExecutionReport(
            outboxId: "entity-framework-outbox",
            channelId: "catalog-events",
            outcome: EventDispatchExecutionOutcomes.RetryScheduled,
            observedAtUtc: new DateTimeOffset(2026, 04, 04, 12, 1, 0, TimeSpan.Zero),
            messageId: "evt-020",
            attempt: 2,
            error: "Broker temporarily unavailable",
            metadata: new Dictionary<string, string>
            {
                ["publisherId"] = "outbox-backed-publisher",
                ["nextRetryAtUtc"] = "2026-04-04T12:06:00.0000000+00:00",
                ["retryPolicy"] = "exponential"
            }));

        var publisherState = Assert.Single(runtimeCatalog.States);
        Assert.Equal("entity-framework-outbox", publisherState.OutboxId);
        Assert.Equal(EventDispatchExecutionOutcomes.RetryScheduled, publisherState.LastOutcome);
        Assert.Equal("catalog-events", publisherState.LastChannelId);
        Assert.Equal("evt-020", publisherState.LastMessageId);
        Assert.Equal(2, publisherState.LastAttempt);
        Assert.Equal(1, publisherState.StartedCount);
        Assert.Equal(1, publisherState.RetryScheduledCount);
        Assert.Equal(2, publisherState.TotalReports);
        Assert.True(publisherState.RetryPending);

        var eventingConvention = Assert.Single(diagnosticsCatalog.Conventions, convention => convention.Source == "Cephalon.Eventing");
        Assert.Equal(4210, eventingConvention.MaximumEventId);
        Assert.Contains(eventingConvention.Events, entry => entry.Id == 4209 && entry.Name == "EventPublicationDispatchRetryScheduled");

        var eventingSurfaces = technologyCatalog.GetByTechnology("event-driven-integration");
        var dispatchSurface = Assert.Single(eventingSurfaces, surface => surface.SurfaceId == "event-dispatches");
        var dispatchEntry = Assert.Single(dispatchSurface.Entries, entry => entry.Id == "entity-framework-outbox");
        Assert.Equal("reported", dispatchEntry.Metadata["runtimeState"]);
        Assert.Equal("available", dispatchEntry.Metadata["dispatchStore"]);
        Assert.Equal("retry-scheduled", dispatchEntry.Metadata["lastOutcome"]);
        Assert.Equal("catalog-events", dispatchEntry.Metadata["lastChannelId"]);
        Assert.Equal("evt-020", dispatchEntry.Metadata["lastMessageId"]);
        Assert.Equal("2", dispatchEntry.Metadata["lastAttempt"]);
        Assert.Equal("1", dispatchEntry.Metadata["retryScheduledCount"]);
        Assert.Equal("2", dispatchEntry.Metadata["totalReports"]);
        Assert.Equal("true", dispatchEntry.Metadata["retryPending"]);
        Assert.Equal("outbox-backed-publisher", dispatchEntry.Metadata["reported.publisherId"]);
        Assert.Equal("2026-04-04T12:06:00.0000000+00:00", dispatchEntry.Metadata["reported.nextRetryAtUtc"]);
        Assert.Equal("Broker temporarily unavailable", dispatchEntry.Metadata["lastError"]);
        var remediationSurface = Assert.Single(eventingSurfaces, surface => surface.SurfaceId == "event-dispatch-remediations");
        var remediationEntry = Assert.Single(remediationSurface.Entries, entry => entry.Id == "entity-framework-outbox:evt-020");
        Assert.Equal("retry-pending", remediationEntry.Metadata["remediationState"]);
        Assert.Equal("wait-for-scheduled-retry-or-inspect-downstream", remediationEntry.Metadata["recommendedAction"]);
        Assert.Equal("bounded-dispatch-store-command-ready", remediationEntry.Metadata["operatorCommandState"]);
        Assert.Equal("retry-now-ready", remediationEntry.Metadata["replayCommand"]);
        Assert.Equal("ready", remediationEntry.Metadata["retryLaterCommand"]);
        Assert.Equal("dispatch-store-ready", remediationEntry.Metadata["deadLetterCommand"]);
        Assert.Equal("not-claimed", remediationEntry.Metadata["brokerDeadLetterCommand"]);
        Assert.Equal("ready", remediationEntry.Metadata["quarantineCommand"]);
        Assert.Equal("ready", remediationEntry.Metadata["skipCommand"]);
        Assert.Equal("/engine/event-dispatches/{outboxId}/commands/{operationId}", remediationEntry.Metadata["operatorCommandRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands", remediationEntry.Metadata["operatorCommandListRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/{commandId}", remediationEntry.Metadata["operatorCommandResultRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/outboxes/{outboxId}", remediationEntry.Metadata["operatorCommandOutboxRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/summary", remediationEntry.Metadata["operatorCommandSummaryRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/latest", remediationEntry.Metadata["operatorCommandLatestRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/retention", remediationEntry.Metadata["operatorCommandRetentionRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/in-doubt", remediationEntry.Metadata["operatorCommandInDoubtRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/in-doubt/summary", remediationEntry.Metadata["operatorCommandInDoubtSummaryRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/in-doubt/oldest", remediationEntry.Metadata["operatorCommandOldestInDoubtRoute"]);
        Assert.Equal("beforeUtc", remediationEntry.Metadata["operatorCommandInDoubtQuery"]);
        Assert.Equal("inclusive-observed-utc-before-or-equal", remediationEntry.Metadata["operatorCommandInDoubtCutoffPolicy"]);
        Assert.Equal("newest-first", remediationEntry.Metadata["operatorCommandInDoubtDetailOrder"]);
        Assert.Equal("beforeUtc", remediationEntry.Metadata["operatorCommandInDoubtSummaryQuery"]);
        Assert.Equal("retained-reserved-summary-observed-utc-before-or-equal", remediationEntry.Metadata["operatorCommandInDoubtSummaryPolicy"]);
        Assert.Equal("beforeUtc", remediationEntry.Metadata["operatorCommandOldestInDoubtQuery"]);
        Assert.Equal("oldest-retained-reserved-observed-utc-before-or-equal", remediationEntry.Metadata["operatorCommandOldestInDoubtPolicy"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/observations?fromUtc={fromUtc}&toUtc={toUtc}", remediationEntry.Metadata["operatorCommandObservationRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/observations/summary?fromUtc={fromUtc}&toUtc={toUtc}", remediationEntry.Metadata["operatorCommandObservationSummaryRoute"]);
        Assert.Equal("fromUtc,toUtc", remediationEntry.Metadata["operatorCommandObservationWindowQuery"]);
        Assert.Equal("inclusive-observed-utc", remediationEntry.Metadata["operatorCommandObservationWindowPolicy"]);
        Assert.Equal("newest-first", remediationEntry.Metadata["operatorCommandObservationWindowDetailOrder"]);
        Assert.Equal("available", remediationEntry.Metadata["operatorCommandObservationWindowSummary"]);
        Assert.Equal("reject-reversed-window", remediationEntry.Metadata["operatorCommandObservationWindowInvalidBounds"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/operations/{operationId}", remediationEntry.Metadata["operatorCommandOperationRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/actors/{actorId}", remediationEntry.Metadata["operatorCommandActorRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/correlations/{correlationId}", remediationEntry.Metadata["operatorCommandCorrelationRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/reasons/{reason}", remediationEntry.Metadata["operatorCommandReasonRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/messages/{messageId}", remediationEntry.Metadata["operatorCommandMessageRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/channels/{channelId}", remediationEntry.Metadata["operatorCommandChannelRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/dispatch-outcomes/{dispatchOutcome}", remediationEntry.Metadata["operatorCommandDispatchOutcomeRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/outcomes/{outcome}", remediationEntry.Metadata["operatorCommandOutcomeRoute"]);
        Assert.Equal("limit", remediationEntry.Metadata["operatorCommandReadLimitQuery"]);
        Assert.Equal("positive-integer-newest-first", remediationEntry.Metadata["operatorCommandReadLimitPolicy"]);
        Assert.Equal("list-and-filter-routes", remediationEntry.Metadata["operatorCommandReadLimitAppliesTo"]);
        Assert.Equal("all,in-doubt,observations,outboxes,messages,channels,operations,actors,correlations,reasons,dispatch-outcomes,outcomes", remediationEntry.Metadata["operatorCommandReadLimitRoutes"]);
        Assert.Equal("pageSize,continuationToken", remediationEntry.Metadata["operatorCommandPaginationQuery"]);
        Assert.Equal("opaque-signed-route-bound-continuation-token-newest-first", remediationEntry.Metadata["operatorCommandPaginationPolicy"]);
        Assert.Equal("retry-now,retry-later,skip,quarantine,dead-letter", remediationEntry.Metadata["operatorCommandOperations"]);
        Assert.Equal("false", remediationEntry.Metadata["wolverineRequired"]);
        Assert.Equal("true", remediationEntry.Metadata["providerNeutral"]);
        Assert.Equal("2026-04-04T12:06:00.0000000+00:00", remediationEntry.Metadata["nextRetryAtUtc"]);
        Assert.Equal("exponential", remediationEntry.Metadata["retryPolicy"]);

        await reporter.ReportAsync(new EventDispatchExecutionReport(
            outboxId: "entity-framework-outbox",
            channelId: "catalog-events",
            outcome: EventDispatchExecutionOutcomes.Failed,
            observedAtUtc: new DateTimeOffset(2026, 04, 04, 12, 8, 0, TimeSpan.Zero),
            messageId: "evt-020",
            attempt: 3,
            error: "Dispatch retry budget exhausted.",
            metadata: new Dictionary<string, string>
            {
                ["publisherId"] = "outbox-backed-publisher",
                [EventDispatchRuntimeMetadataKeys.RetryOutcome] = "max-attempts-exhausted",
                [EventDispatchRuntimeMetadataKeys.RetryExhausted] = "true",
                [EventDispatchRuntimeMetadataKeys.TerminalFailure] = "true"
            }));

        var terminalState = Assert.Single(runtimeCatalog.States);
        Assert.Equal(EventDispatchExecutionOutcomes.Failed, terminalState.LastOutcome);
        Assert.False(terminalState.RetryPending);
        Assert.True(terminalState.TerminalFailure);
        Assert.Equal(1, terminalState.TerminalFailureCount);

        eventingSurfaces = technologyCatalog.GetByTechnology("event-driven-integration");
        dispatchSurface = Assert.Single(eventingSurfaces, surface => surface.SurfaceId == "event-dispatches");
        dispatchEntry = Assert.Single(dispatchSurface.Entries, entry => entry.Id == "entity-framework-outbox");
        Assert.Equal("failed", dispatchEntry.Metadata["lastOutcome"]);
        Assert.Equal("false", dispatchEntry.Metadata["retryPending"]);
        Assert.Equal("true", dispatchEntry.Metadata["terminalFailure"]);
        Assert.Equal("1", dispatchEntry.Metadata["terminalFailureCount"]);
        Assert.Equal("max-attempts-exhausted", dispatchEntry.Metadata["reported.retryOutcome"]);
        Assert.Equal("true", dispatchEntry.Metadata["reported.terminalFailure"]);
        remediationSurface = Assert.Single(eventingSurfaces, surface => surface.SurfaceId == "event-dispatch-remediations");
        remediationEntry = Assert.Single(remediationSurface.Entries, entry => entry.Id == "entity-framework-outbox:evt-020");
        Assert.Equal("terminal-failure", remediationEntry.Metadata["remediationState"]);
        Assert.Equal("inspect-terminal-failure-before-replay", remediationEntry.Metadata["recommendedAction"]);
        Assert.Equal("Dispatch retry budget exhausted.", remediationEntry.Metadata["lastError"]);
        Assert.Equal("true", remediationEntry.Metadata["terminalFailure"]);
        Assert.Equal("1", remediationEntry.Metadata["terminalFailureCount"]);
        Assert.Equal("max-attempts-exhausted", remediationEntry.Metadata["retryOutcome"]);
        Assert.Equal("true", remediationEntry.Metadata["retryExhausted"]);
        Assert.Equal("true", remediationEntry.Metadata["reported.terminalFailure"]);
        Assert.Equal("unique-command-id", remediationEntry.Metadata[EventDispatchRemediationMetadataKeys.CommandIdempotencyPolicy]);
        Assert.Equal("reject-without-mutation", remediationEntry.Metadata[EventDispatchRemediationMetadataKeys.DuplicateCommandPolicy]);

        var retryReason = "Downstream recovered.";
        var duplicateReason = "Duplicate command id should not mutate dispatch state.";
        using (var commandScope = provider.CreateScope())
        {
            var dispatcher = commandScope.ServiceProvider.GetRequiredService<IEventDispatchRemediationDispatcher>();
            var commandResult = await dispatcher.DispatchAsync(new EventDispatchRemediationRequest(
                outboxId: "entity-framework-outbox",
                messageId: "evt-020",
                channelId: "catalog-events",
                operationId: EventDispatchRemediationOperationIds.RetryNow,
                commandId: "cmd-evt-020-retry",
                requestedAtUtc: new DateTimeOffset(2026, 04, 04, 12, 12, 0, TimeSpan.Zero),
                reason: retryReason,
                actorId: "operator-001",
                correlationId: "corr-command-020"));

            Assert.Equal(EventDispatchRemediationOutcomes.Accepted, commandResult.Outcome);
            Assert.Equal(EventDispatchExecutionOutcomes.RetryScheduled, commandResult.DispatchOutcome);
            Assert.Equal("operator-001", commandResult.Metadata[EventDispatchRemediationMetadataKeys.OperatorActorId]);
            Assert.Equal("corr-command-020", commandResult.Metadata[EventDispatchRemediationMetadataKeys.OperatorCorrelationId]);
            Assert.Equal(retryReason, commandResult.Metadata[EventDispatchRemediationMetadataKeys.OperatorCommandReason]);
            Assert.Equal("unique-command-id", commandResult.Metadata[EventDispatchRemediationMetadataKeys.CommandIdempotencyPolicy]);
            Assert.Equal("reject-without-mutation", commandResult.Metadata[EventDispatchRemediationMetadataKeys.DuplicateCommandPolicy]);

            var duplicateResult = await dispatcher.DispatchAsync(new EventDispatchRemediationRequest(
                outboxId: "entity-framework-outbox",
                messageId: "evt-020",
                channelId: "catalog-events",
                operationId: EventDispatchRemediationOperationIds.Skip,
                commandId: "cmd-evt-020-retry",
                requestedAtUtc: new DateTimeOffset(2026, 04, 04, 12, 13, 0, TimeSpan.Zero),
                reason: duplicateReason,
                actorId: "operator-002",
                correlationId: "corr-command-020-duplicate"));

            Assert.Equal(EventDispatchRemediationOutcomes.Rejected, duplicateResult.Outcome);
            Assert.Equal("corr-command-020-duplicate", duplicateResult.Metadata[EventDispatchRemediationMetadataKeys.OperatorCorrelationId]);
            Assert.Equal(duplicateReason, duplicateResult.Metadata[EventDispatchRemediationMetadataKeys.OperatorCommandReason]);
            Assert.Equal("true", duplicateResult.Metadata[EventDispatchRemediationMetadataKeys.DuplicateCommand]);
            Assert.Equal(EventDispatchRemediationOutcomes.Accepted, duplicateResult.Metadata[EventDispatchRemediationMetadataKeys.ExistingCommandOutcome]);
            Assert.Equal("retry-now", duplicateResult.Metadata[EventDispatchRemediationMetadataKeys.ExistingCommandOperationId]);
        }

        var remediationCommandState = Assert.Single(remediationCommandCatalog.States);
        Assert.Equal("cmd-evt-020-retry", remediationCommandState.CommandId);
        Assert.Equal("entity-framework-outbox", remediationCommandState.OutboxId);
        Assert.Equal(EventDispatchRemediationOutcomes.Accepted, remediationCommandState.Outcome);
        Assert.Equal(EventDispatchExecutionOutcomes.RetryScheduled, remediationCommandState.DispatchOutcome);
        Assert.Equal("operator-001", remediationCommandState.Metadata[EventDispatchRemediationMetadataKeys.OperatorActorId]);
        Assert.Equal("corr-command-020", remediationCommandState.Metadata[EventDispatchRemediationMetadataKeys.OperatorCorrelationId]);
        Assert.Equal(retryReason, remediationCommandState.Metadata[EventDispatchRemediationMetadataKeys.OperatorCommandReason]);
        Assert.Equal("unique-command-id", remediationCommandState.Metadata[EventDispatchRemediationMetadataKeys.CommandIdempotencyPolicy]);
        Assert.Equal("reject-without-mutation", remediationCommandState.Metadata[EventDispatchRemediationMetadataKeys.DuplicateCommandPolicy]);
        Assert.Equal(remediationCommandState, remediationCommandCatalog.GetByCommandId("cmd-evt-020-retry"));
        Assert.Equal(remediationCommandState, remediationCommandCatalog.Latest);
        Assert.Equal(remediationCommandState, Assert.Single(remediationCommandCatalog.GetByOutboxId("entity-framework-outbox")));
        Assert.Equal(remediationCommandState, Assert.Single(remediationCommandCatalog.GetByMessageId("evt-020")));
        Assert.Empty(remediationCommandCatalog.GetByMessageId("evt-missing"));
        Assert.Equal(remediationCommandState, Assert.Single(remediationCommandCatalog.GetByChannelId("catalog-events")));
        Assert.Empty(remediationCommandCatalog.GetByChannelId("billing-events"));
        Assert.Equal(remediationCommandState, Assert.Single(remediationCommandCatalog.GetByOperationId("retry-now")));
        Assert.Empty(remediationCommandCatalog.GetByOperationId("skip"));
        Assert.Equal(remediationCommandState, Assert.Single(remediationCommandCatalog.GetByActorId("operator-001")));
        Assert.Empty(remediationCommandCatalog.GetByActorId("operator-002"));
        Assert.Equal(remediationCommandState, Assert.Single(remediationCommandCatalog.GetByCorrelationId("corr-command-020")));
        Assert.Empty(remediationCommandCatalog.GetByCorrelationId("corr-command-020-duplicate"));
        Assert.Equal(remediationCommandState, Assert.Single(remediationCommandCatalog.GetByReason(retryReason)));
        Assert.Empty(remediationCommandCatalog.GetByReason(duplicateReason));
        Assert.Equal(remediationCommandState, Assert.Single(remediationCommandCatalog.GetByOutcome("accepted")));
        Assert.Equal(remediationCommandState, Assert.Single(remediationCommandCatalog.GetByDispatchOutcome("retry-scheduled")));
        Assert.Empty(remediationCommandCatalog.GetByDispatchOutcome("skipped"));
        Assert.Equal(1, remediationCommandCatalog.Summary.TotalCommandCount);
        Assert.Equal(1, remediationCommandCatalog.Summary.AcceptedCount);
        Assert.Equal(0, remediationCommandCatalog.Summary.RejectedCount);
        Assert.Equal(0, remediationCommandCatalog.Summary.ErrorCount);
        Assert.Equal(0, remediationCommandCatalog.Summary.DuplicateCommandCount);
        Assert.Equal("cmd-evt-020-retry", remediationCommandCatalog.Summary.LastCommandId);
        Assert.Equal("retry-now", remediationCommandCatalog.Summary.LastOperationId);
        Assert.Equal(EventDispatchRemediationOutcomes.Accepted, remediationCommandCatalog.Summary.LastOutcome);
        Assert.Equal(EventDispatchExecutionOutcomes.RetryScheduled, remediationCommandCatalog.Summary.LastDispatchOutcome);
        Assert.True(remediationCommandCatalog.Summary.HasCommands);
        Assert.False(remediationCommandCatalog.Summary.HasFailures);
        Assert.Equal(256, remediationCommandCatalog.Retention.HistoryLimit);
        Assert.Equal(1, remediationCommandCatalog.Retention.RetainedCommandCount);
        Assert.Equal(1, remediationCommandCatalog.Retention.TotalRecordedCommandCount);
        Assert.Equal(0, remediationCommandCatalog.Retention.DroppedCommandCount);
        Assert.False(remediationCommandCatalog.Retention.Truncated);
        Assert.Equal("cmd-evt-020-retry", remediationCommandCatalog.Retention.OldestRetainedCommandId);
        Assert.Equal("cmd-evt-020-retry", remediationCommandCatalog.Retention.LatestRetainedCommandId);
        eventingSurfaces = technologyCatalog.GetByTechnology("event-driven-integration");
        var remediationCommandSurface = Assert.Single(eventingSurfaces, surface => surface.SurfaceId == "event-dispatch-remediation-commands");
        var remediationCommandCatalogEntry = Assert.Single(remediationCommandSurface.Entries, entry => entry.Id == "event-dispatch-remediation-commands");
        Assert.Equal("catalog", remediationCommandCatalogEntry.Metadata["entryKind"]);
        Assert.Equal("1", remediationCommandCatalogEntry.Metadata["commandStateCount"]);
        Assert.Equal("1", remediationCommandCatalogEntry.Metadata["summaryTotalCommandCount"]);
        Assert.Equal("1", remediationCommandCatalogEntry.Metadata["summaryAcceptedCount"]);
        Assert.Equal("0", remediationCommandCatalogEntry.Metadata["summaryRejectedCount"]);
        Assert.Equal("false", remediationCommandCatalogEntry.Metadata["summaryHasFailures"]);
        Assert.Equal("0", remediationCommandCatalogEntry.Metadata["commandHistoryLimit"]);
        Assert.Equal("1", remediationCommandCatalogEntry.Metadata["retainedCommandCount"]);
        Assert.Equal("1", remediationCommandCatalogEntry.Metadata["totalRecordedCommandCount"]);
        Assert.Equal("0", remediationCommandCatalogEntry.Metadata["droppedCommandCount"]);
        Assert.Equal("false", remediationCommandCatalogEntry.Metadata["retentionTruncated"]);
        Assert.Equal("durable", remediationCommandCatalogEntry.Metadata["commandJournalDurability"]);
        Assert.Equal("cross-node", remediationCommandCatalogEntry.Metadata["commandJournalScope"]);
        Assert.Equal("Cephalon.Data.EntityFramework", remediationCommandCatalogEntry.Metadata["commandJournalProvider"]);
        Assert.Equal("entity-framework-table", remediationCommandCatalogEntry.Metadata["commandJournalStorage"]);
        Assert.Equal("true", remediationCommandCatalogEntry.Metadata["commandCrossNodeCommandAudit"]);
        Assert.Equal("durable", remediationCommandCatalogEntry.Metadata["commandJournalReplayCursor"]);
        Assert.Equal("oldest-first-observed-utc-command-id", remediationCommandCatalogEntry.Metadata["commandJournalReplayCursorOrder"]);
        Assert.Equal("command-journal", remediationCommandCatalogEntry.Metadata["commandJournalReplayCursorScope"]);
        Assert.Equal("true", remediationCommandCatalogEntry.Metadata["hasLatestCommand"]);
        Assert.Equal("cmd-evt-020-retry", remediationCommandCatalogEntry.Metadata["latestCommandId"]);
        Assert.Equal("retry-now", remediationCommandCatalogEntry.Metadata["latestOperationId"]);
        Assert.Equal("accepted", remediationCommandCatalogEntry.Metadata["latestOutcome"]);
        Assert.Equal("retry-scheduled", remediationCommandCatalogEntry.Metadata["latestDispatchOutcome"]);
        Assert.Equal("cmd-evt-020-retry", remediationCommandCatalogEntry.Metadata["oldestRetainedCommandId"]);
        Assert.Equal("cmd-evt-020-retry", remediationCommandCatalogEntry.Metadata["latestRetainedCommandId"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands", remediationCommandCatalogEntry.Metadata["commandListRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/{commandId}", remediationCommandCatalogEntry.Metadata["commandResultRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/summary", remediationCommandCatalogEntry.Metadata["commandSummaryRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/in-doubt", remediationCommandCatalogEntry.Metadata["commandInDoubtRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/in-doubt/summary", remediationCommandCatalogEntry.Metadata["commandInDoubtSummaryRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/in-doubt/oldest", remediationCommandCatalogEntry.Metadata["commandOldestInDoubtRoute"]);
        Assert.Equal("beforeUtc", remediationCommandCatalogEntry.Metadata["commandInDoubtQuery"]);
        Assert.Equal("inclusive-observed-utc-before-or-equal", remediationCommandCatalogEntry.Metadata["commandInDoubtCutoffPolicy"]);
        Assert.Equal("beforeUtc", remediationCommandCatalogEntry.Metadata["commandInDoubtSummaryQuery"]);
        Assert.Equal("retained-reserved-summary-observed-utc-before-or-equal", remediationCommandCatalogEntry.Metadata["commandInDoubtSummaryPolicy"]);
        Assert.Equal("beforeUtc", remediationCommandCatalogEntry.Metadata["commandOldestInDoubtQuery"]);
        Assert.Equal("oldest-retained-reserved-observed-utc-before-or-equal", remediationCommandCatalogEntry.Metadata["commandOldestInDoubtPolicy"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/outboxes/{outboxId}", remediationCommandCatalogEntry.Metadata["commandOutboxRoute"]);
        Assert.Equal("inclusive-observed-utc", remediationCommandCatalogEntry.Metadata["commandObservationWindowPolicy"]);
        Assert.Equal("positive-integer-newest-first", remediationCommandCatalogEntry.Metadata["commandReadLimitPolicy"]);
        Assert.Equal("pageSize,continuationToken", remediationCommandCatalogEntry.Metadata["commandPaginationQuery"]);
        Assert.Equal("opaque-signed-route-bound-continuation-token-newest-first", remediationCommandCatalogEntry.Metadata["commandPaginationPolicy"]);
        Assert.Equal("false", remediationCommandCatalogEntry.Metadata["wolverineRequired"]);
        Assert.Equal("true", remediationCommandCatalogEntry.Metadata["providerNeutral"]);
        Assert.Equal("unique-command-id", remediationCommandCatalogEntry.Metadata[EventDispatchRemediationMetadataKeys.CommandIdempotencyPolicy]);
        Assert.Equal("reject-without-mutation", remediationCommandCatalogEntry.Metadata[EventDispatchRemediationMetadataKeys.DuplicateCommandPolicy]);
        var remediationCommandEntry = Assert.Single(remediationCommandSurface.Entries, entry => entry.Id == "cmd-evt-020-retry");
        Assert.Equal("command-result", remediationCommandEntry.Metadata["entryKind"]);
        Assert.Equal("event-dispatch-remediation-commands", remediationCommandEntry.Metadata["commandCatalogEntryId"]);
        Assert.Equal("retry-now", remediationCommandEntry.Metadata["operationId"]);
        Assert.Equal("accepted", remediationCommandEntry.Metadata["outcome"]);
        Assert.Equal("retry-scheduled", remediationCommandEntry.Metadata["dispatchOutcome"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands", remediationCommandEntry.Metadata["commandListRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/{commandId}", remediationCommandEntry.Metadata["commandResultRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/summary", remediationCommandEntry.Metadata["commandSummaryRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/latest", remediationCommandEntry.Metadata["commandLatestRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/retention", remediationCommandEntry.Metadata["commandRetentionRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/in-doubt", remediationCommandEntry.Metadata["commandInDoubtRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/in-doubt/summary", remediationCommandEntry.Metadata["commandInDoubtSummaryRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/in-doubt/oldest", remediationCommandEntry.Metadata["commandOldestInDoubtRoute"]);
        Assert.Equal("beforeUtc", remediationCommandEntry.Metadata["commandInDoubtQuery"]);
        Assert.Equal("inclusive-observed-utc-before-or-equal", remediationCommandEntry.Metadata["commandInDoubtCutoffPolicy"]);
        Assert.Equal("newest-first", remediationCommandEntry.Metadata["commandInDoubtDetailOrder"]);
        Assert.Equal("beforeUtc", remediationCommandEntry.Metadata["commandInDoubtSummaryQuery"]);
        Assert.Equal("retained-reserved-summary-observed-utc-before-or-equal", remediationCommandEntry.Metadata["commandInDoubtSummaryPolicy"]);
        Assert.Equal("beforeUtc", remediationCommandEntry.Metadata["commandOldestInDoubtQuery"]);
        Assert.Equal("oldest-retained-reserved-observed-utc-before-or-equal", remediationCommandEntry.Metadata["commandOldestInDoubtPolicy"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/outboxes/{outboxId}", remediationCommandEntry.Metadata["commandOutboxRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/observations?fromUtc={fromUtc}&toUtc={toUtc}", remediationCommandEntry.Metadata["commandObservationRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/observations/summary?fromUtc={fromUtc}&toUtc={toUtc}", remediationCommandEntry.Metadata["commandObservationSummaryRoute"]);
        Assert.Equal("fromUtc,toUtc", remediationCommandEntry.Metadata["commandObservationWindowQuery"]);
        Assert.Equal("inclusive-observed-utc", remediationCommandEntry.Metadata["commandObservationWindowPolicy"]);
        Assert.Equal("newest-first", remediationCommandEntry.Metadata["commandObservationWindowDetailOrder"]);
        Assert.Equal("available", remediationCommandEntry.Metadata["commandObservationWindowSummary"]);
        Assert.Equal("reject-reversed-window", remediationCommandEntry.Metadata["commandObservationWindowInvalidBounds"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/operations/{operationId}", remediationCommandEntry.Metadata["commandOperationRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/actors/{actorId}", remediationCommandEntry.Metadata["commandActorRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/correlations/{correlationId}", remediationCommandEntry.Metadata["commandCorrelationRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/reasons/{reason}", remediationCommandEntry.Metadata["commandReasonRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/messages/{messageId}", remediationCommandEntry.Metadata["commandMessageRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/channels/{channelId}", remediationCommandEntry.Metadata["commandChannelRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/dispatch-outcomes/{dispatchOutcome}", remediationCommandEntry.Metadata["commandDispatchOutcomeRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/outcomes/{outcome}", remediationCommandEntry.Metadata["commandOutcomeRoute"]);
        Assert.Equal("limit", remediationCommandEntry.Metadata["commandReadLimitQuery"]);
        Assert.Equal("positive-integer-newest-first", remediationCommandEntry.Metadata["commandReadLimitPolicy"]);
        Assert.Equal("list-and-filter-routes", remediationCommandEntry.Metadata["commandReadLimitAppliesTo"]);
        Assert.Equal("all,in-doubt,observations,outboxes,messages,channels,operations,actors,correlations,reasons,dispatch-outcomes,outcomes", remediationCommandEntry.Metadata["commandReadLimitRoutes"]);
        Assert.Equal("pageSize,continuationToken", remediationCommandEntry.Metadata["commandPaginationQuery"]);
        Assert.Equal("opaque-signed-route-bound-continuation-token-newest-first", remediationCommandEntry.Metadata["commandPaginationPolicy"]);
        Assert.Equal("items,pageSize,returnedCount,totalRetainedCount,continuationToken,nextContinuationToken,hasMore", remediationCommandEntry.Metadata["commandPaginationResponse"]);
        Assert.Equal("false", remediationCommandEntry.Metadata["wolverineRequired"]);
        Assert.Equal("unique-command-id", remediationCommandEntry.Metadata[EventDispatchRemediationMetadataKeys.CommandIdempotencyPolicy]);
        Assert.Equal("reject-without-mutation", remediationCommandEntry.Metadata[EventDispatchRemediationMetadataKeys.DuplicateCommandPolicy]);

        var commandState = Assert.Single(runtimeCatalog.States);
        Assert.Equal(EventDispatchExecutionOutcomes.RetryScheduled, commandState.LastOutcome);
        Assert.True(commandState.RetryPending);
        Assert.False(commandState.TerminalFailure);
        Assert.Equal(4, commandState.LastAttempt);
        Assert.Equal("cmd-evt-020-retry", commandState.Metadata["operatorCommandId"]);
        Assert.Equal("retry-now", commandState.Metadata["operatorCommand"]);
        Assert.Equal("operator-command", commandState.Metadata[EventDispatchRuntimeMetadataKeys.RetryScope]);
        Assert.Equal("dispatch-store", commandState.Metadata[EventDispatchRuntimeMetadataKeys.RetryDurability]);
        Assert.Equal("operator-retry-now", commandState.Metadata[EventDispatchRuntimeMetadataKeys.RetryOutcome]);
    }

    [Fact]
    public async Task AddEntityFrameworkDataCanPersistDispatchStoreStateForPendingMessages()
    {
        var databaseName = $"cephalon-data-ef-event-dispatch-store-{Guid.NewGuid():N}";
        var nextRetryAtUtc = DateTimeOffset.UtcNow.AddMinutes(5);
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS", "Outbox"],
                technologies: ["EventDrivenIntegration"],
                transports: ["RestApi"],
                data: new DataSettings(
                    provider: "EntityFramework",
                    outboxEnabled: true),
                messaging: new MessagingSettings(provider: "InMemoryChannels")));
            engine.AddModule(new PlatformTestModule());
            engine.AddEventing(options =>
            {
                options.Channels.Add(new EventChannelDescriptor(
                    id: "catalog-events",
                    displayName: "Catalog Events",
                    description: "Catalog integration events."));
            });
            engine.AddEntityFrameworkData<OutboxCatalogDbContext>(
                options => options.UseInMemoryDatabase(databaseName),
                configure: options => options.RegisterOutbox = true);
        });

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();
        var dispatchStore = scope.ServiceProvider.GetRequiredService<IEventDispatchStore>();
        var dbContext = scope.ServiceProvider.GetRequiredService<OutboxCatalogDbContext>();

        await publisher.PublishAsync(new EventPublication(
            id: "evt-200",
            channelId: "catalog-events",
            eventType: "catalog.item.created",
            payload: "{\"id\":\"item-200\"}",
            occurredAtUtc: DateTimeOffset.UtcNow,
            correlationId: "corr-200"));

        var pendingDispatches = await dispatchStore.ReadPendingAsync(10);
        var pendingDispatch = Assert.Single(pendingDispatches);
        Assert.Equal("entity-framework-outbox", pendingDispatch.OutboxId);
        Assert.Equal("evt-200", pendingDispatch.MessageId);
        Assert.Equal("catalog-events", pendingDispatch.ChannelId);
        Assert.Equal(0, pendingDispatch.DispatchAttemptCount);

        await dispatchStore.ApplyReportAsync(new EventDispatchExecutionReport(
            outboxId: "entity-framework-outbox",
            channelId: "catalog-events",
            outcome: EventDispatchExecutionOutcomes.Started,
            observedAtUtc: DateTimeOffset.UtcNow,
            messageId: "evt-200",
            attempt: 1));
        await dispatchStore.ApplyReportAsync(new EventDispatchExecutionReport(
            outboxId: "entity-framework-outbox",
            channelId: "catalog-events",
            outcome: EventDispatchExecutionOutcomes.RetryScheduled,
            observedAtUtc: DateTimeOffset.UtcNow,
            messageId: "evt-200",
            attempt: 1,
            error: "Broker temporarily unavailable",
            metadata: new Dictionary<string, string>
            {
                [EventDispatchRuntimeMetadataKeys.NextRetryAtUtc] = nextRetryAtUtc.ToString("O")
            }));

        var pendingDuringBackoff = await dispatchStore.ReadPendingAsync(10);
        Assert.Empty(pendingDuringBackoff);

        await dispatchStore.ApplyReportAsync(new EventDispatchExecutionReport(
            outboxId: "entity-framework-outbox",
            channelId: "catalog-events",
            outcome: EventDispatchExecutionOutcomes.Succeeded,
            observedAtUtc: DateTimeOffset.UtcNow.AddMinutes(10),
            messageId: "evt-200",
            attempt: 2));

        var outboxEntry = await dbContext.OutboxMessages.SingleAsync();
        Assert.Equal(2, outboxEntry.DispatchAttemptCount);
        Assert.NotNull(outboxEntry.DispatchedAtUtc);
        Assert.Null(outboxEntry.NextAttemptAtUtc);
        Assert.Empty(await dispatchStore.ReadPendingAsync(10));
    }

    [Fact]
    public async Task AddEntityFrameworkDataStopsPendingReadsForTerminalDispatchFailures()
    {
        var databaseName = $"cephalon-data-ef-event-dispatch-terminal-{Guid.NewGuid():N}";
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS", "Outbox"],
                technologies: ["EventDrivenIntegration"],
                transports: ["RestApi"],
                data: new DataSettings(
                    provider: "EntityFramework",
                    outboxEnabled: true),
                messaging: new MessagingSettings(provider: "InMemoryChannels")));
            engine.AddModule(new PlatformTestModule());
            engine.AddEventing(options =>
            {
                options.Channels.Add(new EventChannelDescriptor(
                    id: "catalog-events",
                    displayName: "Catalog Events",
                    description: "Catalog integration events."));
            });
            engine.AddEntityFrameworkData<OutboxCatalogDbContext>(
                options => options.UseInMemoryDatabase(databaseName),
                configure: options => options.RegisterOutbox = true);
        });

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();
        var dispatchStore = scope.ServiceProvider.GetRequiredService<IEventDispatchStore>();
        var dbContext = scope.ServiceProvider.GetRequiredService<OutboxCatalogDbContext>();

        await publisher.PublishAsync(new EventPublication(
            id: "evt-terminal-200",
            channelId: "catalog-events",
            eventType: "catalog.item.created",
            payload: "{\"id\":\"item-terminal-200\"}",
            occurredAtUtc: DateTimeOffset.UtcNow,
            correlationId: "corr-terminal-200"));

        Assert.Single(await dispatchStore.ReadPendingAsync(10));

        await dispatchStore.ApplyReportAsync(new EventDispatchExecutionReport(
            outboxId: "entity-framework-outbox",
            channelId: "catalog-events",
            outcome: EventDispatchExecutionOutcomes.Failed,
            observedAtUtc: DateTimeOffset.UtcNow,
            messageId: "evt-terminal-200",
            attempt: 3,
            error: "Dispatch retry budget exhausted.",
            metadata: new Dictionary<string, string>
            {
                [EventDispatchRuntimeMetadataKeys.TerminalFailure] = "true",
                [EventDispatchRuntimeMetadataKeys.RetryExhausted] = "true"
            }));

        var outboxEntry = await dbContext.OutboxMessages.SingleAsync();
        Assert.Equal(3, outboxEntry.DispatchAttemptCount);
        Assert.NotNull(outboxEntry.DispatchedAtUtc);
        Assert.Null(outboxEntry.NextAttemptAtUtc);
        Assert.Empty(await dispatchStore.ReadPendingAsync(10));
    }

    [Fact]
    public async Task AddBehaviorEventingBridgeCanStageSagaChoreographyPublicationsThroughOutbox()
    {
        var databaseName = $"cephalon-data-ef-saga-choreography-{Guid.NewGuid():N}";
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS", "Outbox"],
                technologies: ["EventDrivenIntegration"],
                transports: ["RestApi"],
                data: new DataSettings(
                    provider: "EntityFramework",
                    outboxEnabled: true),
                messaging: new MessagingSettings(provider: "InMemoryChannels")));
            engine.AddModule(new PlatformTestModule());
            engine.AddBehaviors(behaviors => behaviors.AddBehaviorPatterns());
            engine.AddEventing(options =>
            {
                options.Channels.Add(new EventChannelDescriptor(
                    id: "catalog-events",
                    displayName: "Catalog Events",
                    description: "Catalog integration events."));
            });
            engine.AddBehaviorEventingBridge();
            engine.AddEntityFrameworkData<OutboxCatalogDbContext>(
                options => options.UseInMemoryDatabase(databaseName),
                configure: options => options.RegisterOutbox = true);
        });

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var runtime = scope.ServiceProvider.GetRequiredService<IRuntime>();
        var publisher = scope.ServiceProvider.GetRequiredService<ISagaChoreographyPublisher>();
        var dbContext = scope.ServiceProvider.GetRequiredService<OutboxCatalogDbContext>();

        await publisher.PublishAsync(new SagaChoreographyPublication(
            id: "evt-choreo-001",
            channelId: "catalog-events",
            eventType: "catalog.inventory.reserved",
            payload: "{\"reservationId\":\"res-001\"}",
            occurredAtUtc: DateTimeOffset.UtcNow,
            correlationId: "corr-saga-001",
            tenantId: "tenant-001",
            isCompensation: true,
            headers: new Dictionary<string, string>
            {
                ["X-Saga-Id"] = "saga-001"
            },
            metadata: new Dictionary<string, string>
            {
                ["step"] = "reserve-inventory"
            }));

        var outboxEntry = await dbContext.OutboxMessages.SingleAsync();
        var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(outboxEntry.HeadersJson);
        var metadata = JsonSerializer.Deserialize<Dictionary<string, string>>(outboxEntry.MetadataJson);

        Assert.Equal("evt-choreo-001", outboxEntry.Id);
        Assert.Equal("catalog-events", outboxEntry.ChannelId);
        Assert.Equal("catalog.inventory.reserved", outboxEntry.MessageType);
        Assert.Equal("corr-saga-001", outboxEntry.CorrelationId);
        Assert.Equal("tenant-001", outboxEntry.TenantId);
        Assert.NotNull(headers);
        Assert.NotNull(metadata);
        Assert.Equal("saga-001", headers["X-Saga-Id"]);
        Assert.Equal("reserve-inventory", metadata["step"]);
        Assert.Equal("saga-choreography", metadata["cephalon.pattern"]);
        Assert.Equal("eventing.behaviors", metadata["cephalon.publisherBridge"]);
        Assert.Equal("true", metadata["cephalon.isCompensation"]);
        Assert.Contains(
            runtime.Manifest.Capabilities,
            capability => capability.Key == "eventing.behaviors.saga-choreography" &&
                capability.Metadata["handoff"] == "eventing.publish");
    }

    [Fact]
    public void AddBehaviorEventingBridgePreservesExplicitSagaChoreographyPublisherOverrides()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ISagaChoreographyPublisher, TestSagaChoreographyPublisher>();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS"],
                technologies: ["EventDrivenIntegration"],
                transports: ["RestApi"],
                messaging: new MessagingSettings(provider: "InMemoryChannels")));
            engine.AddModule(new PlatformTestModule());
            engine.AddBehaviors(behaviors => behaviors.AddBehaviorPatterns());
            engine.AddEventing(options =>
            {
                options.Channels.Add(new EventChannelDescriptor(
                    id: "catalog-events",
                    displayName: "Catalog Events",
                    description: "Catalog integration events."));
            });
            engine.AddBehaviorEventingBridge();
        });

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<ISagaChoreographyPublisher>();
        var runtime = scope.ServiceProvider.GetRequiredService<IRuntime>();

        Assert.IsType<TestSagaChoreographyPublisher>(publisher);
        Assert.DoesNotContain(
            runtime.Manifest.Capabilities,
            capability => capability.Key == "eventing.behaviors.saga-choreography");
    }

    [Fact]
    public void AddBehaviorEventingBridgeRequiresActiveEventingPublishingPath()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS"],
                technologies: ["EventDrivenIntegration"],
                transports: ["RestApi"],
                messaging: new MessagingSettings(provider: "InMemoryChannels")));
            engine.AddModule(new PlatformTestModule());
            engine.AddBehaviors(behaviors => behaviors.AddBehaviorPatterns());
            engine.AddEventing(options =>
            {
                options.Channels.Add(new EventChannelDescriptor(
                    id: "catalog-events",
                    displayName: "Catalog Events",
                    description: "Catalog integration events."));
            });
            engine.AddBehaviorEventingBridge();
        });

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var exception = Assert.Throws<InvalidOperationException>(() =>
            scope.ServiceProvider.GetRequiredService<ISagaChoreographyPublisher>());

        Assert.Contains("Cephalon.Eventing.Behaviors requires the Cephalon.Eventing publishing path", exception.Message, StringComparison.Ordinal);
    }

    private static void AssertRetention(
        EventDispatchRemediationRuntimeRetention retention,
        int expectedCount,
        string? expectedOldestCommandId,
        string? expectedLatestCommandId,
        int? expectedHistoryLimit = null)
    {
        if (expectedHistoryLimit is { } historyLimit)
        {
            Assert.Equal(historyLimit, retention.HistoryLimit);
        }

        Assert.Equal(expectedCount, retention.RetainedCommandCount);
        Assert.Equal(expectedCount, retention.TotalRecordedCommandCount);
        Assert.Equal(0, retention.DroppedCommandCount);
        Assert.False(retention.Truncated);
        Assert.Equal(expectedOldestCommandId, retention.OldestRetainedCommandId);
        Assert.Equal(expectedLatestCommandId, retention.LatestRetainedCommandId);
    }

    private sealed class MutableTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset now = now;

        public override DateTimeOffset GetUtcNow()
        {
            return now;
        }

        public void Advance(TimeSpan duration)
        {
            now = now.Add(duration);
        }
    }

    private sealed class TestSagaChoreographyPublisher : ISagaChoreographyPublisher
    {
        public ValueTask PublishAsync(SagaChoreographyPublication publication, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(publication);
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.CompletedTask;
        }
    }
}
