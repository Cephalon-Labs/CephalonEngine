using Cephalon.Abstractions.Data;
using Cephalon.Engine.Composition;
using Cephalon.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Composition;

public sealed class DatabaseMigrationCatalogTests
{
    [Fact]
    public void DatabaseMigrationCatalogAggregatesContributors()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IDatabaseMigrationContributor>(new StubDatabaseMigrationContributor(
        [
            new DatabaseMigrationDescriptor(
                id: "history",
                displayName: "History Database Migration",
                description: "Startup schema apply for the history database role.",
                requestedRoleId: "history",
                resolvedRoleId: "history",
                executionMode: "startup-hosted-service",
                status: DatabaseMigrationStatus.Planned,
                applyOnStartup: true,
                exitAfterApply: false,
                provider: "PostgreSql",
                recommendedExecutionOrder: 3)
        ]));
        services.AddSingleton<IDatabaseMigrationContributor>(new StubDatabaseMigrationContributor(
        [
            new DatabaseMigrationDescriptor(
                id: "write",
                displayName: "Write Database Migration",
                description: "Startup schema apply for the write database role.",
                requestedRoleId: "write",
                resolvedRoleId: "write",
                executionMode: "startup-hosted-service",
                status: DatabaseMigrationStatus.Planned,
                applyOnStartup: true,
                exitAfterApply: false,
                provider: "PostgreSql",
                recommendedExecutionOrder: 1,
                commands:
                [
                    new DatabaseMigrationCommandDescriptor(
                        id: "bundle",
                        displayName: "Migration bundle",
                        description: "Build a migration bundle for the write role.",
                        commandTemplate: "dotnet ef migrations bundle --context SampleWriteDbContext",
                        recommendedForProduction: true,
                        toolId: "dotnet-ef",
                        executionCategory: "deploy-time",
                        workingDirectoryHint: "startup-project")
                ])
        ]));
        services.AddCephalon(engine =>
        {
            engine.AddModule(new PlatformTestModule());
        });

        using var provider = services.BuildServiceProvider();
        var catalog = provider.GetRequiredService<IDatabaseMigrationCatalog>();

        var migrations = catalog.DatabaseMigrations;
        Assert.Equal(2, migrations.Count);

        var writeMigration = migrations[0];
        var historyMigration = migrations[1];

        Assert.Equal("write", writeMigration.Id);
        Assert.Equal(1, writeMigration.RecommendedExecutionOrder);
        Assert.Equal(DatabaseMigrationStatus.Planned, writeMigration.Status);
        Assert.Equal("startup-hosted-service", writeMigration.ExecutionMode);
        Assert.Equal("history", historyMigration.Id);
        Assert.Equal(3, historyMigration.RecommendedExecutionOrder);
        var command = Assert.Single(writeMigration.Commands);
        Assert.Equal("bundle", command.Id);
        Assert.Equal("dotnet-ef", command.ToolId);
        Assert.Equal("deploy-time", command.ExecutionCategory);
        Assert.Equal("startup-project", command.WorkingDirectoryHint);
    }

    [Fact]
    public void DatabaseMigrationCatalogRejectsDuplicateMigrationIds()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IDatabaseMigrationContributor>(new StubDatabaseMigrationContributor(
        [
            new DatabaseMigrationDescriptor(
                id: "write",
                displayName: "Write Database Migration",
                description: "First writer.",
                requestedRoleId: "write",
                resolvedRoleId: "write",
                executionMode: "startup-hosted-service",
                status: DatabaseMigrationStatus.Planned,
                applyOnStartup: true,
                exitAfterApply: false)
        ]));
        services.AddSingleton<IDatabaseMigrationContributor>(new StubDatabaseMigrationContributor(
        [
            new DatabaseMigrationDescriptor(
                id: "write",
                displayName: "Write Database Migration Duplicate",
                description: "Second writer.",
                requestedRoleId: "write",
                resolvedRoleId: "write",
                executionMode: "startup-hosted-service",
                status: DatabaseMigrationStatus.Planned,
                applyOnStartup: true,
                exitAfterApply: false)
        ]));
        services.AddCephalon(engine =>
        {
            engine.AddModule(new PlatformTestModule());
        });

        using var provider = services.BuildServiceProvider();
        var catalog = provider.GetRequiredService<IDatabaseMigrationCatalog>();

        var exception = Assert.Throws<InvalidOperationException>(() => _ = catalog.DatabaseMigrations);

        Assert.Contains("write", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("multiple times", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class StubDatabaseMigrationContributor(
        IReadOnlyList<DatabaseMigrationDescriptor> migrations) : IDatabaseMigrationContributor
    {
        public IReadOnlyList<DatabaseMigrationDescriptor> DescribeDatabaseMigrations()
        {
            return migrations;
        }
    }
}
