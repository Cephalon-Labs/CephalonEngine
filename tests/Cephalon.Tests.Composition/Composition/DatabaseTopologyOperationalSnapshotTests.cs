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
        var playbook = provider.GetRequiredService<IDatabaseMigrationOperationalPlaybookProvider>().CreatePlaybook();
        var snapshot = provider.GetRequiredService<IDatabaseTopologyOperationalSnapshotProvider>().CreateSnapshot();

        Assert.Equal(1, playbook.TargetCount);
        Assert.Equal(1, playbook.ProductionReadyTargetCount);
        Assert.Equal(0, playbook.ManualPathTargetCount);
        Assert.Equal(1, playbook.ApplyOnStartupTargetCount);
        Assert.Equal(0, playbook.CoordinationRequiredTargetCount);
        Assert.Equal(1, playbook.ExecutionGroupCount);
        Assert.Equal(0, playbook.CoordinationRequiredGroupCount);
        var playbookStep = Assert.Single(playbook.Steps);
        var executionGroup = Assert.Single(playbook.ExecutionGroups);
        Assert.Equal(1, playbookStep.Order);
        Assert.Equal("write", playbookStep.DatabaseMigrationId);
        Assert.Equal("write", playbookStep.RequestedRoleId);
        Assert.Equal("write", playbookStep.ResolvedRoleId);
        Assert.Equal(DatabaseMigrationStatus.Succeeded, playbookStep.Status);
        Assert.Equal("startup-hosted-service", playbookStep.ExecutionMode);
        Assert.True(playbookStep.ApplyOnStartup);
        Assert.True(playbookStep.HasProductionRecommendedCommand);
        Assert.NotNull(playbookStep.ProductionCommand);
        Assert.Equal("bundle", playbookStep.ProductionCommand!.Id);
        Assert.False(playbookStep.RequiresPhysicalTargetCoordination);
        Assert.Empty(playbookStep.CoordinatedMigrationIds);
        Assert.NotNull(provider.GetRequiredService<IDatabaseRoleCatalog>().GetById("write"));
        Assert.Null(playbookStep.ManualCommand);
        Assert.Equal(1, executionGroup.Order);
        Assert.Equal(DatabaseMigrationStatus.Succeeded, executionGroup.Status);
        Assert.Equal(1, executionGroup.TargetCount);
        Assert.Equal(["write"], executionGroup.DatabaseMigrationIds);
        Assert.Equal(["write"], executionGroup.RequestedRoleIds);
        Assert.Equal(["write"], executionGroup.ResolvedRoleIds);
        Assert.False(executionGroup.RequiresPhysicalTargetCoordination);
        Assert.Null(executionGroup.CoordinationHint);
        Assert.Equal(1, executionGroup.ProductionReadyTargetCount);
        Assert.Equal(0, executionGroup.ManualPathTargetCount);
        Assert.Equal(1, executionGroup.ApplyOnStartupTargetCount);
        var productionCommand = Assert.Single(executionGroup.ProductionCommands);
        Assert.Equal(1, productionCommand.Order);
        Assert.Equal("write", productionCommand.DatabaseMigrationId);
        Assert.Equal("write", productionCommand.RequestedRoleId);
        Assert.Equal("write", productionCommand.ResolvedRoleId);
        Assert.Equal("bundle", productionCommand.Command.Id);
        Assert.NotNull(executionGroup.ProductionCommandBatch);
        Assert.Equal("production", executionGroup.ProductionCommandBatch!.Id);
        Assert.Equal(1, executionGroup.ProductionCommandBatch.CommandCount);
        Assert.Equal(["write"], executionGroup.ProductionCommandBatch.DatabaseMigrationIds);
        Assert.Equal(["bundle"], executionGroup.ProductionCommandBatch.CommandIds);
        Assert.Equal(["dotnet ef migrations bundle --context WriteDbContext"], executionGroup.ProductionCommandBatch.CommandTemplate.Split('\n'));
        Assert.Empty(executionGroup.ManualCommands);
        Assert.Null(executionGroup.ManualCommandBatch);
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
    public void DatabaseTopologyOperationalSnapshotFlagsSharedPhysicalMigrationTargetsForCoordination()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SharedDb"] = "Host=localhost;Database=cephalon_shared",
                ["Engine:Patterns:0"] = "Cqrs",
                ["Engine:Databases:Write:Provider"] = "PostgreSql",
                ["Engine:Databases:Write:ConnectionStringName"] = "SharedDb",
                ["Engine:Databases:Read:Provider"] = "PostgreSql",
                ["Engine:Databases:Read:ConnectionStringName"] = "SharedDb",
                ["Engine:Databases:Migrations:ApplyOnStartup"] = "false",
                ["Engine:Databases:Migrations:Targets:0"] = "write",
                ["Engine:Databases:Migrations:Targets:1"] = "read"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<IDatabaseMigrationContributor>(new StaticDatabaseMigrationContributor(
            new DatabaseMigrationDescriptor(
                id: "write",
                displayName: "Write database migration",
                description: "Applies the write-store schema.",
                requestedRoleId: "write",
                resolvedRoleId: "write",
                executionMode: "manual-or-deploy-time",
                status: DatabaseMigrationStatus.Planned,
                applyOnStartup: false,
                exitAfterApply: false,
                commands:
                [
                    new DatabaseMigrationCommandDescriptor(
                        id: "bundle",
                        displayName: "EF Core migration bundle",
                        description: "Recommended production path.",
                        commandTemplate: "dotnet ef migrations bundle --context WriteDbContext",
                        recommendedForProduction: true),
                    new DatabaseMigrationCommandDescriptor(
                        id: "update",
                        displayName: "EF Core direct update",
                        description: "Manual fallback path.",
                        commandTemplate: "dotnet ef database update --context WriteDbContext",
                        recommendedForProduction: false)
                ]),
            new DatabaseMigrationDescriptor(
                id: "read",
                displayName: "Read database migration",
                description: "Applies the read-store schema.",
                requestedRoleId: "read",
                resolvedRoleId: "read",
                executionMode: "manual-or-deploy-time",
                status: DatabaseMigrationStatus.Planned,
                applyOnStartup: false,
                exitAfterApply: false,
                commands:
                [
                    new DatabaseMigrationCommandDescriptor(
                        id: "bundle",
                        displayName: "EF Core migration bundle",
                        description: "Recommended production path.",
                        commandTemplate: "dotnet ef migrations bundle --context ReadDbContext",
                        recommendedForProduction: true),
                    new DatabaseMigrationCommandDescriptor(
                        id: "update",
                        displayName: "EF Core direct update",
                        description: "Manual fallback path.",
                        commandTemplate: "dotnet ef database update --context ReadDbContext",
                        recommendedForProduction: false)
                ])));
        services.AddCephalon(engine =>
        {
            engine.UseSettings(EngineSettings.FromConfiguration(configuration));
            engine.AddModule(new PlatformTestModule());
        });

        using var provider = services.BuildServiceProvider();
        var roleCatalog = provider.GetRequiredService<IDatabaseRoleCatalog>();
        var writeRole = roleCatalog.GetById("write");
        var readRole = roleCatalog.GetById("read");
        var playbook = provider.GetRequiredService<IDatabaseMigrationOperationalPlaybookProvider>().CreatePlaybook();
        var snapshot = provider.GetRequiredService<IDatabaseTopologyOperationalSnapshotProvider>().CreateSnapshot();

        Assert.NotNull(writeRole);
        Assert.NotNull(readRole);
        Assert.NotNull(writeRole!.PhysicalTargetId);
        Assert.Equal(writeRole.PhysicalTargetId, readRole!.PhysicalTargetId);
        Assert.NotNull(writeRole.PhysicalTargetDisplayName);
        Assert.Contains("SharedDb", writeRole.PhysicalTargetDisplayName!, StringComparison.Ordinal);
        Assert.Equal(["read"], writeRole.PhysicalCoLocatedRoles);
        Assert.Equal(["write"], readRole.PhysicalCoLocatedRoles);

        Assert.Equal(2, playbook.TargetCount);
        Assert.Equal(2, playbook.CoordinationRequiredTargetCount);
        Assert.Equal(1, playbook.ExecutionGroupCount);
        Assert.Equal(1, playbook.CoordinationRequiredGroupCount);
        Assert.All(playbook.Steps, static step => Assert.True(step.RequiresPhysicalTargetCoordination));
        var executionGroup = Assert.Single(playbook.ExecutionGroups);
        Assert.Equal(1, executionGroup.Order);
        Assert.Equal(writeRole.PhysicalTargetId, executionGroup.PhysicalTargetId);
        Assert.Equal(DatabaseMigrationStatus.Planned, executionGroup.Status);
        Assert.Equal(2, executionGroup.TargetCount);
        Assert.True(executionGroup.RequiresPhysicalTargetCoordination);
        Assert.Equal(["read", "write"], executionGroup.DatabaseMigrationIds);
        Assert.Equal(["read", "write"], executionGroup.RequestedRoleIds);
        Assert.Equal(["read", "write"], executionGroup.ResolvedRoleIds);
        Assert.Equal(2, executionGroup.ProductionReadyTargetCount);
        Assert.Equal(2, executionGroup.ManualPathTargetCount);
        Assert.Equal(0, executionGroup.ApplyOnStartupTargetCount);
        Assert.NotNull(executionGroup.CoordinationHint);
        Assert.Contains("coordinated physical-target batch", executionGroup.CoordinationHint!, StringComparison.Ordinal);
        Assert.Equal(["read", "write"], executionGroup.ProductionCommands.Select(static command => command.DatabaseMigrationId));
        Assert.All(executionGroup.ProductionCommands, static command => Assert.Equal("bundle", command.Command.Id));
        Assert.NotNull(executionGroup.ProductionCommandBatch);
        Assert.Equal("production", executionGroup.ProductionCommandBatch!.Id);
        Assert.Equal(2, executionGroup.ProductionCommandBatch.CommandCount);
        Assert.Equal(["read", "write"], executionGroup.ProductionCommandBatch.DatabaseMigrationIds);
        Assert.Equal(["bundle"], executionGroup.ProductionCommandBatch.CommandIds);
        Assert.Equal(
            [
                "dotnet ef migrations bundle --context ReadDbContext",
                "dotnet ef migrations bundle --context WriteDbContext"
            ],
            executionGroup.ProductionCommandBatch.CommandTemplate.Split('\n'));
        Assert.Equal(["read", "write"], executionGroup.ManualCommands.Select(static command => command.DatabaseMigrationId));
        Assert.All(executionGroup.ManualCommands, static command => Assert.Equal("update", command.Command.Id));
        Assert.NotNull(executionGroup.ManualCommandBatch);
        Assert.Equal("manual", executionGroup.ManualCommandBatch!.Id);
        Assert.Equal(2, executionGroup.ManualCommandBatch.CommandCount);
        Assert.Equal(["read", "write"], executionGroup.ManualCommandBatch.DatabaseMigrationIds);
        Assert.Equal(["update"], executionGroup.ManualCommandBatch.CommandIds);
        Assert.Equal(
            [
                "dotnet ef database update --context ReadDbContext",
                "dotnet ef database update --context WriteDbContext"
            ],
            executionGroup.ManualCommandBatch.CommandTemplate.Split('\n'));
        var writeStep = Assert.Single(playbook.Steps, static step => step.DatabaseMigrationId == "write");
        var readStep = Assert.Single(playbook.Steps, static step => step.DatabaseMigrationId == "read");
        Assert.Equal(["read"], writeStep.CoordinatedMigrationIds);
        Assert.Equal(["write"], readStep.CoordinatedMigrationIds);
        Assert.NotNull(writeStep.CoordinationHint);
        Assert.Contains("separate migrations projects", writeStep.CoordinationHint!, StringComparison.Ordinal);
        Assert.NotNull(writeStep.PhysicalTargetDisplayName);
        Assert.Contains("SharedDb", writeStep.PhysicalTargetDisplayName!, StringComparison.Ordinal);

        Assert.Equal("Attention", snapshot.Summary.Status);
        Assert.Contains(snapshot.ActionPlan.Actions, action =>
            action.Id == "coordinate-shared-database-migrations" &&
            action.Category == "migration-guidance" &&
            action.ActionPath == "/engine/database-migration-playbook" &&
            action.SourceRoleIds.SequenceEqual(["read", "write"]) &&
            action.SourceMigrationIds.SequenceEqual(["read", "write"]));
        Assert.Contains(snapshot.Advisories, advisory =>
            advisory.Id == "shared-physical-target-migration-coordination" &&
            advisory.Tone == "Warning" &&
            advisory.ActionPath == "/engine/database-migration-playbook" &&
            advisory.SourceMigrationIds.SequenceEqual(["read", "write"]));
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

        Assert.NotNull(snapshot.DatabaseMigrationPlaybook);
        Assert.Equal(0, snapshot.DatabaseMigrationPlaybook.TargetCount);
        Assert.Equal(0, snapshot.DatabaseMigrationPlaybook.CoordinationRequiredTargetCount);
        Assert.Equal(0, snapshot.DatabaseMigrationPlaybook.ExecutionGroupCount);
        Assert.Equal(0, snapshot.DatabaseMigrationPlaybook.CoordinationRequiredGroupCount);
        Assert.Empty(snapshot.DatabaseMigrationPlaybook.Steps);
        Assert.Empty(snapshot.DatabaseMigrationPlaybook.ExecutionGroups);
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
