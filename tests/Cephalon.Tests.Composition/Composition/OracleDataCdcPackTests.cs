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
    private const decimal ExpectedDatabaseId = 147258369m;
    private const string ExpectedDatabaseUniqueName = "CEPHALON_XEPDB1";

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
                        ExpectedDatabaseId = ExpectedDatabaseId,
                        ExpectedDatabaseUniqueName = ExpectedDatabaseUniqueName,
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
                static current => HasObservedInsertedChange(current),
                TimeSpan.FromSeconds(10));

            Assert.NotNull(state);
            Assert.Equal(OracleRuntimeId, state.ExecutionBinding.EffectiveExecutionRuntimeId);
            Assert.True(IsCapturedOrIdle(state.LastOutcome));
            Assert.True(state.CapturedCount > 0);
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
            Assert.Equal(ExpectedDatabaseId.ToString(System.Globalization.CultureInfo.InvariantCulture), state.Metadata["databaseId"]);
            Assert.Equal(ExpectedDatabaseUniqueName, state.Metadata["databaseUniqueName"]);
            Assert.Equal("PRIMARY", state.Metadata["databaseRole"]);
            Assert.Equal("READ WRITE", state.Metadata["databaseOpenMode"]);
            Assert.Equal("ARCHIVELOG", state.Metadata["archiveLogMode"]);
            Assert.Equal("500", state.Metadata["resetLogsChangeNumber"]);
            Assert.Equal("YES", state.Metadata["supplementalLogDataMin"]);
            Assert.Equal("expected-match", state.Metadata["databaseIdentityState"]);
            Assert.Equal("accept", state.Metadata["databaseIdentityAction"]);
            Assert.Equal("available", state.Metadata["archiveLogLifecycleState"]);
            Assert.Equal("start", state.Metadata["archiveLogLifecycleAction"]);
            Assert.Equal("CEPHALON_CDC_CHECKPOINTS", state.Metadata["checkpointStore"]);
            Assert.Equal("cephalon-checkpoint-table", state.Metadata["checkpointSource"]);
            Assert.Equal("online-catalog", state.Metadata["logMinerDictionary"]);
            Assert.Equal("committed-only", state.Metadata["logMinerMode"]);
            Assert.Equal("commit-scn|change-scn|rs-id|ssn", state.Metadata["redoCursor"]);
            Assert.Equal("false", state.Metadata["resumeFromEarliestAvailableScnIfCheckpointUnavailable"]);
            Assert.Equal("fail-when-checkpoint-unavailable", state.Metadata["archiveLogLifecyclePolicy"]);
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
            Assert.Equal(ExpectedDatabaseId.ToString(System.Globalization.CultureInfo.InvariantCulture), capture.Metadata["expectedDatabaseId"]);
            Assert.Equal(ExpectedDatabaseUniqueName, capture.Metadata["expectedDatabaseUniqueName"]);
            Assert.Equal("false", capture.Metadata["resumeFromEarliestAvailableScnIfCheckpointUnavailable"]);
            Assert.Equal("fail-when-checkpoint-unavailable", capture.Metadata["archiveLogLifecyclePolicy"]);

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
            Assert.True(IsCapturedOrIdle(oracleRuntime.Summary.LastOutcome));
            Assert.True(oracleRuntime.Summary.CapturedCount > 0);
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

    [Fact]
    public async Task AddOracleData_OracleLifecycleFailureSurfacesArchiveLogAndIdentityMetadata()
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
                        SourceModuleId = "platform",
                        TableSchema = "SALES",
                        TableName = "ORDERS",
                        OutboxId = "tenant-event-outbox",
                        ChannelId = "orders",
                        MessageType = "orders.oracle.changed",
                        InitialPosition = "latest-available",
                        ExpectedDatabaseId = ExpectedDatabaseId,
                        ExpectedDatabaseUniqueName = ExpectedDatabaseUniqueName,
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
            var stateCatalog = provider.GetRequiredService<ICdcCaptureRuntimeStateCatalog>();
            var runtimeCatalog = provider.GetRequiredService<ICdcCaptureExecutionRuntimeCatalog>();

            var state = await WaitForAsync(
                () => Task.FromResult(stateCatalog.GetById(CaptureId)),
                static current => current is not null && current.LastOutcome == CdcCaptureRuntimeOutcomes.Failed,
                TimeSpan.FromSeconds(10));

            Assert.NotNull(state);
            Assert.Equal(OracleRuntimeId, state.ExecutionBinding.EffectiveExecutionRuntimeId);
            Assert.Equal(CdcCaptureRuntimeOutcomes.Failed, state.LastOutcome);
            Assert.Equal("checkpoint-scn-unavailable", state.Metadata["failureKind"]);
            Assert.Contains("1200|1190|0x009|4", state.LastError);
            Assert.Equal("checkpoint", state.Metadata["resumeMode"]);
            Assert.Equal("1200|1190|0x009|4", state.Metadata["resumeCheckpoint"]);
            Assert.Equal(ExpectedDatabaseId.ToString(System.Globalization.CultureInfo.InvariantCulture), state.Metadata["databaseId"]);
            Assert.Equal(ExpectedDatabaseUniqueName, state.Metadata["databaseUniqueName"]);
            Assert.Equal(ExpectedDatabaseUniqueName, state.Metadata["checkpointDatabaseUniqueName"]);
            Assert.Equal("checkpoint-match", state.Metadata["databaseIdentityState"]);
            Assert.Equal("resume", state.Metadata["databaseIdentityAction"]);
            Assert.Equal("ARCHIVELOG", state.Metadata["archiveLogMode"]);
            Assert.Equal("checkpoint-pruned", state.Metadata["archiveLogLifecycleState"]);
            Assert.Equal("fail", state.Metadata["archiveLogLifecycleAction"]);
            Assert.Equal("1400", state.Metadata["currentScn"]);
            Assert.Equal("1300", state.Metadata["earliestAvailableScn"]);
            Assert.Equal("false", state.Metadata["resumeFromEarliestAvailableScnIfCheckpointUnavailable"]);
            Assert.Equal("fail-when-checkpoint-unavailable", state.Metadata["archiveLogLifecyclePolicy"]);
            Assert.Empty(executionState.StagedMessages);
            Assert.Empty(harness.CommittedCheckpoints);

            var oracleRuntime = runtimeCatalog.GetById(OracleRuntimeId);
            Assert.NotNull(oracleRuntime);
            Assert.True(oracleRuntime.Summary.HasReports);
            Assert.Equal(CaptureId, oracleRuntime.Summary.LastCdcCaptureId);
            Assert.Equal(CdcCaptureRuntimeOutcomes.Failed, oracleRuntime.Summary.LastOutcome);
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
