using System.Net.Http.Json;
using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Execution;
using Cephalon.AspNetCore.Hosting;
using Cephalon.Data.Postgres.Configuration;
using Cephalon.Data.Postgres.Registration;
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
public sealed class PostgresDataCdcHostingTests
{
    private const string SharedRuntimeId = "data-cdc-capture-pump";
    private const string PostgresRuntimeId = "postgresql-logical-replication-capture-pump";
    private const string CaptureId = "pg-orders-cdc";
    private static readonly TimeSpan ProviderNativeCdcTimeout = TimeSpan.FromSeconds(90);

    [Fact]
    public async Task MapCephalonExposesPostgresProviderNativeCdcRuntimeSurfaces()
    {
        var executionState = new TestCdcExecutionState();
        var harness = new PostgresDataCdcTestHarness();
        var batch = new PostgresLogicalReplicationTestBatch();
        batch.Metadata["publicationName"] = "orders_publication";
        batch.Metadata["slotName"] = "orders_slot";
        batch.Metadata["replicationCheckpointSource"] = "slot-confirmed-flush-lsn";
        batch.Metadata["publicationState"] = "includes-table";
        batch.Metadata["slotLifecycleState"] = "ready";
        batch.Metadata["slotLifecycleAction"] = "reuse";
        batch.Metadata["slotResumeMode"] = "slot-confirmed-flush-lsn";
        batch.Metadata["slotRestartLsn"] = "0/16B6D80";
        batch.Metadata["slotConfirmedFlushLsn"] = "0/16B6E00";
        batch.Metadata["slotWalStatus"] = "reserved";
        batch.Changes.Add(new PostgresLogicalReplicationTestChange
        {
            CommitLsn = "0/16B6E00",
            TransactionEndLsn = "0/16B6E30",
            ChangeId = "lsn-0001",
            OperationName = "insert",
            Payload = """{"orderId":"order-001","status":"created"}"""
        });
        harness.EnqueueBatch(batch);

        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton(executionState);
        builder.Services.AddPostgresDataCdcTestHarness(harness);
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
            cephalon.AddPostgresData(
                connectionString: "Host=localhost;Username=postgres;Password=postgres;Database=cephalon",
                databaseName: "cephalon",
                configure: options =>
                {
                    options.CdcCaptures.Add(new PostgresLogicalReplicationCaptureOptions
                    {
                        Id = CaptureId,
                        DisplayName = "PostgreSQL Orders CDC",
                        Description = "Captures PostgreSQL order changes through a provider-native logical-replication runner.",
                        SourceModuleId = "platform",
                        PublicationName = "orders_publication",
                        SlotName = "orders_slot",
                        TableSchema = "public",
                        TableName = "orders",
                        OutboxId = "tenant-event-outbox",
                        ChannelId = "orders",
                        MessageType = "orders.postgresql.changed",
                        InitialPosition = "slot-consistent-point",
                        RecreateSlotIfInvalidated = true,
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
            var postgresRuntime = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor>($"/engine/cdc-capture-runtimes/{PostgresRuntimeId}");
            var capturesByRuntime = await client.GetFromJsonAsync<CdcCaptureDescriptor[]>($"/engine/cdc-captures/execution-runtimes/{PostgresRuntimeId}");
            var captureStatesByRuntime = await client.GetFromJsonAsync<CdcCaptureRuntimeState[]>($"/engine/cdc-captures/runtime/execution-runtimes/{PostgresRuntimeId}");
            var hostedExecutions = await client.GetFromJsonAsync<HostedExecutionDescriptor[]>("/engine/hosted-executions");
            var executionGraphs = await client.GetFromJsonAsync<ExecutionGraphDescriptor[]>("/engine/execution-graphs");
            var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

            Assert.NotNull(cdcCaptureRuntimes);
            Assert.NotNull(postgresRuntime);
            Assert.Contains(cdcCaptureRuntimes, runtime => runtime.Id == SharedRuntimeId);
            Assert.Contains(cdcCaptureRuntimes, runtime => runtime.Id == PostgresRuntimeId);

            var sharedRuntime = Assert.Single(cdcCaptureRuntimes, runtime => runtime.Id == SharedRuntimeId);
            Assert.Empty(sharedRuntime.CdcCaptureIds);

            Assert.Equal("host-managed", postgresRuntime.ExecutionOwnership);
            Assert.Equal("provider-native", postgresRuntime.ExecutionTopology);
            Assert.Equal("provider-native", postgresRuntime.AcknowledgementMode);
            Assert.Equal([CaptureId], postgresRuntime.CdcCaptureIds);
            Assert.True(postgresRuntime.Summary.HasReports);
            Assert.Equal(CaptureId, postgresRuntime.Summary.LastCdcCaptureId);
            Assert.True(IsCapturedOrIdle(postgresRuntime.Summary.LastOutcome));
            Assert.True(postgresRuntime.Summary.CapturedCount > 0);
            Assert.Equal(1, postgresRuntime.Summary.TotalCapturedChangeCount);
            Assert.Equal(1, postgresRuntime.Summary.TotalProducedMessageCount);

            var capture = Assert.Single(capturesByRuntime!);
            Assert.Equal(CaptureId, capture.Id);
            Assert.Equal("platform", capture.SourceModuleId);
            Assert.Equal(PostgresRuntimeId, capture.ExecutionBinding.EffectiveExecutionRuntimeId);
            Assert.Equal("provider-native", capture.ExecutionBinding.ExecutionTopology);
            Assert.Equal("postgres-data", capture.Metadata["contributorModuleId"]);

            var captureState = Assert.Single(captureStatesByRuntime!);
            Assert.Equal(CaptureId, captureState.CdcCaptureId);
            Assert.Equal(PostgresRuntimeId, captureState.ExecutionBinding.EffectiveExecutionRuntimeId);
            Assert.True(IsCapturedOrIdle(captureState.LastOutcome));
            Assert.True(captureState.CapturedCount > 0);
            Assert.Equal(1, captureState.TotalCapturedChangeCount);
            Assert.Equal(1, captureState.TotalProducedMessageCount);
            Assert.Equal(CdcCapturePublicationStates.PendingPublication, captureState.Publication.State);

            Assert.NotNull(cdcState);
            Assert.Equal(PostgresRuntimeId, cdcState.ExecutionBinding.EffectiveExecutionRuntimeId);
            Assert.True(IsCapturedOrIdle(cdcState.LastOutcome));
            Assert.True(cdcState.CapturedCount > 0);
            Assert.Equal(1, cdcState.TotalCapturedChangeCount);
            Assert.Equal(1, cdcState.TotalProducedMessageCount);
            Assert.Equal("postgresql-provider-native-runtime", cdcState.Metadata["captureExecution"]);
            Assert.Equal(PostgresRuntimeId, cdcState.Metadata["cdcCaptureExecutionRuntimeId"]);
            Assert.Equal("provider-native", cdcState.Metadata["acknowledgement"]);
            Assert.Equal("orders_publication", cdcState.Metadata["publicationName"]);
            Assert.Equal("orders_slot", cdcState.Metadata["slotName"]);
            Assert.Equal("slot-confirmed-flush-lsn", cdcState.Metadata["replicationCheckpointSource"]);
            Assert.Equal("ready", cdcState.Metadata["slotLifecycleState"]);
            Assert.Equal("reuse", cdcState.Metadata["slotLifecycleAction"]);
            Assert.Equal("slot-confirmed-flush-lsn", cdcState.Metadata["slotResumeMode"]);
            Assert.Equal("0/16B6D80", cdcState.Metadata["slotRestartLsn"]);
            Assert.Equal("0/16B6E00", cdcState.Metadata["slotConfirmedFlushLsn"]);
            Assert.Equal("reserved", cdcState.Metadata["slotWalStatus"]);

            var hostedExecution = Assert.Single(hostedExecutions!, item => item.Id == PostgresRuntimeId);
            Assert.Equal("postgres-data", hostedExecution.SourceModuleId);
            Assert.Equal("background-service", hostedExecution.Kind);

            var executionGraph = Assert.Single(executionGraphs!, item => item.Id == "postgresql-logical-replication-capture-flow");
            Assert.Equal("postgres-data", executionGraph.SourceModuleId);
            Assert.Equal("resolve-postgresql-cdc-captures", executionGraph.EntryNodeId);

            Assert.NotNull(snapshot);
            Assert.Contains(snapshot.CdcCaptures, item => item.Id == CaptureId &&
                item.ExecutionBinding.EffectiveExecutionRuntimeId == PostgresRuntimeId);
            Assert.Contains(snapshot.CdcCaptureStates, item => item.CdcCaptureId == CaptureId &&
                HasObservedInsertedChange(item) &&
                item.Publication.State == CdcCapturePublicationStates.PendingPublication);
            Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == PostgresRuntimeId &&
                HasObservedInsertedChange(item));
        }
        finally
        {
            await app.StopAsync();
        }
    }

    [Fact]
    public async Task MapCephalonExposesPostgresLifecycleFailureMetadata()
    {
        var executionState = new TestCdcExecutionState();
        var harness = new PostgresDataCdcTestHarness();
        var batch = new PostgresLogicalReplicationTestBatch
        {
            FailureKind = "slot-invalidated",
            FailureMessage = "PostgreSQL replication slot 'orders_slot' is no longer usable because invalidation reason 'wal_removed' WAL status 'lost'. Set RecreateSlotIfInvalidated to true or recreate the slot before starting the Cephalon PostgreSQL CDC runner."
        };
        batch.Metadata["publicationName"] = "orders_publication";
        batch.Metadata["slotName"] = "orders_slot";
        batch.Metadata["replicationCheckpointSource"] = "slot-confirmed-flush-lsn";
        batch.Metadata["publicationState"] = "includes-table";
        batch.Metadata["slotExists"] = "true";
        batch.Metadata["slotLifecycleState"] = "invalidated";
        batch.Metadata["slotLifecycleAction"] = "fail";
        batch.Metadata["slotResumeMode"] = "slot-confirmed-flush-lsn";
        batch.Metadata["slotRestartLsn"] = "0/16B6D80";
        batch.Metadata["slotConfirmedFlushLsn"] = "0/16B6E00";
        batch.Metadata["slotWalStatus"] = "lost";
        batch.Metadata["slotInvalidationReason"] = "wal_removed";
        harness.EnqueueBatch(batch);

        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton(executionState);
        builder.Services.AddPostgresDataCdcTestHarness(harness);
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
            cephalon.AddPostgresData(
                connectionString: "Host=localhost;Username=postgres;Password=postgres;Database=cephalon",
                databaseName: "cephalon",
                configure: options =>
                {
                    options.CdcCaptures.Add(new PostgresLogicalReplicationCaptureOptions
                    {
                        Id = CaptureId,
                        DisplayName = "PostgreSQL Orders CDC",
                        SourceModuleId = "platform",
                        PublicationName = "orders_publication",
                        SlotName = "orders_slot",
                        TableSchema = "public",
                        TableName = "orders",
                        OutboxId = "tenant-event-outbox",
                        ChannelId = "orders",
                        MessageType = "orders.postgresql.changed",
                        InitialPosition = "slot-consistent-point",
                        RecreateSlotIfInvalidated = false,
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
                    state.Metadata.ContainsKey("slotLifecycleState") &&
                    state.Metadata.ContainsKey("slotLifecycleAction"),
                ProviderNativeCdcTimeout);

            var postgresRuntime = await WaitForAsync(
                () => client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor>($"/engine/cdc-capture-runtimes/{PostgresRuntimeId}")!,
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
            Assert.True(IsFailedOrIdle(cdcState.LastOutcome));
            Assert.True(cdcState.FailedCount > 0);
            Assert.Equal(CdcCapturePublicationStates.CaptureFailed, cdcState.Publication.State);
            Assert.Equal("slot-invalidated", cdcState.Metadata["failureKind"]);
            Assert.Equal("invalidated", cdcState.Metadata["slotLifecycleState"]);
            Assert.Equal("fail", cdcState.Metadata["slotLifecycleAction"]);
            Assert.Equal("slot-confirmed-flush-lsn", cdcState.Metadata["slotResumeMode"]);
            Assert.Equal("0/16B6D80", cdcState.Metadata["slotRestartLsn"]);
            Assert.Equal("0/16B6E00", cdcState.Metadata["slotConfirmedFlushLsn"]);
            Assert.Equal("lost", cdcState.Metadata["slotWalStatus"]);
            Assert.Equal("wal_removed", cdcState.Metadata["slotInvalidationReason"]);
            Assert.Equal("false", cdcState.Metadata["recreateSlotIfInvalidated"]);
            Assert.Equal("fail-on-invalidated-slot", cdcState.Metadata["slotLifecyclePolicy"]);

            Assert.NotNull(postgresRuntime);
            Assert.True(postgresRuntime.Summary.HasReports);
            Assert.True(IsFailedOrIdle(postgresRuntime.Summary.LastOutcome));
            Assert.True(postgresRuntime.Summary.FailedCount > 0);
            Assert.Equal(CaptureId, postgresRuntime.Summary.LastCdcCaptureId);

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

        throw new TimeoutException("Timed out while waiting for the expected PostgreSQL CDC hosting condition.");
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
