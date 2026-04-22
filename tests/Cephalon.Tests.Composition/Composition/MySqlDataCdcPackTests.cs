using Cephalon.Abstractions.Data;
using Cephalon.Data.MySql.Configuration;
using Cephalon.Data.MySql.Registration;
using Cephalon.Data.Registration;
using Cephalon.Data.Services;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cephalon.Tests.Composition;

public sealed class MySqlDataCdcPackTests
{
    private const string SharedRuntimeId = "data-cdc-capture-pump";
    private const string MySqlRuntimeId = "mysql-binlog-capture-pump";
    private const string CaptureId = "mysql-orders-cdc";

    [Fact]
    public async Task AddMySqlData_ProviderNativeCdcRuntimeStagesPublicationsAndCommitsCheckpoint()
    {
        var executionState = new TestCdcExecutionState();
        var harness = new MySqlDataCdcTestHarness();
        var batch = new MySqlBinlogTestBatch();
        batch.Metadata["checkpointStore"] = "cephalon.cephalon_cdc_checkpoints";
        batch.Metadata["binlogCheckpointSource"] = "cephalon-checkpoint-table";
        batch.Metadata["binlogResumeMode"] = "earliest-available";
        batch.Metadata["binlogFile"] = "mysql-bin.000001";
        batch.Metadata["binlogPosition"] = "1260";
        batch.Changes.Add(new MySqlBinlogTestChange
        {
            BinlogFile = "mysql-bin.000001",
            BinlogPosition = 1260,
            ChangeId = "binlog-0001",
            OperationName = "insert",
            Payload = """{"orderId":"order-001","status":"created"}"""
        });
        harness.EnqueueBatch(batch);

        var services = new ServiceCollection();
        services.AddSingleton(executionState);
        services.AddMySqlDataCdcTestHarness(harness);
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
            engine.AddMySqlData(
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
            Assert.Equal(MySqlRuntimeId, state.ExecutionBinding.EffectiveExecutionRuntimeId);
            Assert.Equal(CdcCaptureRuntimeOutcomes.Captured, state.LastOutcome);
            Assert.Equal(1, state.LastCapturedChangeCount);
            Assert.Equal(1, state.LastProducedMessageCount);
            Assert.Equal(1, state.TotalCapturedChangeCount);
            Assert.Equal(1, state.TotalProducedMessageCount);
            Assert.Equal("binlog-0001", state.LastChangeId);
            Assert.Equal("mysql-bin.000001|1260", state.LastCheckpoint);
            Assert.Equal("mysql-provider-native-runtime", state.Metadata["captureExecution"]);
            Assert.Equal(MySqlRuntimeId, state.Metadata["cdcCaptureExecutionRuntimeId"]);
            Assert.Equal("provider-native", state.Metadata["acknowledgement"]);
            Assert.Equal("insert", state.Metadata["lastOperationType"]);
            Assert.Equal("cephalon.cephalon_cdc_checkpoints", state.Metadata["checkpointStore"]);
            Assert.Equal("cephalon-checkpoint-table", state.Metadata["binlogCheckpointSource"]);
            Assert.Equal("earliest-available", state.Metadata["binlogResumeMode"]);
            Assert.Equal("mysql-bin.000001", state.Metadata["binlogFile"]);
            Assert.Equal("1260", state.Metadata["binlogPosition"]);
            Assert.Equal(CdcCapturePublicationStates.PendingPublication, state.Publication.State);
            Assert.Equal(1, state.Publication.PendingPublicationCount);

            var stagedMessage = Assert.Single(executionState.StagedMessages);
            Assert.Equal("orders", stagedMessage.ChannelId);
            Assert.Equal("orders.mysql.changed", stagedMessage.MessageType);
            Assert.Equal("application/vnd.cephalon.mysql.binlog+json", stagedMessage.ContentType);
            Assert.Equal(MySqlDataOptions.ProviderId, stagedMessage.Headers["provider"]);
            Assert.Equal(CaptureId, stagedMessage.Headers["cdcCaptureId"]);
            Assert.Equal("cephalon", stagedMessage.Headers["schemaName"]);
            Assert.Equal("orders", stagedMessage.Headers["tableName"]);
            Assert.Equal("700101", stagedMessage.Headers["serverId"]);
            Assert.Equal("insert", stagedMessage.Headers["operation"]);
            Assert.Equal("mysql-bin.000001", stagedMessage.Headers["binlogFile"]);

            Assert.Equal(["mysql-bin.000001|1260"], harness.CommittedCheckpoints);

            var capture = captureCatalog.GetById(CaptureId);
            Assert.NotNull(capture);
            Assert.Equal("platform", capture.SourceModuleId);
            Assert.Equal(MySqlRuntimeId, capture.ExecutionBinding.EffectiveExecutionRuntimeId);
            Assert.Equal("host-managed", capture.ExecutionBinding.ExecutionOwnership);
            Assert.Equal("provider-native", capture.ExecutionBinding.ExecutionTopology);
            Assert.Equal("requested-execution-runtime", capture.ExecutionBinding.ResolutionMode);
            Assert.Equal("mysql-data", capture.Metadata["contributorModuleId"]);
            Assert.Equal("cephalon", capture.Metadata["tableSchema"]);
            Assert.Equal("orders", capture.Metadata["tableName"]);

            var sharedRuntime = runtimeCatalog.GetById(SharedRuntimeId);
            Assert.NotNull(sharedRuntime);
            Assert.Empty(sharedRuntime.CdcCaptureIds);

            var mySqlRuntime = runtimeCatalog.GetById(MySqlRuntimeId);
            Assert.NotNull(mySqlRuntime);
            Assert.Equal("host-managed", mySqlRuntime.ExecutionOwnership);
            Assert.Equal("provider-native", mySqlRuntime.ExecutionTopology);
            Assert.Equal("provider-native", mySqlRuntime.AcknowledgementMode);
            Assert.Equal([CaptureId], mySqlRuntime.CdcCaptureIds);
            Assert.True(mySqlRuntime.Summary.HasReports);
            Assert.Equal(CaptureId, mySqlRuntime.Summary.LastCdcCaptureId);
            Assert.Equal(CdcCaptureRuntimeOutcomes.Captured, mySqlRuntime.Summary.LastOutcome);
            Assert.Equal(1, mySqlRuntime.Summary.TotalCapturedChangeCount);
            Assert.Equal(1, mySqlRuntime.Summary.TotalProducedMessageCount);
            Assert.Equal("provider-native", mySqlRuntime.Summary.LastAcknowledgement);
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

        throw new TimeoutException("Timed out while waiting for the expected MySQL CDC condition.");
    }
}
