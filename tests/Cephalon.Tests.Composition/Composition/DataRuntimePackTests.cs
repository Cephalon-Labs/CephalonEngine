using System.Globalization;
using Cephalon.Abstractions.Data;
using Cephalon.Data.Registration;
using Cephalon.Data.Services;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Runtime;
using Cephalon.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

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
}
