using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.EventSourcing;
using Cephalon.Data.Neo4j.Registration;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.EventSourcing.Neo4j;
using Cephalon.EventSourcing.Neo4j.Hosting;
using Cephalon.Tests.Support;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Neo4j.Driver;

namespace Cephalon.Tests.Composition;

/// <summary>
/// Composition and capability tests for the Neo4j data companion pack and Neo4j event-store provider.
/// Neo4j.Driver connects lazily — <c>GraphDatabase.Driver(uri, auth)</c> does not open a socket immediately.
/// The first actual network call happens when a Cypher query is issued, so service registration and
/// resolution tests work without a live Neo4j server.
/// </summary>
public sealed class Neo4jDataPackTests
{
    private const string OfflineUri = "bolt://localhost:7687";
    private const string OfflineUsername = "neo4j";
    private const string OfflinePassword = "test";

    [Fact]
    public void AddNeo4jData_RegistersDriver()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS"],
                data: new DataSettings(provider: "Neo4j")));
            engine.AddModule(new PlatformTestModule());
            engine.AddNeo4jData(OfflineUri, OfflineUsername, OfflinePassword);
        });

        using var provider = services.BuildServiceProvider();
        var driver = provider.GetRequiredService<IDriver>();
        Assert.NotNull(driver);
    }

    [Fact]
    public void AddNeo4jData_RegistersOutbox_WhenEnabled()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS", "Outbox"],
                data: new DataSettings(provider: "Neo4j", outboxEnabled: true)));
            engine.AddModule(new PlatformTestModule());
            engine.AddNeo4jData(OfflineUri, OfflineUsername, OfflinePassword, configure: options =>
            {
                options.RegisterOutbox = true;
            });
        });

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutbox>();
        Assert.NotNull(outbox);
    }

    [Fact]
    public void AddNeo4jData_RegistersInbox_WhenEnabled()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS"],
                data: new DataSettings(provider: "Neo4j")));
            engine.AddModule(new PlatformTestModule());
            engine.AddNeo4jData(OfflineUri, OfflineUsername, OfflinePassword, configure: options =>
            {
                options.RegisterInbox = true;
            });
        });

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var inbox = scope.ServiceProvider.GetRequiredService<IInbox>();
        Assert.NotNull(inbox);
    }

    [Fact]
    public void Neo4jDataModule_ExposesCapabilities()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS", "Outbox"],
                data: new DataSettings(provider: "Neo4j", outboxEnabled: true)));
            engine.AddModule(new PlatformTestModule());
            engine.AddNeo4jData(OfflineUri, OfflineUsername, OfflinePassword, configure: options =>
            {
                options.RegisterOutbox = true;
                options.RegisterInbox = true;
            });
        });

        using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<Cephalon.Engine.Runtime.IRuntime>();

        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "data.neo4j");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "data.graph-store");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "data.outbox.neo4j");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "data.inbox.neo4j");
    }

    [Fact]
    public void Neo4jDataModule_RegistersOutboxDescriptor()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS", "Outbox"],
                data: new DataSettings(provider: "Neo4j", outboxEnabled: true)));
            engine.AddModule(new PlatformTestModule());
            engine.AddNeo4jData(OfflineUri, OfflineUsername, OfflinePassword, configure: options =>
            {
                options.RegisterOutbox = true;
            });
        });

        using var provider = services.BuildServiceProvider();
        var outboxCatalog = provider.GetRequiredService<IOutboxCatalog>();

        var outboxDescriptor = Assert.Single(outboxCatalog.Outboxes);
        Assert.Equal("neo4j-outbox", outboxDescriptor.Id);
        Assert.Equal("neo4j", outboxDescriptor.Provider);
        Assert.Equal("graph-node", outboxDescriptor.Mode);
    }

    [Fact]
    public void Neo4jDataModule_RegistersInboxDescriptor()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS"],
                data: new DataSettings(provider: "Neo4j")));
            engine.AddModule(new PlatformTestModule());
            engine.AddNeo4jData(OfflineUri, OfflineUsername, OfflinePassword, configure: options =>
            {
                options.RegisterInbox = true;
            });
        });

        using var provider = services.BuildServiceProvider();
        var inboxCatalog = provider.GetRequiredService<IInboxCatalog>();

        var inboxDescriptor = Assert.Single(inboxCatalog.Inboxes);
        Assert.Equal("neo4j-inbox", inboxDescriptor.Id);
        Assert.Equal("neo4j", inboxDescriptor.Provider);
        Assert.Equal("graph-node", inboxDescriptor.Mode);
        Assert.Equal("merge-on-message-id", inboxDescriptor.Metadata["idempotency"]);
    }

    [Fact]
    public void AddCephalonNeo4jEventSourcing_RegistersEventStore()
    {
        var services = new ServiceCollection();
        services.AddCephalonNeo4jEventSourcing(OfflineUri, OfflineUsername, OfflinePassword);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var eventStore = scope.ServiceProvider.GetRequiredService<IEventStore>();
        Assert.NotNull(eventStore);
        Assert.IsType<Neo4jEventStore>(eventStore);
    }

    [Fact]
    public void Neo4jDataModule_WithoutOutboxOrInbox_PublishesBaseCapabilitiesOnly()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS"],
                data: new DataSettings(provider: "Neo4j")));
            engine.AddModule(new PlatformTestModule());
            engine.AddNeo4jData(OfflineUri, OfflineUsername, OfflinePassword);
        });

        using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<Cephalon.Engine.Runtime.IRuntime>();

        Assert.Contains(runtime.Manifest.Capabilities, c => c.Key == "data.neo4j");
        Assert.Contains(runtime.Manifest.Capabilities, c => c.Key == "data.graph-store");
        Assert.DoesNotContain(runtime.Manifest.Capabilities, c => c.Key == "data.outbox.neo4j");
        Assert.DoesNotContain(runtime.Manifest.Capabilities, c => c.Key == "data.inbox.neo4j");
    }

    [Fact]
    public void AddNeo4jData_UsesNamedUriFromConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Uris:Graph"] = OfflineUri
            })
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS"],
                data: new DataSettings(provider: "Neo4j")));
            engine.AddModule(new PlatformTestModule());
            engine.AddNeo4jData(options =>
            {
                options.UriName = "Graph";
                options.Username = OfflineUsername;
                options.Password = OfflinePassword;
            });
        });

        using var provider = services.BuildServiceProvider();
        var driver = provider.GetRequiredService<IDriver>();
        var runtime = provider.GetRequiredService<Cephalon.Engine.Runtime.IRuntime>();
        var capability = Assert.Single(runtime.Manifest.Capabilities, c => c.Key == "data.neo4j");

        Assert.NotNull(driver);
        Assert.Equal("Graph", capability.Metadata["uriName"]);
    }
}
