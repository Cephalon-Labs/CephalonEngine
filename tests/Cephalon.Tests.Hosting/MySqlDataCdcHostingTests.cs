using System.Net.Http.Json;
using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Execution;
using Cephalon.AspNetCore.Hosting;
using Cephalon.Data.MySql.Configuration;
using Cephalon.Data.MySql.Registration;
using Cephalon.Data.Registration;
using Cephalon.Data.Services;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Runtime;
using Cephalon.Tests.Support;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Hosting;

public sealed class MySqlDataCdcHostingTests
{
    private const string SharedRuntimeId = "data-cdc-capture-pump";
    private const string MySqlRuntimeId = "mysql-binlog-capture-pump";
    private const string CaptureId = "mysql-orders-cdc";
    private const string SourceServerUuid = "6f9619ff-8b86-d011-b42d-00cf4fc964ff";

    [Fact]
    public async Task MapCephalonExposesMySqlProviderNativeLifecycleAndResumeSurfaces()
    {
        var executionState = new TestCdcExecutionState();
        var harness = new MySqlDataCdcTestHarness();
        var batch = new MySqlBinlogTestBatch();
        batch.Metadata["checkpointStore"] = "cephalon.cephalon_cdc_checkpoints";
        batch.Metadata["binlogCheckpointSource"] = "cephalon-checkpoint-table";
        batch.Metadata["binlogResumeMode"] = "earliest-available";
        batch.Metadata["binlogLifecycleState"] = "available";
        batch.Metadata["binlogLifecycleAction"] = "start";
        batch.Metadata["binlogFile"] = "mysql-bin.000001";
        batch.Metadata["binlogPosition"] = "1260";
        batch.Metadata["currentBinlogFile"] = "mysql-bin.000001";
        batch.Metadata["currentBinlogPosition"] = "1260";
        batch.Metadata["sourceServerUuid"] = SourceServerUuid;
        batch.Metadata["sourceServerId"] = "210101";
        batch.Metadata["sourceServerIdentityState"] = "expected-match";
        batch.Metadata["sourceServerIdentityAction"] = "accept";
        batch.Metadata["gtidMode"] = "ON";
        batch.Metadata["gtidExecutedSet"] = $"{SourceServerUuid}:1-24";
        batch.Metadata["binaryLoggingEnabled"] = "true";
        batch.Metadata["binlogFormat"] = "ROW";
        batch.Metadata["binlogRowImage"] = "FULL";
        batch.Changes.Add(new MySqlBinlogTestChange
        {
            BinlogFile = "mysql-bin.000001",
            BinlogPosition = 1260,
            ChangeId = "binlog-0001",
            OperationName = "insert",
            Payload = """{"orderId":"order-001","status":"created"}"""
        });
        harness.EnqueueBatch(batch);

        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton(executionState);
        builder.Services.AddMySqlDataCdcTestHarness(harness);
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
            cephalon.AddMySqlData(
                connectionString: "Server=localhost;User ID=root;Password=mysql;Database=cephalon",
                databaseName: "cephalon",
                configure: options =>
                {
                    options.CdcCaptures.Add(new MySqlBinlogCaptureOptions
                    {
                        Id = CaptureId,
                        DisplayName = "MySQL Orders CDC",
                        Description = "Captures MySQL order changes through a provider-native binlog runner.",
                        SourceModuleId = "platform",
                        TableSchema = "cephalon",
                        TableName = "orders",
                        ServerId = 700101,
                        OutboxId = "tenant-event-outbox",
                        ChannelId = "orders",
                        MessageType = "orders.mysql.changed",
                        InitialPosition = "earliest-available",
                        ExpectedSourceServerUuid = SourceServerUuid,
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
            var mySqlRuntime = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor>($"/engine/cdc-capture-runtimes/{MySqlRuntimeId}");
            var capturesByRuntime = await client.GetFromJsonAsync<CdcCaptureDescriptor[]>($"/engine/cdc-captures/execution-runtimes/{MySqlRuntimeId}");
            var captureStatesByRuntime = await client.GetFromJsonAsync<CdcCaptureRuntimeState[]>($"/engine/cdc-captures/runtime/execution-runtimes/{MySqlRuntimeId}");
            var hostedExecutions = await client.GetFromJsonAsync<HostedExecutionDescriptor[]>("/engine/hosted-executions");
            var executionGraphs = await client.GetFromJsonAsync<ExecutionGraphDescriptor[]>("/engine/execution-graphs");
            var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

            Assert.NotNull(cdcCaptureRuntimes);
            Assert.NotNull(mySqlRuntime);
            Assert.Contains(cdcCaptureRuntimes, runtime => runtime.Id == SharedRuntimeId);
            Assert.Contains(cdcCaptureRuntimes, runtime => runtime.Id == MySqlRuntimeId);

            var sharedRuntime = Assert.Single(cdcCaptureRuntimes, runtime => runtime.Id == SharedRuntimeId);
            Assert.Empty(sharedRuntime.CdcCaptureIds);

            Assert.Equal("host-managed", mySqlRuntime.ExecutionOwnership);
            Assert.Equal("provider-native", mySqlRuntime.ExecutionTopology);
            Assert.Equal("provider-native", mySqlRuntime.AcknowledgementMode);
            Assert.Equal([CaptureId], mySqlRuntime.CdcCaptureIds);
            Assert.True(mySqlRuntime.Summary.HasReports);
            Assert.Equal(CaptureId, mySqlRuntime.Summary.LastCdcCaptureId);
            Assert.True(
                mySqlRuntime.Summary.LastOutcome == CdcCaptureRuntimeOutcomes.Captured ||
                mySqlRuntime.Summary.LastOutcome == CdcCaptureRuntimeOutcomes.Idle);
            Assert.Equal(1, mySqlRuntime.Summary.TotalCapturedChangeCount);
            Assert.Equal(1, mySqlRuntime.Summary.TotalProducedMessageCount);

            var capture = Assert.Single(capturesByRuntime!);
            Assert.Equal(CaptureId, capture.Id);
            Assert.Equal("platform", capture.SourceModuleId);
            Assert.Equal(MySqlRuntimeId, capture.ExecutionBinding.EffectiveExecutionRuntimeId);
            Assert.Equal("provider-native", capture.ExecutionBinding.ExecutionTopology);
            Assert.Equal("mysql-data", capture.Metadata["contributorModuleId"]);
            Assert.Equal("checkpoint-validation", capture.Metadata["binlogLifecyclePolicy"]);
            Assert.Equal("configured-uuid-match", capture.Metadata["sourceServerIdentityMode"]);

            var captureState = Assert.Single(captureStatesByRuntime!);
            Assert.Equal(CaptureId, captureState.CdcCaptureId);
            Assert.Equal(MySqlRuntimeId, captureState.ExecutionBinding.EffectiveExecutionRuntimeId);
            Assert.True(
                captureState.LastOutcome == CdcCaptureRuntimeOutcomes.Captured ||
                captureState.LastOutcome == CdcCaptureRuntimeOutcomes.Idle);
            Assert.Equal(CdcCapturePublicationStates.PendingPublication, captureState.Publication.State);

            Assert.NotNull(cdcState);
            Assert.Equal(MySqlRuntimeId, cdcState.ExecutionBinding.EffectiveExecutionRuntimeId);
            Assert.Equal("mysql-provider-native-runtime", cdcState.Metadata["captureExecution"]);
            Assert.Equal(MySqlRuntimeId, cdcState.Metadata["cdcCaptureExecutionRuntimeId"]);
            Assert.Equal("provider-native", cdcState.Metadata["acknowledgement"]);
            Assert.Equal("cephalon.cephalon_cdc_checkpoints", cdcState.Metadata["checkpointStore"]);
            Assert.Equal("cephalon-checkpoint-table", cdcState.Metadata["binlogCheckpointSource"]);
            Assert.Equal("earliest-available", cdcState.Metadata["binlogResumeMode"]);
            Assert.Equal("available", cdcState.Metadata["binlogLifecycleState"]);
            Assert.Equal("start", cdcState.Metadata["binlogLifecycleAction"]);
            Assert.Equal("mysql-bin.000001", cdcState.Metadata["binlogFile"]);
            Assert.Equal("1260", cdcState.Metadata["binlogPosition"]);
            Assert.Equal(SourceServerUuid, cdcState.Metadata["sourceServerUuid"]);
            Assert.Equal("expected-match", cdcState.Metadata["sourceServerIdentityState"]);
            Assert.Equal("accept", cdcState.Metadata["sourceServerIdentityAction"]);
            Assert.Equal("ON", cdcState.Metadata["gtidMode"]);
            Assert.Equal("ROW", cdcState.Metadata["binlogFormat"]);
            Assert.Equal("FULL", cdcState.Metadata["binlogRowImage"]);

            var hostedExecution = Assert.Single(hostedExecutions!, item => item.Id == MySqlRuntimeId);
            Assert.Equal("mysql-data", hostedExecution.SourceModuleId);
            Assert.Equal("background-service", hostedExecution.Kind);

            var executionGraph = Assert.Single(executionGraphs!, item => item.Id == "mysql-binlog-capture-flow");
            Assert.Equal("mysql-data", executionGraph.SourceModuleId);
            Assert.Equal("resolve-mysql-cdc-captures", executionGraph.EntryNodeId);

            Assert.NotNull(snapshot);
            Assert.Contains(snapshot.CdcCaptures, item => item.Id == CaptureId &&
                item.ExecutionBinding.EffectiveExecutionRuntimeId == MySqlRuntimeId);
            Assert.Contains(snapshot.CdcCaptureStates, item => item.CdcCaptureId == CaptureId &&
                (item.LastOutcome == CdcCaptureRuntimeOutcomes.Captured || item.LastOutcome == CdcCaptureRuntimeOutcomes.Idle) &&
                item.Publication.State == CdcCapturePublicationStates.PendingPublication);
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == MySqlRuntimeId &&
                (item.Summary.LastOutcome == CdcCaptureRuntimeOutcomes.Captured || item.Summary.LastOutcome == CdcCaptureRuntimeOutcomes.Idle) &&
                item.Summary.TotalCapturedChangeCount == 1);
        }
        finally
        {
            await app.StopAsync();
        }
    }

    [Fact]
    public async Task MapCephalonExposesMySqlLifecycleFailureMetadata()
    {
        var executionState = new TestCdcExecutionState();
        var harness = new MySqlDataCdcTestHarness();
        var batch = new MySqlBinlogTestBatch
        {
            FailureKind = "checkpoint-binlog-unavailable",
            FailureMessage = "MySQL binlog capture 'mysql-orders-cdc' cannot resume checkpoint 'mysql-bin.000001|1260' because binlog file 'mysql-bin.000001' is no longer retained on the source server. Reset the checkpoint or restore the missing binlog file before starting the Cephalon MySQL CDC runner."
        };
        batch.Metadata["checkpointStore"] = "cephalon.cephalon_cdc_checkpoints";
        batch.Metadata["binlogCheckpointSource"] = "cephalon-checkpoint-table";
        batch.Metadata["binlogResumeMode"] = "checkpoint";
        batch.Metadata["binlogLifecycleState"] = "purged";
        batch.Metadata["binlogLifecycleAction"] = "fail";
        batch.Metadata["binlogFile"] = "mysql-bin.000001";
        batch.Metadata["binlogPosition"] = "1260";
        batch.Metadata["resumeCheckpoint"] = "mysql-bin.000001|1260";
        batch.Metadata["sourceServerUuid"] = SourceServerUuid;
        batch.Metadata["checkpointSourceServerUuid"] = SourceServerUuid;
        batch.Metadata["sourceServerIdentityState"] = "checkpoint-match";
        batch.Metadata["sourceServerIdentityAction"] = "resume";
        batch.Metadata["gtidMode"] = "ON";
        batch.Metadata["gtidExecutedSet"] = $"{SourceServerUuid}:1-24";
        batch.Metadata["binaryLoggingEnabled"] = "true";
        batch.Metadata["binlogFormat"] = "ROW";
        batch.Metadata["binlogRowImage"] = "FULL";
        batch.Metadata["earliestAvailableBinlogFile"] = "mysql-bin.000010";
        batch.Metadata["latestAvailableBinlogFile"] = "mysql-bin.000012";
        harness.EnqueueBatch(batch);

        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton(executionState);
        builder.Services.AddMySqlDataCdcTestHarness(harness);
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
            cephalon.AddMySqlData(
                connectionString: "Server=localhost;User ID=root;Password=mysql;Database=cephalon",
                databaseName: "cephalon",
                configure: options =>
                {
                    options.CdcCaptures.Add(new MySqlBinlogCaptureOptions
                    {
                        Id = CaptureId,
                        DisplayName = "MySQL Orders CDC",
                        SourceModuleId = "platform",
                        TableSchema = "cephalon",
                        TableName = "orders",
                        ServerId = 700101,
                        OutboxId = "tenant-event-outbox",
                        ChannelId = "orders",
                        MessageType = "orders.mysql.changed",
                        InitialPosition = "latest-available",
                        ExpectedSourceServerUuid = SourceServerUuid,
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
            var client = app.GetTestClient();
            var cdcState = await WaitForAsync(
                () => client.GetFromJsonAsync<CdcCaptureRuntimeState>($"/engine/cdc-captures/runtime/{CaptureId}")!,
                static state => state is not null && state.LastOutcome == CdcCaptureRuntimeOutcomes.Failed,
                TimeSpan.FromSeconds(10));

            var mySqlRuntime = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor>($"/engine/cdc-capture-runtimes/{MySqlRuntimeId}");
            var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

            Assert.NotNull(cdcState);
            Assert.Equal(MySqlRuntimeId, cdcState.ExecutionBinding.EffectiveExecutionRuntimeId);
            Assert.Equal(CdcCaptureRuntimeOutcomes.Failed, cdcState.LastOutcome);
            Assert.Equal("checkpoint-binlog-unavailable", cdcState.Metadata["failureKind"]);
            Assert.Equal("checkpoint", cdcState.Metadata["binlogResumeMode"]);
            Assert.Equal("purged", cdcState.Metadata["binlogLifecycleState"]);
            Assert.Equal("fail", cdcState.Metadata["binlogLifecycleAction"]);
            Assert.Equal("mysql-bin.000001|1260", cdcState.Metadata["resumeCheckpoint"]);
            Assert.Equal(SourceServerUuid, cdcState.Metadata["sourceServerUuid"]);
            Assert.Equal(SourceServerUuid, cdcState.Metadata["checkpointSourceServerUuid"]);
            Assert.Equal("checkpoint-match", cdcState.Metadata["sourceServerIdentityState"]);
            Assert.Equal("resume", cdcState.Metadata["sourceServerIdentityAction"]);
            Assert.Equal("ON", cdcState.Metadata["gtidMode"]);
            Assert.Equal("ROW", cdcState.Metadata["binlogFormat"]);
            Assert.Equal("mysql-bin.000010", cdcState.Metadata["earliestAvailableBinlogFile"]);
            Assert.Equal("mysql-bin.000012", cdcState.Metadata["latestAvailableBinlogFile"]);

            Assert.NotNull(mySqlRuntime);
            Assert.True(mySqlRuntime.Summary.HasReports);
            Assert.Equal(CaptureId, mySqlRuntime.Summary.LastCdcCaptureId);
            Assert.Equal(CdcCaptureRuntimeOutcomes.Failed, mySqlRuntime.Summary.LastOutcome);

            Assert.NotNull(snapshot);
            Assert.Contains(snapshot.CdcCaptureStates, item => item.CdcCaptureId == CaptureId &&
                item.Publication.State == CdcCapturePublicationStates.CaptureFailed &&
                item.Metadata["binlogLifecycleState"] == "purged");
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

        throw new TimeoutException("Timed out while waiting for the expected MySQL CDC hosting condition.");
    }
}
