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
                id: "write",
                displayName: "Write Database Migration",
                description: "Startup schema apply for the write database role.",
                requestedRoleId: "write",
                resolvedRoleId: "write",
                executionMode: "startup-hosted-service",
                status: DatabaseMigrationStatus.Planned,
                applyOnStartup: true,
                exitAfterApply: false,
                provider: "PostgreSql")
        ]));
        services.AddCephalon(engine =>
        {
            engine.AddModule(new PlatformTestModule());
        });

        using var provider = services.BuildServiceProvider();
        var catalog = provider.GetRequiredService<IDatabaseMigrationCatalog>();

        var migration = Assert.Single(catalog.DatabaseMigrations);

        Assert.Equal("write", migration.Id);
        Assert.Equal(DatabaseMigrationStatus.Planned, migration.Status);
        Assert.Equal("startup-hosted-service", migration.ExecutionMode);
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
