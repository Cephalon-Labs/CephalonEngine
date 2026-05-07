using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.EventSourcing;
using Cephalon.Abstractions.Modules;
using Cephalon.Data.Redis.Configuration;
using Cephalon.Data.Redis.Registration;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Eventing.Registration;
using Cephalon.Eventing.Services;
using Cephalon.EventSourcing.Hosting;
using Cephalon.EventSourcing.Redis;
using Cephalon.EventSourcing.Redis.Hosting;
using Cephalon.Tests.ProviderIntegration.ExternalServices;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Testcontainers.Redis;

namespace Cephalon.Tests.ProviderIntegration;

public sealed class RedisProviderIntegrationTests : IAsyncLifetime
{
    private const string RedisImage = "redis:7-alpine";
    private RedisContainer? _container;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync().ConfigureAwait(false);
        }
    }

    [ExternalProviderServiceFact(ExternalProviderServiceProvider.Redis)]
    public async Task RedisProvider_StagesOutboxInboxDispatchAndEventStreamAgainstLiveRedis()
    {
        var gate = ExternalProviderServiceGate.FromEnvironment();
        var connectionString = await ResolveRedisConnectionStringAsync(gate).ConfigureAwait(false);
        var uniqueId = Guid.NewGuid().ToString("N");
        var keyPrefix = $"cephalon:test:{uniqueId}:";
        var streamId = $"orders-{uniqueId}";
        var outboxMessageId = $"outbox-{uniqueId}";
        var inboxMessageId = $"inbox-{uniqueId}";

        var services = new ServiceCollection();
        services.AddCephalonEventSourcing();
        services.AddCephalonEventType<RedisProviderEvent>("tests.redis-provider-event");
        services.AddCephalonRedisEventSourcing(connectionString, keyPrefix);
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS", "Outbox"],
                technologies: ["EventDrivenIntegration"],
                data: new DataSettings(provider: "Redis", outboxEnabled: true)));
            engine.AddModule(new ProviderIntegrationModule());
            engine.AddEventing(options =>
            {
                options.Channels.Add(new EventChannelDescriptor(
                    id: "provider.redis.events",
                    displayName: "Redis Provider Events",
                    description: "Provider-integration dispatch lane."));
            });
            engine.AddRedisData(connectionString, configure: options =>
            {
                options.KeyPrefix = keyPrefix;
                options.RegisterOutbox = true;
                options.RegisterInbox = true;
            });
        });

        using var provider = services.BuildServiceProvider();
        var multiplexer = provider.GetRequiredService<IConnectionMultiplexer>();

        try
        {
            var runtime = provider.GetRequiredService<Cephalon.Engine.Runtime.IRuntime>();
            Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "data.redis");
            Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "data.outbox.redis");
            Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "data.inbox.redis");

            var outboxDescriptor = Assert.Single(provider.GetRequiredService<IOutboxCatalog>().Outboxes);
            Assert.Equal("redis-outbox", outboxDescriptor.Id);
            Assert.Equal(RedisDataOptions.ProviderId, outboxDescriptor.Provider);
            Assert.Equal(keyPrefix, outboxDescriptor.Metadata["keyPrefix"]);

            var inboxDescriptor = Assert.Single(provider.GetRequiredService<IInboxCatalog>().Inboxes);
            Assert.Equal("redis-inbox", inboxDescriptor.Id);
            Assert.Equal(RedisDataOptions.ProviderId, inboxDescriptor.Provider);
            Assert.Equal(keyPrefix, inboxDescriptor.Metadata["keyPrefix"]);

            var eventStream = Assert.Single(provider.GetRequiredService<IEventStoreCatalog>().All);
            Assert.Equal("redis-event-store", eventStream.Id);
            Assert.Equal(RedisDataOptions.ProviderId, eventStream.Provider);
            Assert.Equal(keyPrefix, eventStream.Metadata["keyPrefix"]);

            using var scope = provider.CreateScope();
            var outbox = scope.ServiceProvider.GetRequiredService<IOutbox>();
            var inbox = scope.ServiceProvider.GetRequiredService<IInbox>();
            var dispatchStore = scope.ServiceProvider.GetRequiredService<IEventDispatchStore>();
            var eventStore = scope.ServiceProvider.GetRequiredService<IEventStore>();
            var database = multiplexer.GetDatabase();

            var outboxMessage = new OutboxMessage(
                id: outboxMessageId,
                channelId: "provider.redis.events",
                messageType: "tests.redis-provider-event",
                payload: """{"id":"redis-provider"}""",
                occurredAtUtc: DateTimeOffset.UtcNow,
                contentType: "application/json",
                correlationId: $"corr-{uniqueId}",
                tenantId: $"tenant-{uniqueId}",
                headers: new Dictionary<string, string> { ["cephalon-test"] = "redis-provider" },
                metadata: new Dictionary<string, string> { ["case"] = "provider-integration" });

            await outbox.EnqueueAsync(outboxMessage).ConfigureAwait(false);
            await outbox.EnqueueAsync(outboxMessage).ConfigureAwait(false);

            var outboxHashKey = $"{keyPrefix}outbox:msg:{outboxMessageId}";
            var pendingSetKey = $"{keyPrefix}outbox:pending";
            Assert.True(await database.KeyExistsAsync(outboxHashKey).ConfigureAwait(false));
            Assert.Equal(1, await database.SortedSetLengthAsync(pendingSetKey).ConfigureAwait(false));

            var pending = await dispatchStore.ReadPendingAsync(10).ConfigureAwait(false);
            var dispatchItem = Assert.Single(pending);
            Assert.Equal(outboxMessageId, dispatchItem.MessageId);
            Assert.Equal("provider.redis.events", dispatchItem.ChannelId);
            Assert.Equal("tests.redis-provider-event", dispatchItem.EventType);
            Assert.Equal("redis-provider", dispatchItem.Headers["cephalon-test"]);

            await dispatchStore.ApplyReportAsync(new EventDispatchExecutionReport(
                    outboxId: dispatchItem.OutboxId,
                    channelId: dispatchItem.ChannelId,
                    outcome: EventDispatchExecutionOutcomes.Succeeded,
                    observedAtUtc: DateTimeOffset.UtcNow,
                    messageId: dispatchItem.MessageId,
                    attempt: 1))
                .ConfigureAwait(false);

            Assert.Empty(await dispatchStore.ReadPendingAsync(10).ConfigureAwait(false));
            Assert.Equal(0, await database.SortedSetLengthAsync(pendingSetKey).ConfigureAwait(false));
            Assert.False((await database.HashGetAsync(outboxHashKey, "DispatchedAtUtc").ConfigureAwait(false)).IsNullOrEmpty);

            Assert.False(await inbox.HasProcessedAsync(inboxMessageId).ConfigureAwait(false));
            var inboxMessage = new InboxMessage(
                id: inboxMessageId,
                channelId: "provider.redis.events",
                messageType: "tests.redis-provider-event",
                payload: """{"id":"redis-provider-inbox"}""",
                receivedAtUtc: DateTimeOffset.UtcNow,
                correlationId: $"corr-{uniqueId}",
                tenantId: $"tenant-{uniqueId}");
            await inbox.MarkProcessedAsync(inboxMessage).ConfigureAwait(false);
            await inbox.MarkProcessedAsync(inboxMessage).ConfigureAwait(false);

            Assert.True(await inbox.HasProcessedAsync(inboxMessageId).ConfigureAwait(false));
            Assert.Equal(1, await database.SetLengthAsync($"{keyPrefix}inbox:receipts").ConfigureAwait(false));

            Assert.Equal(-1, await eventStore.GetVersionAsync(streamId).ConfigureAwait(false));
            await eventStore.AppendAsync(
                    streamId,
                    [
                        new RedisProviderEvent(streamId, 0, new DateTime(2026, 5, 7, 0, 0, 0, DateTimeKind.Utc)),
                        new RedisProviderEvent(streamId, 1, new DateTime(2026, 5, 7, 0, 1, 0, DateTimeKind.Utc))
                    ],
                    -1)
                .ConfigureAwait(false);

            var replayed = new List<IDomainEvent>();
            await foreach (var evt in eventStore.ReadStreamAsync(streamId).ConfigureAwait(false))
            {
                replayed.Add(evt);
            }

            Assert.Collection(
                replayed,
                static evt => Assert.Equal(0, evt.StreamVersion),
                static evt => Assert.Equal(1, evt.StreamVersion));
            Assert.All(replayed, static evt => Assert.IsType<RedisProviderEvent>(evt));
            Assert.Equal(1, await eventStore.GetVersionAsync(streamId).ConfigureAwait(false));
            Assert.Equal(2, await database.StreamLengthAsync(RedisEventSourcingConfiguration.StreamKey(keyPrefix, streamId)).ConfigureAwait(false));

            var exception = await Assert.ThrowsAsync<EventStreamConcurrencyException>(() =>
                    eventStore.AppendAsync(
                        streamId,
                        [new RedisProviderEvent(streamId, 2, new DateTime(2026, 5, 7, 0, 2, 0, DateTimeKind.Utc))],
                        0))
                .ConfigureAwait(false);

            Assert.Equal(streamId, exception.StreamId);
            Assert.Equal(0, exception.ExpectedVersion);
            Assert.Equal(1, exception.ActualVersion);
        }
        finally
        {
            await CleanupKeysAsync(multiplexer, keyPrefix, streamId, outboxMessageId).ConfigureAwait(false);
        }
    }

    private async Task<string> ResolveRedisConnectionStringAsync(ExternalProviderServiceGate gate)
    {
        return gate.ResolveRedisMode() switch
        {
            ExternalProviderServiceMode.PreProvisionedConnectionString => gate.RedisConnectionString!,
            ExternalProviderServiceMode.Testcontainers => await StartContainerAsync().ConfigureAwait(false),
            _ => throw new InvalidOperationException(ExternalProviderServiceGate.SkipReason)
        };
    }

    private async Task<string> StartContainerAsync()
    {
        _container = new RedisBuilder(RedisImage).Build();
        await _container.StartAsync().ConfigureAwait(false);
        return _container.GetConnectionString();
    }

    private static Task<long> CleanupKeysAsync(
        IConnectionMultiplexer multiplexer,
        string keyPrefix,
        string streamId,
        string outboxMessageId)
    {
        var database = multiplexer.GetDatabase();
        RedisKey[] keys =
        [
            $"{keyPrefix}outbox:msg:{outboxMessageId}",
            $"{keyPrefix}outbox:pending",
            $"{keyPrefix}inbox:receipts",
            RedisEventSourcingConfiguration.StreamKey(keyPrefix, streamId)
        ];

        return database.KeyDeleteAsync(keys);
    }

    private sealed record RedisProviderEvent(
        string StreamId,
        long StreamVersion,
        DateTime OccurredAtUtc) : DomainEvent(StreamId, StreamVersion, OccurredAtUtc);

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
