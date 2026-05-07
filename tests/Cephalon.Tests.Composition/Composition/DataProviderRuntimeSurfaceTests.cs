using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Technologies;
using Cephalon.Data.Cassandra.Registration;
using Cephalon.Data.ClickHouse.Registration;
using Cephalon.Data.Elasticsearch.Registration;
using Cephalon.Data.MongoDB.Registration;
using Cephalon.Data.Nats.Registration;
using Cephalon.Data.Neo4j.Registration;
using Cephalon.Data.OpenSearch.Registration;
using Cephalon.Data.Qdrant.Registration;
using Cephalon.Data.Redis.Registration;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Runtime;
using Cephalon.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Composition;

public sealed class DataProviderRuntimeSurfaceTests
{
    private static readonly string[] ExpectedOutboxIds =
    [
        "cassandra-outbox",
        "clickhouse-outbox",
        "elasticsearch-outbox",
        "mongodb-outbox",
        "nats-outbox",
        "neo4j-outbox",
        "opensearch-outbox",
        "qdrant-outbox",
        "redis-outbox"
    ];

    private static readonly string[] ExpectedInboxIds =
    [
        "cassandra-inbox",
        "clickhouse-inbox",
        "elasticsearch-inbox",
        "mongodb-inbox",
        "nats-inbox",
        "neo4j-inbox",
        "opensearch-inbox",
        "qdrant-inbox",
        "redis-inbox"
    ];

    private static readonly string[] SecretSentinels =
    [
        "redis-secret",
        "elastic-secret",
        "elastic-token",
        "mongo-secret",
        "nats-secret",
        "nats-token",
        "neo4j-secret",
        "neo4j-token",
        "open-secret",
        "open-token",
        "clickhouse-secret"
    ];

