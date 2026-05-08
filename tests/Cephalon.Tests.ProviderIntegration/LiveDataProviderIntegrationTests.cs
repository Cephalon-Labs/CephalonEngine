using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Technologies;
using Cephalon.Data.Cassandra.Configuration;
using Cephalon.Data.Cassandra.Registration;
using Cephalon.Data.ClickHouse.Configuration;
using Cephalon.Data.ClickHouse.Registration;
using Cephalon.Data.Elasticsearch.Configuration;
using Cephalon.Data.Elasticsearch.Registration;
using Cephalon.Data.Nats.Configuration;
using Cephalon.Data.Nats.Registration;
using Cephalon.Data.Neo4j.Configuration;
using Cephalon.Data.Neo4j.Registration;
using Cephalon.Data.OpenSearch.Configuration;
using Cephalon.Data.OpenSearch.Registration;
using Cephalon.Data.Qdrant.Configuration;
using Cephalon.Data.Qdrant.Registration;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Runtime;
using Cephalon.Eventing.Registration;
using Cephalon.Eventing.Services;
using Cephalon.Tests.ProviderIntegration.ExternalServices;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.ProviderIntegration;

public sealed class LiveDataProviderIntegrationTests : IAsyncLifetime, IDisposable
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(250);
    private readonly ExternalProviderTestcontainerRuntime _testcontainerRuntime = new();

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _testcontainerRuntime.DisposeAsync().ConfigureAwait(false);
    }

    public void Dispose()
    {
        _testcontainerRuntime.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    [ExternalProviderServiceFact(ExternalProviderServiceProvider.Cassandra)]
    public async Task CassandraProvider_StagesOutboxInboxAndDispatchAgainstLiveService()
    {
        var uniqueId = NewUniqueId();
        var service = await _testcontainerRuntime.ResolveCassandraAsync(ExternalProviderServiceGate.FromEnvironment(), uniqueId).ConfigureAwait(false);
        await RunProviderProofAsync(CreateCassandraScenario(service, uniqueId)).ConfigureAwait(false);
    }

    [ExternalProviderServiceFact(ExternalProviderServiceProvider.ClickHouse)]
    public async Task ClickHouseProvider_StagesOutboxAndInboxAgainstLiveService()
    {
        var uniqueId = NewUniqueId();
        var service = await _testcontainerRuntime.ResolveClickHouseAsync(ExternalProviderServiceGate.FromEnvironment(), uniqueId).ConfigureAwait(false);
        await RunProviderProofAsync(CreateClickHouseScenario(service, uniqueId)).ConfigureAwait(false);
    }

    [ExternalProviderServiceFact(ExternalProviderServiceProvider.Elasticsearch)]
    public async Task ElasticsearchProvider_StagesOutboxInboxAndDispatchAgainstLiveService()
    {
        var uniqueId = NewUniqueId();
        var service = await _testcontainerRuntime.ResolveElasticsearchAsync(ExternalProviderServiceGate.FromEnvironment()).ConfigureAwait(false);
        await RunProviderProofAsync(CreateElasticsearchScenario(service, uniqueId)).ConfigureAwait(false);
    }

    [ExternalProviderServiceFact(ExternalProviderServiceProvider.Nats)]
    public async Task NatsProvider_StagesOutboxInboxAndDispatchAgainstLiveJetStream()
    {
        var uniqueId = NewUniqueId();
        var service = await _testcontainerRuntime.ResolveNatsAsync(ExternalProviderServiceGate.FromEnvironment()).ConfigureAwait(false);
        await RunProviderProofAsync(CreateNatsScenario(service, uniqueId)).ConfigureAwait(false);
    }

    [ExternalProviderServiceFact(ExternalProviderServiceProvider.Neo4j)]
    public async Task Neo4jProvider_StagesOutboxInboxAndDispatchAgainstLiveService()
    {
        var uniqueId = NewUniqueId();
        var service = await _testcontainerRuntime.ResolveNeo4jAsync(ExternalProviderServiceGate.FromEnvironment()).ConfigureAwait(false);
        await RunProviderProofAsync(CreateNeo4jScenario(service, uniqueId)).ConfigureAwait(false);
    }

    [ExternalProviderServiceFact(ExternalProviderServiceProvider.OpenSearch)]
    public async Task OpenSearchProvider_StagesOutboxInboxAndDispatchAgainstLiveService()
    {
        var uniqueId = NewUniqueId();
        var service = await _testcontainerRuntime.ResolveOpenSearchAsync(ExternalProviderServiceGate.FromEnvironment()).ConfigureAwait(false);
        await RunProviderProofAsync(CreateOpenSearchScenario(service, uniqueId)).ConfigureAwait(false);
    }

    [ExternalProviderServiceFact(ExternalProviderServiceProvider.Qdrant)]
    public async Task QdrantProvider_StagesOutboxInboxAndDispatchAgainstLiveService()
    {
        var uniqueId = NewUniqueId();
        var service = await _testcontainerRuntime.ResolveQdrantAsync(ExternalProviderServiceGate.FromEnvironment()).ConfigureAwait(false);
        await RunProviderProofAsync(CreateQdrantScenario(service, uniqueId)).ConfigureAwait(false);
    }

    private static async Task RunProviderProofAsync(LiveDataProviderScenario scenario)
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS", "Outbox"],
                technologies: ["EventDrivenIntegration"],
                data: new DataSettings(provider: scenario.DisplayName, outboxEnabled: true)));
            engine.AddModule(new ProviderIntegrationModule());
            engine.AddEventing(options =>
            {
                options.Channels.Add(new EventChannelDescriptor(
                    id: scenario.ChannelId,
                    displayName: $"{scenario.DisplayName} Provider Events",
                    description: "Provider-integration dispatch lane."));
            });
            scenario.RegisterProvider(engine);
        });

        using var provider = services.BuildServiceProvider();
        AssertRuntimeProjection(provider, scenario);

        using var scope = provider.CreateScope();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutbox>();
        var inbox = scope.ServiceProvider.GetRequiredService<IInbox>();
        var outboxMessage = new OutboxMessage(
            id: scenario.OutboxMessageId,
            channelId: scenario.ChannelId,
            messageType: scenario.MessageType,
            payload: $$"""{"id":"{{scenario.ProviderId}}-provider"}""",
            occurredAtUtc: DateTimeOffset.UtcNow,
            contentType: "application/json",
            correlationId: $"corr-{scenario.UniqueId}",
            tenantId: $"tenant-{scenario.UniqueId}",
            headers: new Dictionary<string, string> { ["cephalon-test"] = scenario.ProviderId },
            metadata: new Dictionary<string, string> { ["case"] = "provider-integration" });

        await outbox.EnqueueAsync(outboxMessage).ConfigureAwait(false);
        await outbox.EnqueueAsync(outboxMessage).ConfigureAwait(false);

        if (scenario.SupportsDispatchStore)
        {
            var dispatchStore = scope.ServiceProvider.GetRequiredService<IEventDispatchStore>();
            Assert.Contains(scenario.OutboxId, dispatchStore.OutboxIds);

            var pending = await ReadPendingWithRetryAsync(dispatchStore, scenario.OutboxMessageId).ConfigureAwait(false);
            var dispatchItem = Assert.Single(pending, item => string.Equals(item.MessageId, scenario.OutboxMessageId, StringComparison.OrdinalIgnoreCase));
            Assert.Equal(scenario.OutboxId, dispatchItem.OutboxId);
            Assert.Equal(scenario.ChannelId, dispatchItem.ChannelId);
            Assert.Equal(scenario.MessageType, dispatchItem.EventType);
            Assert.Equal(scenario.ProviderId, dispatchItem.Headers["cephalon-test"]);

            await dispatchStore.ApplyReportAsync(new EventDispatchExecutionReport(
                    outboxId: dispatchItem.OutboxId,
                    channelId: dispatchItem.ChannelId,
                    outcome: EventDispatchExecutionOutcomes.Succeeded,
                    observedAtUtc: DateTimeOffset.UtcNow,
                    messageId: dispatchItem.MessageId,
                    attempt: 1))
                .ConfigureAwait(false);

            await AssertNoPendingWithRetryAsync(dispatchStore, scenario.OutboxMessageId).ConfigureAwait(false);
        }
        else
        {
            Assert.DoesNotContain(
                scope.ServiceProvider.GetServices<IEventDispatchStore>(),
                store => store.OutboxIds.Contains(scenario.OutboxId, StringComparer.OrdinalIgnoreCase));
        }

        Assert.False(await inbox.HasProcessedAsync(scenario.InboxMessageId).ConfigureAwait(false));

        var inboxMessage = new InboxMessage(
            id: scenario.InboxMessageId,
            channelId: scenario.ChannelId,
            messageType: scenario.MessageType,
            payload: $$"""{"id":"{{scenario.ProviderId}}-provider-inbox"}""",
            receivedAtUtc: DateTimeOffset.UtcNow,
            correlationId: $"corr-{scenario.UniqueId}",
            tenantId: $"tenant-{scenario.UniqueId}");

        await inbox.MarkProcessedAsync(inboxMessage).ConfigureAwait(false);
        await inbox.MarkProcessedAsync(inboxMessage).ConfigureAwait(false);

        Assert.True(await inbox.HasProcessedAsync(scenario.InboxMessageId).ConfigureAwait(false));
    }

    private static void AssertRuntimeProjection(IServiceProvider provider, LiveDataProviderScenario scenario)
    {
        var runtime = provider.GetRequiredService<IRuntime>();
        foreach (var capabilityKey in scenario.CapabilityKeys)
        {
            Assert.Contains(
                runtime.Manifest.Capabilities,
                capability => string.Equals(capability.Key, capabilityKey, StringComparison.OrdinalIgnoreCase));
        }

        var outboxDescriptor = Assert.Single(provider.GetRequiredService<IOutboxCatalog>().Outboxes);
        Assert.Equal(scenario.OutboxId, outboxDescriptor.Id);
        Assert.Equal(scenario.ProviderId, outboxDescriptor.Provider);
        Assert.Equal(scenario.OutboxMetadataValue, outboxDescriptor.Metadata[scenario.OutboxMetadataKey]);
        if (scenario.ExpectedDispatchPolicyId is not null)
        {
            Assert.Equal(scenario.ExpectedDispatchPolicyId, outboxDescriptor.DispatchPolicy.PolicyId);
        }

        var inboxDescriptor = Assert.Single(provider.GetRequiredService<IInboxCatalog>().Inboxes);
        Assert.Equal(scenario.InboxId, inboxDescriptor.Id);
        Assert.Equal(scenario.ProviderId, inboxDescriptor.Provider);
        Assert.Equal(scenario.InboxMetadataValue, inboxDescriptor.Metadata[scenario.InboxMetadataKey]);

        var runtimeCatalog = provider.GetRequiredService<ITechnologyRuntimeCatalog>();
        var surfaces = runtimeCatalog.GetByTechnology("event-driven-integration");
        var outboxSurface = Assert.Single(surfaces, surface => surface.SurfaceId == "outbox-producers");
        var inboxSurface = Assert.Single(surfaces, surface => surface.SurfaceId == "inbox-stores");
        Assert.Contains(outboxSurface.Entries, entry => entry.Id == scenario.OutboxId && entry.Metadata["provider"] == scenario.ProviderId);
        Assert.Contains(inboxSurface.Entries, entry => entry.Id == scenario.InboxId && entry.Metadata["provider"] == scenario.ProviderId);
    }

    private static async Task<IReadOnlyList<EventDispatchItem>> ReadPendingWithRetryAsync(
        IEventDispatchStore dispatchStore,
        string messageId)
    {
        IReadOnlyList<EventDispatchItem> pending = [];
        for (var attempt = 0; attempt < 20; attempt++)
        {
            pending = await dispatchStore.ReadPendingAsync(10).ConfigureAwait(false);
            if (pending.Any(item => string.Equals(item.MessageId, messageId, StringComparison.OrdinalIgnoreCase)))
            {
                return pending;
            }

            await Task.Delay(RetryDelay).ConfigureAwait(false);
        }

        return pending;
    }

    private static async Task AssertNoPendingWithRetryAsync(
        IEventDispatchStore dispatchStore,
        string messageId)
    {
        IReadOnlyList<EventDispatchItem> pending = [];
        for (var attempt = 0; attempt < 20; attempt++)
        {
            pending = await dispatchStore.ReadPendingAsync(10).ConfigureAwait(false);
            if (!pending.Any(item => string.Equals(item.MessageId, messageId, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            await Task.Delay(RetryDelay).ConfigureAwait(false);
        }

        Assert.DoesNotContain(pending, item => string.Equals(item.MessageId, messageId, StringComparison.OrdinalIgnoreCase));
    }

    private static LiveDataProviderScenario CreateCassandraScenario(CassandraProviderService service, string uniqueId)
    {
        var tablePrefix = $"it_{uniqueId}_";
        return new LiveDataProviderScenario(
            UniqueId: uniqueId,
            DisplayName: "Cassandra",
            ProviderId: CassandraDataOptions.ProviderId,
            CapabilityKeys:
            [
                "data.cassandra",
                "data.wide-column-store",
                "data.outbox.cassandra",
                "data.inbox.cassandra"
            ],
            OutboxId: "cassandra-outbox",
            InboxId: "cassandra-inbox",
            OutboxMetadataKey: "table",
            OutboxMetadataValue: $"{tablePrefix}outbox_messages",
            InboxMetadataKey: "table",
            InboxMetadataValue: $"{tablePrefix}inbox_receipts",
            SupportsDispatchStore: true,
            ExpectedDispatchPolicyId: "disabled",
            RegisterProvider: engine => engine.AddCassandraData(service.ContactPoints, service.Keyspace, options =>
            {
                options.Port = service.Port;
                options.TablePrefix = tablePrefix;
                options.RegisterOutbox = true;
                options.RegisterInbox = true;
            }));
    }

    private static LiveDataProviderScenario CreateClickHouseScenario(ClickHouseProviderService service, string uniqueId)
    {
        var tablePrefix = $"it_{uniqueId}_";
        return new LiveDataProviderScenario(
            UniqueId: uniqueId,
            DisplayName: "ClickHouse",
            ProviderId: ClickHouseDataOptions.ProviderId,
            CapabilityKeys:
            [
                "data.clickhouse",
                "data.analytics-store",
                "data.outbox.clickhouse",
                "data.inbox.clickhouse"
            ],
            OutboxId: "clickhouse-outbox",
            InboxId: "clickhouse-inbox",
            OutboxMetadataKey: "table",
            OutboxMetadataValue: $"{tablePrefix}outbox_messages",
            InboxMetadataKey: "table",
            InboxMetadataValue: $"{tablePrefix}inbox_receipts",
            SupportsDispatchStore: false,
            ExpectedDispatchPolicyId: "unsupported",
            RegisterProvider: engine => engine.AddClickHouseData(service.Host, service.Database, options =>
            {
                options.Port = service.Port;
                options.Username = service.Username;
                options.Password = service.Password;
                options.TablePrefix = tablePrefix;
                options.RegisterOutbox = true;
                options.RegisterInbox = true;
            }));
    }

    private static LiveDataProviderScenario CreateElasticsearchScenario(ElasticsearchProviderService service, string uniqueId)
    {
        var indexPrefix = $"it-{uniqueId}-";
        return new LiveDataProviderScenario(
            UniqueId: uniqueId,
            DisplayName: "Elasticsearch",
            ProviderId: ElasticsearchDataOptions.ProviderId,
            CapabilityKeys:
            [
                "data.elasticsearch",
                "data.search-store",
                "data.outbox.elasticsearch",
                "data.inbox.elasticsearch"
            ],
            OutboxId: "elasticsearch-outbox",
            InboxId: "elasticsearch-inbox",
            OutboxMetadataKey: "index",
            OutboxMetadataValue: $"{indexPrefix}outbox-messages",
            InboxMetadataKey: "index",
            InboxMetadataValue: $"{indexPrefix}inbox-receipts",
            SupportsDispatchStore: true,
            ExpectedDispatchPolicyId: "disabled",
            RegisterProvider: engine => engine.AddElasticsearchData(service.Uri, options =>
            {
                options.Username = service.Username;
                options.Password = service.Password;
                options.IndexPrefix = indexPrefix;
                options.RegisterOutbox = true;
                options.RegisterInbox = true;
            }));
    }

    private static LiveDataProviderScenario CreateNatsScenario(NatsProviderService service, string uniqueId)
    {
        var bucketPrefix = $"cephalon-it-{uniqueId}";
        return new LiveDataProviderScenario(
            UniqueId: uniqueId,
            DisplayName: "NATS",
            ProviderId: NatsDataOptions.ProviderId,
            CapabilityKeys:
            [
                "data.nats",
                "data.ledger-store",
                "data.outbox.nats",
                "data.inbox.nats"
            ],
            OutboxId: "nats-outbox",
            InboxId: "nats-inbox",
            OutboxMetadataKey: "bucket",
            OutboxMetadataValue: $"{bucketPrefix}-outbox",
            InboxMetadataKey: "bucket",
            InboxMetadataValue: $"{bucketPrefix}-inbox",
            SupportsDispatchStore: true,
            ExpectedDispatchPolicyId: "disabled",
            RegisterProvider: engine => engine.AddNatsData(service.Uri, options =>
            {
                options.BucketPrefix = bucketPrefix;
                options.RegisterOutbox = true;
                options.RegisterInbox = true;
            }));
    }

    private static LiveDataProviderScenario CreateNeo4jScenario(Neo4jProviderService service, string uniqueId)
    {
        var labelPrefix = $"CephalonIt{uniqueId}";
        return new LiveDataProviderScenario(
            UniqueId: uniqueId,
            DisplayName: "Neo4j",
            ProviderId: Neo4jDataOptions.ProviderId,
            CapabilityKeys:
            [
                "data.neo4j",
                "data.graph-store",
                "data.outbox.neo4j",
                "data.inbox.neo4j"
            ],
            OutboxId: "neo4j-outbox",
            InboxId: "neo4j-inbox",
            OutboxMetadataKey: "nodeLabel",
            OutboxMetadataValue: $"{labelPrefix}OutboxMessage",
            InboxMetadataKey: "nodeLabel",
            InboxMetadataValue: $"{labelPrefix}InboxReceipt",
            SupportsDispatchStore: true,
            ExpectedDispatchPolicyId: "disabled",
            RegisterProvider: engine => engine.AddNeo4jData(service.Uri, service.Username, service.Password, options =>
            {
                options.LabelPrefix = labelPrefix;
                options.RegisterOutbox = true;
                options.RegisterInbox = true;
            }));
    }

    private static LiveDataProviderScenario CreateOpenSearchScenario(OpenSearchProviderService service, string uniqueId)
    {
        var indexPrefix = $"it-{uniqueId}-";
        return new LiveDataProviderScenario(
            UniqueId: uniqueId,
            DisplayName: "OpenSearch",
            ProviderId: OpenSearchDataOptions.ProviderId,
            CapabilityKeys:
            [
                "data.opensearch",
                "data.search-store",
                "data.outbox.opensearch",
                "data.inbox.opensearch"
            ],
            OutboxId: "opensearch-outbox",
            InboxId: "opensearch-inbox",
            OutboxMetadataKey: "index",
            OutboxMetadataValue: $"{indexPrefix}outbox-messages",
            InboxMetadataKey: "index",
            InboxMetadataValue: $"{indexPrefix}inbox-receipts",
            SupportsDispatchStore: true,
            ExpectedDispatchPolicyId: "disabled",
            RegisterProvider: engine => engine.AddOpenSearchData(service.Uri, options =>
            {
                options.Username = service.Username;
                options.Password = service.Password;
                options.IndexPrefix = indexPrefix;
                options.RegisterOutbox = true;
                options.RegisterInbox = true;
            }));
    }

    private static LiveDataProviderScenario CreateQdrantScenario(QdrantProviderService service, string uniqueId)
    {
        var collectionPrefix = $"it_{uniqueId}_";
        return new LiveDataProviderScenario(
            UniqueId: uniqueId,
            DisplayName: "Qdrant",
            ProviderId: QdrantDataOptions.ProviderId,
            CapabilityKeys:
            [
                "data.qdrant",
                "data.vector-store",
                "data.outbox.qdrant",
                "data.inbox.qdrant"
            ],
            OutboxId: "qdrant-outbox",
            InboxId: "qdrant-inbox",
            OutboxMetadataKey: "collection",
            OutboxMetadataValue: $"{collectionPrefix}outbox_messages",
            InboxMetadataKey: "collection",
            InboxMetadataValue: $"{collectionPrefix}inbox_receipts",
            SupportsDispatchStore: true,
            ExpectedDispatchPolicyId: "disabled",
            RegisterProvider: engine => engine.AddQdrantData(service.Host, service.Port, options =>
            {
                options.ApiKey = service.ApiKey;
                options.CollectionPrefix = collectionPrefix;
                options.RegisterOutbox = true;
                options.RegisterInbox = true;
            }));
    }

    private static string NewUniqueId()
    {
        return Guid.NewGuid().ToString("N");
    }

    private sealed record LiveDataProviderScenario(
        string UniqueId,
        string DisplayName,
        string ProviderId,
        IReadOnlyList<string> CapabilityKeys,
        string OutboxId,
        string InboxId,
        string OutboxMetadataKey,
        string OutboxMetadataValue,
        string InboxMetadataKey,
        string InboxMetadataValue,
        bool SupportsDispatchStore,
        string? ExpectedDispatchPolicyId,
        Action<EngineBuilder> RegisterProvider)
    {
        public string ChannelId => $"provider.{ProviderId}.events";

        public string MessageType => $"tests.{ProviderId}-provider-event";

        public string OutboxMessageId => $"outbox-{UniqueId}";

        public string InboxMessageId => $"inbox-{UniqueId}";
    }

    private sealed class ProviderIntegrationModule : ModuleBase
    {
        private static readonly ModuleDescriptor DescriptorInstance = new(
            id: "provider-integration-test",
            displayName: "Provider Integration Test",
            description: "Test module used to bind provider-integration runtime composition.",
            tags: ["test", "provider-integration"],
            version: "1.0.0");

        public override ModuleDescriptor Descriptor => DescriptorInstance;
    }
}
