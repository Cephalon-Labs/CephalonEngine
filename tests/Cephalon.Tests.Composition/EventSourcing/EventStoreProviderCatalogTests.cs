using Cephalon.Abstractions.EventSourcing;
using Cephalon.Abstractions.Technologies;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.EventSourcing.Cassandra.Hosting;
using Cephalon.EventSourcing.ClickHouse.Hosting;
using Cephalon.EventSourcing.Elasticsearch.Hosting;
using Cephalon.EventSourcing.EntityFramework;
using Cephalon.EventSourcing.EntityFramework.Hosting;
using Cephalon.EventSourcing.MongoDB.Hosting;
using Cephalon.EventSourcing.Nats.Hosting;
using Cephalon.EventSourcing.Neo4j.Hosting;
using Cephalon.EventSourcing.OpenSearch.Hosting;
using Cephalon.EventSourcing.Qdrant.Hosting;
using Cephalon.EventSourcing.Redis.Hosting;
using Cephalon.EventSourcing.Registration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.EventSourcing;

public sealed class EventStoreProviderCatalogTests
{
    private static readonly string[] ExpectedProviders =
    [
        "cassandra",
        "clickhouse",
        "elasticsearch",
        "entity-framework",
        "mongodb",
        "nats",
        "neo4j",
        "opensearch",
        "qdrant",
        "redis"
    ];

    private static readonly string[] SecretValues =
    [
        "clickhouse-secret",
        "elastic-secret",
        "mongo-secret",
        "nats-secret",
        "neo4j-secret",
        "opensearch-secret",
        "redis-secret"
    ];

    [Fact]
    public void ProviderRegistrationsContributeSanitizedEventStoreRuntimeTruth()
    {
        var services = new ServiceCollection();
        services.AddDbContext<TestEventContext>(options =>
            options.UseInMemoryDatabase($"event-store-provider-catalog-{Guid.NewGuid():N}"));

        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice"));
            engine.AddEventSourcing(options =>
            {
                options.DefaultProvider = "mongodb";
                options.EnableSnapshots = true;
            });
        });

        services.AddCephalonCassandraEventSourcing("node-a,node-b", "orders", "order_events");
        services.AddCephalonClickHouseEventSourcing(
            host: "clickhouse.local",
            database: "orders",
            tableName: "order_events",
            username: "default",
            password: "clickhouse-secret");
        services.AddCephalonElasticsearchEventSourcing(
            uri: "https://elastic:elastic-secret@localhost:9200",
            indexName: "order-events");
        services.AddCephalonEntityFrameworkEventSourcing<TestEventContext>();
        services.AddCephalonMongoDbEventSourcing(
            connectionString: "mongodb://mongo:mongo-secret@localhost:27017",
            databaseName: "orders",
            collectionName: "order_events");
        services.AddCephalonNatsEventSourcing(
            url: "nats://user:nats-secret@localhost:4222",
            bucketName: "order-events");
        services.AddCephalonNeo4jEventSourcing(
            uri: "bolt://localhost:7687",
            username: "neo4j",
            password: "neo4j-secret",
            eventLabel: "OrderEvent");
        services.AddCephalonOpenSearchEventSourcing(
            uri: "https://opensearch:opensearch-secret@localhost:9201",
            indexName: "order-events");
        services.AddCephalonQdrantEventSourcing(
            host: "qdrant.local",
            port: 6335,
            collectionName: "order-events");
        services.AddCephalonRedisEventSourcing(
            configuration: "localhost:6379,password=redis-secret,abortConnect=false",
            keyPrefix: "orders:");

        using var provider = services.BuildServiceProvider();
        var eventStoreCatalog = provider.GetRequiredService<IEventStoreCatalog>();

        Assert.Equal(10, eventStoreCatalog.All.Count);
        foreach (var expectedProvider in ExpectedProviders)
        {
            var descriptor = Assert.Single(eventStoreCatalog.GetByProvider(expectedProvider));
            Assert.Equal(expectedProvider, descriptor.Provider);
            Assert.Equal("append-only", descriptor.Mode);
            Assert.DoesNotContain(descriptor.Metadata.Values, ContainsSecret);
        }

        var runtimeCatalog = provider.GetRequiredService<ITechnologyRuntimeCatalog>();
        var surface = Assert.Single(runtimeCatalog.GetByTechnology("event-sourcing"));
        var summary = Assert.Single(surface.Entries, entry => entry.Id == "event-sourcing-runtime");

        Assert.Equal("10", summary.Metadata["activeStoreCount"]);
        Assert.Equal("10", summary.Metadata["activeProviderCount"]);
        Assert.Equal("mongodb", summary.Metadata["defaultProvider"]);
        Assert.Equal("true", summary.Metadata["enableSnapshots"]);
        Assert.Equal("true", summary.Metadata["enableInMemorySnapshotStore"]);
        Assert.Equal("true", summary.Metadata["enableReplayWorker"]);
        Assert.Equal("provider-durable", summary.Metadata["snapshotLifecycle"]);
        Assert.Equal("entity-framework", summary.Metadata["providerDurableSnapshotProviders"]);
        Assert.Equal("on-demand-domain-event-projections", summary.Metadata["projectionRebuild"]);
        Assert.Equal("not-claimed", summary.Metadata["hostedBackgroundRunner"]);

        var replayWorker = Assert.Single(surface.Entries, entry => entry.Id == "event-sourcing-managed-replay-worker");
        Assert.Equal("cephalon-managed", replayWorker.Metadata["managedExecution"]);
        Assert.Equal("on-demand", replayWorker.Metadata["mode"]);
        Assert.Equal("provider-durable", replayWorker.Metadata["snapshotAssistedReplay"]);
        Assert.Equal("not-claimed", replayWorker.Metadata["hostedBackgroundRunner"]);
        Assert.Equal("claimed", replayWorker.Metadata["providerDurableSnapshots"]);
        Assert.Equal("entity-framework", replayWorker.Metadata["providerDurableSnapshotProviders"]);

        foreach (var expectedProvider in ExpectedProviders)
        {
            Assert.Contains(expectedProvider, summary.Metadata["activeProviders"], StringComparison.OrdinalIgnoreCase);
        }

        foreach (var entry in surface.Entries.Where(static entry =>
            entry.Id != "event-sourcing-runtime" &&
            entry.Id != "event-sourcing-managed-replay-worker"))
        {
            Assert.Contains("provider", entry.Metadata.Keys);
            Assert.DoesNotContain(entry.Metadata.Values, ContainsSecret);
        }

        var entityFrameworkEntry = Assert.Single(surface.Entries, entry =>
            entry.Metadata.TryGetValue("provider", out var provider) &&
            provider == "entity-framework");
        Assert.Equal("provider-durable", entityFrameworkEntry.Metadata["provider.snapshotLifecycle"]);
        Assert.Equal("CephalonEventSnapshots", entityFrameworkEntry.Metadata["provider.snapshotStorage"]);
    }

    private static bool ContainsSecret(string value)
    {
        return SecretValues.Any(secret => value.Contains(secret, StringComparison.OrdinalIgnoreCase));
    }

    private sealed class TestEventContext(DbContextOptions<TestEventContext> options)
        : DbContext(options), IEntityFrameworkEventContext
    {
        public DbSet<EntityFrameworkEventEntry> Events => Set<EntityFrameworkEventEntry>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            EntityFrameworkEventSourcingConfiguration.ConfigureCephalonEvents(modelBuilder);
        }
    }
}
