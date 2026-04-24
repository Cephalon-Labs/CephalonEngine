using System.Net.Http.Json;
using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Execution;
using Cephalon.AspNetCore.Hosting;
using Cephalon.Data.MongoDB.Configuration;
using Cephalon.Data.MongoDB.Registration;
using Cephalon.Data.Registration;
using Cephalon.Data.Services;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Runtime;
using Cephalon.Tests.Support;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cephalon.Tests.Hosting;

public sealed class MongoDbDataCdcHostingTests : IAsyncLifetime
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
    public async Task MapCephalonExposesMongoDbProviderNativeCdcRuntimeSurfaces()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularVerticalSlice";
        builder.Configuration[$"{EngineSettings.SectionName}:Patterns:0"] = "CQRS";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
        builder.AddCephalon(cephalon =>
        {
            cephalon.AddModule(new PlatformTestModule());
            cephalon.AddData(options =>
            {
                options.EnableCdcExecution = true;
                options.CdcPollingIntervalSeconds = 600;
            });
            cephalon.AddMongoDbData(ConnectionString, "cephalon-cdc-hosting-test", configure: options =>
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
                options.ChangeStreamCaptures.Add(capture);
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();
        await app.StartAsync();

        try
        {
            var stateCatalog = app.Services.GetRequiredService<ICdcCaptureRuntimeStateCatalog>();
            await WaitForAsync(
                () => Task.FromResult(stateCatalog.GetById(CaptureId)),
                static state => state is not null && state.StartedCount > 0,
                TimeSpan.FromSeconds(10));

            var database = app.Services.GetRequiredService<IMongoDatabase>();
            await database.GetCollection<BsonDocument>("orders").InsertOneAsync(new BsonDocument
            {
                ["orderId"] = "order-001",
                ["status"] = "created"
            });

            var client = app.GetTestClient();
            var cdcState = await WaitForAsync(
                () => client.GetFromJsonAsync<CdcCaptureRuntimeState>($"/engine/cdc-captures/runtime/{CaptureId}")!,
                static state => state is not null && state.LastOutcome == CdcCaptureRuntimeOutcomes.Captured,
                TimeSpan.FromSeconds(15));

            var cdcCaptureRuntimes = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes");
            var mongoRuntime = await WaitForAsync(
                () => client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor>($"/engine/cdc-capture-runtimes/{MongoRuntimeId}")!,
                static runtime => runtime is not null &&
                    runtime.Summary.LastOutcome == CdcCaptureRuntimeOutcomes.Captured &&
                    runtime.Summary.TotalCapturedChangeCount == 1 &&
                    runtime.Summary.TotalProducedMessageCount == 1,
                TimeSpan.FromSeconds(15));
            var capturesByMongoRuntime = await client.GetFromJsonAsync<CdcCaptureDescriptor[]>($"/engine/cdc-captures/execution-runtimes/{MongoRuntimeId}");
            var captureStatesByMongoRuntime = await client.GetFromJsonAsync<CdcCaptureRuntimeState[]>($"/engine/cdc-captures/runtime/execution-runtimes/{MongoRuntimeId}");
            var hostedExecutions = await client.GetFromJsonAsync<HostedExecutionDescriptor[]>("/engine/hosted-executions");
            var executionGraphs = await client.GetFromJsonAsync<ExecutionGraphDescriptor[]>("/engine/execution-graphs");
            var snapshot = await WaitForAsync(
                () => client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot")!,
                static current => current is not null &&
                    current.CdcCaptureStates.Any(item =>
                        item.CdcCaptureId == CaptureId &&
                        item.LastOutcome == CdcCaptureRuntimeOutcomes.Captured) &&
                    current.CdcCaptureExecutionRuntimes.Any(item =>
                        item.Id == MongoRuntimeId &&
                        item.Summary.LastOutcome == CdcCaptureRuntimeOutcomes.Captured),
                TimeSpan.FromSeconds(15));

            Assert.NotNull(cdcCaptureRuntimes);
            Assert.NotNull(mongoRuntime);
            Assert.Contains(cdcCaptureRuntimes, runtime => runtime.Id == SharedRuntimeId);
            Assert.Contains(cdcCaptureRuntimes, runtime => runtime.Id == MongoRuntimeId);

            var sharedRuntime = Assert.Single(cdcCaptureRuntimes, runtime => runtime.Id == SharedRuntimeId);
            Assert.Empty(sharedRuntime.CdcCaptureIds);

            Assert.Equal("host-managed", mongoRuntime.ExecutionOwnership);
            Assert.Equal("provider-native", mongoRuntime.ExecutionTopology);
            Assert.Equal("provider-native", mongoRuntime.AcknowledgementMode);
            Assert.Equal([CaptureId], mongoRuntime.CdcCaptureIds);
            Assert.True(mongoRuntime.Summary.HasReports);
            Assert.Equal(CaptureId, mongoRuntime.Summary.LastCdcCaptureId);
            Assert.Equal(CdcCaptureRuntimeOutcomes.Captured, mongoRuntime.Summary.LastOutcome);
            Assert.Equal(1, mongoRuntime.Summary.TotalCapturedChangeCount);
            Assert.Equal(1, mongoRuntime.Summary.TotalProducedMessageCount);

            var capture = Assert.Single(capturesByMongoRuntime!);
            Assert.Equal(CaptureId, capture.Id);
            Assert.Equal("platform", capture.SourceModuleId);
            Assert.Equal(MongoRuntimeId, capture.ExecutionBinding.EffectiveExecutionRuntimeId);
            Assert.Equal("provider-native", capture.ExecutionBinding.ExecutionTopology);
            Assert.Equal("mongodb-data", capture.Metadata["contributorModuleId"]);

            var captureState = Assert.Single(captureStatesByMongoRuntime!);
            Assert.Equal(CaptureId, captureState.CdcCaptureId);
            Assert.Equal(MongoRuntimeId, captureState.ExecutionBinding.EffectiveExecutionRuntimeId);
            Assert.Equal(CdcCaptureRuntimeOutcomes.Captured, captureState.LastOutcome);
            Assert.Equal(CdcCapturePublicationStates.PendingPublication, captureState.Publication.State);

            Assert.NotNull(cdcState);
            Assert.Equal(MongoRuntimeId, cdcState.ExecutionBinding.EffectiveExecutionRuntimeId);
            Assert.Equal("mongodb-provider-native-runtime", cdcState.Metadata["captureExecution"]);
            Assert.Equal(MongoRuntimeId, cdcState.Metadata["cdcCaptureExecutionRuntimeId"]);
            Assert.Equal("provider-native", cdcState.Metadata["acknowledgement"]);

            var hostedExecution = Assert.Single(hostedExecutions!, item => item.Id == MongoRuntimeId);
            Assert.Equal("mongodb-data", hostedExecution.SourceModuleId);
            Assert.Equal("background-service", hostedExecution.Kind);

            var executionGraph = Assert.Single(executionGraphs!, item => item.Id == "mongodb-change-stream-capture-flow");
            Assert.Equal("mongodb-data", executionGraph.SourceModuleId);
            Assert.Equal("resolve-mongodb-change-stream-captures", executionGraph.EntryNodeId);

            Assert.NotNull(snapshot);
            Assert.Contains(snapshot.CdcCaptures, item => item.Id == CaptureId &&
                item.ExecutionBinding.EffectiveExecutionRuntimeId == MongoRuntimeId);
            Assert.Contains(snapshot.CdcCaptureStates, item => item.CdcCaptureId == CaptureId &&
                item.LastOutcome == CdcCaptureRuntimeOutcomes.Captured);
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == MongoRuntimeId &&
                item.Summary.LastOutcome == CdcCaptureRuntimeOutcomes.Captured);
        }
        finally
        {
            await app.StopAsync();
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

        throw new TimeoutException("Timed out while waiting for the expected MongoDB CDC hosting condition.");
    }
}