    [Fact]
    public void ProviderOutboxAndInboxPacksProjectRuntimeSurfacesForEveryConfiguredProvider()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS", "Outbox"],
                technologies: ["EventDrivenIntegration"]));
            engine.AddModule(new PlatformTestModule());
            AddProviderDataPacks(engine);
        });

        using var provider = services.BuildServiceProvider();

        var outboxIds = provider.GetRequiredService<IOutboxCatalog>()
            .Outboxes
            .Select(static descriptor => descriptor.Id)
            .OrderBy(static id => id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var inboxIds = provider.GetRequiredService<IInboxCatalog>()
            .Inboxes
            .Select(static descriptor => descriptor.Id)
            .OrderBy(static id => id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var runtimeCatalog = provider.GetRequiredService<ITechnologyRuntimeCatalog>();
        var surfaces = runtimeCatalog.GetByTechnology("event-driven-integration");
        var outboxEntries = surfaces
            .Where(static surface => string.Equals(surface.SurfaceId, "outbox-producers", StringComparison.OrdinalIgnoreCase))
            .SelectMany(static surface => surface.Entries)
            .ToDictionary(static entry => entry.Id, StringComparer.OrdinalIgnoreCase);
        var inboxEntries = surfaces
            .Where(static surface => string.Equals(surface.SurfaceId, "inbox-stores", StringComparison.OrdinalIgnoreCase))
            .SelectMany(static surface => surface.Entries)
            .ToDictionary(static entry => entry.Id, StringComparer.OrdinalIgnoreCase);

        Assert.Equal(ExpectedOutboxIds.OrderBy(static id => id, StringComparer.OrdinalIgnoreCase), outboxIds);
        Assert.Equal(ExpectedInboxIds.OrderBy(static id => id, StringComparer.OrdinalIgnoreCase), inboxIds);

        foreach (var outboxId in ExpectedOutboxIds)
        {
            var entry = Assert.Contains(outboxId, outboxEntries);
            Assert.False(string.IsNullOrWhiteSpace(entry.Metadata["provider"]));
            AssertNoSecretProjection(entry.Metadata);
        }

        foreach (var inboxId in ExpectedInboxIds)
        {
            var entry = Assert.Contains(inboxId, inboxEntries);
            Assert.False(string.IsNullOrWhiteSpace(entry.Metadata["provider"]));
            AssertNoSecretProjection(entry.Metadata);
        }
    }

    [Fact]
    public void ProviderCapabilityMetadataRedactsInlineUriCredentials()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(blueprint: "ModularVerticalSlice"));
            engine.AddModule(new PlatformTestModule());
            AddProviderDataPacks(engine);
        });

        using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<IRuntime>();

        AssertSanitizedUriCapability(runtime, "data.elasticsearch", "http://localhost:9200/search");
        AssertSanitizedUriCapability(runtime, "data.nats", "nats://localhost:4222");
        AssertSanitizedUriCapability(runtime, "data.neo4j", "bolt://localhost:7687/db");
        AssertSanitizedUriCapability(runtime, "data.opensearch", "http://localhost:9200/os");

        var searchStoreCapability = Assert.Single(runtime.Manifest.Capabilities, capability =>
            string.Equals(capability.Key, "data.search-store", StringComparison.OrdinalIgnoreCase));

        Assert.Equal("2", searchStoreCapability.Metadata["contributorCount"]);
        Assert.Equal("elasticsearch,opensearch", searchStoreCapability.Metadata["providers"]);
        Assert.Equal("Cephalon.Data.Elasticsearch,Cephalon.Data.OpenSearch", searchStoreCapability.Metadata["packs"]);
        Assert.Equal("elasticsearch-data,opensearch-data", searchStoreCapability.Metadata["sourceModuleIds"]);

        foreach (var capability in runtime.Manifest.Capabilities)
        {
            AssertNoSecretProjection(capability.Metadata);
        }
    }

    private static void AddProviderDataPacks(EngineBuilder engine)
    {
        engine.AddCassandraData("localhost", "cephalon_test", options =>
        {
            options.RegisterOutbox = true;
            options.RegisterInbox = true;
        });
        engine.AddClickHouseData("localhost", "cephalon_test", options =>
        {
            options.Password = "clickhouse-secret";
            options.RegisterOutbox = true;
            options.RegisterInbox = true;
        });
        engine.AddElasticsearchData("http://elastic-user:elastic-secret@localhost:9200/search?apikey=elastic-token", options =>
        {
            options.Username = "elastic-user";
            options.Password = "elastic-secret";
            options.RegisterOutbox = true;
            options.RegisterInbox = true;
        });
        engine.AddMongoDbData("mongodb://mongo-user:mongo-secret@localhost:27017/?authSource=admin", "cephalon_test", options =>
        {
            options.RegisterOutbox = true;
            options.RegisterInbox = true;
        });
        engine.AddNatsData("nats://nats-user:nats-secret@localhost:4222?token=nats-token", options =>
        {
            options.RegisterOutbox = true;
            options.RegisterInbox = true;
        });
        engine.AddNeo4jData("bolt://neo4j-user:neo4j-secret@localhost:7687/db?token=neo4j-token", "neo4j-user", "neo4j-secret", options =>
        {
            options.RegisterOutbox = true;
            options.RegisterInbox = true;
        });
        engine.AddOpenSearchData("http://open-user:open-secret@localhost:9200/os?apikey=open-token", options =>
        {
            options.Username = "open-user";
            options.Password = "open-secret";
            options.RegisterOutbox = true;
            options.RegisterInbox = true;
        });
        engine.AddQdrantData("localhost", configure: options =>
        {
            options.RegisterOutbox = true;
            options.RegisterInbox = true;
        });
        engine.AddRedisData("localhost:6379,password=redis-secret,abortConnect=false", options =>
        {
            options.RegisterOutbox = true;
            options.RegisterInbox = true;
        });
    }

    private static void AssertSanitizedUriCapability(IRuntime runtime, string capabilityKey, string expectedUri)
    {
        var capability = Assert.Single(runtime.Manifest.Capabilities, capability =>
            string.Equals(capability.Key, capabilityKey, StringComparison.OrdinalIgnoreCase));

        Assert.Equal(expectedUri, capability.Metadata["uri"]);
        Assert.Equal("true", capability.Metadata["uriCredentialsConfigured"]);
        Assert.Equal("redacted", capability.Metadata["secretProjection"]);
    }

    private static void AssertNoSecretProjection(IReadOnlyDictionary<string, string> metadata)
    {
        foreach (var value in metadata.Values)
        {
            foreach (var secret in SecretSentinels)
            {
                Assert.DoesNotContain(secret, value, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
