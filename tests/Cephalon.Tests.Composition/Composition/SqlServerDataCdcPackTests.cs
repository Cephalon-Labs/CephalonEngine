using System.Collections.Concurrent;
using System.Threading.Channels;
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

    [Theory]
    [InlineData(1, "delete")]
    [InlineData(2, "insert")]
    [InlineData(3, "update-before")]
    [InlineData(4, "update-after")]
    public async Task AddSqlServerData_ProviderNativeCdcRuntimeStagesPublicationsAndCommitsCheckpoint(
        int operation, string operationName)
    {
        var executionState = new TestCdcExecutionState();
        var harness = new SqlServerCdcTestHarness();
        var batch = new SqlServerCdcTestBatch();
        batch.Metadata["checkpointStore"] = "dbo.cephalon_cdc_checkpoints";
        batch.Changes.Add(new SqlServerCdcTestChange
        {
            StartLsn = "0x00000000000000000001",
            SequenceValue = "0x0000000000000000000A",
            Operation = operation,
            ChangeId = "lsn-0001",
            Payload = """{"orderId":"order-001","status":"created"}"""
        });
        harness.EnqueueBatch(batch);

        using var provider = CreateProvider(harness, new TestOutbox(executionState));
        var observations = provider.GetRequiredService<CdcObservations>();
        var hostedServices = provider.GetServices<IHostedService>().ToArray();
        foreach (var hostedService in hostedServices)
        {
            await hostedService.StartAsync(CancellationToken.None);
        }

        try
        {
            var captureCatalog = provider.GetRequiredService<ICdcCaptureCatalog>();
            var runtimeCatalog = provider.GetRequiredService<ICdcCaptureExecutionRuntimeCatalog>();

            // Keep the accepted capture snapshot even when the consumer runs after idle.
            var captured = await observations.WaitForAsync(CdcCaptureRuntimeOutcomes.Captured);
            var idle = await observations.WaitForAsync(CdcCaptureRuntimeOutcomes.Idle);
            var state = captured.State;
            Assert.Equal(CdcCaptureRuntimeOutcomes.Idle, idle.State.LastOutcome);
            Assert.Equal(0, idle.State.LastCapturedChangeCount);
            Assert.Equal(0, idle.State.LastProducedMessageCount);
            Assert.Equal(1, idle.State.TotalCapturedChangeCount);
            Assert.Equal(1, idle.State.TotalProducedMessageCount);
            Assert.Equal(state.LastCheckpoint, idle.State.LastCheckpoint);
            Assert.Equal(state.LastChangeId, idle.State.LastChangeId);
            Assert.Equal(state.Publication, idle.State.Publication);
            Assert.False(idle.State.Metadata.ContainsKey("lastOperationType"));

            Assert.NotNull(state);
            Assert.Equal(SqlRuntimeId, state.ExecutionBinding.EffectiveExecutionRuntimeId);
            Assert.Equal(CdcCaptureRuntimeOutcomes.Captured, state.LastOutcome);
            Assert.True(state.CapturedCount > 0);
            Assert.Equal(1, state.TotalCapturedChangeCount);
            Assert.Equal(1, state.TotalProducedMessageCount);
            Assert.Equal("lsn-0001", state.LastChangeId);
            Assert.Equal($"0x00000000000000000001|0x0000000000000000000A|{operation}", state.LastCheckpoint);
            Assert.Equal("sqlserver-provider-native-runtime", state.Metadata["captureExecution"]);
            Assert.Equal(SqlRuntimeId, state.Metadata["cdcCaptureExecutionRuntimeId"]);
            Assert.Equal("provider-native", state.Metadata["acknowledgement"]);
            Assert.Equal(operationName, state.Metadata["lastOperationType"]);
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
            Assert.Equal(operationName, stagedMessage.Headers["operation"]);

            Assert.Equal([$"0x00000000000000000001|0x0000000000000000000A|{operation}"], harness.CommittedCheckpoints);

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

            var sqlRuntime = idle.Runtime;
            Assert.NotNull(sqlRuntime);
            Assert.Equal("host-managed", sqlRuntime.ExecutionOwnership);
            Assert.Equal("provider-native", sqlRuntime.ExecutionTopology);
            Assert.Equal("provider-native", sqlRuntime.AcknowledgementMode);
            Assert.Equal([CaptureId], sqlRuntime.CdcCaptureIds);
            Assert.True(sqlRuntime.Summary.HasReports);
            Assert.Equal(CaptureId, sqlRuntime.Summary.LastCdcCaptureId);
            Assert.Equal(CdcCaptureRuntimeOutcomes.Idle, sqlRuntime.Summary.LastOutcome);
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

    [Theory]
    [InlineData("capture", 0)]
    [InlineData("outbox-stage", 0)]
    [InlineData("outbox-stage", 1)]
    [InlineData("checkpoint", 2)]
    public async Task FailedIterationPreservesSpecificEvidenceOnceAndRetries(string failureKind, int stagedBeforeFailure)
    {
        var harness = new SqlServerCdcTestHarness();
        var batch = CreateTwoChangeBatch();
        harness.EnqueueBatch(batch);
        // Failed staging/commit leaves the source checkpoint uncommitted, so the
        // controlled source replays the same batch on the next poll.
        if (failureKind != "capture")
        {
            harness.EnqueueBatch(batch);
        }

        const string error = "controlled CDC failure";
        var failed = false;
        var outbox = new ControlledOutbox();
        Task FailOnce(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (!failed)
            {
                failed = true;
                throw new InvalidOperationException(error);
            }

            return Task.CompletedTask;
        }

        if (failureKind == "capture")
        {
            harness.BeforeReadAsync = FailOnce;
        }
        else if (failureKind == "checkpoint")
        {
            harness.BeforeCommitAsync = FailOnce;
        }
        else
        {
            outbox.BeforeEnqueueAsync = token => outbox.Messages.Count == stagedBeforeFailure
                ? FailOnce(token)
                : Task.CompletedTask;
        }

        using var provider = CreateProvider(harness, outbox);
        var observations = provider.GetRequiredService<CdcObservations>();
        var hostedServices = provider.GetServices<IHostedService>().ToArray();
        try
        {
            foreach (var service in hostedServices)
            {
                await service.StartAsync(CancellationToken.None);
            }

            var failure = await observations.WaitForAsync(CdcCaptureRuntimeOutcomes.Failed);
            Assert.Equal(failureKind, failure.State.Metadata["failureKind"]);
            Assert.Equal(error, failure.State.LastError);
            Assert.Equal(stagedBeforeFailure, failure.State.LastProducedMessageCount);
            Assert.Null(failure.State.LastCheckpoint);
            Assert.Equal(0, failure.State.CapturedCount);
            Assert.Equal(0, failure.State.TotalCapturedChangeCount);
            Assert.Equal("provider-native", failure.State.Metadata["acknowledgement"]);
            if (stagedBeforeFailure > 0)
            {
                Assert.Equal($"change-{stagedBeforeFailure}", failure.State.Metadata["pendingChangeId"]);
                Assert.Contains("pendingCheckpoint", failure.State.Metadata.Keys);
                Assert.Equal(stagedBeforeFailure, failure.State.Publication.PendingPublicationCount);
            }
            else
            {
                Assert.False(failure.State.Metadata.ContainsKey("pendingCheckpoint"));
            }

            // Wait through the retry: a second generic failure cannot hide behind
            // a short observation window or a test that stops at the first report.
            var captured = await observations.WaitForAsync(CdcCaptureRuntimeOutcomes.Captured);
            var idle = await observations.WaitForAsync(CdcCaptureRuntimeOutcomes.Idle);
            Assert.Single(observations.All, item => item.Report.Outcome == CdcCaptureRuntimeOutcomes.Failed);
            Assert.Equal(1, idle.State.FailedCount);
            Assert.Equal(1, idle.State.CapturedCount);
            Assert.Equal(2, idle.State.TotalCapturedChangeCount);
            Assert.Equal(stagedBeforeFailure + 2, idle.State.TotalProducedMessageCount);
            Assert.Equal(stagedBeforeFailure + 2, outbox.Messages.Count);
            Assert.NotNull(captured.State.LastCheckpoint);
            Assert.Equal([captured.State.LastCheckpoint], harness.CommittedCheckpoints);
            Assert.Null(idle.State.LastError);
            Assert.False(idle.State.Metadata.ContainsKey("failureKind"));
            Assert.False(idle.State.Metadata.ContainsKey("pendingCheckpoint"));
        }
        finally
        {
            foreach (var service in hostedServices.Reverse())
            {
                await service.StopAsync(CancellationToken.None);
            }
        }
    }

    [Theory]
    [InlineData("read", 0)]
    [InlineData("stage", 0)]
    [InlineData("checkpoint", 2)]
    public async Task StopCancelsPendingIoWithoutCommittingOrReportingFalseSuccess(string boundary, int stagedCount)
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task WaitForStop(CancellationToken token)
        {
            entered.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
        }

        var harness = new SqlServerCdcTestHarness();
        harness.EnqueueBatch(CreateTwoChangeBatch());
        var outbox = new ControlledOutbox();
        switch (boundary)
        {
            case "read": harness.BeforeReadAsync = WaitForStop; break;
            case "stage": outbox.BeforeEnqueueAsync = WaitForStop; break;
            case "checkpoint": harness.BeforeCommitAsync = WaitForStop; break;
        }

        using var provider = CreateProvider(harness, outbox);
        var observations = provider.GetRequiredService<CdcObservations>();
        var services = provider.GetServices<IHostedService>().ToArray();
        try
        {
            foreach (var service in services)
            {
                await service.StartAsync(CancellationToken.None);
            }

            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        }
        finally
        {
            foreach (var service in services.Reverse())
            {
                await service.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(10));
            }
        }

        Assert.Empty(harness.CommittedCheckpoints);
        Assert.Equal(stagedCount, outbox.Messages.Count);
        Assert.Equal(CdcCaptureRuntimeOutcomes.Started, Assert.Single(observations.All).Report.Outcome);
    }

    private static SqlServerCdcTestBatch CreateTwoChangeBatch()
    {
        var batch = new SqlServerCdcTestBatch();
        batch.Changes.Add(new SqlServerCdcTestChange { ChangeId = "change-1" });
        batch.Changes.Add(new SqlServerCdcTestChange
        {
            ChangeId = "change-2", SequenceValue = "0x00000000000000000002"
        });
        return batch;
    }

    private sealed class ControlledOutbox : IOutbox
    {
        public string OutboxId => "tenant-event-outbox";
        public ConcurrentQueue<OutboxMessage> Messages { get; } = new();
        public Func<CancellationToken, Task>? BeforeEnqueueAsync { get; set; }

        public async ValueTask EnqueueAsync(OutboxMessage message, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (BeforeEnqueueAsync is not null)
            {
                await BeforeEnqueueAsync(cancellationToken);
            }

            Messages.Enqueue(message);
        }
    }

    private sealed record CdcObservation(
        CdcCaptureExecutionReport Report,
        CdcCaptureRuntimeState State,
        CdcCaptureExecutionRuntimeDescriptor Runtime);

    private sealed class CdcObservations(
        ICdcCaptureRuntimeStateCatalog states,
        ICdcCaptureExecutionRuntimeCatalog runtimes) : ICdcCaptureRuntimeReporter
    {
        private readonly Channel<CdcObservation> channel = Channel.CreateUnbounded<CdcObservation>();
        public ConcurrentQueue<CdcObservation> All { get; } = new();

        public async ValueTask ReportAsync(CdcCaptureExecutionReport report, CancellationToken cancellationToken = default)
        {
            // Forward to the real catalog before recording the public readback.
            // This test decorator does not replace runtime state transitions.
            await ((ICdcCaptureRuntimeReporter)states).ReportAsync(report, cancellationToken);
            var observation = new CdcObservation(report, states.GetById(report.CdcCaptureId)!, runtimes.GetById(SqlRuntimeId)!);
            All.Enqueue(observation);
            channel.Writer.TryWrite(observation);
        }

        public async Task<CdcObservation> WaitForAsync(string outcome)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            while (true)
            {
                var observation = await channel.Reader.ReadAsync(timeout.Token);
                if (observation.Report.Outcome == outcome)
                {
                    return observation;
                }
            }
        }
    }

    private static ServiceProvider CreateProvider(SqlServerCdcTestHarness harness, IOutbox outbox)
    {
        var services = new ServiceCollection();
        services.AddSqlServerCdcTestHarness(harness);
        services.AddSingleton(outbox);
        services.AddSingleton<CdcObservations>();
        services.AddSingleton<ICdcCaptureRuntimeReporter>(provider => provider.GetRequiredService<CdcObservations>());
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

        return services.BuildServiceProvider();
    }
}
