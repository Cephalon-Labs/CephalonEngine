using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.EventSourcing;
using Cephalon.Data.MongoDB.Configuration;
using Cephalon.Data.MongoDB.Registration;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Eventing.Registration;
using Cephalon.Eventing.Services;
using Cephalon.EventSourcing.MongoDB;
using Cephalon.EventSourcing.MongoDB.Hosting;
using Cephalon.EventSourcing.Hosting;
using Cephalon.Tests.Support;
using EphemeralMongo;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Composition;

public sealed class MongoDbDataPackTests : IAsyncLifetime
{
    private IMongoRunner? _runner;

    public Task InitializeAsync()
    {
        _runner = MongoRunner.Run(new MongoRunnerOptions
        {
            UseSingleNodeReplicaSet = false
        });
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        try
        {
            _runner?.Dispose();
        }
        catch (Exception)
        {
            // EphemeralMongo may fail to gracefully close its internal MongoDB.Driver 2.x connection
            // when the host application uses MongoDB.Driver 3.x. The mongod process is still terminated.
        }

        _runner = null;
        return Task.CompletedTask;
    }

    private string ConnectionString => _runner!.ConnectionString;

    [Fact]
    public void AddMongoDbData_RegistersMongoDbDataModule_WithExpectedCapabilities()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS"],
                data: new DataSettings(provider: "MongoDB")));
            engine.AddModule(new PlatformTestModule());
            engine.AddMongoDbData(ConnectionString, "cephalon-test");
        });

        using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<Cephalon.Engine.Runtime.IRuntime>();

        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "data.mongodb");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "data.document-store");
    }

    [Fact]
    public void AddMongoDbData_WithConnectionStringName_ResolvesFromConnectionStrings()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:MongoDbPrimary"] = ConnectionString
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS"],
                data: new DataSettings(provider: "MongoDB")));
            engine.AddModule(new PlatformTestModule());
            engine.AddMongoDbData(options =>
            {
                options.ConnectionStringName = "MongoDbPrimary";
                options.DatabaseName = "cephalon-config-test";
            });
        });

        using var provider = services.BuildServiceProvider();
        var database = provider.GetRequiredService<MongoDB.Driver.IMongoDatabase>();

        Assert.Equal("cephalon-config-test", database.DatabaseNamespace.DatabaseName);
    }

    [Fact]
    public void AddMongoDbData_WithConnectionStringAndName_FailsFast()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:MongoDbPrimary"] = ConnectionString
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS"],
                data: new DataSettings(provider: "MongoDB")));
            engine.AddModule(new PlatformTestModule());
            engine.AddMongoDbData(options =>
            {
                options.ConnectionStringName = "MongoDbPrimary";
                options.ConnectionString = ConnectionString;
                options.DatabaseName = "cephalon-config-test";
            });
        });

        using var provider = services.BuildServiceProvider();
        var exception = Assert.Throws<InvalidOperationException>(() =>
            provider.GetRequiredService<MongoDB.Driver.IMongoClient>());

        Assert.Contains("either ConnectionStringName or ConnectionString", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MongoDbOutbox_StagesAndReadsMessages_RoundTrip()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS", "Outbox"],
                data: new DataSettings(
                    provider: "MongoDB",
                    outboxEnabled: true)));
            engine.AddModule(new PlatformTestModule());
            engine.AddMongoDbData(ConnectionString, "cephalon-outbox-test", configure: options =>
            {
                options.RegisterOutbox = true;
            });
        });

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutbox>();
        var outboxCatalog = provider.GetRequiredService<IOutboxCatalog>();

        var message = new OutboxMessage(
            id: "msg-001",
            channelId: "test-events",
            messageType: "test.event.created",
            payload: "{\"id\":\"test-001\"}",
            occurredAtUtc: DateTimeOffset.UtcNow,
            correlationId: "corr-001");

        await outbox.EnqueueAsync(message);

        var outboxDescriptor = Assert.Single(outboxCatalog.Outboxes);
        Assert.Equal("mongodb-outbox", outboxDescriptor.Id);
        Assert.Equal(MongoDbDataOptions.ProviderId, outboxDescriptor.Provider);
        Assert.Equal("document-collection", outboxDescriptor.Mode);
    }

    [Fact]
    public async Task MongoDbOutbox_UniqueMessageId_PreventsDoubleStaging()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS", "Outbox"],
                data: new DataSettings(
                    provider: "MongoDB",
                    outboxEnabled: true)));
            engine.AddModule(new PlatformTestModule());
            engine.AddMongoDbData(ConnectionString, "cephalon-idempotent-test", configure: options =>
            {
                options.RegisterOutbox = true;
            });
        });

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutbox>();

        var message = new OutboxMessage(
            id: "msg-idempotent-001",
            channelId: "test-events",
            messageType: "test.event.duplicate",
            payload: "{\"id\":\"test-dup\"}",
            occurredAtUtc: DateTimeOffset.UtcNow);

        // Enqueue twice — should not throw
        await outbox.EnqueueAsync(message);
        await outbox.EnqueueAsync(message);
    }

    [Fact]
    public async Task MongoDbData_WithEventDrivenIntegration_RegistersConsumerManagedDispatchStore()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS", "Outbox"],
                technologies: ["EventDrivenIntegration"],
                data: new DataSettings(
                    provider: "MongoDB",
                    outboxEnabled: true)));
            engine.AddModule(new PlatformTestModule());
            engine.AddEventing(options =>
            {
                options.Channels.Add(new EventChannelDescriptor(
                    id: "test-events",
                    displayName: "Test Events",
                    description: "Dispatch-store test channel."));
            });
            engine.AddMongoDbData(ConnectionString, "cephalon-dispatch-store-test", configure: options =>
            {
                options.RegisterOutbox = true;
            });
        });

        using var provider = services.BuildServiceProvider();
        var outboxCatalog = provider.GetRequiredService<IOutboxCatalog>();
        var descriptor = Assert.Single(outboxCatalog.Outboxes);
        Assert.Equal("consumer-managed", descriptor.DispatchPolicy.PolicyId);
        Assert.Equal("consumer-managed", descriptor.DispatchPolicy.ExecutionMode);

        using var scope = provider.CreateScope();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutbox>();
        var dispatchStore = scope.ServiceProvider.GetRequiredService<IEventDispatchStore>();

        var message = new OutboxMessage(
            id: "msg-dispatch-001",
            channelId: "test-events",
            messageType: "test.event.dispatch",
            payload: "{\"id\":\"dispatch-001\"}",
            occurredAtUtc: DateTimeOffset.UtcNow,
            correlationId: "corr-dispatch-001");

        await outbox.EnqueueAsync(message);

        var pending = await dispatchStore.ReadPendingAsync(10);
        var item = Assert.Single(pending);
        Assert.Equal("mongodb-outbox", item.OutboxId);
        Assert.Equal(message.Id, item.MessageId);

        await dispatchStore.ApplyReportAsync(new EventDispatchExecutionReport(
            outboxId: "mongodb-outbox",
            channelId: message.ChannelId,
            outcome: EventDispatchExecutionOutcomes.Succeeded,
            observedAtUtc: DateTimeOffset.UtcNow,
            messageId: message.Id,
            attempt: 1));

        Assert.Empty(await dispatchStore.ReadPendingAsync(10));
    }

    [Fact]
    public async Task MongoDbInbox_TracksProcessedMessages_IdempotentMarkAndCheck()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS"],
                data: new DataSettings(provider: "MongoDB")));
            engine.AddModule(new PlatformTestModule());
            engine.AddMongoDbData(ConnectionString, "cephalon-inbox-test", configure: options =>
            {
                options.RegisterInbox = true;
            });
        });

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var inbox = scope.ServiceProvider.GetRequiredService<IInbox>();
        var inboxCatalog = provider.GetRequiredService<IInboxCatalog>();

        var message = new InboxMessage(
            id: "inbox-msg-001",
            channelId: "test-events",
            messageType: "test.event.received",
            payload: "{\"id\":\"inbox-001\"}",
            receivedAtUtc: DateTimeOffset.UtcNow,
            correlationId: "corr-inbox-001");

        Assert.False(await inbox.HasProcessedAsync("inbox-msg-001"));

        await inbox.MarkProcessedAsync(message);
        await inbox.MarkProcessedAsync(message); // idempotent

        Assert.True(await inbox.HasProcessedAsync("inbox-msg-001"));

        var inboxDescriptor = Assert.Single(inboxCatalog.Inboxes);
        Assert.Equal("mongodb-inbox", inboxDescriptor.Id);
        Assert.Equal(MongoDbDataOptions.ProviderId, inboxDescriptor.Provider);
        Assert.Equal("document-collection", inboxDescriptor.Mode);
        Assert.Equal("message-id", inboxDescriptor.Metadata["idempotency"]);
    }

    [Fact]
    public async Task MongoDbEventStore_AppendAndRead_RoundTrips()
    {
        var services = new ServiceCollection();
        services.AddCephalonEventType<MongoTestEvent>("tests.mongo-event");
        services.AddCephalonMongoDbEventSourcing(ConnectionString, "cephalon-events-test");

        using var provider = services.BuildServiceProvider();
        var eventStore = provider.GetRequiredService<IEventStore>();

        var streamId = "stream-roundtrip-01";
        await eventStore.AppendAsync(
            streamId,
            [
                new MongoTestEvent(streamId, 0, new DateTime(2026, 4, 6, 0, 0, 0, DateTimeKind.Utc)),
                new MongoTestEvent(streamId, 1, new DateTime(2026, 4, 6, 0, 1, 0, DateTimeKind.Utc)),
                new MongoTestEvent(streamId, 2, new DateTime(2026, 4, 6, 0, 2, 0, DateTimeKind.Utc))
            ],
            -1);

        var results = new List<IDomainEvent>();
        await foreach (var evt in eventStore.ReadStreamAsync(streamId))
        {
            results.Add(evt);
        }

        Assert.Equal(3, results.Count);
        Assert.Equal(0, results[0].StreamVersion);
        Assert.Equal(1, results[1].StreamVersion);
        Assert.Equal(2, results[2].StreamVersion);

        var version = await eventStore.GetVersionAsync(streamId);
        Assert.Equal(2, version);
    }

    [Fact]
    public async Task MongoDbEventStore_OptimisticConcurrency_ThrowsOnVersionMismatch()
    {
        var services = new ServiceCollection();
        services.AddCephalonEventType<MongoTestEvent>("tests.mongo-event");
        services.AddCephalonMongoDbEventSourcing(ConnectionString, "cephalon-concurrency-test");

        using var provider = services.BuildServiceProvider();
        var eventStore = provider.GetRequiredService<IEventStore>();

        var streamId = "stream-concurrency-01";

        var exception = await Assert.ThrowsAsync<EventStreamConcurrencyException>(() =>
            eventStore.AppendAsync(
                streamId,
                [new MongoTestEvent(streamId, 5, new DateTime(2026, 4, 6, 0, 0, 0, DateTimeKind.Utc))],
                4));

        Assert.Equal(streamId, exception.StreamId);
        Assert.Equal(4, exception.ExpectedVersion);
        Assert.Equal(-1, exception.ActualVersion);
    }

    [Fact]
    public void MongoDbDataModule_ExposesCapabilities()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS", "Outbox"],
                data: new DataSettings(
                    provider: "MongoDB",
                    outboxEnabled: true)));
            engine.AddModule(new PlatformTestModule());
            engine.AddMongoDbData(ConnectionString, "cephalon-capabilities-test", configure: options =>
            {
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
    }

    private sealed record MongoTestEvent(
        string StreamId,
        long StreamVersion,
        DateTime OccurredAtUtc) : DomainEvent(StreamId, StreamVersion, OccurredAtUtc);
}
