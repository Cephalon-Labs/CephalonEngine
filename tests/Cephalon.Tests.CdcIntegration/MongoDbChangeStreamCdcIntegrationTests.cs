using Cephalon.Abstractions.Data;
using Cephalon.Data.MongoDB.Configuration;
using Cephalon.Data.MongoDB.Registration;
using Cephalon.Data.Registration;
using Cephalon.Data.Services;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Eventing.Services;
using Cephalon.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cephalon.Tests.CdcIntegration;

public sealed class MongoDbChangeStreamCdcIntegrationTests : IAsyncLifetime
{
    private const string SharedRuntimeId = "data-cdc-capture-pump";
    private const string MongoRuntimeId = "mongodb-change-stream-capture-pump";
    private const string CaptureId = "mongo-orders-cdc";
    private MongoDbReplicaSetRunner? runner;

    public async Task InitializeAsync()
    {
        runner = await MongoDbReplicaSetRunner.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (runner is not null)
        {
            await runner.DisposeAsync();
        }

        runner = null;
    }

    private string ConnectionString => runner!.ConnectionString;

    [Fact]
    public async Task MongoDbChangeStream_StagesOutboxAndPersistsCheckpointAgainstReplicaSet()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS"]));
            engine.AddModule(new PlatformTestModule());
            engine.AddData(options =>
            {
                options.EnableCdcExecution = true;
                options.CdcPollingIntervalSeconds = 600;
            });
            engine.AddMongoDbData(ConnectionString, "cephalon-cdc-integration-test", configure: options =>
            {
                options.RegisterOutbox = true;

                var capture = new MongoDbChangeStreamCaptureOptions
                {
                    Id = CaptureId,
                    DisplayName = "Mongo Orders CDC",
                    Description = "Captures MongoDB order changes through a provider-native change stream.",
                    SourceModuleId = "platform",
                    CollectionName = "orders",
                    ChannelId = "orders",
                    MessageType = "orders.mongo.changed",
                    FullDocumentMode = "update-lookup",
                    MaxAwaitTimeSeconds = 1
                };
                capture.Tags.Add("orders");
                capture.ResourceIds.Add("cephalon-cdc-integration-test.orders");
                options.ChangeStreamCaptures.Add(capture);
            });
        });

        using var provider = services.BuildServiceProvider();
        var hostedServices = provider.GetServices<IHostedService>().ToArray();
        foreach (var hostedService in hostedServices)
        {
            await hostedService.StartAsync(CancellationToken.None);
        }

        try
        {
            var stateCatalog = provider.GetRequiredService<ICdcCaptureRuntimeStateCatalog>();
            await WaitForAsync(
                () => Task.FromResult(stateCatalog.GetById(CaptureId)),
                static state => state is not null && state.StartedCount > 0,
                TimeSpan.FromSeconds(10));

            var database = provider.GetRequiredService<IMongoDatabase>();
            await database.GetCollection<BsonDocument>("orders").InsertOneAsync(new BsonDocument
            {
                ["orderId"] = "order-001",
                ["status"] = "created"
            });

            using var dispatchScope = provider.CreateScope();
            var dispatchStore = dispatchScope.ServiceProvider.GetRequiredService<IEventDispatchStore>();
            var pending = await WaitForAsync(
                () => dispatchStore.ReadPendingAsync(10).AsTask(),
                static items => items.Count > 0,
                TimeSpan.FromSeconds(15));

            var item = Assert.Single(pending);
            Assert.Equal("mongodb-outbox", item.OutboxId);
            Assert.Equal("orders", item.ChannelId);
            Assert.Equal("orders.mongo.changed", item.EventType);
            Assert.Equal("application/vnd.cephalon.mongodb.change-stream+json", item.ContentType);
            Assert.Equal(MongoDbDataOptions.ProviderId, item.Headers["provider"]);
            Assert.Equal(CaptureId, item.Headers["cdcCaptureId"]);
            Assert.Equal("orders", item.Headers["collectionName"]);

            var payload = BsonDocument.Parse(item.Payload);
            Assert.Equal("insert", payload["operationType"].AsString);

            var captureCatalog = provider.GetRequiredService<ICdcCaptureCatalog>();
            var runtimeCatalog = provider.GetRequiredService<ICdcCaptureExecutionRuntimeCatalog>();
            var capture = captureCatalog.GetById(CaptureId);
            Assert.NotNull(capture);
            Assert.Equal("platform", capture.SourceModuleId);
            Assert.Equal(MongoRuntimeId, capture.ExecutionBinding.EffectiveExecutionRuntimeId);
            Assert.Equal("host-managed", capture.ExecutionBinding.ExecutionOwnership);
            Assert.Equal("provider-native", capture.ExecutionBinding.ExecutionTopology);
            Assert.Equal("requested-execution-runtime", capture.ExecutionBinding.ResolutionMode);
            Assert.Equal("mongodb-data", capture.Metadata["contributorModuleId"]);

            Assert.Empty(runtimeCatalog.GetById(SharedRuntimeId)!.CdcCaptureIds);
            var mongoRuntime = runtimeCatalog.GetById(MongoRuntimeId);
            Assert.NotNull(mongoRuntime);
            Assert.Equal("host-managed", mongoRuntime.ExecutionOwnership);
            Assert.Equal("provider-native", mongoRuntime.ExecutionTopology);
            Assert.Equal("provider-native", mongoRuntime.AcknowledgementMode);
            Assert.Equal([CaptureId], mongoRuntime.CdcCaptureIds);

            var state = await WaitForAsync(
                () => Task.FromResult(stateCatalog.GetById(CaptureId)),
                static current => HasObservedInsertedChange(current),
                TimeSpan.FromSeconds(15));

            Assert.NotNull(state);
            Assert.Equal(MongoRuntimeId, state.ExecutionBinding.EffectiveExecutionRuntimeId);
            Assert.True(IsCapturedOrIdle(state.LastOutcome));
            Assert.True(state.CapturedCount > 0);
            Assert.Equal(1, state.TotalCapturedChangeCount);
            Assert.Equal(1, state.TotalProducedMessageCount);
            Assert.False(string.IsNullOrWhiteSpace(state.LastCheckpoint));
            Assert.False(string.IsNullOrWhiteSpace(state.LastChangeId));
            Assert.Equal("mongodb-provider-native-runtime", state.Metadata["captureExecution"]);
            Assert.Equal(MongoRuntimeId, state.Metadata["cdcCaptureExecutionRuntimeId"]);
            Assert.Equal("provider-native", state.Metadata["acknowledgement"]);
            Assert.Equal(CdcCapturePublicationStates.PendingPublication, state.Publication.State);
            Assert.Equal(1, state.Publication.PendingPublicationCount);

            mongoRuntime = runtimeCatalog.GetById(MongoRuntimeId);
            Assert.NotNull(mongoRuntime);
            Assert.True(mongoRuntime.Summary.HasReports);
            Assert.Equal(CaptureId, mongoRuntime.Summary.LastCdcCaptureId);
            Assert.True(IsCapturedOrIdle(mongoRuntime.Summary.LastOutcome));
            Assert.True(mongoRuntime.Summary.CapturedCount > 0);
            Assert.Equal(1, mongoRuntime.Summary.TotalCapturedChangeCount);
            Assert.Equal(1, mongoRuntime.Summary.TotalProducedMessageCount);
            Assert.Equal("provider-native", mongoRuntime.Summary.LastAcknowledgement);

            var checkpointCollection = database.GetCollection<BsonDocument>("cdc_change_stream_checkpoints");
            var checkpoint = await checkpointCollection.Find(Builders<BsonDocument>.Filter.Eq("_id", CaptureId))
                .SingleOrDefaultAsync();
            Assert.NotNull(checkpoint);
            Assert.Equal(state.LastChangeId, checkpoint["ChangeId"].AsString);
            Assert.False(string.IsNullOrWhiteSpace(checkpoint["ResumeTokenJson"].AsString));
        }
        finally
        {
            foreach (var hostedService in hostedServices.Reverse())
            {
                await hostedService.StopAsync(CancellationToken.None);
            }
        }
    }

    private static async Task<T> WaitForAsync<T>(
        Func<Task<T>> producer,
        Func<T, bool> predicate,
        TimeSpan timeout)
    {
        using var cancellationTokenSource = new CancellationTokenSource(timeout);
        while (!cancellationTokenSource.IsCancellationRequested)
        {
            var current = await producer().ConfigureAwait(false);
            if (predicate(current))
            {
                return current;
            }

            await Task.Delay(200, cancellationTokenSource.Token).ConfigureAwait(false);
        }

        throw new TimeoutException("Timed out while waiting for the expected MongoDB CDC integration condition.");
    }

    private static bool HasObservedInsertedChange(CdcCaptureRuntimeState? state)
    {
        return state is not null &&
            IsCapturedOrIdle(state.LastOutcome) &&
            state.CapturedCount > 0 &&
            state.TotalCapturedChangeCount == 1 &&
            state.TotalProducedMessageCount == 1;
    }

    private static bool IsCapturedOrIdle(string? outcome)
    {
        return string.Equals(outcome, CdcCaptureRuntimeOutcomes.Captured, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(outcome, CdcCaptureRuntimeOutcomes.Idle, StringComparison.OrdinalIgnoreCase);
    }
}
