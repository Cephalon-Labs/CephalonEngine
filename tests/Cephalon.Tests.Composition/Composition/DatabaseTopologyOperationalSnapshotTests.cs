using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Health;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Runtime;
using Cephalon.Tests.Support;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Composition;

public sealed class DatabaseTopologyOperationalSnapshotTests
{
    [Fact]
    public void DatabaseTopologyOperationalSnapshotSummarizesHealthyTopologyWithProductionGuidance()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:WriteDb"] = "Host=localhost;Database=cephalon_write",
                ["Engine:Databases:Write:Provider"] = "PostgreSql",
                ["Engine:Databases:Write:ConnectionStringName"] = "WriteDb",
                ["Engine:Databases:Migrations:Targets:0"] = "write"
            })
            .Build();
        var runtimeContributor = new MutableDatabaseRoleRuntimeContributor();
        runtimeContributor.Replace(new DatabaseRoleRuntimeDescriptor(
            databaseRoleId: "write",
            healthState: HealthState.Healthy,
            healthDescription: "Connectivity probe succeeded.",
            migrationState: "succeeded",
            migrationDescription: "Schema is current.",
            observedAtUtc: new DateTimeOffset(2026, 04, 13, 8, 0, 0, TimeSpan.Zero)));
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<IDatabaseRoleRuntimeContributor>(runtimeContributor);
        services.AddSingleton<IDatabaseMigrationContributor>(new StaticDatabaseMigrationContributor(
            new DatabaseMigrationDescriptor(
                id: "write",
                displayName: "Write database migration",
                description: "Applies the write-store schema.",
                requestedRoleId: "write",
                resolvedRoleId: "write",
                executionMode: "startup-hosted-service",
                status: DatabaseMigrationStatus.Succeeded,
                applyOnStartup: true,
                exitAfterApply: false,
                commands:
                [
                    new DatabaseMigrationCommandDescriptor(
                        id: "bundle",
                        displayName: "EF Core migration bundle",
                        description: "Recommended production path.",
                        commandTemplate: "dotnet ef migrations bundle --context WriteDbContext",
                        recommendedForProduction: true)
                ])));
        services.AddCephalon(engine =>
        {
            engine.UseSettings(EngineSettings.FromConfiguration(configuration));
            engine.AddModule(new PlatformTestModule());
        });

        using var provider = services.BuildServiceProvider();
        var snapshot = provider.GetRequiredService<IDatabaseTopologyOperationalSnapshotProvider>().CreateSnapshot();

        Assert.Equal("Ready", snapshot.Summary.Status);
        Assert.Equal("Database topology is ready", snapshot.Summary.Headline);
        Assert.Equal("/engine/snapshot", snapshot.Summary.ActionPath);
        Assert.Equal(1, snapshot.Summary.RoleCount);
        Assert.Equal(1, snapshot.Summary.HealthyRoleCount);
        Assert.Equal(0, snapshot.Summary.DegradedRoleCount);
        Assert.Equal(0, snapshot.Summary.UnhealthyRoleCount);
        Assert.Equal(1, snapshot.Summary.MigrationTargetCount);
        Assert.Equal(1, snapshot.Summary.SucceededMigrationTargetCount);
        Assert.Equal(0, snapshot.Summary.PendingMigrationTargetCount);
        Assert.Equal(1, snapshot.Summary.ProductionReadyMigrationTargetCount);
        Assert.Equal(1, snapshot.ActionPlan.TotalActionCount);
        Assert.Equal(0, snapshot.ActionPlan.BlockingActionCount);
        Assert.Equal(0, snapshot.ActionPlan.AttentionActionCount);
        Assert.Equal(1, snapshot.ActionPlan.ReadyActionCount);
        Assert.Contains(snapshot.ActionPlan.Actions, action =>
            action.Id == "topology-ready-for-validation" &&
            action.Category == "topology-posture" &&
            action.Tone == "Success" &&
            action.ActionPath == "/engine/snapshot" &&
            action.SourceRoleIds.SequenceEqual(["write"]) &&
            action.SourceMigrationIds.SequenceEqual(["write"]));
        Assert.Contains(snapshot.Advisories, advisory =>
            advisory.Id == "topology-aligned" &&
            advisory.Tone == "Success" &&
            advisory.ActionPath == "/engine/snapshot");
        Assert.Contains(snapshot.Advisories, advisory =>
            advisory.Id == "migration-production-guidance" &&
            advisory.Tone == "Success" &&
            advisory.ActionPath == "/engine/database-migrations");
    }

    [Fact]
    public void DatabaseTopologyOperationalSnapshotBlocksUnhealthyRole()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:WriteDb"] = "Host=localhost;Database=cephalon_write",
                ["Engine:Databases:Write:Provider"] = "PostgreSql",
                ["Engine:Databases:Write:ConnectionStringName"] = "WriteDb"
            })
            .Build();
        var runtimeContributor = new MutableDatabaseRoleRuntimeContributor();
        runtimeContributor.Replace(new DatabaseRoleRuntimeDescriptor(
            databaseRoleId: "write",
            healthState: HealthState.Unhealthy,
            healthDescription: "Connection probe failed.",
            migrationState: "failed",
            migrationDescription: "Startup schema apply failed.",
            observedAtUtc: new DateTimeOffset(2026, 04, 13, 8, 0, 0, TimeSpan.Zero)));

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<IDatabaseRoleRuntimeContributor>(runtimeContributor);
        services.AddCephalon(engine =>
        {
            engine.UseSettings(EngineSettings.FromConfiguration(configuration));
            engine.AddModule(new PlatformTestModule());
        });

        using var provider = services.BuildServiceProvider();
        var snapshot = provider.GetRequiredService<IDatabaseTopologyOperationalSnapshotProvider>().CreateSnapshot();

        Assert.Equal("Blocked", snapshot.Summary.Status);
        Assert.Equal("Database topology is blocked", snapshot.Summary.Headline);
        Assert.Equal("/engine/database-roles", snapshot.Summary.ActionPath);
        Assert.Equal(1, snapshot.Summary.UnhealthyRoleCount);
        Assert.Equal(1, snapshot.ActionPlan.TotalActionCount);
        Assert.Equal(1, snapshot.ActionPlan.BlockingActionCount);
        var action = Assert.Single(snapshot.ActionPlan.Actions);
        Assert.Equal("restore-unhealthy-roles", action.Id);
        Assert.Equal("role-health", action.Category);
        Assert.Equal("Error", action.Tone);
        Assert.Equal(["write"], action.SourceRoleIds);
        var advisory = Assert.Single(snapshot.Advisories, static item => item.Id == "role-health-attention");
        Assert.Equal("Error", advisory.Tone);
        Assert.Equal(["write"], advisory.SourceRoleIds);
    }

    [Fact]
    public void RuntimeSnapshotIncludesDatabaseTopologyOperationalSnapshot()
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

        Assert.NotNull(snapshot.DatabaseTopology);
        Assert.Equal("Ready", snapshot.DatabaseTopology.Summary.Status);
        Assert.Equal(1, snapshot.DatabaseTopology.Summary.RoleCount);
        Assert.Equal(1, snapshot.DatabaseTopology.ActionPlan.TotalActionCount);
        Assert.Contains(snapshot.DatabaseTopology.ActionPlan.Actions, action => action.Id == "topology-ready-for-validation");
        Assert.Contains(snapshot.DatabaseTopology.Advisories, advisory => advisory.Id == "topology-aligned");
    }

    private sealed class StaticDatabaseMigrationContributor(params DatabaseMigrationDescriptor[] descriptors)
        : IDatabaseMigrationContributor
    {
        public IReadOnlyList<DatabaseMigrationDescriptor> DescribeDatabaseMigrations() => descriptors;
    }

    private sealed class MutableDatabaseRoleRuntimeContributor : IDatabaseRoleRuntimeContributor
    {
        private readonly object syncRoot = new();
        private DatabaseRoleRuntimeDescriptor[] descriptors = [];

        public IReadOnlyList<DatabaseRoleRuntimeDescriptor> DescribeDatabaseRoleRuntime()
        {
            lock (syncRoot)
            {
                return descriptors.ToArray();
            }
        }

        public void Replace(params DatabaseRoleRuntimeDescriptor[] next)
        {
            lock (syncRoot)
            {
                descriptors = next;
            }
        }
    }
}
