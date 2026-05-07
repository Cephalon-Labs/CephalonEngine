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

[Collection(ProviderNativeCdcHostingCollectionDefinition.Name)]
public sealed class MongoDbDataCdcHostingTests : IAsyncLifetime
{
    private const string SharedRuntimeId = "data-cdc-capture-pump";
    private const string MongoRuntimeId = "mongodb-change-stream-capture-pump";
    private const string CaptureId = "mongo-orders-cdc";
    private static readonly TimeSpan ProviderNativeCdcTimeout = TimeSpan.FromSeconds(90);
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
                ProviderNativeCdcTimeout,
                "MongoDB CDC capture startup",
                DescribeCaptureState);

            var database = app.Services.GetRequiredService<IMongoDatabase>();
            await database.GetCollection<BsonDocument>("orders").InsertOneAsync(new BsonDocument
            {
                ["orderId"] = "order-001",
                ["status"] = "created"
            });

            var client = app.GetTestClient();
            var cdcState = await WaitForAsync(
                () => client.GetFromJsonAsync<CdcCaptureRuntimeState>($"/engine/cdc-captures/runtime/{CaptureId}")!,
                static state => HasObservedInsertedChange(state),
                ProviderNativeCdcTimeout,
                "MongoDB CDC capture state",
                DescribeCaptureState);

            var cdcCaptureRuntimes = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes");
            var mongoRuntime = await WaitForAsync(
                () => client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor>($"/engine/cdc-capture-runtimes/{MongoRuntimeId}")!,
                static runtime => HasObservedInsertedChange(runtime),
                ProviderNativeCdcTimeout,
                "MongoDB CDC execution-runtime summary",
                DescribeExecutionRuntime);
            var capturesByMongoRuntime = await client.GetFromJsonAsync<CdcCaptureDescriptor[]>($"/engine/cdc-captures/execution-runtimes/{MongoRuntimeId}");
            var captureStatesByMongoRuntime = await client.GetFromJsonAsync<CdcCaptureRuntimeState[]>($"/engine/cdc-captures/runtime/execution-runtimes/{MongoRuntimeId}");
            var hostedExecutions = await client.GetFromJsonAsync<HostedExecutionDescriptor[]>("/engine/hosted-executions");
            var executionGraphs = await client.GetFromJsonAsync<ExecutionGraphDescriptor[]>("/engine/execution-graphs");
            var snapshot = await WaitForAsync(
                () => client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot")!,
                static current => current is not null &&
                    current.CdcCaptureStates.Any(item =>
                        item.CdcCaptureId == CaptureId &&
                        HasObservedInsertedChange(item)) &&
                    current.CdcCaptureExecutionRuntimes.Any(item =>
                        item.Id == MongoRuntimeId &&
                        HasObservedInsertedChange(item)),
                ProviderNativeCdcTimeout,
                "MongoDB CDC snapshot",
                DescribeSnapshot);

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
            Assert.True(IsCapturedOrIdle(mongoRuntime.Summary.LastOutcome));
            Assert.True(mongoRuntime.Summary.CapturedCount > 0);
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
            Assert.True(IsCapturedOrIdle(captureState.LastOutcome));
            Assert.True(captureState.CapturedCount > 0);
            Assert.Equal(1, captureState.TotalCapturedChangeCount);
            Assert.Equal(1, captureState.TotalProducedMessageCount);
            Assert.Equal(CdcCapturePublicationStates.PendingPublication, captureState.Publication.State);

            Assert.NotNull(cdcState);
            Assert.Equal(MongoRuntimeId, cdcState.ExecutionBinding.EffectiveExecutionRuntimeId);
            Assert.True(IsCapturedOrIdle(cdcState.LastOutcome));
            Assert.True(cdcState.CapturedCount > 0);
            Assert.Equal(1, cdcState.TotalCapturedChangeCount);
            Assert.Equal(1, cdcState.TotalProducedMessageCount);
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
                HasObservedInsertedChange(item));
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == MongoRuntimeId &&
                HasObservedInsertedChange(item));
        }
        finally
        {
            await app.StopAsync();
        }
    }

    private static async Task<T> WaitForAsync<T>(
        Func<Task<T>> producer,
        Func<T, bool> predicate,
        TimeSpan timeout,
        string conditionName,
        Func<T, string>? describe = null)
    {
        using var cancellationTokenSource = new CancellationTokenSource(timeout);
        var lastDescription = "no observation was produced";
        while (!cancellationTokenSource.IsCancellationRequested)
        {
            var current = await producer().ConfigureAwait(false);
            lastDescription = describe?.Invoke(current) ??
                (current is null ? "<null>" : current.ToString() ?? "<null>");
            if (predicate(current))
            {
                return current;
            }

            try
            {
                await Task.Delay(200, cancellationTokenSource.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationTokenSource.IsCancellationRequested)
            {
                break;
            }
        }

        throw new TimeoutException(
            $"Timed out while waiting for {conditionName}. Last observed: {lastDescription}");
    }

    private static bool HasObservedInsertedChange(CdcCaptureRuntimeState? state)
    {
        return state is not null &&
            IsCapturedOrIdle(state.LastOutcome) &&
            state.CapturedCount > 0 &&
            state.TotalCapturedChangeCount == 1 &&
            state.TotalProducedMessageCount == 1;
    }

    private static bool HasObservedInsertedChange(CdcCaptureExecutionRuntimeDescriptor? runtime)
    {
        return runtime is not null &&
            IsCapturedOrIdle(runtime.Summary.LastOutcome) &&
            runtime.Summary.CapturedCount > 0 &&
            runtime.Summary.TotalCapturedChangeCount == 1 &&
            runtime.Summary.TotalProducedMessageCount == 1;
    }

    private static bool IsCapturedOrIdle(string? outcome)
    {
        return string.Equals(outcome, CdcCaptureRuntimeOutcomes.Captured, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(outcome, CdcCaptureRuntimeOutcomes.Idle, StringComparison.OrdinalIgnoreCase);
    }

    private static string DescribeCaptureState(CdcCaptureRuntimeState? state)
    {
        return state is null
            ? "<null>"
            : $"outcome={state.LastOutcome ?? "<null>"}, started={state.StartedCount}, captured={state.CapturedCount}, idle={state.IdleCount}, totalCaptured={state.TotalCapturedChangeCount}, totalProduced={state.TotalProducedMessageCount}, error={state.LastError ?? "<none>"}";
    }

    private static string DescribeExecutionRuntime(CdcCaptureExecutionRuntimeDescriptor? runtime)
    {
        return runtime is null
            ? "<null>"
            : $"outcome={runtime.Summary.LastOutcome ?? "<null>"}, started={runtime.Summary.StartedCount}, captured={runtime.Summary.CapturedCount}, idle={runtime.Summary.IdleCount}, totalCaptured={runtime.Summary.TotalCapturedChangeCount}, totalProduced={runtime.Summary.TotalProducedMessageCount}, error={runtime.Summary.LastError ?? "<none>"}";
    }

    private static string DescribeSnapshot(RuntimeIntrospectionSnapshot? snapshot)
    {
        if (snapshot is null)
        {
            return "<null>";
        }

        var state = snapshot.CdcCaptureStates.FirstOrDefault(item => item.CdcCaptureId == CaptureId);
        var runtime = snapshot.CdcCaptureExecutionRuntimes.FirstOrDefault(item => item.Id == MongoRuntimeId);
        return $"state=({DescribeCaptureState(state)}), runtime=({DescribeExecutionRuntime(runtime)})";
    }
}
