using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Technologies;
using Cephalon.Data.Cassandra.Registration;
using Cephalon.Data.ClickHouse.Registration;
using Cephalon.Data.Elasticsearch.Registration;
using Cephalon.Data.MongoDB.Registration;
using Cephalon.Data.MySql.Configuration;
using Cephalon.Data.MySql.Registration;
using Cephalon.Data.Nats.Registration;
using Cephalon.Data.Neo4j.Registration;
using Cephalon.Data.OpenSearch.Registration;
using Cephalon.Data.Oracle.Configuration;
using Cephalon.Data.Oracle.Registration;
using Cephalon.Data.Postgres.Configuration;
using Cephalon.Data.Postgres.Registration;
using Cephalon.Data.Qdrant.Registration;
using Cephalon.Data.Redis.Registration;
using Cephalon.Data.Registration;
using Cephalon.Data.SqlServer.Configuration;
using Cephalon.Data.SqlServer.Registration;
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
        "clickhouse-secret",
        "sql-secret",
        "postgres-secret",
        "mysql-secret",
        "oracle-secret"
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
    public void RelationalProviderPacksAggregateSharedCapabilityMetadata()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(blueprint: "ModularVerticalSlice"));
            engine.AddModule(new PlatformTestModule());
            AddRelationalProviderDataPacks(engine);
        });

        using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<IRuntime>();

        var relationalStoreCapability = Assert.Single(runtime.Manifest.Capabilities, capability =>
            string.Equals(capability.Key, "data.relational-store", StringComparison.OrdinalIgnoreCase));

        Assert.Equal("4", relationalStoreCapability.Metadata["contributorCount"]);
        Assert.Equal(
            ["mysql", "oracle", "postgresql", "sqlserver"],
            SplitMetadata(relationalStoreCapability.Metadata, "providers"));
        Assert.Equal(
            ["Cephalon.Data.MySql", "Cephalon.Data.Oracle", "Cephalon.Data.Postgres", "Cephalon.Data.SqlServer"],
            SplitMetadata(relationalStoreCapability.Metadata, "packs"));
        Assert.Equal(
            ["mysql-data", "oracle-data", "postgres-data", "sqlserver-data"],
            SplitMetadata(relationalStoreCapability.Metadata, "sourceModuleIds"));

        foreach (var capability in runtime.Manifest.Capabilities)
        {
            AssertNoSecretProjection(capability.Metadata);
        }
    }

    [Fact]
    public void ProviderNativeCdcPacksProjectDataManagementSurfacesForEveryConfiguredRelationalProvider()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS"]));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new PlatformEventingTestModule());
            engine.AddData(options =>
            {
                options.EnableCdcExecution = true;
                options.CdcPollingIntervalSeconds = 600;
            });
            AddRelationalProviderDataPacks(engine, configureCdcCaptures: true);
        });

        using var provider = services.BuildServiceProvider();

        var captureCatalog = provider.GetRequiredService<ICdcCaptureCatalog>();
        var executionRuntimeCatalog = provider.GetRequiredService<ICdcCaptureExecutionRuntimeCatalog>();
        var runtimeCatalog = provider.GetRequiredService<ITechnologyRuntimeCatalog>();
        var dataSurfaces = runtimeCatalog.GetByTechnology("data-management");
        var captureEntries = dataSurfaces
            .Where(static surface => string.Equals(surface.SurfaceId, "cdc-captures", StringComparison.OrdinalIgnoreCase))
            .SelectMany(static surface => surface.Entries)
            .ToDictionary(static entry => entry.Id, StringComparer.OrdinalIgnoreCase);
        var runtimeEntries = dataSurfaces
            .Where(static surface => string.Equals(surface.SurfaceId, "cdc-capture-runtimes", StringComparison.OrdinalIgnoreCase))
            .SelectMany(static surface => surface.Entries)
            .ToDictionary(static entry => entry.Id, StringComparer.OrdinalIgnoreCase);

        Assert.Equal(
            ["mysql-orders-cdc", "oracle-orders-cdc", "postgres-orders-cdc", "sqlserver-orders-cdc"],
            captureCatalog.CdcCaptures
                .Select(static capture => capture.Id)
                .OrderBy(static id => id, StringComparer.OrdinalIgnoreCase)
                .ToArray());
        Assert.Equal(
            ["data-cdc-capture-pump", "mysql-binlog-capture-pump", "oracle-logminer-capture-pump", "postgresql-logical-replication-capture-pump", "sqlserver-cdc-capture-pump"],
            executionRuntimeCatalog.Runtimes
                .Select(static runtime => runtime.Id)
                .OrderBy(static id => id, StringComparer.OrdinalIgnoreCase)
                .ToArray());

        AssertCdcCaptureSurfaceEntry(
            captureEntries,
            "sqlserver-orders-cdc",
            "sqlserver",
            "sqlserver-cdc-capture-pump");
        AssertCdcCaptureSurfaceEntry(
            captureEntries,
            "postgres-orders-cdc",
            "postgresql",
            "postgresql-logical-replication-capture-pump");
        AssertCdcCaptureSurfaceEntry(
            captureEntries,
            "mysql-orders-cdc",
            "mysql",
            "mysql-binlog-capture-pump");
        AssertCdcCaptureSurfaceEntry(
            captureEntries,
            "oracle-orders-cdc",
            "oracle",
            "oracle-logminer-capture-pump");

        foreach (var runtimeId in new[]
        {
            "data-cdc-capture-pump",
            "mysql-binlog-capture-pump",
            "oracle-logminer-capture-pump",
            "postgresql-logical-replication-capture-pump",
            "sqlserver-cdc-capture-pump"
        })
        {
            var entry = Assert.Contains(runtimeId, runtimeEntries);
            Assert.False(string.IsNullOrWhiteSpace(entry.Metadata["executionOwnership"]));
            Assert.False(string.IsNullOrWhiteSpace(entry.Metadata["executionTopology"]));
            AssertNoSecretProjection(entry.Metadata);
        }

        Assert.Equal("redacted", captureEntries["sqlserver-orders-cdc"].Metadata["accessToken"]);
        Assert.Equal("https://example.test/orders", captureEntries["sqlserver-orders-cdc"].Metadata["observerEndpoint"]);
        Assert.Equal("redacted", captureEntries["sqlserver-orders-cdc"].Metadata["secretProjection"]);
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

    private static void AddRelationalProviderDataPacks(
        EngineBuilder engine,
        bool configureCdcCaptures = false)
    {
        engine.AddSqlServerData(
            connectionString: "Server=localhost;Database=cephalon;User Id=sa;Password=sql-secret;TrustServerCertificate=True",
            databaseName: "cephalon",
            configure: options =>
            {
                if (!configureCdcCaptures)
                {
                    return;
                }

                options.CdcCaptures.Add(new SqlServerCdcCaptureOptions
                {
                    Id = "sqlserver-orders-cdc",
                    DisplayName = "SQL Server Orders CDC",
                    Description = "Captures SQL Server order changes through a provider-native CDC runner.",
                    SourceModuleId = "platform",
                    SourceId = "sqlserver:cephalon/dbo.orders",
                    CaptureInstance = "dbo_orders",
                    TableSchema = "dbo",
                    TableName = "orders",
                    OutboxId = "tenant-event-outbox",
                    ChannelId = "orders",
                    MessageType = "orders.sqlserver.changed",
                    InitialPosition = "earliest-available",
                    Metadata =
                    {
                        ["accessToken"] = "sql-secret",
                        ["observerEndpoint"] = "https://cdc-user:sql-secret@example.test/orders?token=sql-secret"
                    },
                    ResourceIds =
                    {
                        "sqlserver:cephalon/dbo.orders"
                    },
                    Tags =
                    {
                        "orders"
                    }
                });
            });
        engine.AddPostgresData(
            connectionString: "Host=localhost;Username=postgres;Password=postgres-secret;Database=cephalon",
            databaseName: "cephalon",
            configure: options =>
            {
                if (!configureCdcCaptures)
                {
                    return;
                }

                options.CdcCaptures.Add(new PostgresLogicalReplicationCaptureOptions
                {
                    Id = "postgres-orders-cdc",
                    DisplayName = "PostgreSQL Orders CDC",
                    Description = "Captures PostgreSQL order changes through a provider-native logical-replication runner.",
                    SourceModuleId = "platform",
                    SourceId = "postgresql:cephalon/public.orders",
                    PublicationName = "orders_publication",
                    SlotName = "orders_slot",
                    TableSchema = "public",
                    TableName = "orders",
                    OutboxId = "tenant-event-outbox",
                    ChannelId = "orders",
                    MessageType = "orders.postgresql.changed",
                    InitialPosition = "slot-consistent-point",
                    Metadata =
                    {
                        ["refreshToken"] = "postgres-secret"
                    },
                    ResourceIds =
                    {
                        "postgresql:cephalon/public.orders"
                    },
                    Tags =
                    {
                        "orders"
                    }
                });
            });
        engine.AddMySqlData(
            connectionString: "Server=localhost;User ID=root;Password=mysql-secret;Database=cephalon",
            databaseName: "cephalon",
            configure: options =>
            {
                if (!configureCdcCaptures)
                {
                    return;
                }

                options.CdcCaptures.Add(new MySqlBinlogCaptureOptions
                {
                    Id = "mysql-orders-cdc",
                    DisplayName = "MySQL Orders CDC",
                    Description = "Captures MySQL order changes through a provider-native binlog runner.",
                    SourceModuleId = "platform",
                    SourceId = "mysql:cephalon/orders",
                    TableSchema = "cephalon",
                    TableName = "orders",
                    ServerId = 700101,
                    OutboxId = "tenant-event-outbox",
                    ChannelId = "orders",
                    MessageType = "orders.mysql.changed",
                    InitialPosition = "earliest-available",
                    Metadata =
                    {
                        ["clientSecret"] = "mysql-secret"
                    },
                    ResourceIds =
                    {
                        "mysql:cephalon/orders"
                    },
                    Tags =
                    {
                        "orders"
                    }
                });
            });
        engine.AddOracleData(
            connectionString: "User Id=cephalon;Password=oracle-secret;Data Source=localhost/XEPDB1",
            databaseName: "XEPDB1",
            configure: options =>
            {
                if (!configureCdcCaptures)
                {
                    return;
                }

                options.CdcCaptures.Add(new OracleLogMinerCaptureOptions
                {
                    Id = "oracle-orders-cdc",
                    DisplayName = "Oracle Orders CDC",
                    Description = "Captures Oracle order changes through a provider-native LogMiner runner.",
                    SourceModuleId = "platform",
                    SourceId = "oracle:XEPDB1/SALES.ORDERS",
                    TableSchema = "SALES",
                    TableName = "ORDERS",
                    OutboxId = "tenant-event-outbox",
                    ChannelId = "orders",
                    MessageType = "orders.oracle.changed",
                    InitialPosition = "earliest-available",
                    Metadata =
                    {
                        ["sharedAccessSignature"] = "oracle-secret"
                    },
                    ResourceIds =
                    {
                        "oracle:XEPDB1/SALES.ORDERS"
                    },
                    Tags =
                    {
                        "orders"
                    }
                });
            });
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

    private static void AssertCdcCaptureSurfaceEntry(
        IReadOnlyDictionary<string, TechnologyRuntimeEntry> entries,
        string captureId,
        string provider,
        string executionRuntimeId)
    {
        var entry = Assert.Contains(captureId, entries);

        Assert.Equal(provider, entry.Metadata["provider"]);
        Assert.Equal("provider-native", entry.Metadata["executionTopology"]);
        Assert.Equal(executionRuntimeId, entry.Metadata["effectiveExecutionRuntimeId"]);
        Assert.False(string.IsNullOrWhiteSpace(entry.Metadata["sourceModuleId"]));
        Assert.False(string.IsNullOrWhiteSpace(entry.Metadata["outboxId"]));
        AssertNoSecretProjection(entry.Metadata);
    }

    private static void AssertSanitizedUriCapability(IRuntime runtime, string capabilityKey, string expectedUri)
    {
        var capability = Assert.Single(runtime.Manifest.Capabilities, capability =>
            string.Equals(capability.Key, capabilityKey, StringComparison.OrdinalIgnoreCase));

        Assert.Equal(expectedUri, capability.Metadata["uri"]);
        Assert.Equal("true", capability.Metadata["uriCredentialsConfigured"]);
        Assert.Equal("redacted", capability.Metadata["secretProjection"]);
    }

    private static string[] SplitMetadata(IReadOnlyDictionary<string, string> metadata, string key)
    {
        return metadata[key]
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();
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
