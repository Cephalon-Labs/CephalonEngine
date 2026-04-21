using Cephalon.Abstractions.Data;
using Cephalon.Data.Postgres.Configuration;
using Cephalon.Data.Postgres.Registration;
using Cephalon.Data.Registration;
using Cephalon.Data.Services;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cephalon.Tests.Composition;

public sealed class PostgresDataCdcPackTests
{
    private const string SharedRuntimeId = "data-cdc-capture-pump";
    private const string PostgresRuntimeId = "postgresql-logical-replication-capture-pump";
    private const string CaptureId = "pg-orders-cdc";

    [Fact]
    public async Task AddPostgresData_ProviderNativeCdcRuntimeStagesPublicationsAndCommitsCheckpoint()
    {
        var executionState = new TestCdcExecutionState();
        var harness = new PostgresDataCdcTestHarness();
        var batch = new PostgresLogicalReplicationTestBatch();
        batch.Metadata["publicationName"] = "orders_publication";
        batch.Metadata["slotName"] = "orders_slot";
        batch.Metadata["replicationCheckpointSource"] = "slot-confirmed-flush-lsn";
        batch.Changes.Add(new PostgresLogicalReplicationTestChange
        {
            CommitLsn = "0/16B6E00",
            TransactionEndLsn = "0/16B6E30",
            ChangeId = "lsn-0001",
            OperationName = "insert",
            Payload = """{"orderId":"order-001","status":"created"}"""
        });
        harness.EnqueueBatch(batch);

        var services = new ServiceCollection();
        services.AddSingleton(executionState);
        services.AddPostgresDataCdcTestHarness(harness);
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
            engine.AddPostgresData(
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
            Assert.Equal(PostgresRuntimeId, state.ExecutionBinding.EffectiveExecutionRuntimeId);
            Assert.Equal(CdcCaptureRuntimeOutcomes.Captured, state.LastOutcome);
            Assert.Equal(1, state.LastCapturedChangeCount);
            Assert.Equal(1, state.LastProducedMessageCount);
            Assert.Equal(1, state.TotalCapturedChangeCount);
            Assert.Equal(1, state.TotalProducedMessageCount);
            Assert.Equal("lsn-0001", state.LastChangeId);
            Assert.Equal("orders_slot|0/16B6E00|0/16B6E30", state.LastCheckpoint);
            Assert.Equal("postgresql-provider-native-runtime", state.Metadata["captureExecution"]);
            Assert.Equal(PostgresRuntimeId, state.Metadata["cdcCaptureExecutionRuntimeId"]);
            Assert.Equal("provider-native", state.Metadata["acknowledgement"]);
            Assert.Equal("insert", state.Metadata["lastOperationType"]);
            Assert.Equal("orders_publication", state.Metadata["publicationName"]);
            Assert.Equal("orders_slot", state.Metadata["slotName"]);
            Assert.Equal("slot-confirmed-flush-lsn", state.Metadata["replicationCheckpointSource"]);
            Assert.Equal(CdcCapturePublicationStates.PendingPublication, state.Publication.State);
            Assert.Equal(1, state.Publication.PendingPublicationCount);

            var stagedMessage = Assert.Single(executionState.StagedMessages);
            Assert.Equal("orders", stagedMessage.ChannelId);
            Assert.Equal("orders.postgresql.changed", stagedMessage.MessageType);
            Assert.Equal("application/vnd.cephalon.postgresql.logical-replication+json", stagedMessage.ContentType);
            Assert.Equal(PostgresDataOptions.ProviderId, stagedMessage.Headers["provider"]);
            Assert.Equal(CaptureId, stagedMessage.Headers["cdcCaptureId"]);
            Assert.Equal("public", stagedMessage.Headers["schemaName"]);
            Assert.Equal("orders", stagedMessage.Headers["tableName"]);
            Assert.Equal("orders_publication", stagedMessage.Headers["publicationName"]);
            Assert.Equal("orders_slot", stagedMessage.Headers["slotName"]);
            Assert.Equal("insert", stagedMessage.Headers["operation"]);

            Assert.Equal(["orders_slot|0/16B6E00|0/16B6E30"], harness.CommittedCheckpoints);

            var capture = captureCatalog.GetById(CaptureId);
            Assert.NotNull(capture);
            Assert.Equal("platform", capture.SourceModuleId);
            Assert.Equal(PostgresRuntimeId, capture.ExecutionBinding.EffectiveExecutionRuntimeId);
            Assert.Equal("host-managed", capture.ExecutionBinding.ExecutionOwnership);
            Assert.Equal("provider-native", capture.ExecutionBinding.ExecutionTopology);
            Assert.Equal("requested-execution-runtime", capture.ExecutionBinding.ResolutionMode);
            Assert.Equal("postgres-data", capture.Metadata["contributorModuleId"]);
            Assert.Equal("orders_publication", capture.Metadata["publicationName"]);
            Assert.Equal("orders_slot", capture.Metadata["slotName"]);

            var sharedRuntime = runtimeCatalog.GetById(SharedRuntimeId);
            Assert.NotNull(sharedRuntime);
            Assert.Empty(sharedRuntime.CdcCaptureIds);

            var postgresRuntime = runtimeCatalog.GetById(PostgresRuntimeId);
            Assert.NotNull(postgresRuntime);
            Assert.Equal("host-managed", postgresRuntime.ExecutionOwnership);
            Assert.Equal("provider-native", postgresRuntime.ExecutionTopology);
            Assert.Equal("provider-native", postgresRuntime.AcknowledgementMode);
            Assert.Equal([CaptureId], postgresRuntime.CdcCaptureIds);
            Assert.True(postgresRuntime.Summary.HasReports);
            Assert.Equal(CaptureId, postgresRuntime.Summary.LastCdcCaptureId);
            Assert.Equal(CdcCaptureRuntimeOutcomes.Captured, postgresRuntime.Summary.LastOutcome);
            Assert.Equal(1, postgresRuntime.Summary.TotalCapturedChangeCount);
            Assert.Equal(1, postgresRuntime.Summary.TotalProducedMessageCount);
            Assert.Equal("provider-native", postgresRuntime.Summary.LastAcknowledgement);
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

        throw new TimeoutException("Timed out while waiting for the expected PostgreSQL CDC condition.");
    }
}
