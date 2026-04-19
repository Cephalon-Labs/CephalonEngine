using System.Globalization;
using Cephalon.Abstractions.Data;
using Cephalon.Data.Registration;
using Cephalon.Data.Services;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Runtime;
using Cephalon.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Cephalon.Abstractions.Execution;

namespace Cephalon.Tests.Composition;

public sealed class DataRuntimePackTests
{
    [Fact]
    public async Task AddDataDispatchesCommandsAndQueriesThroughRegisteredHandlers()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS"]));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new DataDispatchingTestModule());
            engine.AddData();
        });

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var writeStore = scope.ServiceProvider.GetRequiredService<IWriteStore>();
        var readStore = scope.ServiceProvider.GetRequiredService<IReadStore>();

        await writeStore.ExecuteAsync(new ActivateTenantCommand("tenant-001"));
        var orderId = await writeStore.ExecuteAsync(new CreateOrderCommand("Ada"));
        var snapshot = await readStore.ExecuteAsync(new GetDispatchingSnapshotQuery());

        Assert.Equal("order-002", orderId);
        Assert.Equal(1, snapshot.ActivatedTenants);
        Assert.Equal("order-002", snapshot.LastCreatedOrderId);
        Assert.Equal("Ada", snapshot.LastCustomerName);
    }

    [Fact]
    public void AddDataRegistersReadAndWriteCapabilities()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(blueprint: "ModularMonolith"));
            engine.AddModule(new PlatformTestModule());
            engine.AddData();
        });

        using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<Cephalon.Engine.Runtime.IRuntime>();

        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "data.read");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "data.write");
    }

    [Fact]
    public async Task AddDataThrowsHelpfulErrorWhenCommandHandlerIsMissing()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(blueprint: "ModularMonolith"));
            engine.AddModule(new PlatformTestModule());
            engine.AddData();
        });

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var writeStore = scope.ServiceProvider.GetRequiredService<IWriteStore>();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await writeStore.ExecuteAsync(new MissingCommand("missing")));

        Assert.Contains(nameof(MissingCommand), exception.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(ICommandHandler<MissingCommand>), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AddDataExposesCdcCaptureRuntimeStateAndSnapshotTruth()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IEventDispatchRuntimeCatalog>(new TestEventDispatchRuntimeCatalog(
            new EventDispatchRuntimeState(
                OutboxId: "tenant-event-outbox",
                LastChannelId: "tenant-events",
                LastOutcome: "retry-scheduled",
                LastObservedAtUtc: DateTimeOffset.Parse("2026-04-20T09:30:00Z", CultureInfo.InvariantCulture),
                LastMessageId: "dispatch-001",
                LastAttempt: 2,
                StartedCount: 1,
                SucceededCount: 0,
                FailedCount: 1,
                RetryScheduledCount: 1,
                SkippedCount: 0,
                LastError: "dispatch failed",
                Metadata: new Dictionary<string, string>
                {
                    ["dispatchRuntime"] = "phase13"
                })));
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS"],
                transports: ["RestApi"]));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new Phase8CatalogModule());
            engine.AddData();
        });

        using var provider = services.BuildServiceProvider();
        var catalog = provider.GetRequiredService<ICdcCaptureRuntimeStateCatalog>();
        var reporter = provider.GetRequiredService<ICdcCaptureRuntimeReporter>();

        var initial = catalog.GetById("tenant-profile-cdc");
        Assert.NotNull(initial);
        Assert.Equal("phase8-runtime-catalogs", initial.SourceModuleId);
        Assert.Equal("postgresql", initial.Provider);
        Assert.Equal("tenant-event-outbox", initial.OutboxId);
        Assert.Null(initial.LastOutcome);
        Assert.Equal(0, initial.TotalReports);
        Assert.Equal(CdcCaptureFreshnessStates.Unknown, initial.Freshness.State);
        Assert.Equal(CdcCaptureLagStates.Unknown, initial.Lag.State);
        Assert.Equal(CdcCapturePublicationStates.DispatchRetryPending, initial.Publication.State);
        Assert.False(initial.HasFreshnessWindow);
        Assert.False(initial.HasPendingChanges);
        Assert.False(initial.HasPendingPublications);
        Assert.NotNull(initial.OutboxDispatchState);
        Assert.Equal("retry-scheduled", initial.OutboxDispatchState!.LastOutcome);

        await reporter.ReportAsync(new CdcCaptureExecutionReport(
            cdcCaptureId: "tenant-profile-cdc",
            outcome: CdcCaptureRuntimeOutcomes.Started,
            observedAtUtc: DateTimeOffset.Parse("2026-04-20T10:00:00Z", CultureInfo.InvariantCulture)));
        await reporter.ReportAsync(new CdcCaptureExecutionReport(
            cdcCaptureId: "tenant-profile-cdc",
            outcome: CdcCaptureRuntimeOutcomes.Captured,
            observedAtUtc: DateTimeOffset.Parse("2026-04-20T10:05:00Z", CultureInfo.InvariantCulture),
            capturedChangeCount: 3,
            producedMessageCount: 2,
            changeId: "lsn-0003",
            checkpoint: "0/16B6C70",
            freshness: new CdcCaptureFreshnessStatus(
                CdcCaptureFreshnessStates.Fresh,
                DateTimeOffset.Parse("2026-04-20T10:10:00Z", CultureInfo.InvariantCulture),
                "The capture is still within the expected freshness window."),
            lag: new CdcCaptureLagStatus(
                CdcCaptureLagStates.Lagging,
                pendingChangeCount: 5,
                description: "Five source changes are still waiting to be captured."),
            publication: new CdcCapturePublicationStatus(
                CdcCapturePublicationStates.PendingPublication,
                pendingPublicationCount: 2,
                description: "Two publications are still waiting to clear the outbox path."),
            metadata: new Dictionary<string, string>
            {
                ["captureRuntime"] = "phase13"
            }));

        var state = catalog.GetById("tenant-profile-cdc");
        Assert.NotNull(state);
        Assert.Equal(CdcCaptureRuntimeOutcomes.Captured, state.LastOutcome);
        Assert.Equal(1, state.StartedCount);
        Assert.Equal(1, state.CapturedCount);
        Assert.Equal(3, state.LastCapturedChangeCount);
        Assert.Equal(2, state.LastProducedMessageCount);
        Assert.Equal(3, state.TotalCapturedChangeCount);
        Assert.Equal(2, state.TotalProducedMessageCount);
        Assert.Equal("lsn-0003", state.LastChangeId);
        Assert.Equal("0/16B6C70", state.LastCheckpoint);
        Assert.Equal("phase13", state.Metadata["captureRuntime"]);
        Assert.Equal(CdcCaptureFreshnessStates.Fresh, state.Freshness.State);
        Assert.Equal(DateTimeOffset.Parse("2026-04-20T10:10:00Z", CultureInfo.InvariantCulture), state.Freshness.FreshUntilUtc);
        Assert.Equal(CdcCaptureLagStates.Lagging, state.Lag.State);
        Assert.Equal(5, state.Lag.PendingChangeCount);
        Assert.Equal(CdcCapturePublicationStates.DispatchRetryPending, state.Publication.State);
        Assert.Equal(2, state.Publication.PendingPublicationCount);
        Assert.True(state.HasFreshnessWindow);
        Assert.True(state.HasPendingChanges);
        Assert.True(state.HasPendingPublications);
        Assert.Single(catalog.GetBySourceModule("phase8-runtime-catalogs"));
        Assert.Single(catalog.GetByProvider("postgresql"));
        Assert.Single(catalog.GetByOutboxId("tenant-event-outbox"));
        Assert.Single(catalog.GetBySourceId("tenant-db"));
        Assert.Single(catalog.GetByResourceId("public.tenants"));

        var snapshot = provider.GetRequiredService<IRuntimeIntrospectionSnapshotProvider>().CreateSnapshot();
        var snapshotState = Assert.Single(snapshot.CdcCaptureStates);
        Assert.Equal("tenant-profile-cdc", snapshotState.CdcCaptureId);
        Assert.Equal(CdcCapturePublicationStates.DispatchRetryPending, snapshotState.Publication.State);
        Assert.NotNull(snapshotState.OutboxDispatchState);
        Assert.Equal("retry-scheduled", snapshotState.OutboxDispatchState!.LastOutcome);
    }

    [Fact]
    public void AddDataExposesSharedCdcExecutionSurfaceWhenEnabled()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS"]));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new Phase8CatalogModule());
            engine.AddData(options =>
            {
                options.EnableCdcExecution = true;
            });
        });

        using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<Cephalon.Engine.Runtime.IRuntime>();
        var executionGraphs = provider.GetRequiredService<IExecutionRuntimeCatalog>();
        var hostedExecutions = provider.GetRequiredService<IHostedExecutionRuntimeCatalog>();

        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "data.cdc.execution");
        var graph = Assert.Single(executionGraphs.Graphs, item => item.Id == "data-cdc-capture-flow");
        Assert.Equal("data-runtime", graph.SourceModuleId);
        Assert.Equal("resolve-cdc-captures", graph.EntryNodeId);
        Assert.Equal(4, graph.Nodes.Count);
        Assert.Equal(3, graph.Edges.Count);

        var hostedExecution = Assert.Single(hostedExecutions.HostedExecutions, item => item.Id == "data-cdc-capture-pump");
        Assert.Equal("data-runtime", hostedExecution.SourceModuleId);
        Assert.Equal("background-service", hostedExecution.Kind);
        Assert.Equal("data-cdc-capture-flow", hostedExecution.ExecutionGraphId);
        Assert.True(hostedExecution.StartsWithHost);
    }

    [Fact]
    public async Task AddDataSharedCdcExecutionPumpStagesMessagesThroughMatchingOutboxAndReportsRuntimeState()
    {
        var executionState = new TestCdcExecutionState();
        executionState.EnqueueResult(new CdcCaptureExecutionResult(
            messages:
            [
                new OutboxMessage(
                    id: "cdc-msg-001",
                    channelId: "tenant-events",
                    messageType: "tenant.profile.changed",
                    payload: """{"tenantId":"tenant-001"}""",
                    occurredAtUtc: DateTimeOffset.Parse("2026-04-20T10:30:00Z", CultureInfo.InvariantCulture))
            ],
            changeId: "lsn-0005",
            checkpoint: "0/16B6CA0",
            freshness: new CdcCaptureFreshnessStatus(
                CdcCaptureFreshnessStates.Fresh,
                DateTimeOffset.Parse("2026-04-20T10:35:00Z", CultureInfo.InvariantCulture),
                "The capture is still within the expected freshness window."),
            lag: new CdcCaptureLagStatus(
                CdcCaptureLagStates.Current,
                pendingChangeCount: 0,
                description: "The capture is caught up with the source stream."),
            metadata: new Dictionary<string, string>
            {
                ["captureRuntime"] = "phase13-shared"
            }));

        var services = new ServiceCollection();
        services.AddSingleton(executionState);
        services.AddScoped<ICdcCapture, TestCdcCapture>();
        services.AddScoped<IOutbox, TestOutbox>();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS"]));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new Phase8CatalogModule());
            engine.AddData(options =>
            {
                options.EnableCdcExecution = true;
                options.CdcPollingIntervalSeconds = 600;
            });
        });

        using var provider = services.BuildServiceProvider();
        var hostedService = Assert.Single(provider.GetServices<IHostedService>());
        await hostedService.StartAsync(CancellationToken.None);

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await executionState.WaitForStagedMessageAsync(timeout.Token);

        var catalog = provider.GetRequiredService<ICdcCaptureRuntimeStateCatalog>();
        var state = catalog.GetById("tenant-profile-cdc");
        Assert.NotNull(state);
        Assert.Equal(CdcCaptureRuntimeOutcomes.Captured, state.LastOutcome);
        Assert.Equal(1, state.LastCapturedChangeCount);
        Assert.Equal(1, state.LastProducedMessageCount);
        Assert.Equal(1, state.TotalCapturedChangeCount);
        Assert.Equal(1, state.TotalProducedMessageCount);
        Assert.Equal("lsn-0005", state.LastChangeId);
        Assert.Equal("0/16B6CA0", state.LastCheckpoint);
        Assert.Equal("shared-data-runtime", state.Metadata["captureExecution"]);
        Assert.Equal("phase13-shared", state.Metadata["captureRuntime"]);
        Assert.Equal(CdcCaptureFreshnessStates.Fresh, state.Freshness.State);
        Assert.Equal(CdcCaptureLagStates.Current, state.Lag.State);
        Assert.Equal(CdcCapturePublicationStates.PendingPublication, state.Publication.State);
        Assert.Equal(1, state.Publication.PendingPublicationCount);
        Assert.True(state.HasPendingPublications);

        var stagedMessage = Assert.Single(executionState.StagedMessages);
        Assert.Equal("cdc-msg-001", stagedMessage.Id);
        Assert.Equal("tenant-events", stagedMessage.ChannelId);

        var snapshot = provider.GetRequiredService<IRuntimeIntrospectionSnapshotProvider>().CreateSnapshot();
        var snapshotState = Assert.Single(snapshot.CdcCaptureStates);
        Assert.Equal("tenant-profile-cdc", snapshotState.CdcCaptureId);
        Assert.Equal(CdcCapturePublicationStates.PendingPublication, snapshotState.Publication.State);

        await hostedService.StopAsync(CancellationToken.None);
    }
}
