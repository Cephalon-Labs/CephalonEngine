using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Runtime;
using Cephalon.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Composition;

public sealed class RuntimeIntrospectionSectionTests
{
    [Fact]
    public void SnapshotProjectsPackageSectionsWithoutTopLevelContractChanges()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IRuntimeIntrospectionSectionContributor>(
            new TestSectionContributor("test-operator", "Cephalon.TestPack", ["zeta", "alpha"]));
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(blueprint: "ModularMonolith"));
            engine.AddModule(new PlatformTestModule());
        });

        using var provider = services.BuildServiceProvider();
        var snapshot = provider.GetRequiredService<IRuntimeIntrospectionSnapshotProvider>().CreateSnapshot();

        Assert.Equal(["dependency-health", "test-operator"], snapshot.ExtensionSections.Select(static section => section.Id));

        var section = Assert.Single(snapshot.ExtensionSections, static section => section.Id == "test-operator");
        Assert.Equal("1.0.0", section.SchemaVersion);
        Assert.Equal("Cephalon.TestPack", section.Source);
        Assert.Equal(["alpha", "zeta"], section.Entries.Select(static entry => entry.Id));

        var entry = Assert.Single(section.Entries, static entry => entry.Id == "alpha");
        Assert.Equal("ready", entry.DesiredState);
        Assert.Equal("ready", entry.ObservedState);
        Assert.Equal("true", Assert.Single(entry.Conditions).Status);
        Assert.True(Assert.Single(entry.Actions).RequiresApproval);
    }

    [Fact]
    public void SnapshotRejectsDuplicateSectionIdsAcrossPackages()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IRuntimeIntrospectionSectionContributor>(
            new TestSectionContributor("shared-id", "Cephalon.Alpha", ["alpha"]));
        services.AddSingleton<IRuntimeIntrospectionSectionContributor>(
            new TestSectionContributor("SHARED-ID", "Cephalon.Beta", ["beta"]));
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(blueprint: "ModularMonolith"));
            engine.AddModule(new PlatformTestModule());
        });

        using var provider = services.BuildServiceProvider();
        var snapshotProvider = provider.GetRequiredService<IRuntimeIntrospectionSnapshotProvider>();

        var exception = Assert.Throws<InvalidOperationException>(snapshotProvider.CreateSnapshot);

        Assert.Contains("shared-id", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Cephalon.Alpha", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Beta", exception.Message, StringComparison.Ordinal);
    }

    private sealed class TestSectionContributor(
        string sectionId,
        string source,
        IReadOnlyList<string> entryIds) : IRuntimeIntrospectionSectionContributor
    {
        public RuntimeIntrospectionSection DescribeSection()
        {
            return new RuntimeIntrospectionSection(
                sectionId,
                "1.0.0",
                source,
                "Test operator",
                "Exercises package-owned runtime introspection sections.",
                entryIds.Select(static entryId => new RuntimeIntrospectionSectionEntry(
                    entryId,
                    entryId,
                    $"Operator entry {entryId}.",
                    "ready",
                    "ready",
                    conditions:
                    [
                        new RuntimeOperatorCondition(
                            "ready",
                            "true",
                            "info",
                            "entry-ready",
                            $"Entry {entryId} is ready.")
                    ],
                    actions:
                    [
                        new RuntimeOperatorAction(
                            "reconcile",
                            "Reconcile",
                            "Requests reconciliation through the owning package.",
                            requiresApproval: true)
                    ])).ToArray());
        }
    }
}
