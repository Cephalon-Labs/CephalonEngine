using System.Net.Http.Json;
using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Execution;
using Cephalon.AspNetCore.Hosting;
using Cephalon.Data.Oracle.Configuration;
using Cephalon.Data.Oracle.Registration;
using Cephalon.Data.Registration;
using Cephalon.Data.Services;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Runtime;
using Cephalon.Tests.Support;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Hosting;

public sealed class OracleDataCdcHostingTests
{
    private const string SharedRuntimeId = "data-cdc-capture-pump";
    private const string OracleRuntimeId = "oracle-logminer-capture-pump";
    private const string CaptureId = "oracle-orders-cdc";

    [Fact]
    public async Task MapCephalonExposesOracleProviderNativeLogMinerSurfaces()
    {
        var executionState = new TestCdcExecutionState();
        var harness = new OracleDataCdcTestHarness();
        var batch = new OracleLogMinerTestBatch();
        batch.Metadata["startScn"] = "1000";
        batch.Metadata["endScn"] = "1100";
        batch.Metadata["currentScn"] = "1100";
        batch.Metadata["earliestAvailableScn"] = "900";
        batch.Metadata["resumeMode"] = "earliest-available";
        batch.Metadata["checkpointStore"] = "CEPHALON_CDC_CHECKPOINTS";
        batch.Metadata["checkpointSource"] = "cephalon-checkpoint-table";
        batch.Metadata["logMinerDictionary"] = "online-catalog";
        batch.Metadata["logMinerMode"] = "committed-only";
        batch.Metadata["redoCursor"] = "commit-scn|change-scn|rs-id|ssn";
        batch.Metadata["logFileCount"] = "2";
        batch.Changes.Add(new OracleLogMinerTestChange
        {
            CommitScn = 1100m,
            ChangeScn = 1095m,
            RecordSetId = "0x001",
            SqlSequenceNumber = 1L,
            ChangeId = "1100|1095|0x001|1",
            OperationName = "INSERT",
            OperationCode = 1,
            Payload = """{"orderId":"order-001","status":"created"}"""
        });
        harness.EnqueueBatch(batch);

        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton(executionState);
        builder.Services.AddOracleDataCdcTestHarness(harness);
        builder.Services.AddScoped<IOutbox, TestOutbox>();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularVerticalSlice";
        builder.Configuration[$"{EngineSettings.SectionName}:Patterns:0"] = "CQRS";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
        builder.AddCephalon(cephalon =>
        {
            cephalon.AddModule(new PlatformTestModule());
            cephalon.AddModule(new PlatformEventingTestModule());
            cephalon.AddData(options =>
            {
                options.EnableCdcExecution = true;
                options.CdcPollingIntervalSeconds = 600;
            });
            cephalon.AddOracleData(
                connectionString: "User Id=cephalon;Password=oracle;Data Source=localhost/XEPDB1",
                databaseName: "XEPDB1",
                configure: options =>
                {
                    options.CdcCaptures.Add(new OracleLogMinerCaptureOptions
                    {
                        Id = CaptureId,
                        DisplayName = "Oracle Orders CDC",
                        Description = "Captures Oracle order changes through a provider-native LogMiner runner.",
                        SourceModuleId = "platform",
                        TableSchema = "SALES",
                        TableName = "ORDERS",
                        OutboxId = "tenant-event-outbox",
                        ChannelId = "orders",
                        MessageType = "orders.oracle.changed",
                        InitialPosition = "earliest-available",
                        PollingIntervalSeconds = 1,
                        MaxChangesPerRead = 64,
                        MaxAwaitTimeSeconds = 5
                    });
                });
        });

        await using var app = builder.Build();
        app.MapCephalon();
        await app.StartAsync();

        try
        {
            await executionState.WaitForStagedMessageAsync();

            var client = app.GetTestClient();
            var cdcState = await WaitForAsync(
                () => client.GetFromJsonAsync<CdcCaptureRuntimeState>($"/engine/cdc-captures/runtime/{CaptureId}")!,
                static state => state is not null && state.LastOutcome == CdcCaptureRuntimeOutcomes.Captured,
                TimeSpan.FromSeconds(10));

            var cdcCaptureRuntimes = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes");
            var oracleRuntime = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor>($"/engine/cdc-capture-runtimes/{OracleRuntimeId}");
            var capturesByRuntime = await client.GetFromJsonAsync<CdcCaptureDescriptor[]>($"/engine/cdc-captures/execution-runtimes/{OracleRuntimeId}");
            var captureStatesByRuntime = await client.GetFromJsonAsync<CdcCaptureRuntimeState[]>($"/engine/cdc-captures/runtime/execution-runtimes/{OracleRuntimeId}");
            var hostedExecutions = await client.GetFromJsonAsync<HostedExecutionDescriptor[]>("/engine/hosted-executions");
            var executionGraphs = await client.GetFromJsonAsync<ExecutionGraphDescriptor[]>("/engine/execution-graphs");
            var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

            Assert.NotNull(cdcCaptureRuntimes);
            Assert.NotNull(oracleRuntime);
            Assert.Contains(cdcCaptureRuntimes, runtime => runtime.Id == SharedRuntimeId);
            Assert.Contains(cdcCaptureRuntimes, runtime => runtime.Id == OracleRuntimeId);

            var sharedRuntime = Assert.Single(cdcCaptureRuntimes, runtime => runtime.Id == SharedRuntimeId);
            Assert.Empty(sharedRuntime.CdcCaptureIds);

            Assert.Equal("host-managed", oracleRuntime.ExecutionOwnership);
            Assert.Equal("provider-native", oracleRuntime.ExecutionTopology);
            Assert.Equal("provider-native", oracleRuntime.AcknowledgementMode);
            Assert.Equal([CaptureId], oracleRuntime.CdcCaptureIds);
            Assert.True(oracleRuntime.Summary.HasReports);
            Assert.Equal(CaptureId, oracleRuntime.Summary.LastCdcCaptureId);
            Assert.Equal(CdcCaptureRuntimeOutcomes.Captured, oracleRuntime.Summary.LastOutcome);
            Assert.Equal(1, oracleRuntime.Summary.TotalCapturedChangeCount);
            Assert.Equal(1, oracleRuntime.Summary.TotalProducedMessageCount);

            var capture = Assert.Single(capturesByRuntime!);
            Assert.Equal(CaptureId, capture.Id);
            Assert.Equal("platform", capture.SourceModuleId);
            Assert.Equal(OracleRuntimeId, capture.ExecutionBinding.EffectiveExecutionRuntimeId);
            Assert.Equal("provider-native", capture.ExecutionBinding.ExecutionTopology);
            Assert.Equal("oracle-data", capture.Metadata["contributorModuleId"]);
            Assert.Equal("committed-only", capture.Metadata["logMinerMode"]);
            Assert.Equal("online-catalog", capture.Metadata["logMinerDictionary"]);

            var captureState = Assert.Single(captureStatesByRuntime!);
            Assert.Equal(CaptureId, captureState.CdcCaptureId);
            Assert.Equal(OracleRuntimeId, captureState.ExecutionBinding.EffectiveExecutionRuntimeId);
            Assert.Equal(CdcCaptureRuntimeOutcomes.Captured, captureState.LastOutcome);
            Assert.Equal(CdcCapturePublicationStates.PendingPublication, captureState.Publication.State);

            Assert.NotNull(cdcState);
            Assert.Equal(OracleRuntimeId, cdcState.ExecutionBinding.EffectiveExecutionRuntimeId);
            Assert.Equal("oracle-provider-native-runtime", cdcState.Metadata["captureExecution"]);
            Assert.Equal(OracleRuntimeId, cdcState.Metadata["cdcCaptureExecutionRuntimeId"]);
            Assert.Equal("provider-native", cdcState.Metadata["acknowledgement"]);
            Assert.Equal("1000", cdcState.Metadata["startScn"]);
            Assert.Equal("1100", cdcState.Metadata["endScn"]);
            Assert.Equal("1100", cdcState.Metadata["currentScn"]);
            Assert.Equal("900", cdcState.Metadata["earliestAvailableScn"]);
            Assert.Equal("earliest-available", cdcState.Metadata["resumeMode"]);
            Assert.Equal("CEPHALON_CDC_CHECKPOINTS", cdcState.Metadata["checkpointStore"]);
            Assert.Equal("cephalon-checkpoint-table", cdcState.Metadata["checkpointSource"]);
            Assert.Equal("online-catalog", cdcState.Metadata["logMinerDictionary"]);
            Assert.Equal("committed-only", cdcState.Metadata["logMinerMode"]);
            Assert.Equal("commit-scn|change-scn|rs-id|ssn", cdcState.Metadata["redoCursor"]);

            var hostedExecution = Assert.Single(hostedExecutions!, item => item.Id == OracleRuntimeId);
            Assert.Equal("oracle-data", hostedExecution.SourceModuleId);
            Assert.Equal("background-service", hostedExecution.Kind);

            var executionGraph = Assert.Single(executionGraphs!, item => item.Id == "oracle-logminer-capture-flow");
            Assert.Equal("oracle-data", executionGraph.SourceModuleId);
            Assert.Equal("resolve-oracle-cdc-captures", executionGraph.EntryNodeId);

            Assert.NotNull(snapshot);
            Assert.Contains(snapshot.CdcCaptures, item => item.Id == CaptureId &&
                item.ExecutionBinding.EffectiveExecutionRuntimeId == OracleRuntimeId);
            Assert.Contains(snapshot.CdcCaptureStates, item => item.CdcCaptureId == CaptureId &&
                item.LastOutcome == CdcCaptureRuntimeOutcomes.Captured &&
                item.Metadata["logMinerMode"] == "committed-only");
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == OracleRuntimeId &&
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

        throw new TimeoutException("Timed out while waiting for the expected Oracle CDC hosting condition.");
    }
}
