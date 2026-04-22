using Cephalon.Abstractions.Data;
using Cephalon.Data.Oracle.Configuration;
using Cephalon.Data.Oracle.Registration;
using Cephalon.Data.Registration;
using Cephalon.Data.Services;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cephalon.Tests.Composition;

public sealed class OracleDataCdcPackTests
{
    private const string SharedRuntimeId = "data-cdc-capture-pump";
    private const string OracleRuntimeId = "oracle-logminer-capture-pump";
    private const string CaptureId = "oracle-orders-cdc";

    [Fact]
    public async Task AddOracleData_ProviderNativeCdcRuntimeStagesPublicationsAndCommitsCheckpoint()
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

        var services = new ServiceCollection();
        services.AddSingleton(executionState);
        services.AddOracleDataCdcTestHarness(harness);
        services.AddScoped<IOutbox, TestOutbox>();
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
            engine.AddOracleData(
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

        using var provider = services.BuildServiceProvider();
        var hostedServices = provider.GetServices<IHostedService>().ToArray();
        foreach (var hostedService in hostedServices)
        {
            await hostedService.StartAsync(CancellationToken.None);
        }

        try
        {
            await executionState.WaitForStagedMessageAsync();

            var stateCatalog = provider.GetRequiredService<ICdcCaptureRuntimeStateCatalog>();
            var captureCatalog = provider.GetRequiredService<ICdcCaptureCatalog>();
            var runtimeCatalog = provider.GetRequiredService<ICdcCaptureExecutionRuntimeCatalog>();

            var state = await WaitForAsync(
                () => Task.FromResult(stateCatalog.GetById(CaptureId)),
                static current => current is not null && current.LastOutcome == CdcCaptureRuntimeOutcomes.Captured,
                TimeSpan.FromSeconds(10));

            Assert.NotNull(state);
            Assert.Equal(OracleRuntimeId, state.ExecutionBinding.EffectiveExecutionRuntimeId);
            Assert.Equal(CdcCaptureRuntimeOutcomes.Captured, state.LastOutcome);
            Assert.Equal(1, state.LastCapturedChangeCount);
            Assert.Equal(1, state.LastProducedMessageCount);
            Assert.Equal(1, state.TotalCapturedChangeCount);
            Assert.Equal(1, state.TotalProducedMessageCount);
            Assert.Equal("1100|1095|0x001|1", state.LastChangeId);
            Assert.Equal("1100|1095|0x001|1", state.LastCheckpoint);
            Assert.Equal("oracle-provider-native-runtime", state.Metadata["captureExecution"]);
            Assert.Equal(OracleRuntimeId, state.Metadata["cdcCaptureExecutionRuntimeId"]);
            Assert.Equal("provider-native", state.Metadata["acknowledgement"]);
            Assert.Equal("INSERT", state.Metadata["lastOperationType"]);
            Assert.Equal("1", state.Metadata["lastOperationCode"]);
            Assert.Equal("1000", state.Metadata["startScn"]);
            Assert.Equal("1100", state.Metadata["endScn"]);
            Assert.Equal("1100", state.Metadata["currentScn"]);
            Assert.Equal("900", state.Metadata["earliestAvailableScn"]);
            Assert.Equal("earliest-available", state.Metadata["resumeMode"]);
            Assert.Equal("CEPHALON_CDC_CHECKPOINTS", state.Metadata["checkpointStore"]);
            Assert.Equal("cephalon-checkpoint-table", state.Metadata["checkpointSource"]);
            Assert.Equal("online-catalog", state.Metadata["logMinerDictionary"]);
            Assert.Equal("committed-only", state.Metadata["logMinerMode"]);
            Assert.Equal("commit-scn|change-scn|rs-id|ssn", state.Metadata["redoCursor"]);
            Assert.Equal(CdcCapturePublicationStates.PendingPublication, state.Publication.State);
            Assert.Equal(1, state.Publication.PendingPublicationCount);

            var stagedMessage = Assert.Single(executionState.StagedMessages);
            Assert.Equal("orders", stagedMessage.ChannelId);
            Assert.Equal("orders.oracle.changed", stagedMessage.MessageType);
            Assert.Equal("application/vnd.cephalon.oracle.logminer+json", stagedMessage.ContentType);
            Assert.Equal(OracleDataOptions.ProviderId, stagedMessage.Headers["provider"]);
            Assert.Equal(CaptureId, stagedMessage.Headers["cdcCaptureId"]);
            Assert.Equal("SALES", stagedMessage.Headers["schemaName"]);
            Assert.Equal("ORDERS", stagedMessage.Headers["tableName"]);
            Assert.Equal("INSERT", stagedMessage.Headers["operation"]);
            Assert.Equal("1", stagedMessage.Headers["operationCode"]);
            Assert.Equal("1100", stagedMessage.Headers["commitScn"]);
            Assert.Equal("1095", stagedMessage.Headers["changeScn"]);
            Assert.Equal("0x001", stagedMessage.Headers["recordSetId"]);
            Assert.Equal("1", stagedMessage.Headers["sqlSequenceNumber"]);

            Assert.Equal(["1100|1095|0x001|1"], harness.CommittedCheckpoints);

            var capture = captureCatalog.GetById(CaptureId);
            Assert.NotNull(capture);
            Assert.Equal("platform", capture.SourceModuleId);
            Assert.Equal(OracleRuntimeId, capture.ExecutionBinding.EffectiveExecutionRuntimeId);
            Assert.Equal("host-managed", capture.ExecutionBinding.ExecutionOwnership);
            Assert.Equal("provider-native", capture.ExecutionBinding.ExecutionTopology);
            Assert.Equal("requested-execution-runtime", capture.ExecutionBinding.ResolutionMode);
            Assert.Equal("oracle-data", capture.Metadata["contributorModuleId"]);
            Assert.Equal("SALES", capture.Metadata["tableSchema"]);
            Assert.Equal("ORDERS", capture.Metadata["tableName"]);
            Assert.Equal("committed-only", capture.Metadata["logMinerMode"]);
            Assert.Equal("online-catalog", capture.Metadata["logMinerDictionary"]);
            Assert.Equal("commit-scn|change-scn|rs-id|ssn", capture.Metadata["redoCursor"]);

            var sharedRuntime = runtimeCatalog.GetById(SharedRuntimeId);
            Assert.NotNull(sharedRuntime);
            Assert.Empty(sharedRuntime.CdcCaptureIds);

            var oracleRuntime = runtimeCatalog.GetById(OracleRuntimeId);
            Assert.NotNull(oracleRuntime);
            Assert.Equal("host-managed", oracleRuntime.ExecutionOwnership);
            Assert.Equal("provider-native", oracleRuntime.ExecutionTopology);
            Assert.Equal("provider-native", oracleRuntime.AcknowledgementMode);
            Assert.Equal([CaptureId], oracleRuntime.CdcCaptureIds);
            Assert.True(oracleRuntime.Summary.HasReports);
            Assert.Equal(CaptureId, oracleRuntime.Summary.LastCdcCaptureId);
            Assert.Equal(CdcCaptureRuntimeOutcomes.Captured, oracleRuntime.Summary.LastOutcome);
            Assert.Equal(1, oracleRuntime.Summary.TotalCapturedChangeCount);
            Assert.Equal(1, oracleRuntime.Summary.TotalProducedMessageCount);
            Assert.Equal("provider-native", oracleRuntime.Summary.LastAcknowledgement);
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

        throw new TimeoutException("Timed out while waiting for the expected Oracle CDC condition.");
    }
}
