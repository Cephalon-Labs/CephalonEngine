using Cephalon.Abstractions.Data;
using Cephalon.Data.Registration;
using Cephalon.Data.Services;
using Cephalon.Data.SqlServer.Configuration;
using Cephalon.Data.SqlServer.Registration;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cephalon.Tests.Composition;

public sealed class SqlServerDataCdcPackTests
{
    private const string SharedRuntimeId = "data-cdc-capture-pump";
    private const string SqlRuntimeId = "sqlserver-cdc-capture-pump";
    private const string CaptureId = "sql-orders-cdc";

    [Fact]
    public async Task AddSqlServerData_ProviderNativeCdcRuntimeStagesPublicationsAndCommitsCheckpoint()
    {
        var executionState = new TestCdcExecutionState();
        var harness = new SqlServerCdcTestHarness();
        var batch = new SqlServerCdcTestBatch();
        batch.Metadata["checkpointStore"] = "dbo.cephalon_cdc_checkpoints";
        batch.Changes.Add(new SqlServerCdcTestChange
        {
            StartLsn = "0x00000000000000000001",
            SequenceValue = "0x0000000000000000000A",
            Operation = 2,
            ChangeId = "lsn-0001",
            Payload = """{"orderId":"order-001","status":"created"}"""
        });
        harness.EnqueueBatch(batch);

        var services = new ServiceCollection();
        services.AddSingleton(executionState);
        services.AddSqlServerCdcTestHarness(harness);
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
            engine.AddSqlServerData(
                connectionString: "Server=(local);Database=cephalon;Integrated Security=true;TrustServerCertificate=true",
                databaseName: "cephalon",
                configure: options =>
                {
                    options.CdcCaptures.Add(new SqlServerCdcCaptureOptions
                    {
                        Id = CaptureId,
                        DisplayName = "SQL Orders CDC",
                        Description = "Captures SQL Server order changes through a provider-native CDC runner.",
                        SourceModuleId = "platform",
                        CaptureInstance = "dbo_orders",
                        TableSchema = "dbo",
                        TableName = "orders",
                        OutboxId = "tenant-event-outbox",
                        ChannelId = "orders",
                        MessageType = "orders.sql.changed",
                        InitialPosition = "earliest-available",
                        PollingIntervalSeconds = 1,
                        MaxChangesPerPoll = 64
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
            Assert.Equal(SqlRuntimeId, state.ExecutionBinding.EffectiveExecutionRuntimeId);
            Assert.True(IsCapturedOrIdle(state.LastOutcome));
            Assert.True(state.CapturedCount > 0);
            Assert.Equal(1, state.TotalCapturedChangeCount);
            Assert.Equal(1, state.TotalProducedMessageCount);
            Assert.Equal("lsn-0001", state.LastChangeId);
            Assert.Equal("0x00000000000000000001|0x0000000000000000000A|2", state.LastCheckpoint);
            Assert.Equal("sqlserver-provider-native-runtime", state.Metadata["captureExecution"]);
            Assert.Equal(SqlRuntimeId, state.Metadata["cdcCaptureExecutionRuntimeId"]);
            Assert.Equal("provider-native", state.Metadata["acknowledgement"]);
            Assert.Equal("insert", state.Metadata["lastOperationType"]);
            Assert.Equal("dbo.cephalon_cdc_checkpoints", state.Metadata["checkpointStore"]);
            Assert.Equal(CdcCapturePublicationStates.PendingPublication, state.Publication.State);
            Assert.Equal(1, state.Publication.PendingPublicationCount);

            var stagedMessage = Assert.Single(executionState.StagedMessages);
            Assert.Equal("orders", stagedMessage.ChannelId);
            Assert.Equal("orders.sql.changed", stagedMessage.MessageType);
            Assert.Equal("application/vnd.cephalon.sqlserver.cdc+json", stagedMessage.ContentType);
            Assert.Equal(SqlServerDataOptions.ProviderId, stagedMessage.Headers["provider"]);
            Assert.Equal(CaptureId, stagedMessage.Headers["cdcCaptureId"]);
            Assert.Equal("dbo", stagedMessage.Headers["schemaName"]);
            Assert.Equal("orders", stagedMessage.Headers["tableName"]);
            Assert.Equal("dbo_orders", stagedMessage.Headers["captureInstance"]);
            Assert.Equal("insert", stagedMessage.Headers["operation"]);

            Assert.Equal(["0x00000000000000000001|0x0000000000000000000A|2"], harness.CommittedCheckpoints);

            var capture = captureCatalog.GetById(CaptureId);
            Assert.NotNull(capture);
            Assert.Equal("platform", capture.SourceModuleId);
            Assert.Equal(SqlRuntimeId, capture.ExecutionBinding.EffectiveExecutionRuntimeId);
            Assert.Equal("host-managed", capture.ExecutionBinding.ExecutionOwnership);
            Assert.Equal("provider-native", capture.ExecutionBinding.ExecutionTopology);
            Assert.Equal("requested-execution-runtime", capture.ExecutionBinding.ResolutionMode);
            Assert.Equal("sqlserver-data", capture.Metadata["contributorModuleId"]);
            Assert.Equal("dbo_orders", capture.Metadata["captureInstance"]);

            var sharedRuntime = runtimeCatalog.GetById(SharedRuntimeId);
            Assert.NotNull(sharedRuntime);
            Assert.Empty(sharedRuntime.CdcCaptureIds);

            var sqlRuntime = runtimeCatalog.GetById(SqlRuntimeId);
            Assert.NotNull(sqlRuntime);
            Assert.Equal("host-managed", sqlRuntime.ExecutionOwnership);
            Assert.Equal("provider-native", sqlRuntime.ExecutionTopology);
            Assert.Equal("provider-native", sqlRuntime.AcknowledgementMode);
            Assert.Equal([CaptureId], sqlRuntime.CdcCaptureIds);
            Assert.True(sqlRuntime.Summary.HasReports);
            Assert.Equal(CaptureId, sqlRuntime.Summary.LastCdcCaptureId);
            Assert.True(IsCapturedOrIdle(sqlRuntime.Summary.LastOutcome));
            Assert.True(sqlRuntime.Summary.CapturedCount > 0);
            Assert.Equal(1, sqlRuntime.Summary.TotalCapturedChangeCount);
            Assert.Equal(1, sqlRuntime.Summary.TotalProducedMessageCount);
            Assert.Equal("provider-native", sqlRuntime.Summary.LastAcknowledgement);
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

        throw new TimeoutException("Timed out while waiting for the expected SQL Server CDC condition.");
    }
}
