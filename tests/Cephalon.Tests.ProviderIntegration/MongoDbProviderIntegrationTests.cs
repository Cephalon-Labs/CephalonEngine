using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Technologies;
using Cephalon.Data.MongoDB.Configuration;
using Cephalon.Data.MongoDB.Registration;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Eventing.Registration;
using Cephalon.Eventing.Services;
using Cephalon.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cephalon.Tests.ProviderIntegration;

public sealed class MongoDbProviderIntegrationTests : IAsyncLifetime
{
    private MongoDbReplicaSetRunner? _runner;

    public async Task InitializeAsync()
    {
        _runner = await MongoDbReplicaSetRunner.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (_runner is not null)
        {
            await _runner.DisposeAsync();
        }
    }

    [Fact]
    public async Task MongoDbProvider_StagesOutboxInboxAndDispatchAgainstDisposableReplicaSet()
    {
        var uniqueId = Guid.NewGuid().ToString("N");
        var databaseName = $"cephalon_provider_{uniqueId}";
        var collectionPrefix = $"it_{uniqueId}_";
        var outboxMessageId = $"outbox-{uniqueId}";
        var inboxMessageId = $"inbox-{uniqueId}";

        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS", "Outbox"],
                technologies: ["EventDrivenIntegration"],
                data: new DataSettings(provider: "MongoDB", outboxEnabled: true)));
            engine.AddModule(new ProviderIntegrationModule());
            engine.AddEventing(options =>
            {
                options.Channels.Add(new EventChannelDescriptor(
                    id: "provider.mongodb.events",
                    displayName: "MongoDB Provider Events",
                    description: "Provider-integration dispatch lane."));
            });
            engine.AddMongoDbData(_runner!.ConnectionString, databaseName, configure: options =>
            {
                options.CollectionPrefix = collectionPrefix;
                options.RegisterOutbox = true;
                options.RegisterInbox = true;
            });
        });

        using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<Cephalon.Engine.Runtime.IRuntime>();
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "data.mongodb");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "data.document-store");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "data.outbox.mongodb");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "data.inbox.mongodb");

        var outboxDescriptor = Assert.Single(provider.GetRequiredService<IOutboxCatalog>().Outboxes);
        Assert.Equal("mongodb-outbox", outboxDescriptor.Id);
        Assert.Equal(MongoDbDataOptions.ProviderId, outboxDescriptor.Provider);
        Assert.Equal($"{collectionPrefix}outbox_messages", outboxDescriptor.Metadata["collection"]);

        var inboxDescriptor = Assert.Single(provider.GetRequiredService<IInboxCatalog>().Inboxes);
        Assert.Equal("mongodb-inbox", inboxDescriptor.Id);
        Assert.Equal(MongoDbDataOptions.ProviderId, inboxDescriptor.Provider);
        Assert.Equal($"{collectionPrefix}inbox_receipts", inboxDescriptor.Metadata["collection"]);

        var runtimeCatalog = provider.GetRequiredService<ITechnologyRuntimeCatalog>();
        var surfaces = runtimeCatalog.GetByTechnology("event-driven-integration");
        var outboxSurface = Assert.Single(surfaces, surface => surface.SurfaceId == "outbox-producers");
        var inboxSurface = Assert.Single(surfaces, surface => surface.SurfaceId == "inbox-stores");
        Assert.Contains(outboxSurface.Entries, entry => entry.Id == "mongodb-outbox" && entry.Metadata["provider"] == MongoDbDataOptions.ProviderId);
        Assert.Contains(inboxSurface.Entries, entry => entry.Id == "mongodb-inbox" && entry.Metadata["provider"] == MongoDbDataOptions.ProviderId);

        using var scope = provider.CreateScope();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutbox>();
        var inbox = scope.ServiceProvider.GetRequiredService<IInbox>();
        var dispatchStore = scope.ServiceProvider.GetRequiredService<IEventDispatchStore>();
        var database = provider.GetRequiredService<IMongoDatabase>();

        var outboxMessage = new OutboxMessage(
            id: outboxMessageId,
            channelId: "provider.mongodb.events",
            messageType: "tests.mongodb-provider-event",
            payload: """{"id":"mongodb-provider"}""",
            occurredAtUtc: DateTimeOffset.UtcNow,
            contentType: "application/json",
            correlationId: $"corr-{uniqueId}",
            tenantId: $"tenant-{uniqueId}",
            headers: new Dictionary<string, string> { ["cephalon-test"] = "mongodb-provider" },
            metadata: new Dictionary<string, string> { ["case"] = "provider-integration" });

        await outbox.EnqueueAsync(outboxMessage);
        await outbox.EnqueueAsync(outboxMessage);

        var outboxCollection = database.GetCollection<BsonDocument>($"{collectionPrefix}outbox_messages");
        var outboxFilter = Builders<BsonDocument>.Filter.Eq("MessageId", outboxMessageId);
        Assert.Equal(1, await outboxCollection.CountDocumentsAsync(outboxFilter));

        var pending = await dispatchStore.ReadPendingAsync(10);
        var dispatchItem = Assert.Single(pending);
        Assert.Equal("mongodb-outbox", dispatchItem.OutboxId);
        Assert.Equal(outboxMessageId, dispatchItem.MessageId);
        Assert.Equal("provider.mongodb.events", dispatchItem.ChannelId);
        Assert.Equal("tests.mongodb-provider-event", dispatchItem.EventType);
        Assert.Equal("mongodb-provider", dispatchItem.Headers["cephalon-test"]);

        await dispatchStore.ApplyReportAsync(new EventDispatchExecutionReport(
                outboxId: dispatchItem.OutboxId,
                channelId: dispatchItem.ChannelId,
                outcome: EventDispatchExecutionOutcomes.Succeeded,
                observedAtUtc: DateTimeOffset.UtcNow,
                messageId: dispatchItem.MessageId,
                attempt: 1));

        Assert.Empty(await dispatchStore.ReadPendingAsync(10));
        var outboxDocument = await outboxCollection.Find(outboxFilter).SingleAsync();
        Assert.True(outboxDocument.Contains("DispatchedAtUtc"));
        Assert.False(outboxDocument["DispatchedAtUtc"].IsBsonNull);

        Assert.False(await inbox.HasProcessedAsync(inboxMessageId));
        var inboxMessage = new InboxMessage(
            id: inboxMessageId,
            channelId: "provider.mongodb.events",
            messageType: "tests.mongodb-provider-event",
            payload: """{"id":"mongodb-provider-inbox"}""",
            receivedAtUtc: DateTimeOffset.UtcNow,
            correlationId: $"corr-{uniqueId}",
            tenantId: $"tenant-{uniqueId}");
        await inbox.MarkProcessedAsync(inboxMessage);
        await inbox.MarkProcessedAsync(inboxMessage);

        Assert.True(await inbox.HasProcessedAsync(inboxMessageId));
        var inboxCollection = database.GetCollection<BsonDocument>($"{collectionPrefix}inbox_receipts");
        Assert.Equal(1, await inboxCollection.CountDocumentsAsync(Builders<BsonDocument>.Filter.Eq("MessageId", inboxMessageId)));

        await provider.GetRequiredService<IMongoClient>().DropDatabaseAsync(databaseName);
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

