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

[Collection(ProviderNativeCdcHostingCollectionDefinition.Name)]
public sealed class OracleDataCdcHostingTests
{
    private const string SharedRuntimeId = "data-cdc-capture-pump";
    private const string OracleRuntimeId = "oracle-logminer-capture-pump";
    private const string CaptureId = "oracle-orders-cdc";
    private const decimal ExpectedDatabaseId = 147258369m;
    private const string ExpectedDatabaseUniqueName = "CEPHALON_XEPDB1";
    private static readonly TimeSpan ProviderNativeCdcTimeout = TimeSpan.FromSeconds(90);

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
        batch.Metadata["databaseId"] = ExpectedDatabaseId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        batch.Metadata["databaseUniqueName"] = ExpectedDatabaseUniqueName;
        batch.Metadata["databaseRole"] = "PRIMARY";
        batch.Metadata["databaseOpenMode"] = "READ WRITE";
        batch.Metadata["archiveLogMode"] = "ARCHIVELOG";
        batch.Metadata["resetLogsChangeNumber"] = "500";
        batch.Metadata["supplementalLogDataMin"] = "YES";
        batch.Metadata["databaseIdentityState"] = "expected-match";
        batch.Metadata["databaseIdentityAction"] = "accept";
        batch.Metadata["archiveLogLifecycleState"] = "available";
        batch.Metadata["archiveLogLifecycleAction"] = "start";
        batch.Metadata["checkpointStore"] = "CEPHALON_CDC_CHECKPOINTS";
        batch.Metadata["checkpointSource"] = "cephalon-checkpoint-table";
        batch.Metadata["logMinerDictionary"] = "online-catalog";
        batch.Metadata["logMinerMode"] = "committed-only";
        batch.Metadata["redoCursor"] = "commit-scn|change-scn|rs-id|ssn";
        batch.Metadata["logFileCount"] = "2";
        batch.Metadata["resumeFromEarliestAvailableScnIfCheckpointUnavailable"] = "false";
        batch.Metadata["archiveLogLifecyclePolicy"] = "fail-when-checkpoint-unavailable";
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
                        ExpectedDatabaseId = ExpectedDatabaseId,
                        ExpectedDatabaseUniqueName = ExpectedDatabaseUniqueName,
                        PollingIntervalSeconds = 600,
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
                static state => HasObservedInsertedChange(state),
                ProviderNativeCdcTimeout);

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
            Assert.True(IsCapturedOrIdle(oracleRuntime.Summary.LastOutcome));
            Assert.True(oracleRuntime.Summary.CapturedCount > 0);
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
            Assert.True(IsCapturedOrIdle(captureState.LastOutcome));
            Assert.True(captureState.CapturedCount > 0);
            Assert.Equal(1, captureState.TotalCapturedChangeCount);
            Assert.Equal(1, captureState.TotalProducedMessageCount);
            Assert.Equal(CdcCapturePublicationStates.PendingPublication, captureState.Publication.State);

            Assert.NotNull(cdcState);
            Assert.Equal(OracleRuntimeId, cdcState.ExecutionBinding.EffectiveExecutionRuntimeId);
            Assert.True(IsCapturedOrIdle(cdcState.LastOutcome));
            Assert.True(cdcState.CapturedCount > 0);
            Assert.Equal(1, cdcState.TotalCapturedChangeCount);
            Assert.Equal(1, cdcState.TotalProducedMessageCount);
            Assert.Equal("oracle-provider-native-runtime", cdcState.Metadata["captureExecution"]);
            Assert.Equal(OracleRuntimeId, cdcState.Metadata["cdcCaptureExecutionRuntimeId"]);
            Assert.Equal("provider-native", cdcState.Metadata["acknowledgement"]);
            Assert.Equal("1000", cdcState.Metadata["startScn"]);
            Assert.Equal("1100", cdcState.Metadata["endScn"]);
            Assert.Equal("1100", cdcState.Metadata["currentScn"]);
            Assert.Equal("900", cdcState.Metadata["earliestAvailableScn"]);
            Assert.Equal("earliest-available", cdcState.Metadata["resumeMode"]);
            Assert.Equal(ExpectedDatabaseId.ToString(System.Globalization.CultureInfo.InvariantCulture), cdcState.Metadata["databaseId"]);
            Assert.Equal(ExpectedDatabaseUniqueName, cdcState.Metadata["databaseUniqueName"]);
            Assert.Equal("PRIMARY", cdcState.Metadata["databaseRole"]);
            Assert.Equal("READ WRITE", cdcState.Metadata["databaseOpenMode"]);
            Assert.Equal("ARCHIVELOG", cdcState.Metadata["archiveLogMode"]);
            Assert.Equal("500", cdcState.Metadata["resetLogsChangeNumber"]);
            Assert.Equal("YES", cdcState.Metadata["supplementalLogDataMin"]);
            Assert.Equal("expected-match", cdcState.Metadata["databaseIdentityState"]);
            Assert.Equal("accept", cdcState.Metadata["databaseIdentityAction"]);
            Assert.Equal("available", cdcState.Metadata["archiveLogLifecycleState"]);
            Assert.Equal("start", cdcState.Metadata["archiveLogLifecycleAction"]);
            Assert.Equal("CEPHALON_CDC_CHECKPOINTS", cdcState.Metadata["checkpointStore"]);
            Assert.Equal("cephalon-checkpoint-table", cdcState.Metadata["checkpointSource"]);
            Assert.Equal("online-catalog", cdcState.Metadata["logMinerDictionary"]);
            Assert.Equal("committed-only", cdcState.Metadata["logMinerMode"]);
            Assert.Equal("commit-scn|change-scn|rs-id|ssn", cdcState.Metadata["redoCursor"]);
            Assert.Equal("false", cdcState.Metadata["resumeFromEarliestAvailableScnIfCheckpointUnavailable"]);
            Assert.Equal("fail-when-checkpoint-unavailable", cdcState.Metadata["archiveLogLifecyclePolicy"]);

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
                HasObservedInsertedChange(item) &&
                item.Publication.State == CdcCapturePublicationStates.PendingPublication &&
                item.Metadata["logMinerMode"] == "committed-only");
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == OracleRuntimeId &&
                HasObservedInsertedChange(item));
        }
        finally
        {
            await app.StopAsync();
        }
    }

    [Fact]
    public async Task MapCephalonExposesOracleLifecycleFailureMetadata()
    {
        var executionState = new TestCdcExecutionState();
        var harness = new OracleDataCdcTestHarness();
        var batch = new OracleLogMinerTestBatch
        {
            FailureKind = "checkpoint-scn-unavailable",
            FailureMessage = "Oracle LogMiner capture 'oracle-orders-cdc' cannot resume checkpoint '1200|1190|0x009|4' because checkpoint SCN '1190' is older than the earliest retained archive-log SCN '1300'. Set ResumeFromEarliestAvailableScnIfCheckpointUnavailable to true to reseed from the earliest retained SCN."
        };
        batch.Metadata["currentScn"] = "1400";
        batch.Metadata["earliestAvailableScn"] = "1300";
        batch.Metadata["resumeMode"] = "checkpoint";
        batch.Metadata["resumeCheckpoint"] = "1200|1190|0x009|4";
        batch.Metadata["checkpointUpdatedAtUtc"] = "2026-04-22T08:30:00.0000000+00:00";
        batch.Metadata["databaseId"] = ExpectedDatabaseId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        batch.Metadata["databaseUniqueName"] = ExpectedDatabaseUniqueName;
        batch.Metadata["databaseRole"] = "PRIMARY";
        batch.Metadata["databaseOpenMode"] = "READ WRITE";
        batch.Metadata["archiveLogMode"] = "ARCHIVELOG";
        batch.Metadata["resetLogsChangeNumber"] = "500";
        batch.Metadata["supplementalLogDataMin"] = "YES";
        batch.Metadata["checkpointDatabaseId"] = ExpectedDatabaseId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        batch.Metadata["checkpointDatabaseUniqueName"] = ExpectedDatabaseUniqueName;
        batch.Metadata["checkpointResetLogsChangeNumber"] = "500";
        batch.Metadata["databaseIdentityState"] = "checkpoint-match";
        batch.Metadata["databaseIdentityAction"] = "resume";
        batch.Metadata["archiveLogLifecycleState"] = "checkpoint-pruned";
        batch.Metadata["archiveLogLifecycleAction"] = "fail";
        batch.Metadata["checkpointStore"] = "CEPHALON_CDC_CHECKPOINTS";
        batch.Metadata["checkpointSource"] = "cephalon-checkpoint-table";
        batch.Metadata["logMinerDictionary"] = "online-catalog";
        batch.Metadata["logMinerMode"] = "committed-only";
        batch.Metadata["redoCursor"] = "commit-scn|change-scn|rs-id|ssn";
        batch.Metadata["resumeFromEarliestAvailableScnIfCheckpointUnavailable"] = "false";
        batch.Metadata["archiveLogLifecyclePolicy"] = "fail-when-checkpoint-unavailable";
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
                        SourceModuleId = "platform",
                        TableSchema = "SALES",
                        TableName = "ORDERS",
                        OutboxId = "tenant-event-outbox",
                        ChannelId = "orders",
                        MessageType = "orders.oracle.changed",
                        InitialPosition = "latest-available",
                        ExpectedDatabaseId = ExpectedDatabaseId,
                        ExpectedDatabaseUniqueName = ExpectedDatabaseUniqueName,
                        PollingIntervalSeconds = 600,
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
            var client = app.GetTestClient();
            var cdcState = await WaitForAsync(
                () => client.GetFromJsonAsync<CdcCaptureRuntimeState>($"/engine/cdc-captures/runtime/{CaptureId}")!,
                static state => state is not null &&
                    HasObservedFailure(state) &&
                    state.Metadata.ContainsKey("failureKind") &&
                    state.Metadata.ContainsKey("archiveLogLifecycleState") &&
                    state.Metadata.ContainsKey("archiveLogLifecycleAction"),
                ProviderNativeCdcTimeout);

            var oracleRuntime = await WaitForAsync(
                () => client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor>($"/engine/cdc-capture-runtimes/{OracleRuntimeId}")!,
                static runtime => HasObservedFailure(runtime),
                ProviderNativeCdcTimeout);
            var snapshot = await WaitForAsync(
                () => client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot")!,
                static current => current is not null &&
                    current.CdcCaptureStates.Any(item =>
                        item.CdcCaptureId == CaptureId &&
                        HasObservedFailure(item)),
                ProviderNativeCdcTimeout);

            Assert.NotNull(cdcState);
            Assert.Equal(OracleRuntimeId, cdcState.ExecutionBinding.EffectiveExecutionRuntimeId);
            Assert.True(IsFailedOrIdle(cdcState.LastOutcome));
            Assert.True(cdcState.FailedCount > 0);
            Assert.Equal(CdcCapturePublicationStates.CaptureFailed, cdcState.Publication.State);
            Assert.Equal("checkpoint-scn-unavailable", cdcState.Metadata["failureKind"]);
            Assert.Contains("1200|1190|0x009|4", cdcState.LastError);
            Assert.Equal("checkpoint", cdcState.Metadata["resumeMode"]);
            Assert.Equal("1200|1190|0x009|4", cdcState.Metadata["resumeCheckpoint"]);
            Assert.Equal(ExpectedDatabaseId.ToString(System.Globalization.CultureInfo.InvariantCulture), cdcState.Metadata["databaseId"]);
            Assert.Equal(ExpectedDatabaseUniqueName, cdcState.Metadata["databaseUniqueName"]);
            Assert.Equal("checkpoint-match", cdcState.Metadata["databaseIdentityState"]);
            Assert.Equal("resume", cdcState.Metadata["databaseIdentityAction"]);
            Assert.Equal("ARCHIVELOG", cdcState.Metadata["archiveLogMode"]);
            Assert.Equal("checkpoint-pruned", cdcState.Metadata["archiveLogLifecycleState"]);
            Assert.Equal("fail", cdcState.Metadata["archiveLogLifecycleAction"]);
            Assert.Equal("1400", cdcState.Metadata["currentScn"]);
            Assert.Equal("1300", cdcState.Metadata["earliestAvailableScn"]);

            Assert.NotNull(oracleRuntime);
            Assert.True(oracleRuntime.Summary.HasReports);
            Assert.Equal(CaptureId, oracleRuntime.Summary.LastCdcCaptureId);
            Assert.True(IsFailedOrIdle(oracleRuntime.Summary.LastOutcome));
            Assert.True(oracleRuntime.Summary.FailedCount > 0);

            Assert.NotNull(snapshot);
            Assert.Contains(snapshot.CdcCaptureStates, item => item.CdcCaptureId == CaptureId &&
                item.Publication.State == CdcCapturePublicationStates.CaptureFailed);
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

            try
            {
                await Task.Delay(200, cancellationTokenSource.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationTokenSource.IsCancellationRequested)
            {
                break;
            }
        }

        throw new TimeoutException("Timed out while waiting for the expected Oracle CDC hosting condition.");
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

    private static bool HasObservedFailure(CdcCaptureRuntimeState? state)
    {
        return state is not null &&
            IsFailedOrIdle(state.LastOutcome) &&
            state.FailedCount > 0 &&
            state.Publication.State == CdcCapturePublicationStates.CaptureFailed;
    }

    private static bool HasObservedFailure(CdcCaptureExecutionRuntimeDescriptor? runtime)
    {
        return runtime is not null &&
            IsFailedOrIdle(runtime.Summary.LastOutcome) &&
            runtime.Summary.FailedCount > 0;
    }

    private static bool IsCapturedOrIdle(string? outcome)
    {
        return string.Equals(outcome, CdcCaptureRuntimeOutcomes.Captured, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(outcome, CdcCaptureRuntimeOutcomes.Idle, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsFailedOrIdle(string? outcome)
    {
        return string.Equals(outcome, CdcCaptureRuntimeOutcomes.Failed, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(outcome, CdcCaptureRuntimeOutcomes.Idle, StringComparison.OrdinalIgnoreCase);
    }
}
