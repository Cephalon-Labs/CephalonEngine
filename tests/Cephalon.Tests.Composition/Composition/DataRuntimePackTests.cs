using System.Globalization;
using Cephalon.Abstractions.Data;
using Cephalon.Data.Configuration;
using Cephalon.Data.Registration;
using Cephalon.Data.Services;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Runtime;
using Cephalon.Abstractions.Modules;
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
        var descriptorCatalog = provider.GetRequiredService<ICdcCaptureCatalog>();
        var catalog = provider.GetRequiredService<ICdcCaptureRuntimeStateCatalog>();
        var reporter = provider.GetRequiredService<ICdcCaptureRuntimeReporter>();
        var descriptor = descriptorCatalog.GetById("tenant-profile-cdc");

        Assert.NotNull(descriptor);
        Assert.False(descriptor.ExecutionBinding.IsBound);
        Assert.Null(descriptor.ExecutionBinding.EffectiveExecutionRuntimeId);
        Assert.Equal("not-configured", descriptor.ExecutionBinding.ExecutionOwnership);
        Assert.Equal("unbound", descriptor.ExecutionBinding.ResolutionMode);

        var initial = catalog.GetById("tenant-profile-cdc");
        Assert.NotNull(initial);
        Assert.Equal("phase8-runtime-catalogs", initial.SourceModuleId);
        Assert.Equal("postgresql", initial.Provider);
        Assert.Equal("tenant-event-outbox", initial.OutboxId);
        Assert.False(initial.ExecutionBinding.IsBound);
        Assert.Null(initial.ExecutionBinding.EffectiveExecutionRuntimeId);
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
        Assert.False(state.ExecutionBinding.IsBound);
        Assert.Null(state.ExecutionBinding.EffectiveExecutionRuntimeId);
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
        var cdcCaptureCatalog = provider.GetRequiredService<ICdcCaptureCatalog>();
        var executionGraphs = provider.GetRequiredService<IExecutionRuntimeCatalog>();
        var hostedExecutions = provider.GetRequiredService<IHostedExecutionRuntimeCatalog>();
        var cdcCaptureRuntimes = provider.GetRequiredService<ICdcCaptureExecutionRuntimeCatalog>();

        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "data.cdc.execution");
        var graph = Assert.Single(executionGraphs.Graphs, item => item.Id == "data-cdc-capture-flow");
        Assert.Equal("data-runtime", graph.SourceModuleId);
        Assert.Equal("resolve-cdc-captures", graph.EntryNodeId);
        Assert.Equal(5, graph.Nodes.Count);
        Assert.Equal(4, graph.Edges.Count);
        Assert.Contains(graph.Nodes, item => item.Id == "acknowledge-cdc-progress");

        var hostedExecution = Assert.Single(hostedExecutions.HostedExecutions, item => item.Id == "data-cdc-capture-pump");
        Assert.Equal("data-runtime", hostedExecution.SourceModuleId);
        Assert.Equal("background-service", hostedExecution.Kind);
        Assert.Equal("data-cdc-capture-flow", hostedExecution.ExecutionGraphId);
        Assert.True(hostedExecution.StartsWithHost);

        var runtimeDescriptor = Assert.Single(cdcCaptureRuntimes.Runtimes);
        Assert.Equal("data-cdc-capture-pump", runtimeDescriptor.Id);
        Assert.Contains("tenant-profile-cdc", runtimeDescriptor.CdcCaptureIds);
        Assert.Equal("host-managed", runtimeDescriptor.ExecutionOwnership);
        Assert.Equal("shared-in-process-polling", runtimeDescriptor.ExecutionTopology);
        Assert.Equal("post-stage-provider", runtimeDescriptor.AcknowledgementMode);
        Assert.Equal("data-cdc-capture-flow", runtimeDescriptor.ExecutionGraphId);
        Assert.Equal("data-cdc-capture-pump", runtimeDescriptor.HostedExecutionId);
        Assert.False(runtimeDescriptor.Summary.HasReports);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorActionPlanStates.NotApplicable, runtimeDescriptor.ManagedConnectorActionPlan.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorActionPlanActionIds.None, runtimeDescriptor.ManagedConnectorActionPlan.PrimaryActionId);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStates.NotApplicable, runtimeDescriptor.ManagedConnectorWritePathReadiness.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorPreflightStates.NotApplicable, runtimeDescriptor.ManagedConnectorPreflight.State);
        Assert.Equal(CdcCaptureExecutionRuntimeManagedConnectorDryRunStates.NotApplicable, runtimeDescriptor.ManagedConnectorDryRun.State);
        var cdcCapture = cdcCaptureCatalog.GetById("tenant-profile-cdc");
        Assert.NotNull(cdcCapture);
        Assert.True(cdcCapture.ExecutionBinding.IsBound);
        Assert.Equal("data-cdc-capture-pump", cdcCapture.ExecutionBinding.EffectiveExecutionRuntimeId);
        Assert.Equal("host-managed", cdcCapture.ExecutionBinding.ExecutionOwnership);
        Assert.Equal("shared-in-process-polling", cdcCapture.ExecutionBinding.ExecutionTopology);
        Assert.Equal("default-shared-runtime", cdcCapture.ExecutionBinding.ResolutionMode);
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
        var runtimeCatalog = provider.GetRequiredService<ICdcCaptureExecutionRuntimeCatalog>();
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
        Assert.Equal("not-required", state.Metadata["acknowledgement"]);
        Assert.True(state.ExecutionBinding.IsBound);
        Assert.Equal("data-cdc-capture-pump", state.ExecutionBinding.EffectiveExecutionRuntimeId);
        Assert.Equal("host-managed", state.ExecutionBinding.ExecutionOwnership);
        Assert.Equal("shared-in-process-polling", state.ExecutionBinding.ExecutionTopology);
        Assert.Equal(CdcCaptureFreshnessStates.Fresh, state.Freshness.State);
        Assert.Equal(CdcCaptureLagStates.Current, state.Lag.State);
        Assert.Equal(CdcCapturePublicationStates.PendingPublication, state.Publication.State);
        Assert.Equal(1, state.Publication.PendingPublicationCount);
        Assert.True(state.HasPendingPublications);

        var stagedMessage = Assert.Single(executionState.StagedMessages);
        Assert.Equal("cdc-msg-001", stagedMessage.Id);
        Assert.Equal("tenant-events", stagedMessage.ChannelId);

        var runtimeDescriptor = runtimeCatalog.GetById("data-cdc-capture-pump");
        Assert.NotNull(runtimeDescriptor);
        Assert.Equal(["tenant-profile-cdc"], runtimeDescriptor.CdcCaptureIds);
        Assert.True(runtimeDescriptor.Summary.HasReports);
        Assert.Equal(["tenant-profile-cdc"], runtimeDescriptor.Summary.ReportedCdcCaptureIds);
        Assert.Equal("tenant-profile-cdc", runtimeDescriptor.Summary.LastCdcCaptureId);
        Assert.Equal(CdcCaptureRuntimeOutcomes.Captured, runtimeDescriptor.Summary.LastOutcome);
        Assert.Equal(1, runtimeDescriptor.Summary.TotalCapturedChangeCount);
        Assert.Equal(1, runtimeDescriptor.Summary.TotalProducedMessageCount);
        Assert.Equal("not-required", runtimeDescriptor.Summary.LastAcknowledgement);

        var snapshot = provider.GetRequiredService<IRuntimeIntrospectionSnapshotProvider>().CreateSnapshot();
        var snapshotState = Assert.Single(snapshot.CdcCaptureStates);
        Assert.Equal("tenant-profile-cdc", snapshotState.CdcCaptureId);
        Assert.Equal(CdcCapturePublicationStates.PendingPublication, snapshotState.Publication.State);
        var snapshotRuntime = Assert.Single(snapshot.CdcCaptureExecutionRuntimes);
        Assert.Equal("data-cdc-capture-pump", snapshotRuntime.Id);
        Assert.Equal(2, snapshotRuntime.Summary.TotalReports);

        await hostedService.StopAsync(CancellationToken.None);
    }

    [Fact]
    public void AddDataBindsCdcCaptureToRequestedExecutionRuntimeWhenDeclared()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ICdcCaptureExecutionRuntimeContributor>(
            new TestExecutionRuntimeContributor(
                id: "external-cdc-runtime",
                displayName: "External CDC Runtime",
                description: "Represents an externally managed CDC runner.",
                executionOwnership: "external-managed"));
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(blueprint: "ModularVerticalSlice"));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new RequestedExecutionBindingCdcModule());
            engine.AddData();
        });

        using var provider = services.BuildServiceProvider();
        var captureCatalog = provider.GetRequiredService<ICdcCaptureCatalog>();
        var runtimeCatalog = provider.GetRequiredService<ICdcCaptureExecutionRuntimeCatalog>();

        var cdcCapture = captureCatalog.GetById("requested-cdc");
        Assert.NotNull(cdcCapture);
        Assert.Equal("external-cdc-runtime", cdcCapture.ExecutionBinding.AuthoredExecutionRuntimeId);
        Assert.Equal("external-cdc-runtime", cdcCapture.ExecutionBinding.RequestedExecutionRuntimeId);
        Assert.Equal("external-cdc-runtime", cdcCapture.ExecutionBinding.EffectiveExecutionRuntimeId);
        Assert.Equal("external-managed", cdcCapture.ExecutionBinding.ExecutionOwnership);
        Assert.Equal("external-runtime", cdcCapture.ExecutionBinding.ExecutionTopology);
        Assert.Equal("requested-execution-runtime", cdcCapture.ExecutionBinding.ResolutionMode);

        var runtime = runtimeCatalog.GetById("external-cdc-runtime");
        Assert.NotNull(runtime);
        Assert.Equal(["requested-cdc"], runtime.CdcCaptureIds);
        Assert.Equal("external-managed", runtime.ExecutionOwnership);
        Assert.Equal("external-runtime", runtime.ExecutionTopology);
    }

    [Fact]
    public void AddDataPublishesConfiguredProviderNativeCdcExecutionRuntimeAndInverseLookups()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(blueprint: "ModularVerticalSlice"));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new RequestedExecutionBindingCdcModule());
            engine.AddData(options =>
            {
                options.CdcExecutionRuntimes.Add(new CdcCaptureExecutionRuntimeOptions
                {
                    Id = "external-cdc-runtime",
                    DisplayName = "External CDC Runtime",
                    Description = "Represents a provider-native CDC runner declared by the host.",
                    ExecutionOwnership = "external-managed",
                    ExecutionTopology = "provider-native",
                    AcknowledgementMode = "provider-native",
                    HostedExecutionId = "external-cdc-runtime-host",
                    ExecutionGraphId = "external-cdc-runtime-flow"
                });
            });
        });

        using var provider = services.BuildServiceProvider();
        var captureCatalog = provider.GetRequiredService<ICdcCaptureCatalog>();
        var runtimeStateCatalog = provider.GetRequiredService<ICdcCaptureRuntimeStateCatalog>();
        var runtimeCatalog = provider.GetRequiredService<ICdcCaptureExecutionRuntimeCatalog>();

        var capture = Assert.Single(captureCatalog.GetByExecutionRuntimeId("external-cdc-runtime"));
        Assert.Equal("requested-cdc", capture.Id);
        Assert.Equal("external-cdc-runtime", capture.ExecutionBinding.EffectiveExecutionRuntimeId);
        Assert.Equal("external-managed", capture.ExecutionBinding.ExecutionOwnership);
        Assert.Equal("provider-native", capture.ExecutionBinding.ExecutionTopology);
        Assert.Equal("requested-execution-runtime", capture.ExecutionBinding.ResolutionMode);

        var state = Assert.Single(runtimeStateCatalog.GetByExecutionRuntimeId("external-cdc-runtime"));
        Assert.Equal("requested-cdc", state.CdcCaptureId);
        Assert.Null(state.LastOutcome);
        Assert.Equal("external-cdc-runtime", state.ExecutionBinding.EffectiveExecutionRuntimeId);
        Assert.Equal("provider-native", state.ExecutionBinding.ExecutionTopology);

        var runtime = runtimeCatalog.GetById("external-cdc-runtime");
        Assert.NotNull(runtime);
        Assert.Equal("external-managed", runtime.ExecutionOwnership);
        Assert.Equal("provider-native", runtime.ExecutionTopology);
        Assert.Equal("provider-native", runtime.AcknowledgementMode);
        Assert.Equal("external-cdc-runtime-host", runtime.HostedExecutionId);
        Assert.Equal("external-cdc-runtime-flow", runtime.ExecutionGraphId);
        Assert.Equal(["requested-cdc"], runtime.CdcCaptureIds);
        Assert.False(runtime.Summary.HasReports);
    }

    [Fact]
    public async Task AddDataAcceptsExecutionRuntimeObservationsThroughExternalReportSink()
    {
        var timeProvider = new MutableTimeProvider(DateTimeOffset.Parse("2026-04-21T02:10:30Z", CultureInfo.InvariantCulture));
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(timeProvider);
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS"]));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new Phase8CatalogModule());
            engine.AddData(options =>
            {
                options.EnableExternalCdcRuntimeReporting = true;
                options.CdcExecutionRuntimes.Add(new CdcCaptureExecutionRuntimeOptions
                {
                    Id = "external-cdc-runtime",
                    DisplayName = "External CDC Runtime",
                    Description = "Represents an externally managed out-of-process CDC runner.",
                    ExecutionOwnership = "external-managed",
                    ExecutionTopology = "out-of-process-reporting",
                    AcknowledgementMode = "runtime-managed",
                    ObservationStaleAfterSeconds = 60,
                    RejectOutOfOrderReports = true,
                    ReporterLeaseSeconds = 120,
                    RejectConflictingReporterIds = true
                });
                options.CdcExecutionRuntimes[0].CdcCaptureIds.Add("tenant-profile-cdc");
                options.CdcExecutionRuntimes[0].EdgeNodeIds.Add("edge-bkk-01");
            });
        });

        using var provider = services.BuildServiceProvider();
        var reportSink = provider.GetRequiredService<ICdcCaptureExecutionRuntimeReportSink>();
        var runtimeStateCatalog = provider.GetRequiredService<ICdcCaptureRuntimeStateCatalog>();
        var runtimeCatalog = provider.GetRequiredService<ICdcCaptureExecutionRuntimeCatalog>();

        await reportSink.ReportAsync(
            "external-cdc-runtime",
            [
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: "tenant-profile-cdc",
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-21T02:10:00Z", CultureInfo.InvariantCulture),
                    reportId: "external-report-001",
                    capturedChangeCount: 3,
                    producedMessageCount: 3,
                    changeId: "lsn-ext-0003",
                    checkpoint: "ext-checkpoint-0003",
                    freshness: new CdcCaptureFreshnessStatus(
                        CdcCaptureFreshnessStates.Fresh,
                        DateTimeOffset.Parse("2026-04-21T02:15:00Z", CultureInfo.InvariantCulture),
                        "The external runtime is still within the expected freshness window."),
                    lag: new CdcCaptureLagStatus(
                        CdcCaptureLagStates.Lagging,
                        pendingChangeCount: 2,
                        description: "The external runtime still has pending source changes."),
                    publication: new CdcCapturePublicationStatus(
                        CdcCapturePublicationStates.PendingPublication,
                        pendingPublicationCount: 2,
                        description: "The external runtime still has pending publications."),
                    metadata: new Dictionary<string, string>
                    {
                        ["captureExecution"] = "external-runtime-report"
                    },
                    reporterId: "edge-agent-a",
                    edgeNodeId: "edge-bkk-01")
            ]);

        var state = Assert.Single(runtimeStateCatalog.GetByExecutionRuntimeId("external-cdc-runtime"));
        Assert.Equal("tenant-profile-cdc", state.CdcCaptureId);
        Assert.Equal(CdcCaptureRuntimeOutcomes.Captured, state.LastOutcome);
        Assert.Equal(3, state.TotalCapturedChangeCount);
        Assert.Equal(3, state.TotalProducedMessageCount);
        Assert.Equal("lsn-ext-0003", state.LastChangeId);
        Assert.Equal("ext-checkpoint-0003", state.LastCheckpoint);
        Assert.Equal("external-report-001", state.LastReportId);
        Assert.Equal("external-cdc-runtime", state.ExecutionBinding.EffectiveExecutionRuntimeId);
        Assert.Equal("out-of-process-reporting", state.ExecutionBinding.ExecutionTopology);
        Assert.Equal("edge-agent-a", state.LastReporterId);
        Assert.Equal(DateTimeOffset.Parse("2026-04-21T02:12:00Z", CultureInfo.InvariantCulture), state.ReporterLeaseExpiresAtUtc);
        Assert.Equal("edge-bkk-01", state.LastEdgeNodeId);
        Assert.Equal("external-runtime-report", state.Metadata["captureExecution"]);
        Assert.Equal("external-cdc-runtime", state.Metadata["cdcCaptureExecutionRuntimeId"]);
        Assert.Equal("external-report-001", state.Metadata["cdcCaptureReportId"]);
        Assert.Equal("edge-agent-a", state.Metadata["cdcCaptureReporterId"]);
        Assert.Equal("2026-04-21T02:12:00.0000000+00:00", state.Metadata["cdcCaptureReporterLeaseExpiresAtUtc"]);
        Assert.Equal("edge-bkk-01", state.Metadata["cdcCaptureEdgeNodeId"]);
        Assert.Equal(CdcCaptureFreshnessStates.Fresh, state.ObservationFreshness.State);
        Assert.Equal(DateTimeOffset.Parse("2026-04-21T02:11:00Z", CultureInfo.InvariantCulture), state.ObservationFreshness.FreshUntilUtc);
        Assert.Equal(CdcCaptureFreshnessStates.Fresh, state.Metadata["observationFreshnessState"]);
        Assert.Equal("60", state.Metadata["observationStaleAfterSeconds"]);

        var runtime = runtimeCatalog.GetById("external-cdc-runtime");
        Assert.NotNull(runtime);
        Assert.True(runtime.Summary.HasReports);
        Assert.Equal(120, runtime.ReporterLeaseSeconds);
        Assert.True(runtime.RejectConflictingReporterIds);
        Assert.Equal(["edge-bkk-01"], runtime.EdgeNodeIds);
        Assert.Equal("tenant-profile-cdc", runtime.Summary.LastCdcCaptureId);
        Assert.Equal(CdcCaptureRuntimeOutcomes.Captured, runtime.Summary.LastOutcome);
        Assert.Equal("external-report-001", runtime.Summary.LastReportId);
        Assert.Equal("edge-agent-a", runtime.Summary.LastReporterId);
        Assert.Equal("edge-agent-a", runtime.Summary.ActiveReporterId);
        Assert.Equal(DateTimeOffset.Parse("2026-04-21T02:12:00Z", CultureInfo.InvariantCulture), runtime.Summary.ReporterLeaseExpiresAtUtc);
        Assert.Equal(["edge-bkk-01"], runtime.Summary.ObservedEdgeNodeIds);
        Assert.Equal("edge-bkk-01", runtime.Summary.LastEdgeNodeId);
        Assert.Equal(3, runtime.Summary.TotalCapturedChangeCount);
        Assert.Equal(3, runtime.Summary.TotalProducedMessageCount);
        Assert.Equal(CdcCaptureFreshnessStates.Fresh, runtime.Summary.ObservationFreshness.State);
        Assert.Equal(DateTimeOffset.Parse("2026-04-21T02:11:00Z", CultureInfo.InvariantCulture), runtime.Summary.ObservationFreshness.FreshUntilUtc);
    }

    [Fact]
    public async Task AddDataTreatsRepeatedExternalExecutionRuntimeReportIdsAsIdempotent()
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
                options.EnableExternalCdcRuntimeReporting = true;
                options.CdcExecutionRuntimes.Add(new CdcCaptureExecutionRuntimeOptions
                {
                    Id = "external-cdc-runtime",
                    DisplayName = "External CDC Runtime",
                    Description = "Represents an externally managed out-of-process CDC runner.",
                    ExecutionOwnership = "external-managed",
                    ExecutionTopology = "out-of-process-reporting"
                });
                options.CdcExecutionRuntimes[0].CdcCaptureIds.Add("tenant-profile-cdc");
            });
        });

        using var provider = services.BuildServiceProvider();
        var reportSink = provider.GetRequiredService<ICdcCaptureExecutionRuntimeReportSink>();
        var runtimeStateCatalog = provider.GetRequiredService<ICdcCaptureRuntimeStateCatalog>();
        var runtimeCatalog = provider.GetRequiredService<ICdcCaptureExecutionRuntimeCatalog>();

        var observation = new CdcCaptureRuntimeObservation(
            cdcCaptureId: "tenant-profile-cdc",
            outcome: CdcCaptureRuntimeOutcomes.Captured,
            observedAtUtc: DateTimeOffset.Parse("2026-04-21T02:30:00Z", CultureInfo.InvariantCulture),
            reportId: "external-report-duplicate",
            capturedChangeCount: 4,
            producedMessageCount: 4,
            changeId: "lsn-ext-0004",
            checkpoint: "ext-checkpoint-0004",
            metadata: new Dictionary<string, string>
            {
                ["captureExecution"] = "external-runtime-report"
            });

        await reportSink.ReportAsync("external-cdc-runtime", [observation]);
        await reportSink.ReportAsync("external-cdc-runtime", [observation]);

        var state = Assert.Single(runtimeStateCatalog.GetByExecutionRuntimeId("external-cdc-runtime"));
        Assert.Equal("external-report-duplicate", state.LastReportId);
        Assert.Equal(1, state.CapturedCount);
        Assert.Equal(4, state.TotalCapturedChangeCount);
        Assert.Equal(4, state.TotalProducedMessageCount);

        var runtime = runtimeCatalog.GetById("external-cdc-runtime");
        Assert.NotNull(runtime);
        Assert.Equal("external-report-duplicate", runtime.Summary.LastReportId);
        Assert.Equal(1, runtime.Summary.CapturedCount);
        Assert.Equal(4, runtime.Summary.TotalCapturedChangeCount);
        Assert.Equal(4, runtime.Summary.TotalProducedMessageCount);
    }

    [Fact]
    public async Task AddDataRejectsOutOfOrderExternalExecutionRuntimeReportsWhenConfigured()
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
                options.EnableExternalCdcRuntimeReporting = true;
                options.CdcExecutionRuntimes.Add(new CdcCaptureExecutionRuntimeOptions
                {
                    Id = "external-cdc-runtime",
                    DisplayName = "External CDC Runtime",
                    Description = "Represents an externally managed out-of-process CDC runner.",
                    ExecutionOwnership = "external-managed",
                    ExecutionTopology = "out-of-process-reporting",
                    RejectOutOfOrderReports = true
                });
                options.CdcExecutionRuntimes[0].CdcCaptureIds.Add("tenant-profile-cdc");
            });
        });

        using var provider = services.BuildServiceProvider();
        var reportSink = provider.GetRequiredService<ICdcCaptureExecutionRuntimeReportSink>();

        await reportSink.ReportAsync(
            "external-cdc-runtime",
            [
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: "tenant-profile-cdc",
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-21T02:40:00Z", CultureInfo.InvariantCulture),
                    reportId: "external-report-late")
            ]);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => reportSink.ReportAsync(
            "external-cdc-runtime",
            [
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: "tenant-profile-cdc",
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-21T02:35:00Z", CultureInfo.InvariantCulture),
                    reportId: "external-report-early")
            ]).AsTask());

        Assert.Contains("out-of-order", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("external-report-early", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AddDataRejectsConflictingExternalExecutionRuntimeReporterIdsWhileLeaseIsActive()
    {
        var timeProvider = new MutableTimeProvider(DateTimeOffset.Parse("2026-04-21T02:45:00Z", CultureInfo.InvariantCulture));
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(timeProvider);
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS"]));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new Phase8CatalogModule());
            engine.AddData(options =>
            {
                options.EnableExternalCdcRuntimeReporting = true;
                options.CdcExecutionRuntimes.Add(new CdcCaptureExecutionRuntimeOptions
                {
                    Id = "external-cdc-runtime",
                    DisplayName = "External CDC Runtime",
                    Description = "Represents an externally managed out-of-process CDC runner.",
                    ExecutionOwnership = "external-managed",
                    ExecutionTopology = "out-of-process-reporting",
                    ReporterLeaseSeconds = 120,
                    RejectConflictingReporterIds = true
                });
                options.CdcExecutionRuntimes[0].CdcCaptureIds.Add("tenant-profile-cdc");
            });
        });

        using var provider = services.BuildServiceProvider();
        var reportSink = provider.GetRequiredService<ICdcCaptureExecutionRuntimeReportSink>();
        var runtimeStateCatalog = provider.GetRequiredService<ICdcCaptureRuntimeStateCatalog>();
        var runtimeCatalog = provider.GetRequiredService<ICdcCaptureExecutionRuntimeCatalog>();

        await reportSink.ReportAsync(
            "external-cdc-runtime",
            [
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: "tenant-profile-cdc",
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-21T02:44:00Z", CultureInfo.InvariantCulture),
                    reportId: "external-report-reporter-a",
                    reporterId: "edge-agent-a")
            ]);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => reportSink.ReportAsync(
            "external-cdc-runtime",
            [
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: "tenant-profile-cdc",
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-21T02:44:30Z", CultureInfo.InvariantCulture),
                    reportId: "external-report-reporter-b",
                    reporterId: "edge-agent-b")
            ]).AsTask());

        Assert.Contains("edge-agent-b", exception.Message, StringComparison.Ordinal);
        Assert.Contains("edge-agent-a", exception.Message, StringComparison.Ordinal);

        var state = Assert.Single(runtimeStateCatalog.GetByExecutionRuntimeId("external-cdc-runtime"));
        Assert.Equal("edge-agent-a", state.LastReporterId);
        Assert.Equal(DateTimeOffset.Parse("2026-04-21T02:46:00Z", CultureInfo.InvariantCulture), state.ReporterLeaseExpiresAtUtc);
        Assert.Equal(CdcCaptureReporterCoordinationStates.Conflicted, state.ReporterCoordination.State);
        Assert.Equal("edge-agent-a", state.ReporterCoordination.ActiveReporterId);
        Assert.Equal(DateTimeOffset.Parse("2026-04-21T02:46:00Z", CultureInfo.InvariantCulture), state.ReporterCoordination.ActiveReporterLeaseExpiresAtUtc);
        Assert.Equal("edge-agent-b", state.ReporterCoordination.LastConflictingReporterId);
        Assert.Equal(DateTimeOffset.Parse("2026-04-21T02:44:30Z", CultureInfo.InvariantCulture), state.ReporterCoordination.LastConflictedAtUtc);
        Assert.Equal(CdcCaptureReporterTakeoverStates.NotRequired, state.ReporterCoordination.TakeoverState);
        Assert.Equal(CdcCaptureReporterCoordinationIssueReasons.RejectedReporterConflict, state.ReporterCoordination.DegradedReason);
        Assert.True(state.ReporterCoordination.IsDegraded);
        Assert.True(state.HasReporterCoordinationIssue);
        Assert.False(state.ReporterCoordination.HasStandbyReporters);
        Assert.True(state.ReporterCoordination.HasRejectedReporters);
        Assert.Collection(
            state.ReporterCoordination.ReporterParticipants,
            participant =>
            {
                Assert.Equal("edge-agent-a", participant.ReporterId);
                Assert.Equal(CdcCaptureReporterParticipantRoles.Active, participant.Role);
                Assert.Equal(DateTimeOffset.Parse("2026-04-21T02:44:00Z", CultureInfo.InvariantCulture), participant.LastObservedAtUtc);
                Assert.Equal(DateTimeOffset.Parse("2026-04-21T02:46:00Z", CultureInfo.InvariantCulture), participant.LeaseExpiresAtUtc);
                Assert.Equal("tenant-profile-cdc", participant.LastCdcCaptureId);
            },
            participant =>
            {
                Assert.Equal("edge-agent-b", participant.ReporterId);
                Assert.Equal(CdcCaptureReporterParticipantRoles.Rejected, participant.Role);
                Assert.Equal(DateTimeOffset.Parse("2026-04-21T02:44:30Z", CultureInfo.InvariantCulture), participant.LastObservedAtUtc);
                Assert.Equal("tenant-profile-cdc", participant.LastCdcCaptureId);
            });

        var runtime = runtimeCatalog.GetById("external-cdc-runtime");
        Assert.NotNull(runtime);
        Assert.Equal("edge-agent-a", runtime.Summary.ActiveReporterId);
        Assert.Equal(DateTimeOffset.Parse("2026-04-21T02:46:00Z", CultureInfo.InvariantCulture), runtime.Summary.ReporterLeaseExpiresAtUtc);
        Assert.Equal(CdcCaptureReporterCoordinationStates.Conflicted, runtime.Summary.ReporterCoordination.State);
        Assert.Equal("edge-agent-a", runtime.Summary.ReporterCoordination.ActiveReporterId);
        Assert.Equal("edge-agent-b", runtime.Summary.ReporterCoordination.LastConflictingReporterId);
        Assert.Equal(CdcCaptureReporterTakeoverStates.NotRequired, runtime.Summary.ReporterCoordination.TakeoverState);
        Assert.Equal(CdcCaptureReporterCoordinationIssueReasons.RejectedReporterConflict, runtime.Summary.ReporterCoordination.DegradedReason);
        Assert.True(runtime.Summary.ReporterCoordination.IsDegraded);
        Assert.True(runtime.Summary.HasReporterCoordinationIssue);
        Assert.Equal(["edge-agent-a"], runtime.Summary.ReporterCoordinationRollup.ActiveReporterIds);
        Assert.Empty(runtime.Summary.ReporterCoordinationRollup.StandbyReporterIds);
        Assert.Equal(["edge-agent-b"], runtime.Summary.ReporterCoordinationRollup.RejectedReporterIds);
        Assert.Equal(["tenant-profile-cdc"], runtime.Summary.ReporterCoordinationRollup.DegradedCdcCaptureIds);
        Assert.True(runtime.Summary.ReporterCoordinationRollup.HasRejectedReporters);
        Assert.True(runtime.Summary.ReporterCoordinationRollup.HasDegradedCdcCaptures);
        Assert.Collection(
            runtime.Summary.ReporterCoordinationRollup.CoordinationStateBreakdown,
            breakdown =>
            {
                Assert.Equal(CdcCaptureReporterCoordinationStates.Conflicted, breakdown.Id);
                Assert.Equal(1, breakdown.Count);
            });
        Assert.Collection(
            runtime.Summary.ReporterCoordinationRollup.DegradedReasonBreakdown,
            breakdown =>
            {
                Assert.Equal(CdcCaptureReporterCoordinationIssueReasons.RejectedReporterConflict, breakdown.Id);
                Assert.Equal(1, breakdown.Count);
            });
        Assert.Collection(
            runtime.Summary.ReporterCoordination.ReporterParticipants,
            participant =>
            {
                Assert.Equal("edge-agent-a", participant.ReporterId);
                Assert.Equal(CdcCaptureReporterParticipantRoles.Active, participant.Role);
            },
            participant =>
            {
                Assert.Equal("edge-agent-b", participant.ReporterId);
                Assert.Equal(CdcCaptureReporterParticipantRoles.Rejected, participant.Role);
            });
    }

    [Fact]
    public async Task AddDataSupportsReporterAndEdgeAwareOperatorStoryDrillDowns()
    {
        var timeProvider = new MutableTimeProvider(DateTimeOffset.Parse("2026-04-21T02:45:00Z", CultureInfo.InvariantCulture));
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(timeProvider);
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS"]));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new Phase8CatalogModule());
            engine.AddData(options =>
            {
                options.EnableExternalCdcRuntimeReporting = true;
                options.CdcExecutionRuntimes.Add(new CdcCaptureExecutionRuntimeOptions
                {
                    Id = "external-cdc-runtime",
                    DisplayName = "External CDC Runtime",
                    Description = "Represents an externally managed out-of-process CDC runner.",
                    ExecutionOwnership = "external-managed",
                    ExecutionTopology = "edge-reporting",
                    ReporterLeaseSeconds = 120,
                    RejectConflictingReporterIds = true
                });
                options.CdcExecutionRuntimes[0].CdcCaptureIds.Add("tenant-profile-cdc");
                options.CdcExecutionRuntimes[0].EdgeNodeIds.Add("edge-bkk-01");
            });
        });

        using var provider = services.BuildServiceProvider();
        var reportSink = provider.GetRequiredService<ICdcCaptureExecutionRuntimeReportSink>();
        var runtimeStateCatalog = provider.GetRequiredService<ICdcCaptureRuntimeStateCatalog>();
        var runtimeCatalog = provider.GetRequiredService<ICdcCaptureExecutionRuntimeCatalog>();

        await reportSink.ReportAsync(
            "external-cdc-runtime",
            [
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: "tenant-profile-cdc",
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-21T02:44:00Z", CultureInfo.InvariantCulture),
                    reportId: "external-report-reporter-a",
                    reporterId: "edge-agent-a",
                    edgeNodeId: "edge-bkk-01")
            ]);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => reportSink.ReportAsync(
            "external-cdc-runtime",
            [
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: "tenant-profile-cdc",
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-21T02:44:30Z", CultureInfo.InvariantCulture),
                    reportId: "external-report-reporter-b",
                    reporterId: "edge-agent-b",
                    edgeNodeId: "edge-bkk-01")
            ]).AsTask());

        Assert.Contains("edge-agent-b", exception.Message, StringComparison.Ordinal);

        var statesByReporterA = runtimeStateCatalog.GetByReporterId("edge-agent-a");
        var statesByReporterB = runtimeStateCatalog.GetByReporterId("edge-agent-b");
        var statesByEdgeNode = runtimeStateCatalog.GetByEdgeNodeId("edge-bkk-01");
        var statesByCoordination = runtimeStateCatalog.GetByReporterCoordinationState(CdcCaptureReporterCoordinationStates.Conflicted);
        var statesByIssue = runtimeStateCatalog.GetByReporterCoordinationIssueReason(CdcCaptureReporterCoordinationIssueReasons.RejectedReporterConflict);

        var runtimesByReporterA = runtimeCatalog.GetByReporterId("edge-agent-a");
        var runtimesByReporterB = runtimeCatalog.GetByReporterId("edge-agent-b");
        var runtimesByEdgeNode = runtimeCatalog.GetByEdgeNodeId("edge-bkk-01");
        var runtimesByCoordination = runtimeCatalog.GetByReporterCoordinationState(CdcCaptureReporterCoordinationStates.Conflicted);
        var runtimesByIssue = runtimeCatalog.GetByReporterCoordinationIssueReason(CdcCaptureReporterCoordinationIssueReasons.RejectedReporterConflict);

        var reporterAState = Assert.Single(statesByReporterA);
        Assert.Equal("tenant-profile-cdc", reporterAState.CdcCaptureId);
        var reporterBState = Assert.Single(statesByReporterB);
        Assert.Equal("tenant-profile-cdc", reporterBState.CdcCaptureId);
        var edgeNodeState = Assert.Single(statesByEdgeNode);
        Assert.Equal("tenant-profile-cdc", edgeNodeState.CdcCaptureId);
        Assert.Single(statesByCoordination);
        Assert.Single(statesByIssue);
        Assert.Empty(runtimeStateCatalog.GetByReporterId("edge-agent-c"));

        var reporterARuntime = Assert.Single(runtimesByReporterA);
        Assert.Equal("external-cdc-runtime", reporterARuntime.Id);
        var reporterBRuntime = Assert.Single(runtimesByReporterB);
        Assert.Equal("external-cdc-runtime", reporterBRuntime.Id);
        var edgeNodeRuntime = Assert.Single(runtimesByEdgeNode);
        Assert.Equal("external-cdc-runtime", edgeNodeRuntime.Id);
        Assert.Single(runtimesByCoordination);
        Assert.Single(runtimesByIssue);
        Assert.Empty(runtimeCatalog.GetByEdgeNodeId("edge-bkk-99"));
    }

    [Fact]
    public async Task AddDataTracksExternalExecutionRuntimeReportingCoverageAcrossDeclaredCaptures()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS"]));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new MultiCaptureExecutionRuntimeCdcModule());
            engine.AddData(options =>
            {
                options.EnableExternalCdcRuntimeReporting = true;
                options.CdcExecutionRuntimes.Add(new CdcCaptureExecutionRuntimeOptions
                {
                    Id = "external-cdc-runtime",
                    DisplayName = "External CDC Runtime",
                    Description = "Represents an externally managed out-of-process CDC runner.",
                    ExecutionOwnership = "external-managed",
                    ExecutionTopology = "out-of-process-reporting",
                    ReporterLeaseSeconds = 120,
                    RejectConflictingReporterIds = false
                });
                options.CdcExecutionRuntimes[0].CdcCaptureIds.Add("multi-capture-cdc-a");
                options.CdcExecutionRuntimes[0].CdcCaptureIds.Add("multi-capture-cdc-b");
            });
        });

        using var provider = services.BuildServiceProvider();
        var reportSink = provider.GetRequiredService<ICdcCaptureExecutionRuntimeReportSink>();
        var runtimeCatalog = provider.GetRequiredService<ICdcCaptureExecutionRuntimeCatalog>();
        var snapshotProvider = provider.GetRequiredService<IRuntimeIntrospectionSnapshotProvider>();

        var initialRuntime = runtimeCatalog.GetById("external-cdc-runtime");
        Assert.NotNull(initialRuntime);
        Assert.Equal(CdcCaptureExecutionRuntimeReportingCoverageStates.Unreported, initialRuntime.Summary.ReportingCoverage.State);
        Assert.Equal(2, initialRuntime.Summary.ReportingCoverage.DeclaredCaptureCount);
        Assert.Equal(0, initialRuntime.Summary.ReportingCoverage.ReportedCaptureCount);
        Assert.Equal(["multi-capture-cdc-a", "multi-capture-cdc-b"], initialRuntime.Summary.ReportingCoverage.UnreportedCdcCaptureIds);
        Assert.True(initialRuntime.Summary.HasUnreportedDeclaredCaptures);
        Assert.False(initialRuntime.Summary.HasFullCaptureCoverage);

        await reportSink.ReportAsync(
            "external-cdc-runtime",
            [
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: "multi-capture-cdc-a",
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-23T08:00:00Z", CultureInfo.InvariantCulture),
                    reportId: "external-report-coverage-a",
                    reporterId: "edge-agent-a")
            ]);

        var partiallyReportedRuntime = runtimeCatalog.GetById("external-cdc-runtime");
        Assert.NotNull(partiallyReportedRuntime);
        Assert.Equal(CdcCaptureExecutionRuntimeReportingCoverageStates.PartiallyReported, partiallyReportedRuntime.Summary.ReportingCoverage.State);
        Assert.Equal(2, partiallyReportedRuntime.Summary.ReportingCoverage.DeclaredCaptureCount);
        Assert.Equal(1, partiallyReportedRuntime.Summary.ReportingCoverage.ReportedCaptureCount);
        Assert.Equal(["multi-capture-cdc-b"], partiallyReportedRuntime.Summary.ReportingCoverage.UnreportedCdcCaptureIds);
        Assert.Equal(["multi-capture-cdc-a"], partiallyReportedRuntime.Summary.ReportedCdcCaptureIds);
        Assert.True(partiallyReportedRuntime.Summary.HasUnreportedDeclaredCaptures);
        Assert.False(partiallyReportedRuntime.Summary.HasFullCaptureCoverage);

        await reportSink.ReportAsync(
            "external-cdc-runtime",
            [
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: "multi-capture-cdc-b",
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-23T08:00:30Z", CultureInfo.InvariantCulture),
                    reportId: "external-report-coverage-b",
                    reporterId: "edge-agent-a")
            ]);

        var fullyReportedRuntime = runtimeCatalog.GetById("external-cdc-runtime");
        Assert.NotNull(fullyReportedRuntime);
        Assert.Equal(CdcCaptureExecutionRuntimeReportingCoverageStates.FullyReported, fullyReportedRuntime.Summary.ReportingCoverage.State);
        Assert.Equal(2, fullyReportedRuntime.Summary.ReportingCoverage.DeclaredCaptureCount);
        Assert.Equal(2, fullyReportedRuntime.Summary.ReportingCoverage.ReportedCaptureCount);
        Assert.Empty(fullyReportedRuntime.Summary.ReportingCoverage.UnreportedCdcCaptureIds);
        Assert.Equal(["multi-capture-cdc-a", "multi-capture-cdc-b"], fullyReportedRuntime.Summary.ReportedCdcCaptureIds.OrderBy(static id => id, StringComparer.OrdinalIgnoreCase));
        Assert.False(fullyReportedRuntime.Summary.HasUnreportedDeclaredCaptures);
        Assert.True(fullyReportedRuntime.Summary.HasFullCaptureCoverage);

        var snapshot = snapshotProvider.CreateSnapshot();
        var snapshotRuntime = snapshot.CdcCaptureExecutionRuntimes.Single(item => item.Id == "external-cdc-runtime");
        Assert.Equal(CdcCaptureExecutionRuntimeReportingCoverageStates.FullyReported, snapshotRuntime.Summary.ReportingCoverage.State);
        Assert.Equal(2, snapshotRuntime.Summary.ReportingCoverage.DeclaredCaptureCount);
        Assert.Equal(2, snapshotRuntime.Summary.ReportingCoverage.ReportedCaptureCount);
        Assert.Empty(snapshotRuntime.Summary.ReportingCoverage.UnreportedCdcCaptureIds);
        Assert.True(snapshotRuntime.Summary.HasFullCaptureCoverage);
    }

    [Fact]
    public async Task AddDataTracksExternalExecutionRuntimeRemediationAcrossResolvedCaptureBindings()
    {
        var timeProvider = new MutableTimeProvider(DateTimeOffset.Parse("2026-04-23T08:21:31Z", CultureInfo.InvariantCulture));
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(timeProvider);
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS"]));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new BoundMultiCaptureExecutionRuntimeCdcModule());
            engine.AddData(options =>
            {
                options.EnableExternalCdcRuntimeReporting = true;
                options.CdcExecutionRuntimes.Add(new CdcCaptureExecutionRuntimeOptions
                {
                    Id = "external-cdc-runtime",
                    DisplayName = "External CDC Runtime",
                    Description = "Represents an externally managed out-of-process CDC runner.",
                    ExecutionOwnership = "external-managed",
                    ExecutionTopology = "out-of-process-reporting",
                    ReporterLeaseSeconds = 120,
                    RejectConflictingReporterIds = false
                });
            });
        });

        using var provider = services.BuildServiceProvider();
        var reportSink = provider.GetRequiredService<ICdcCaptureExecutionRuntimeReportSink>();
        var runtimeCatalog = provider.GetRequiredService<ICdcCaptureExecutionRuntimeCatalog>();
        var snapshotProvider = provider.GetRequiredService<IRuntimeIntrospectionSnapshotProvider>();

        var initialRuntime = runtimeCatalog.GetById("external-cdc-runtime");
        Assert.NotNull(initialRuntime);
        Assert.Equal(["bound-multi-capture-cdc-a", "bound-multi-capture-cdc-b"], initialRuntime.CdcCaptureIds);
        Assert.Equal(CdcCaptureExecutionRuntimeReportingCoverageStates.Unreported, initialRuntime.Summary.ReportingCoverage.State);
        Assert.Equal(2, initialRuntime.Summary.ReportingCoverage.DeclaredCaptureCount);
        Assert.Equal(
            CdcCaptureExecutionRuntimeRemediationStates.Attention,
            initialRuntime.Summary.Remediation.State);
        Assert.Equal(
            [CdcCaptureExecutionRuntimeRemediationCategories.UnreportedCdcCaptures],
            initialRuntime.Summary.Remediation.CategoryIds);
        Assert.Equal(
            ["bound-multi-capture-cdc-a", "bound-multi-capture-cdc-b"],
            initialRuntime.Summary.Remediation.UnreportedCdcCaptureIds);
        Assert.Equal(
            ["bound-multi-capture-cdc-a", "bound-multi-capture-cdc-b"],
            initialRuntime.Summary.Remediation.AffectedCdcCaptureIds);
        Assert.True(initialRuntime.Summary.RequiresRemediation);
        Assert.False(initialRuntime.Summary.HasBlockingRemediation);
        Assert.Single(runtimeCatalog.GetByRemediationState(CdcCaptureExecutionRuntimeRemediationStates.Attention));
        Assert.Single(runtimeCatalog.GetByRemediationCategory(CdcCaptureExecutionRuntimeRemediationCategories.UnreportedCdcCaptures));

        await reportSink.ReportAsync(
            "external-cdc-runtime",
            [
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: "bound-multi-capture-cdc-a",
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-23T08:20:00Z", CultureInfo.InvariantCulture),
                    reportId: "bound-multi-report-a",
                    reporterId: "edge-agent-a")
            ]);

        var partiallyReportedRuntime = runtimeCatalog.GetById("external-cdc-runtime");
        Assert.NotNull(partiallyReportedRuntime);
        Assert.Equal(CdcCaptureExecutionRuntimeReportingCoverageStates.PartiallyReported, partiallyReportedRuntime.Summary.ReportingCoverage.State);
        Assert.Equal(
            CdcCaptureExecutionRuntimeRemediationStates.Attention,
            partiallyReportedRuntime.Summary.Remediation.State);
        Assert.Equal(
            [CdcCaptureExecutionRuntimeRemediationCategories.UnreportedCdcCaptures],
            partiallyReportedRuntime.Summary.Remediation.CategoryIds);
        Assert.Equal(["bound-multi-capture-cdc-b"], partiallyReportedRuntime.Summary.Remediation.UnreportedCdcCaptureIds);
        Assert.Equal(["bound-multi-capture-cdc-b"], partiallyReportedRuntime.Summary.Remediation.AffectedCdcCaptureIds);

        await reportSink.ReportAsync(
            "external-cdc-runtime",
            [
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: "bound-multi-capture-cdc-b",
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-23T08:20:30Z", CultureInfo.InvariantCulture),
                    reportId: "bound-multi-report-b",
                    reporterId: "edge-agent-a")
            ]);

        var fullyReportedRuntime = runtimeCatalog.GetById("external-cdc-runtime");
        Assert.NotNull(fullyReportedRuntime);
        Assert.Equal(CdcCaptureExecutionRuntimeReportingCoverageStates.FullyReported, fullyReportedRuntime.Summary.ReportingCoverage.State);
        Assert.Equal(
            CdcCaptureExecutionRuntimeRemediationStates.Ready,
            fullyReportedRuntime.Summary.Remediation.State);
        Assert.Empty(fullyReportedRuntime.Summary.Remediation.CategoryIds);
        Assert.Empty(fullyReportedRuntime.Summary.Remediation.AffectedCdcCaptureIds);
        Assert.False(fullyReportedRuntime.Summary.RequiresRemediation);
        Assert.False(fullyReportedRuntime.Summary.HasBlockingRemediation);
        Assert.Single(runtimeCatalog.GetByRemediationState(CdcCaptureExecutionRuntimeRemediationStates.Ready));
        Assert.Empty(runtimeCatalog.GetByRemediationCategory(CdcCaptureExecutionRuntimeRemediationCategories.UnreportedCdcCaptures));

        var snapshot = snapshotProvider.CreateSnapshot();
        var snapshotRuntime = snapshot.CdcCaptureExecutionRuntimes.Single(item => item.Id == "external-cdc-runtime");
        Assert.Equal(CdcCaptureExecutionRuntimeRemediationStates.Ready, snapshotRuntime.Summary.Remediation.State);
        Assert.Empty(snapshotRuntime.Summary.Remediation.CategoryIds);
        Assert.False(snapshotRuntime.Summary.RequiresRemediation);
    }

    [Fact]
    public async Task AddDataAggregatesExternalExecutionRuntimeRemediationCategoriesAcrossCurrentCaptureIssues()
    {
        var timeProvider = new MutableTimeProvider(DateTimeOffset.Parse("2026-04-23T09:02:31Z", CultureInfo.InvariantCulture));
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(timeProvider);
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS"]));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new BoundMultiCaptureExecutionRuntimeCdcModule());
            engine.AddData(options =>
            {
                options.EnableExternalCdcRuntimeReporting = true;
                options.CdcExecutionRuntimes.Add(new CdcCaptureExecutionRuntimeOptions
                {
                    Id = "external-cdc-runtime",
                    DisplayName = "External CDC Runtime",
                    Description = "Represents an externally managed out-of-process CDC runner.",
                    ExecutionOwnership = "external-managed",
                    ExecutionTopology = "out-of-process-reporting",
                    ObservationStaleAfterSeconds = 60,
                    ReporterLeaseSeconds = 300,
                    RejectConflictingReporterIds = false
                });
            });
        });

        using var provider = services.BuildServiceProvider();
        var reportSink = provider.GetRequiredService<ICdcCaptureExecutionRuntimeReportSink>();
        var runtimeCatalog = provider.GetRequiredService<ICdcCaptureExecutionRuntimeCatalog>();

        await reportSink.ReportAsync(
            "external-cdc-runtime",
            [
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: "bound-multi-capture-cdc-a",
                    outcome: CdcCaptureRuntimeOutcomes.Failed,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-23T09:00:00Z", CultureInfo.InvariantCulture),
                    reportId: "bound-multi-remediation-a",
                    reporterId: "edge-agent-a",
                    error: "Primary edge reporter failed to publish the latest checkpoint."),
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: "bound-multi-capture-cdc-b",
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-23T09:00:30Z", CultureInfo.InvariantCulture),
                    reportId: "bound-multi-remediation-b",
                    reporterId: "edge-agent-b")
            ]);

        var runtime = runtimeCatalog.GetById("external-cdc-runtime");
        Assert.NotNull(runtime);
        Assert.Equal(CdcCaptureExecutionRuntimeReportingCoverageStates.FullyReported, runtime.Summary.ReportingCoverage.State);
        Assert.Equal(CdcCaptureExecutionRuntimeRemediationStates.Blocked, runtime.Summary.Remediation.State);
        Assert.Equal(
            [
                CdcCaptureExecutionRuntimeRemediationCategories.FailedCdcCaptures,
                CdcCaptureExecutionRuntimeRemediationCategories.ReporterCoordinationIssues,
                CdcCaptureExecutionRuntimeRemediationCategories.StaleObservations
            ],
            runtime.Summary.Remediation.CategoryIds);
        Assert.Equal(["bound-multi-capture-cdc-a", "bound-multi-capture-cdc-b"], runtime.Summary.Remediation.AffectedCdcCaptureIds);
        Assert.Equal(["bound-multi-capture-cdc-a"], runtime.Summary.Remediation.FailedCdcCaptureIds);
        Assert.Equal(["bound-multi-capture-cdc-a", "bound-multi-capture-cdc-b"], runtime.Summary.Remediation.ReporterCoordinationIssueCdcCaptureIds);
        Assert.Equal(["bound-multi-capture-cdc-a", "bound-multi-capture-cdc-b"], runtime.Summary.Remediation.StaleCdcCaptureIds);
        Assert.True(runtime.Summary.RequiresRemediation);
        Assert.True(runtime.Summary.HasBlockingRemediation);
        Assert.Single(runtimeCatalog.GetByRemediationState(CdcCaptureExecutionRuntimeRemediationStates.Blocked));
        Assert.Single(runtimeCatalog.GetByRemediationCategory(CdcCaptureExecutionRuntimeRemediationCategories.FailedCdcCaptures));
        Assert.Single(runtimeCatalog.GetByRemediationCategory(CdcCaptureExecutionRuntimeRemediationCategories.ReporterCoordinationIssues));
        Assert.Single(runtimeCatalog.GetByRemediationCategory(CdcCaptureExecutionRuntimeRemediationCategories.StaleObservations));
    }

    [Fact]
    public async Task AddDataMarksExternalExecutionRuntimeAsConflictedWhenMultipleCaptureReportersHoldActiveLeases()
    {
        var timeProvider = new MutableTimeProvider(DateTimeOffset.Parse("2026-04-21T02:45:00Z", CultureInfo.InvariantCulture));
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(timeProvider);
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS"]));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new MultiCaptureExecutionRuntimeCdcModule());
            engine.AddData(options =>
            {
                options.EnableExternalCdcRuntimeReporting = true;
                options.CdcExecutionRuntimes.Add(new CdcCaptureExecutionRuntimeOptions
                {
                    Id = "external-cdc-runtime",
                    DisplayName = "External CDC Runtime",
                    Description = "Represents an externally managed out-of-process CDC runner.",
                    ExecutionOwnership = "external-managed",
                    ExecutionTopology = "out-of-process-reporting",
                    ReporterLeaseSeconds = 120,
                    RejectConflictingReporterIds = false
                });
                options.CdcExecutionRuntimes[0].CdcCaptureIds.Add("multi-capture-cdc-a");
                options.CdcExecutionRuntimes[0].CdcCaptureIds.Add("multi-capture-cdc-b");
            });
        });

        using var provider = services.BuildServiceProvider();
        var reportSink = provider.GetRequiredService<ICdcCaptureExecutionRuntimeReportSink>();
        var runtimeStateCatalog = provider.GetRequiredService<ICdcCaptureRuntimeStateCatalog>();
        var runtimeCatalog = provider.GetRequiredService<ICdcCaptureExecutionRuntimeCatalog>();

        await reportSink.ReportAsync(
            "external-cdc-runtime",
            [
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: "multi-capture-cdc-a",
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-21T02:44:00Z", CultureInfo.InvariantCulture),
                    reportId: "external-report-capture-a",
                    reporterId: "edge-agent-a"),
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: "multi-capture-cdc-b",
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-21T02:44:30Z", CultureInfo.InvariantCulture),
                    reportId: "external-report-capture-b",
                    reporterId: "edge-agent-b")
            ]);

        var states = runtimeStateCatalog.GetByExecutionRuntimeId("external-cdc-runtime");
        Assert.Equal(2, states.Count);
        Assert.All(states, state =>
        {
            Assert.Equal(CdcCaptureReporterCoordinationStates.Conflicted, state.ReporterCoordination.State);
            Assert.Equal(CdcCaptureReporterTakeoverStates.NotApplicable, state.ReporterCoordination.TakeoverState);
            Assert.Equal(CdcCaptureReporterCoordinationIssueReasons.MultipleActiveReporters, state.ReporterCoordination.DegradedReason);
            Assert.True(state.ReporterCoordination.IsDegraded);
            Assert.True(state.HasReporterCoordinationIssue);
            Assert.False(state.HasActiveReporterOwner);
            Assert.False(state.ReporterCoordination.HasStandbyReporters);
            Assert.False(state.ReporterCoordination.HasRejectedReporters);
        });

        var runtime = runtimeCatalog.GetById("external-cdc-runtime");
        Assert.NotNull(runtime);
        Assert.Null(runtime.Summary.ActiveReporterId);
        Assert.Null(runtime.Summary.ReporterLeaseExpiresAtUtc);
        Assert.Equal(CdcCaptureReporterCoordinationStates.Conflicted, runtime.Summary.ReporterCoordination.State);
        Assert.Equal(CdcCaptureReporterTakeoverStates.NotApplicable, runtime.Summary.ReporterCoordination.TakeoverState);
        Assert.Equal(CdcCaptureReporterCoordinationIssueReasons.MultipleActiveReporters, runtime.Summary.ReporterCoordination.DegradedReason);
        Assert.True(runtime.Summary.ReporterCoordination.IsDegraded);
        Assert.True(runtime.Summary.HasReporterCoordinationIssue);
        Assert.Equal(["edge-agent-a", "edge-agent-b"], runtime.Summary.ReporterCoordinationRollup.ActiveReporterIds);
        Assert.Empty(runtime.Summary.ReporterCoordinationRollup.StandbyReporterIds);
        Assert.Empty(runtime.Summary.ReporterCoordinationRollup.RejectedReporterIds);
        Assert.Equal(["multi-capture-cdc-a", "multi-capture-cdc-b"], runtime.Summary.ReporterCoordinationRollup.DegradedCdcCaptureIds);
        Assert.True(runtime.Summary.ReporterCoordinationRollup.HasDegradedCdcCaptures);
        Assert.Collection(
            runtime.Summary.ReporterCoordinationRollup.CoordinationStateBreakdown,
            breakdown =>
            {
                Assert.Equal(CdcCaptureReporterCoordinationStates.Conflicted, breakdown.Id);
                Assert.Equal(2, breakdown.Count);
            });
        Assert.Collection(
            runtime.Summary.ReporterCoordinationRollup.DegradedReasonBreakdown,
            breakdown =>
            {
                Assert.Equal(CdcCaptureReporterCoordinationIssueReasons.MultipleActiveReporters, breakdown.Id);
                Assert.Equal(2, breakdown.Count);
            });
        Assert.Collection(
            runtime.Summary.ReporterCoordination.ReporterParticipants,
            participant =>
            {
                Assert.Equal("edge-agent-a", participant.ReporterId);
                Assert.Equal(CdcCaptureReporterParticipantRoles.Active, participant.Role);
            },
            participant =>
            {
                Assert.Equal("edge-agent-b", participant.ReporterId);
                Assert.Equal(CdcCaptureReporterParticipantRoles.Active, participant.Role);
            });
    }

    [Fact]
    public async Task AddDataMarksExternalExecutionRuntimeReporterLeaseAsExpiredAfterLeaseWindow()
    {
        var timeProvider = new MutableTimeProvider(DateTimeOffset.Parse("2026-04-21T02:45:00Z", CultureInfo.InvariantCulture));
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(timeProvider);
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS"]));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new Phase8CatalogModule());
            engine.AddData(options =>
            {
                options.EnableExternalCdcRuntimeReporting = true;
                options.CdcExecutionRuntimes.Add(new CdcCaptureExecutionRuntimeOptions
                {
                    Id = "external-cdc-runtime",
                    DisplayName = "External CDC Runtime",
                    Description = "Represents an externally managed out-of-process CDC runner.",
                    ExecutionOwnership = "external-managed",
                    ExecutionTopology = "out-of-process-reporting",
                    ReporterLeaseSeconds = 120,
                    RejectConflictingReporterIds = true
                });
                options.CdcExecutionRuntimes[0].CdcCaptureIds.Add("tenant-profile-cdc");
            });
        });

        using var provider = services.BuildServiceProvider();
        var reportSink = provider.GetRequiredService<ICdcCaptureExecutionRuntimeReportSink>();
        var runtimeStateCatalog = provider.GetRequiredService<ICdcCaptureRuntimeStateCatalog>();
        var runtimeCatalog = provider.GetRequiredService<ICdcCaptureExecutionRuntimeCatalog>();

        await reportSink.ReportAsync(
            "external-cdc-runtime",
            [
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: "tenant-profile-cdc",
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-21T02:44:00Z", CultureInfo.InvariantCulture),
                    reportId: "external-report-reporter-a",
                    reporterId: "edge-agent-a")
            ]);

        timeProvider.Advance(TimeSpan.FromSeconds(61));

        var state = Assert.Single(runtimeStateCatalog.GetByExecutionRuntimeId("external-cdc-runtime"));
        Assert.Equal(CdcCaptureReporterCoordinationStates.LeaseExpired, state.ReporterCoordination.State);
        Assert.Equal("edge-agent-a", state.ReporterCoordination.PreviousReporterId);
        Assert.Equal(DateTimeOffset.Parse("2026-04-21T02:46:00Z", CultureInfo.InvariantCulture), state.ReporterCoordination.LeaseExpiredAtUtc);
        Assert.Equal(CdcCaptureReporterTakeoverStates.AwaitingTakeover, state.ReporterCoordination.TakeoverState);
        Assert.Equal(CdcCaptureReporterCoordinationIssueReasons.AwaitingTakeover, state.ReporterCoordination.DegradedReason);
        Assert.True(state.ReporterCoordination.IsDegraded);
        Assert.True(state.HasReporterCoordinationIssue);
        Assert.False(state.HasActiveReporterOwner);
        Assert.True(state.ReporterCoordination.HasStandbyReporters);
        Assert.False(state.ReporterCoordination.HasRejectedReporters);
        Assert.Collection(
            state.ReporterCoordination.ReporterParticipants,
            participant =>
            {
                Assert.Equal("edge-agent-a", participant.ReporterId);
                Assert.Equal(CdcCaptureReporterParticipantRoles.Standby, participant.Role);
                Assert.Equal(DateTimeOffset.Parse("2026-04-21T02:44:00Z", CultureInfo.InvariantCulture), participant.LastObservedAtUtc);
                Assert.Equal(DateTimeOffset.Parse("2026-04-21T02:46:00Z", CultureInfo.InvariantCulture), participant.LeaseExpiresAtUtc);
            });

        var runtime = runtimeCatalog.GetById("external-cdc-runtime");
        Assert.NotNull(runtime);
        Assert.Null(runtime.Summary.ActiveReporterId);
        Assert.Null(runtime.Summary.ReporterLeaseExpiresAtUtc);
        Assert.Equal(CdcCaptureReporterCoordinationStates.LeaseExpired, runtime.Summary.ReporterCoordination.State);
        Assert.Equal("edge-agent-a", runtime.Summary.ReporterCoordination.PreviousReporterId);
        Assert.Equal(DateTimeOffset.Parse("2026-04-21T02:46:00Z", CultureInfo.InvariantCulture), runtime.Summary.ReporterCoordination.LeaseExpiredAtUtc);
        Assert.Equal(CdcCaptureReporterTakeoverStates.AwaitingTakeover, runtime.Summary.ReporterCoordination.TakeoverState);
        Assert.Equal(CdcCaptureReporterCoordinationIssueReasons.AwaitingTakeover, runtime.Summary.ReporterCoordination.DegradedReason);
        Assert.True(runtime.Summary.ReporterCoordination.IsDegraded);
        Assert.True(runtime.Summary.HasReporterCoordinationIssue);
        Assert.Collection(
            runtime.Summary.ReporterCoordination.ReporterParticipants,
            participant =>
            {
                Assert.Equal("edge-agent-a", participant.ReporterId);
                Assert.Equal(CdcCaptureReporterParticipantRoles.Standby, participant.Role);
            });
    }

    [Fact]
    public async Task AddDataAllowsExternalExecutionRuntimeReporterTakeoverAfterLeaseExpires()
    {
        var timeProvider = new MutableTimeProvider(DateTimeOffset.Parse("2026-04-21T02:45:00Z", CultureInfo.InvariantCulture));
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(timeProvider);
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS"]));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new Phase8CatalogModule());
            engine.AddData(options =>
            {
                options.EnableExternalCdcRuntimeReporting = true;
                options.CdcExecutionRuntimes.Add(new CdcCaptureExecutionRuntimeOptions
                {
                    Id = "external-cdc-runtime",
                    DisplayName = "External CDC Runtime",
                    Description = "Represents an externally managed out-of-process CDC runner.",
                    ExecutionOwnership = "external-managed",
                    ExecutionTopology = "out-of-process-reporting",
                    ReporterLeaseSeconds = 120,
                    RejectConflictingReporterIds = true
                });
                options.CdcExecutionRuntimes[0].CdcCaptureIds.Add("tenant-profile-cdc");
            });
        });

        using var provider = services.BuildServiceProvider();
        var reportSink = provider.GetRequiredService<ICdcCaptureExecutionRuntimeReportSink>();
        var runtimeStateCatalog = provider.GetRequiredService<ICdcCaptureRuntimeStateCatalog>();
        var runtimeCatalog = provider.GetRequiredService<ICdcCaptureExecutionRuntimeCatalog>();

        await reportSink.ReportAsync(
            "external-cdc-runtime",
            [
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: "tenant-profile-cdc",
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-21T02:44:00Z", CultureInfo.InvariantCulture),
                    reportId: "external-report-reporter-a",
                    reporterId: "edge-agent-a")
            ]);

        timeProvider.Advance(TimeSpan.FromSeconds(61));

        await reportSink.ReportAsync(
            "external-cdc-runtime",
            [
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: "tenant-profile-cdc",
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-21T02:46:30Z", CultureInfo.InvariantCulture),
                    reportId: "external-report-reporter-b",
                    reporterId: "edge-agent-b")
            ]);

        var state = Assert.Single(runtimeStateCatalog.GetByExecutionRuntimeId("external-cdc-runtime"));
        Assert.Equal("edge-agent-b", state.LastReporterId);
        Assert.Equal(CdcCaptureReporterCoordinationStates.Active, state.ReporterCoordination.State);
        Assert.Equal("edge-agent-b", state.ReporterCoordination.ActiveReporterId);
        Assert.Equal(DateTimeOffset.Parse("2026-04-21T02:48:30Z", CultureInfo.InvariantCulture), state.ReporterCoordination.ActiveReporterLeaseExpiresAtUtc);
        Assert.Equal("edge-agent-a", state.ReporterCoordination.PreviousReporterId);
        Assert.Equal(DateTimeOffset.Parse("2026-04-21T02:46:00Z", CultureInfo.InvariantCulture), state.ReporterCoordination.LeaseExpiredAtUtc);
        Assert.Equal(DateTimeOffset.Parse("2026-04-21T02:46:30Z", CultureInfo.InvariantCulture), state.ReporterCoordination.LastTakeoverObservedAtUtc);
        Assert.Null(state.ReporterCoordination.LastConflictingReporterId);
        Assert.Equal(CdcCaptureReporterTakeoverStates.Completed, state.ReporterCoordination.TakeoverState);
        Assert.Equal(CdcCaptureReporterCoordinationIssueReasons.None, state.ReporterCoordination.DegradedReason);
        Assert.False(state.ReporterCoordination.IsDegraded);
        Assert.False(state.HasReporterCoordinationIssue);
        Assert.True(state.ReporterCoordination.HasCompletedTakeover);
        Assert.True(state.ReporterCoordination.HasStandbyReporters);
        Assert.False(state.ReporterCoordination.HasRejectedReporters);
        Assert.Collection(
            state.ReporterCoordination.ReporterParticipants,
            participant =>
            {
                Assert.Equal("edge-agent-b", participant.ReporterId);
                Assert.Equal(CdcCaptureReporterParticipantRoles.Active, participant.Role);
                Assert.Equal(DateTimeOffset.Parse("2026-04-21T02:46:30Z", CultureInfo.InvariantCulture), participant.LastObservedAtUtc);
                Assert.Equal(DateTimeOffset.Parse("2026-04-21T02:48:30Z", CultureInfo.InvariantCulture), participant.LeaseExpiresAtUtc);
            },
            participant =>
            {
                Assert.Equal("edge-agent-a", participant.ReporterId);
                Assert.Equal(CdcCaptureReporterParticipantRoles.Standby, participant.Role);
                Assert.Equal(DateTimeOffset.Parse("2026-04-21T02:44:00Z", CultureInfo.InvariantCulture), participant.LastObservedAtUtc);
                Assert.Equal(DateTimeOffset.Parse("2026-04-21T02:46:00Z", CultureInfo.InvariantCulture), participant.LeaseExpiresAtUtc);
            });

        var runtime = runtimeCatalog.GetById("external-cdc-runtime");
        Assert.NotNull(runtime);
        Assert.Equal("edge-agent-b", runtime.Summary.ActiveReporterId);
        Assert.Equal(DateTimeOffset.Parse("2026-04-21T02:48:30Z", CultureInfo.InvariantCulture), runtime.Summary.ReporterLeaseExpiresAtUtc);
        Assert.Equal(CdcCaptureReporterCoordinationStates.Active, runtime.Summary.ReporterCoordination.State);
        Assert.Equal("edge-agent-b", runtime.Summary.ReporterCoordination.ActiveReporterId);
        Assert.Equal("edge-agent-a", runtime.Summary.ReporterCoordination.PreviousReporterId);
        Assert.Equal(DateTimeOffset.Parse("2026-04-21T02:46:00Z", CultureInfo.InvariantCulture), runtime.Summary.ReporterCoordination.LeaseExpiredAtUtc);
        Assert.Equal(DateTimeOffset.Parse("2026-04-21T02:46:30Z", CultureInfo.InvariantCulture), runtime.Summary.ReporterCoordination.LastTakeoverObservedAtUtc);
        Assert.Equal(CdcCaptureReporterTakeoverStates.Completed, runtime.Summary.ReporterCoordination.TakeoverState);
        Assert.Equal(CdcCaptureReporterCoordinationIssueReasons.None, runtime.Summary.ReporterCoordination.DegradedReason);
        Assert.False(runtime.Summary.ReporterCoordination.IsDegraded);
        Assert.False(runtime.Summary.HasReporterCoordinationIssue);
        Assert.True(runtime.Summary.ReporterCoordination.HasCompletedTakeover);
        Assert.Equal(["edge-agent-b"], runtime.Summary.ReporterCoordinationRollup.ActiveReporterIds);
        Assert.Equal(["edge-agent-a"], runtime.Summary.ReporterCoordinationRollup.StandbyReporterIds);
        Assert.Empty(runtime.Summary.ReporterCoordinationRollup.RejectedReporterIds);
        Assert.Empty(runtime.Summary.ReporterCoordinationRollup.DegradedCdcCaptureIds);
        Assert.True(runtime.Summary.ReporterCoordinationRollup.HasStandbyReporters);
        Assert.False(runtime.Summary.ReporterCoordinationRollup.HasRejectedReporters);
        Assert.False(runtime.Summary.ReporterCoordinationRollup.HasDegradedCdcCaptures);
        Assert.Collection(
            runtime.Summary.ReporterCoordinationRollup.CoordinationStateBreakdown,
            breakdown =>
            {
                Assert.Equal(CdcCaptureReporterCoordinationStates.Active, breakdown.Id);
                Assert.Equal(1, breakdown.Count);
            });
        Assert.Collection(
            runtime.Summary.ReporterCoordinationRollup.DegradedReasonBreakdown,
            breakdown =>
            {
                Assert.Equal(CdcCaptureReporterCoordinationIssueReasons.None, breakdown.Id);
                Assert.Equal(1, breakdown.Count);
            });
        Assert.Collection(
            runtime.Summary.ReporterCoordination.ReporterParticipants,
            participant =>
            {
                Assert.Equal("edge-agent-b", participant.ReporterId);
                Assert.Equal(CdcCaptureReporterParticipantRoles.Active, participant.Role);
            },
            participant =>
            {
                Assert.Equal("edge-agent-a", participant.ReporterId);
                Assert.Equal(CdcCaptureReporterParticipantRoles.Standby, participant.Role);
            });
    }

    [Fact]
    public async Task AddDataClearsRejectedReporterConflictAfterActiveReporterReaffirmsLease()
    {
        var timeProvider = new MutableTimeProvider(DateTimeOffset.Parse("2026-04-21T02:45:00Z", CultureInfo.InvariantCulture));
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(timeProvider);
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS"]));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new Phase8CatalogModule());
            engine.AddData(options =>
            {
                options.EnableExternalCdcRuntimeReporting = true;
                options.CdcExecutionRuntimes.Add(new CdcCaptureExecutionRuntimeOptions
                {
                    Id = "external-cdc-runtime",
                    DisplayName = "External CDC Runtime",
                    Description = "Represents an externally managed out-of-process CDC runner.",
                    ExecutionOwnership = "external-managed",
                    ExecutionTopology = "out-of-process-reporting",
                    ReporterLeaseSeconds = 120,
                    RejectConflictingReporterIds = true
                });
                options.CdcExecutionRuntimes[0].CdcCaptureIds.Add("tenant-profile-cdc");
            });
        });

        using var provider = services.BuildServiceProvider();
        var reportSink = provider.GetRequiredService<ICdcCaptureExecutionRuntimeReportSink>();
        var runtimeStateCatalog = provider.GetRequiredService<ICdcCaptureRuntimeStateCatalog>();
        var runtimeCatalog = provider.GetRequiredService<ICdcCaptureExecutionRuntimeCatalog>();

        await reportSink.ReportAsync(
            "external-cdc-runtime",
            [
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: "tenant-profile-cdc",
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-21T02:44:00Z", CultureInfo.InvariantCulture),
                    reportId: "external-report-reporter-a",
                    reporterId: "edge-agent-a")
            ]);

        await Assert.ThrowsAsync<InvalidOperationException>(() => reportSink.ReportAsync(
            "external-cdc-runtime",
            [
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: "tenant-profile-cdc",
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-21T02:44:30Z", CultureInfo.InvariantCulture),
                    reportId: "external-report-reporter-b",
                    reporterId: "edge-agent-b")
            ]).AsTask());

        await reportSink.ReportAsync(
            "external-cdc-runtime",
            [
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: "tenant-profile-cdc",
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-21T02:45:00Z", CultureInfo.InvariantCulture),
                    reportId: "external-report-reporter-a-2",
                    reporterId: "edge-agent-a")
            ]);

        var state = Assert.Single(runtimeStateCatalog.GetByExecutionRuntimeId("external-cdc-runtime"));
        Assert.Equal("edge-agent-a", state.LastReporterId);
        Assert.Equal(CdcCaptureReporterCoordinationStates.Active, state.ReporterCoordination.State);
        Assert.Equal("edge-agent-a", state.ReporterCoordination.ActiveReporterId);
        Assert.Equal(DateTimeOffset.Parse("2026-04-21T02:47:00Z", CultureInfo.InvariantCulture), state.ReporterCoordination.ActiveReporterLeaseExpiresAtUtc);
        Assert.Null(state.ReporterCoordination.LastConflictingReporterId);
        Assert.Null(state.ReporterCoordination.LastConflictedAtUtc);
        Assert.Equal(CdcCaptureReporterTakeoverStates.NotRequired, state.ReporterCoordination.TakeoverState);
        Assert.Equal(CdcCaptureReporterCoordinationIssueReasons.None, state.ReporterCoordination.DegradedReason);
        Assert.Equal(1, state.ReporterCoordination.ParticipantCount);
        Assert.Equal(1, state.ReporterCoordination.ActiveReporterCount);
        Assert.Equal(0, state.ReporterCoordination.StandbyReporterCount);
        Assert.Equal(0, state.ReporterCoordination.RejectedReporterCount);
        Assert.False(state.ReporterCoordination.HasStandbyReporters);
        Assert.False(state.ReporterCoordination.HasRejectedReporters);
        Assert.False(state.ReporterCoordination.HasMultipleActiveReporters);
        Assert.False(state.ReporterCoordination.IsDegraded);
        Assert.False(state.HasReporterCoordinationIssue);
        Assert.Collection(
            state.ReporterCoordination.ReporterParticipants,
            participant =>
            {
                Assert.Equal("edge-agent-a", participant.ReporterId);
                Assert.Equal(CdcCaptureReporterParticipantRoles.Active, participant.Role);
                Assert.Equal(DateTimeOffset.Parse("2026-04-21T02:45:00Z", CultureInfo.InvariantCulture), participant.LastObservedAtUtc);
                Assert.Equal(DateTimeOffset.Parse("2026-04-21T02:47:00Z", CultureInfo.InvariantCulture), participant.LeaseExpiresAtUtc);
            });

        var runtime = runtimeCatalog.GetById("external-cdc-runtime");
        Assert.NotNull(runtime);
        Assert.Equal("edge-agent-a", runtime.Summary.ActiveReporterId);
        Assert.Equal(DateTimeOffset.Parse("2026-04-21T02:47:00Z", CultureInfo.InvariantCulture), runtime.Summary.ReporterLeaseExpiresAtUtc);
        Assert.Equal(CdcCaptureReporterCoordinationStates.Active, runtime.Summary.ReporterCoordination.State);
        Assert.Null(runtime.Summary.ReporterCoordination.LastConflictingReporterId);
        Assert.Null(runtime.Summary.ReporterCoordination.LastConflictedAtUtc);
        Assert.Equal(CdcCaptureReporterTakeoverStates.NotRequired, runtime.Summary.ReporterCoordination.TakeoverState);
        Assert.Equal(CdcCaptureReporterCoordinationIssueReasons.None, runtime.Summary.ReporterCoordination.DegradedReason);
        Assert.Equal(1, runtime.Summary.ReporterCoordination.ParticipantCount);
        Assert.Equal(1, runtime.Summary.ReporterCoordination.ActiveReporterCount);
        Assert.Equal(0, runtime.Summary.ReporterCoordination.StandbyReporterCount);
        Assert.Equal(0, runtime.Summary.ReporterCoordination.RejectedReporterCount);
        Assert.False(runtime.Summary.ReporterCoordination.HasRejectedReporters);
        Assert.False(runtime.Summary.ReporterCoordination.IsDegraded);
        Assert.False(runtime.Summary.HasReporterCoordinationIssue);
        Assert.Collection(
            runtime.Summary.ReporterCoordination.ReporterParticipants,
            participant =>
            {
                Assert.Equal("edge-agent-a", participant.ReporterId);
                Assert.Equal(CdcCaptureReporterParticipantRoles.Active, participant.Role);
            });
    }

    [Fact]
    public async Task AddDataRemovesHistoricalStandbyReporterAfterReplacementReporterReaffirmsLease()
    {
        var timeProvider = new MutableTimeProvider(DateTimeOffset.Parse("2026-04-21T02:45:00Z", CultureInfo.InvariantCulture));
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(timeProvider);
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS"]));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new Phase8CatalogModule());
            engine.AddData(options =>
            {
                options.EnableExternalCdcRuntimeReporting = true;
                options.CdcExecutionRuntimes.Add(new CdcCaptureExecutionRuntimeOptions
                {
                    Id = "external-cdc-runtime",
                    DisplayName = "External CDC Runtime",
                    Description = "Represents an externally managed out-of-process CDC runner.",
                    ExecutionOwnership = "external-managed",
                    ExecutionTopology = "out-of-process-reporting",
                    ReporterLeaseSeconds = 120,
                    RejectConflictingReporterIds = true
                });
                options.CdcExecutionRuntimes[0].CdcCaptureIds.Add("tenant-profile-cdc");
            });
        });

        using var provider = services.BuildServiceProvider();
        var reportSink = provider.GetRequiredService<ICdcCaptureExecutionRuntimeReportSink>();
        var runtimeStateCatalog = provider.GetRequiredService<ICdcCaptureRuntimeStateCatalog>();
        var runtimeCatalog = provider.GetRequiredService<ICdcCaptureExecutionRuntimeCatalog>();

        await reportSink.ReportAsync(
            "external-cdc-runtime",
            [
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: "tenant-profile-cdc",
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-21T02:44:00Z", CultureInfo.InvariantCulture),
                    reportId: "external-report-reporter-a",
                    reporterId: "edge-agent-a")
            ]);

        timeProvider.Advance(TimeSpan.FromSeconds(61));

        await reportSink.ReportAsync(
            "external-cdc-runtime",
            [
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: "tenant-profile-cdc",
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-21T02:46:30Z", CultureInfo.InvariantCulture),
                    reportId: "external-report-reporter-b",
                    reporterId: "edge-agent-b")
            ]);

        await reportSink.ReportAsync(
            "external-cdc-runtime",
            [
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: "tenant-profile-cdc",
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-21T02:47:00Z", CultureInfo.InvariantCulture),
                    reportId: "external-report-reporter-b-2",
                    reporterId: "edge-agent-b")
            ]);

        var state = Assert.Single(runtimeStateCatalog.GetByExecutionRuntimeId("external-cdc-runtime"));
        Assert.Equal("edge-agent-b", state.LastReporterId);
        Assert.Equal(CdcCaptureReporterCoordinationStates.Active, state.ReporterCoordination.State);
        Assert.Equal("edge-agent-b", state.ReporterCoordination.ActiveReporterId);
        Assert.Equal(DateTimeOffset.Parse("2026-04-21T02:49:00Z", CultureInfo.InvariantCulture), state.ReporterCoordination.ActiveReporterLeaseExpiresAtUtc);
        Assert.Equal("edge-agent-a", state.ReporterCoordination.PreviousReporterId);
        Assert.Equal(DateTimeOffset.Parse("2026-04-21T02:46:00Z", CultureInfo.InvariantCulture), state.ReporterCoordination.LeaseExpiredAtUtc);
        Assert.Equal(DateTimeOffset.Parse("2026-04-21T02:46:30Z", CultureInfo.InvariantCulture), state.ReporterCoordination.LastTakeoverObservedAtUtc);
        Assert.Equal(CdcCaptureReporterTakeoverStates.Completed, state.ReporterCoordination.TakeoverState);
        Assert.Equal(CdcCaptureReporterCoordinationIssueReasons.None, state.ReporterCoordination.DegradedReason);
        Assert.Equal(1, state.ReporterCoordination.ParticipantCount);
        Assert.Equal(1, state.ReporterCoordination.ActiveReporterCount);
        Assert.Equal(0, state.ReporterCoordination.StandbyReporterCount);
        Assert.Equal(0, state.ReporterCoordination.RejectedReporterCount);
        Assert.False(state.ReporterCoordination.HasStandbyReporters);
        Assert.False(state.ReporterCoordination.HasRejectedReporters);
        Assert.True(state.ReporterCoordination.HasCompletedTakeover);
        Assert.False(state.ReporterCoordination.IsDegraded);
        Assert.False(state.HasReporterCoordinationIssue);
        Assert.Collection(
            state.ReporterCoordination.ReporterParticipants,
            participant =>
            {
                Assert.Equal("edge-agent-b", participant.ReporterId);
                Assert.Equal(CdcCaptureReporterParticipantRoles.Active, participant.Role);
                Assert.Equal(DateTimeOffset.Parse("2026-04-21T02:47:00Z", CultureInfo.InvariantCulture), participant.LastObservedAtUtc);
                Assert.Equal(DateTimeOffset.Parse("2026-04-21T02:49:00Z", CultureInfo.InvariantCulture), participant.LeaseExpiresAtUtc);
            });

        var runtime = runtimeCatalog.GetById("external-cdc-runtime");
        Assert.NotNull(runtime);
        Assert.Equal("edge-agent-b", runtime.Summary.ActiveReporterId);
        Assert.Equal(DateTimeOffset.Parse("2026-04-21T02:49:00Z", CultureInfo.InvariantCulture), runtime.Summary.ReporterLeaseExpiresAtUtc);
        Assert.Equal(CdcCaptureReporterCoordinationStates.Active, runtime.Summary.ReporterCoordination.State);
        Assert.Equal("edge-agent-a", runtime.Summary.ReporterCoordination.PreviousReporterId);
        Assert.Equal(DateTimeOffset.Parse("2026-04-21T02:46:30Z", CultureInfo.InvariantCulture), runtime.Summary.ReporterCoordination.LastTakeoverObservedAtUtc);
        Assert.Equal(CdcCaptureReporterTakeoverStates.Completed, runtime.Summary.ReporterCoordination.TakeoverState);
        Assert.Equal(CdcCaptureReporterCoordinationIssueReasons.None, runtime.Summary.ReporterCoordination.DegradedReason);
        Assert.Equal(1, runtime.Summary.ReporterCoordination.ParticipantCount);
        Assert.Equal(1, runtime.Summary.ReporterCoordination.ActiveReporterCount);
        Assert.Equal(0, runtime.Summary.ReporterCoordination.StandbyReporterCount);
        Assert.Equal(0, runtime.Summary.ReporterCoordination.RejectedReporterCount);
        Assert.False(runtime.Summary.ReporterCoordination.HasStandbyReporters);
        Assert.True(runtime.Summary.ReporterCoordination.HasCompletedTakeover);
        Assert.False(runtime.Summary.ReporterCoordination.IsDegraded);
        Assert.False(runtime.Summary.HasReporterCoordinationIssue);
        Assert.Collection(
            runtime.Summary.ReporterCoordination.ReporterParticipants,
            participant =>
            {
                Assert.Equal("edge-agent-b", participant.ReporterId);
                Assert.Equal(CdcCaptureReporterParticipantRoles.Active, participant.Role);
            });
    }

    [Fact]
    public async Task AddDataRejectsExternalExecutionRuntimeReportsFromUndeclaredEdgeNodes()
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
                options.EnableExternalCdcRuntimeReporting = true;
                options.CdcExecutionRuntimes.Add(new CdcCaptureExecutionRuntimeOptions
                {
                    Id = "external-cdc-runtime",
                    DisplayName = "External CDC Runtime",
                    Description = "Represents an externally managed out-of-process CDC runner.",
                    ExecutionOwnership = "external-managed",
                    ExecutionTopology = "edge-reporting"
                });
                options.CdcExecutionRuntimes[0].CdcCaptureIds.Add("tenant-profile-cdc");
                options.CdcExecutionRuntimes[0].EdgeNodeIds.Add("edge-bkk-01");
            });
        });

        using var provider = services.BuildServiceProvider();
        var reportSink = provider.GetRequiredService<ICdcCaptureExecutionRuntimeReportSink>();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => reportSink.ReportAsync(
            "external-cdc-runtime",
            [
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: "tenant-profile-cdc",
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-21T02:55:00Z", CultureInfo.InvariantCulture),
                    reportId: "external-report-edge-001",
                    edgeNodeId: "edge-bkk-02")
            ]).AsTask());

        Assert.Contains("edge-bkk-02", exception.Message, StringComparison.Ordinal);
        Assert.Contains("edge-bkk-01", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AddDataExpiresExternalExecutionRuntimeObservationFreshnessAfterConfiguredWindow()
    {
        var timeProvider = new MutableTimeProvider(DateTimeOffset.Parse("2026-04-21T02:50:00Z", CultureInfo.InvariantCulture));
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(timeProvider);
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS"]));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new Phase8CatalogModule());
            engine.AddData(options =>
            {
                options.EnableExternalCdcRuntimeReporting = true;
                options.CdcExecutionRuntimes.Add(new CdcCaptureExecutionRuntimeOptions
                {
                    Id = "external-cdc-runtime",
                    DisplayName = "External CDC Runtime",
                    Description = "Represents an externally managed out-of-process CDC runner.",
                    ExecutionOwnership = "external-managed",
                    ExecutionTopology = "out-of-process-reporting",
                    ObservationStaleAfterSeconds = 60
                });
                options.CdcExecutionRuntimes[0].CdcCaptureIds.Add("tenant-profile-cdc");
            });
        });

        using var provider = services.BuildServiceProvider();
        var reportSink = provider.GetRequiredService<ICdcCaptureExecutionRuntimeReportSink>();
        var runtimeStateCatalog = provider.GetRequiredService<ICdcCaptureRuntimeStateCatalog>();
        var runtimeCatalog = provider.GetRequiredService<ICdcCaptureExecutionRuntimeCatalog>();

        await reportSink.ReportAsync(
            "external-cdc-runtime",
            [
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: "tenant-profile-cdc",
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-21T02:50:00Z", CultureInfo.InvariantCulture),
                    reportId: "external-report-freshness")
            ]);

        var freshState = Assert.Single(runtimeStateCatalog.GetByExecutionRuntimeId("external-cdc-runtime"));
        Assert.Equal(CdcCaptureFreshnessStates.Fresh, freshState.ObservationFreshness.State);
        Assert.Equal(DateTimeOffset.Parse("2026-04-21T02:51:00Z", CultureInfo.InvariantCulture), freshState.ObservationFreshness.FreshUntilUtc);

        var freshRuntime = runtimeCatalog.GetById("external-cdc-runtime");
        Assert.NotNull(freshRuntime);
        Assert.Equal(CdcCaptureFreshnessStates.Fresh, freshRuntime.Summary.ObservationFreshness.State);

        timeProvider.Advance(TimeSpan.FromSeconds(61));

        var staleState = Assert.Single(runtimeStateCatalog.GetByExecutionRuntimeId("external-cdc-runtime"));
        Assert.Equal(CdcCaptureFreshnessStates.Stale, staleState.ObservationFreshness.State);
        Assert.Equal(DateTimeOffset.Parse("2026-04-21T02:51:00Z", CultureInfo.InvariantCulture), staleState.ObservationFreshness.FreshUntilUtc);
        Assert.True(staleState.IsObservationStale);

        var staleRuntime = runtimeCatalog.GetById("external-cdc-runtime");
        Assert.NotNull(staleRuntime);
        Assert.Equal(CdcCaptureFreshnessStates.Stale, staleRuntime.Summary.ObservationFreshness.State);
        Assert.True(staleRuntime.Summary.HasStaleObservations);
    }

    [Fact]
    public async Task AddDataRejectsExecutionRuntimeObservationsWhenCaptureIsOwnedByAnotherRuntime()
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
                options.EnableExternalCdcRuntimeReporting = true;
                options.CdcExecutionRuntimes.Add(new CdcCaptureExecutionRuntimeOptions
                {
                    Id = "external-cdc-runtime",
                    DisplayName = "External CDC Runtime",
                    Description = "Represents an externally managed out-of-process CDC runner.",
                    ExecutionOwnership = "external-managed",
                    ExecutionTopology = "out-of-process-reporting"
                });
                options.CdcExecutionRuntimes[0].CdcCaptureIds.Add("tenant-profile-cdc");
            });
        });

        using var provider = services.BuildServiceProvider();
        var reportSink = provider.GetRequiredService<ICdcCaptureExecutionRuntimeReportSink>();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => reportSink.ReportAsync(
            "another-cdc-runtime",
            [
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: "tenant-profile-cdc",
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-21T02:20:00Z", CultureInfo.InvariantCulture))
            ]).AsTask());

        Assert.Contains("tenant-profile-cdc", exception.Message, StringComparison.Ordinal);
        Assert.Contains("external-cdc-runtime", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddDataRejectsAmbiguousClaimedCdcExecutionOwnership()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ICdcCaptureExecutionRuntimeContributor>(
            new TestExecutionRuntimeContributor(
                id: "external-cdc-runtime-a",
                displayName: "External CDC Runtime A",
                description: "Claims the tenant-profile CDC capture.",
                cdcCaptureIds: ["tenant-profile-cdc"]));
        services.AddSingleton<ICdcCaptureExecutionRuntimeContributor>(
            new TestExecutionRuntimeContributor(
                id: "external-cdc-runtime-b",
                displayName: "External CDC Runtime B",
                description: "Also claims the tenant-profile CDC capture.",
                cdcCaptureIds: ["tenant-profile-cdc"]));
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(blueprint: "ModularVerticalSlice"));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new Phase8CatalogModule());
            engine.AddData();
        });

        using var provider = services.BuildServiceProvider();
        var exception = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<ICdcCaptureCatalog>());
        Assert.Contains("tenant-profile-cdc", exception.Message, StringComparison.Ordinal);
        Assert.Contains("multiple execution runtimes", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AddDataSharedCdcExecutionPumpSkipsCapturesOwnedByAnotherRuntime()
    {
        var executionState = new TestCdcExecutionState();
        executionState.EnqueueResult(new CdcCaptureExecutionResult(
            messages:
            [
                new OutboxMessage(
                    id: "cdc-msg-skip-001",
                    channelId: "tenant-events",
                    messageType: "tenant.profile.changed",
                    payload: """{"tenantId":"tenant-001"}""",
                    occurredAtUtc: DateTimeOffset.Parse("2026-04-20T10:30:00Z", CultureInfo.InvariantCulture))
            ]));

        var services = new ServiceCollection();
        services.AddSingleton(executionState);
        services.AddSingleton<ICdcCaptureExecutionRuntimeContributor>(
            new TestExecutionRuntimeContributor(
                id: "external-cdc-runtime",
                displayName: "External CDC Runtime",
                description: "Owns the tenant-profile capture outside the shared host loop.",
                executionOwnership: "external-managed",
                cdcCaptureIds: ["tenant-profile-cdc"]));
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
        var captureCatalog = provider.GetRequiredService<ICdcCaptureCatalog>();
        var runtimeCatalog = provider.GetRequiredService<ICdcCaptureExecutionRuntimeCatalog>();
        var hostedService = Assert.Single(provider.GetServices<IHostedService>());
        await hostedService.StartAsync(CancellationToken.None);

        var cdcCapture = captureCatalog.GetById("tenant-profile-cdc");
        Assert.NotNull(cdcCapture);
        Assert.Equal("external-cdc-runtime", cdcCapture.ExecutionBinding.EffectiveExecutionRuntimeId);
        Assert.Equal("runtime-claim", cdcCapture.ExecutionBinding.ResolutionMode);

        var sharedRuntime = runtimeCatalog.GetById("data-cdc-capture-pump");
        Assert.NotNull(sharedRuntime);
        Assert.Empty(sharedRuntime.CdcCaptureIds);
        var externalRuntime = runtimeCatalog.GetById("external-cdc-runtime");
        Assert.NotNull(externalRuntime);
        Assert.Equal(["tenant-profile-cdc"], externalRuntime.CdcCaptureIds);

        using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => executionState.WaitForCaptureInvocationAsync(timeout.Token));

        var state = provider.GetRequiredService<ICdcCaptureRuntimeStateCatalog>().GetById("tenant-profile-cdc");
        Assert.NotNull(state);
        Assert.Null(state.LastOutcome);
        Assert.Equal(0, state.TotalReports);
        Assert.Equal("external-cdc-runtime", state.ExecutionBinding.EffectiveExecutionRuntimeId);
        Assert.Empty(executionState.StagedMessages);

        await hostedService.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task AddDataSharedCdcExecutionPumpSkipsCapturesOwnedByConfiguredExecutionRuntime()
    {
        var executionState = new TestCdcExecutionState();
        executionState.EnqueueResult(new CdcCaptureExecutionResult(
            messages:
            [
                new OutboxMessage(
                    id: "cdc-msg-configured-skip-001",
                    channelId: "tenant-events",
                    messageType: "tenant.profile.changed",
                    payload: """{"tenantId":"tenant-001"}""",
                    occurredAtUtc: DateTimeOffset.Parse("2026-04-20T10:30:00Z", CultureInfo.InvariantCulture))
            ]));

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
                options.CdcExecutionRuntimes.Add(new CdcCaptureExecutionRuntimeOptions
                {
                    Id = "external-cdc-runtime",
                    DisplayName = "Configured External CDC Runtime",
                    Description = "Owns the tenant-profile capture outside the shared host loop through host configuration.",
                    ExecutionOwnership = "external-managed",
                    ExecutionTopology = "provider-native",
                    AcknowledgementMode = "provider-native"
                });
                options.CdcExecutionRuntimes[0].CdcCaptureIds.Add("tenant-profile-cdc");
            });
        });

        using var provider = services.BuildServiceProvider();
        var captureCatalog = provider.GetRequiredService<ICdcCaptureCatalog>();
        var runtimeCatalog = provider.GetRequiredService<ICdcCaptureExecutionRuntimeCatalog>();
        var hostedService = Assert.Single(provider.GetServices<IHostedService>());
        await hostedService.StartAsync(CancellationToken.None);

        var cdcCapture = captureCatalog.GetById("tenant-profile-cdc");
        Assert.NotNull(cdcCapture);
        Assert.Equal("external-cdc-runtime", cdcCapture.ExecutionBinding.EffectiveExecutionRuntimeId);
        Assert.Equal("external-managed", cdcCapture.ExecutionBinding.ExecutionOwnership);
        Assert.Equal("provider-native", cdcCapture.ExecutionBinding.ExecutionTopology);
        Assert.Equal("runtime-claim", cdcCapture.ExecutionBinding.ResolutionMode);

        var sharedRuntime = runtimeCatalog.GetById("data-cdc-capture-pump");
        Assert.NotNull(sharedRuntime);
        Assert.Empty(sharedRuntime.CdcCaptureIds);
        var externalRuntime = runtimeCatalog.GetById("external-cdc-runtime");
        Assert.NotNull(externalRuntime);
        Assert.Equal("external-managed", externalRuntime.ExecutionOwnership);
        Assert.Equal("provider-native", externalRuntime.ExecutionTopology);
        Assert.Equal(["tenant-profile-cdc"], externalRuntime.CdcCaptureIds);

        using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => executionState.WaitForCaptureInvocationAsync(timeout.Token));

        var state = Assert.Single(provider.GetRequiredService<ICdcCaptureRuntimeStateCatalog>().GetByExecutionRuntimeId("external-cdc-runtime"));
        Assert.Equal("tenant-profile-cdc", state.CdcCaptureId);
        Assert.Null(state.LastOutcome);
        Assert.Equal(0, state.TotalReports);
        Assert.Equal("external-cdc-runtime", state.ExecutionBinding.EffectiveExecutionRuntimeId);
        Assert.Equal("provider-native", state.ExecutionBinding.ExecutionTopology);
        Assert.Empty(executionState.StagedMessages);

        await hostedService.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task AddDataSharedCdcExecutionPumpAcknowledgesDurableProgressAfterSuccessfulStaging()
    {
        var executionState = new TestCdcExecutionState();
        executionState.EnqueueResult(new CdcCaptureExecutionResult(
            messages:
            [
                new OutboxMessage(
                    id: "cdc-msg-010",
                    channelId: "tenant-events",
                    messageType: "tenant.profile.changed",
                    payload: """{"tenantId":"tenant-010"}""",
                    occurredAtUtc: DateTimeOffset.Parse("2026-04-20T11:00:00Z", CultureInfo.InvariantCulture))
            ],
            changeId: "lsn-0010",
            checkpoint: "0/16B6D10",
            freshness: new CdcCaptureFreshnessStatus(
                CdcCaptureFreshnessStates.Fresh,
                DateTimeOffset.Parse("2026-04-20T11:05:00Z", CultureInfo.InvariantCulture),
                "The capture is still within the expected freshness window."),
            lag: new CdcCaptureLagStatus(
                CdcCaptureLagStates.Current,
                pendingChangeCount: 0,
                description: "The capture is caught up with the source stream.")));

        var services = new ServiceCollection();
        services.AddSingleton(executionState);
        services.AddScoped<ICdcCapture, TestAcknowledgingCdcCapture>();
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
        await executionState.WaitForAcknowledgementAsync(timeout.Token);

        var acknowledgement = Assert.Single(executionState.Acknowledgements);
        Assert.Equal("tenant-profile-cdc", acknowledgement.CdcCaptureId);
        Assert.Equal("tenant-event-outbox", acknowledgement.OutboxId);
        Assert.Equal(1, acknowledgement.CapturedChangeCount);
        Assert.Equal(1, acknowledgement.StagedMessageCount);
        Assert.Equal("lsn-0010", acknowledgement.ChangeId);
        Assert.Equal("0/16B6D10", acknowledgement.Checkpoint);
        Assert.Single(acknowledgement.Messages);

        var catalog = provider.GetRequiredService<ICdcCaptureRuntimeStateCatalog>();
        var state = catalog.GetById("tenant-profile-cdc");
        Assert.NotNull(state);
        Assert.Equal(CdcCaptureRuntimeOutcomes.Captured, state.LastOutcome);
        Assert.Equal("lsn-0010", state.LastChangeId);
        Assert.Equal("0/16B6D10", state.LastCheckpoint);
        Assert.Equal("performed", state.Metadata["acknowledgement"]);
        Assert.EndsWith(nameof(TestAcknowledgingCdcCapture), state.Metadata["acknowledgerServiceType"], StringComparison.Ordinal);

        await hostedService.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task AddDataSharedCdcExecutionPumpDoesNotAcknowledgeProgressWhenOutboxStagingFails()
    {
        var executionState = new TestCdcExecutionState
        {
            ThrowOnEnqueue = true
        };
        executionState.EnqueueResult(new CdcCaptureExecutionResult(
            messages:
            [
                new OutboxMessage(
                    id: "cdc-msg-020",
                    channelId: "tenant-events",
                    messageType: "tenant.profile.changed",
                    payload: """{"tenantId":"tenant-020"}""",
                    occurredAtUtc: DateTimeOffset.Parse("2026-04-20T11:10:00Z", CultureInfo.InvariantCulture))
            ],
            changeId: "lsn-0020",
            checkpoint: "0/16B6D20"));

        var services = new ServiceCollection();
        services.AddSingleton(executionState);
        services.AddScoped<ICdcCapture, TestAcknowledgingCdcCapture>();
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
        await executionState.WaitForEnqueueAttemptAsync(timeout.Token);

        var catalog = provider.GetRequiredService<ICdcCaptureRuntimeStateCatalog>();
        var state = await WaitForStateAsync(catalog, CdcCaptureRuntimeOutcomes.Failed);
        Assert.Equal(CdcCaptureRuntimeOutcomes.Failed, state.LastOutcome);
        Assert.Equal("outbox-stage", state.Metadata["failureKind"]);
        Assert.Equal(0, executionState.AcknowledgementAttemptCount);
        Assert.Empty(executionState.Acknowledgements);

        await hostedService.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task AddDataSharedCdcExecutionPumpKeepsCheckpointPendingWhenAcknowledgementFails()
    {
        var executionState = new TestCdcExecutionState
        {
            ThrowOnAcknowledge = true
        };
        executionState.EnqueueResult(new CdcCaptureExecutionResult(
            messages:
            [
                new OutboxMessage(
                    id: "cdc-msg-030",
                    channelId: "tenant-events",
                    messageType: "tenant.profile.changed",
                    payload: """{"tenantId":"tenant-030"}""",
                    occurredAtUtc: DateTimeOffset.Parse("2026-04-20T11:20:00Z", CultureInfo.InvariantCulture))
            ],
            changeId: "lsn-0030",
            checkpoint: "0/16B6D30",
            publication: new CdcCapturePublicationStatus(
                CdcCapturePublicationStates.PendingPublication,
                pendingPublicationCount: 1,
                description: "One publication is waiting for downstream dispatch.")));

        var services = new ServiceCollection();
        services.AddSingleton(executionState);
        services.AddScoped<ICdcCapture, TestAcknowledgingCdcCapture>();
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
        await executionState.WaitForAcknowledgementAttemptAsync(timeout.Token);

        var catalog = provider.GetRequiredService<ICdcCaptureRuntimeStateCatalog>();
        var state = await WaitForStateAsync(catalog, CdcCaptureRuntimeOutcomes.Failed);
        Assert.Equal(CdcCaptureRuntimeOutcomes.Failed, state.LastOutcome);
        Assert.Null(state.LastChangeId);
        Assert.Null(state.LastCheckpoint);
        Assert.Equal(1, state.LastProducedMessageCount);
        Assert.Equal(1, state.TotalProducedMessageCount);
        Assert.Equal(CdcCapturePublicationStates.CaptureFailed, state.Publication.State);
        Assert.Equal(1, state.Publication.PendingPublicationCount);
        Assert.Equal("acknowledgement", state.Metadata["failureKind"]);
        Assert.Equal("failed", state.Metadata["acknowledgement"]);
        Assert.Equal("lsn-0030", state.Metadata["pendingChangeId"]);
        Assert.Equal("0/16B6D30", state.Metadata["pendingCheckpoint"]);
        Assert.Equal(1, executionState.AcknowledgementAttemptCount);
        Assert.Empty(executionState.Acknowledgements);
        Assert.Single(executionState.StagedMessages);

        await hostedService.StopAsync(CancellationToken.None);
    }

    private static async Task<CdcCaptureRuntimeState> WaitForStateAsync(
        ICdcCaptureRuntimeStateCatalog catalog,
        string expectedOutcome)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedOutcome);

        for (var attempt = 0; attempt < 50; attempt++)
        {
            var state = catalog.GetById("tenant-profile-cdc");
            if (state is not null &&
                string.Equals(state.LastOutcome, expectedOutcome, StringComparison.OrdinalIgnoreCase))
            {
                return state;
            }

            await Task.Delay(20);
        }

        var finalState = catalog.GetById("tenant-profile-cdc");
        Assert.NotNull(finalState);
        Assert.Equal(expectedOutcome, finalState.LastOutcome);
        return finalState;
    }

    private sealed class MutableTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset utcNow = now;

        public override DateTimeOffset GetUtcNow()
        {
            return utcNow;
        }

        public void Advance(TimeSpan duration)
        {
            utcNow = utcNow.Add(duration);
        }
    }

    private sealed class RequestedExecutionBindingCdcModule : ModuleBase, ICdcCaptureContributor, IOutboxContributor
    {
        private static readonly ModuleDescriptor DescriptorInstance = new(
            id: "requested-execution-binding-cdc",
            displayName: "Requested Execution Binding CDC",
            description: "Contributes a CDC capture with an authored execution-runtime binding.",
            version: "1.0.0",
            tags: ["cdc", "execution-binding"]);

        public override ModuleDescriptor Descriptor => DescriptorInstance;

        public override void RegisterCapabilities(Cephalon.Abstractions.Capabilities.ICapabilityRegistry capabilities)
        {
        }

        public void RegisterCdcCaptures(ICdcCaptureRegistry cdcCaptures)
        {
            cdcCaptures.Add(new CdcCaptureDescriptor(
                id: "requested-cdc",
                displayName: "Requested CDC",
                description: "Binds to an external execution runtime through authored ownership.",
                sourceModuleId: Descriptor.Id,
                provider: "postgresql",
                sourceId: "requested-db",
                outboxId: "requested-outbox",
                executionBinding: new CdcCaptureExecutionBindingDescriptor(
                    cdcCaptureId: "requested-cdc",
                    authoredExecutionRuntimeId: "external-cdc-runtime")));
        }

        public void RegisterOutboxes(IOutboxRegistry outboxes)
        {
            outboxes.Add(new OutboxDescriptor(
                id: "requested-outbox",
                displayName: "Requested Outbox",
                description: "Provides the outbox required by the requested CDC capture.",
                sourceModuleId: Descriptor.Id,
                provider: "relational"));
        }
    }

    private sealed class MultiCaptureExecutionRuntimeCdcModule : ModuleBase, ICdcCaptureContributor, IOutboxContributor
    {
        private static readonly ModuleDescriptor DescriptorInstance = new(
            id: "multi-capture-execution-runtime-cdc",
            displayName: "Multi-Capture Execution Runtime CDC",
            description: "Contributes two CDC captures so runtime-level reporter ambiguity can be exercised across multiple captures.",
            version: "1.0.0",
            tags: ["cdc", "execution-runtime", "multi-capture"]);

        public override ModuleDescriptor Descriptor => DescriptorInstance;

        public override void RegisterCapabilities(Cephalon.Abstractions.Capabilities.ICapabilityRegistry capabilities)
        {
        }

        public void RegisterCdcCaptures(ICdcCaptureRegistry cdcCaptures)
        {
            cdcCaptures.Add(new CdcCaptureDescriptor(
                id: "multi-capture-cdc-a",
                displayName: "Multi Capture CDC A",
                description: "First CDC capture used to exercise runtime-level reporter ambiguity.",
                sourceModuleId: Descriptor.Id,
                provider: "postgresql",
                sourceId: "multi-capture-db-a",
                outboxId: "multi-capture-outbox-a",
                resourceIds: ["public.multi_capture_a"]));
            cdcCaptures.Add(new CdcCaptureDescriptor(
                id: "multi-capture-cdc-b",
                displayName: "Multi Capture CDC B",
                description: "Second CDC capture used to exercise runtime-level reporter ambiguity.",
                sourceModuleId: Descriptor.Id,
                provider: "postgresql",
                sourceId: "multi-capture-db-b",
                outboxId: "multi-capture-outbox-b",
                resourceIds: ["public.multi_capture_b"]));
        }

        public void RegisterOutboxes(IOutboxRegistry outboxes)
        {
            outboxes.Add(new OutboxDescriptor(
                id: "multi-capture-outbox-a",
                displayName: "Multi Capture Outbox A",
                description: "Outbox for the first multi-capture CDC stream.",
                sourceModuleId: Descriptor.Id,
                provider: "relational"));
            outboxes.Add(new OutboxDescriptor(
                id: "multi-capture-outbox-b",
                displayName: "Multi Capture Outbox B",
                description: "Outbox for the second multi-capture CDC stream.",
                sourceModuleId: Descriptor.Id,
                provider: "relational"));
        }
    }

    private sealed class BoundMultiCaptureExecutionRuntimeCdcModule : ModuleBase, ICdcCaptureContributor, IOutboxContributor
    {
        private static readonly ModuleDescriptor DescriptorInstance = new(
            id: "bound-multi-capture-execution-runtime-cdc",
            displayName: "Bound Multi-Capture Execution Runtime CDC",
            description: "Contributes two CDC captures that both bind to the same external execution runtime.",
            version: "1.0.0",
            tags: ["cdc", "execution-runtime", "multi-capture", "bound"]);

        public override ModuleDescriptor Descriptor => DescriptorInstance;

        public override void RegisterCapabilities(Cephalon.Abstractions.Capabilities.ICapabilityRegistry capabilities)
        {
        }

        public void RegisterCdcCaptures(ICdcCaptureRegistry cdcCaptures)
        {
            cdcCaptures.Add(new CdcCaptureDescriptor(
                id: "bound-multi-capture-cdc-a",
                displayName: "Bound Multi Capture CDC A",
                description: "First CDC capture used to exercise resolved execution-runtime ownership.",
                sourceModuleId: Descriptor.Id,
                provider: "postgresql",
                sourceId: "bound-multi-capture-db-a",
                outboxId: "bound-multi-capture-outbox-a",
                executionBinding: new CdcCaptureExecutionBindingDescriptor(
                    cdcCaptureId: "bound-multi-capture-cdc-a",
                    authoredExecutionRuntimeId: "external-cdc-runtime"),
                resourceIds: ["public.bound_multi_capture_a"]));
            cdcCaptures.Add(new CdcCaptureDescriptor(
                id: "bound-multi-capture-cdc-b",
                displayName: "Bound Multi Capture CDC B",
                description: "Second CDC capture used to exercise resolved execution-runtime ownership.",
                sourceModuleId: Descriptor.Id,
                provider: "postgresql",
                sourceId: "bound-multi-capture-db-b",
                outboxId: "bound-multi-capture-outbox-b",
                executionBinding: new CdcCaptureExecutionBindingDescriptor(
                    cdcCaptureId: "bound-multi-capture-cdc-b",
                    authoredExecutionRuntimeId: "external-cdc-runtime"),
                resourceIds: ["public.bound_multi_capture_b"]));
        }

        public void RegisterOutboxes(IOutboxRegistry outboxes)
        {
            outboxes.Add(new OutboxDescriptor(
                id: "bound-multi-capture-outbox-a",
                displayName: "Bound Multi Capture Outbox A",
                description: "Outbox for the first bound multi-capture CDC stream.",
                sourceModuleId: Descriptor.Id,
                provider: "relational"));
            outboxes.Add(new OutboxDescriptor(
                id: "bound-multi-capture-outbox-b",
                displayName: "Bound Multi Capture Outbox B",
                description: "Outbox for the second bound multi-capture CDC stream.",
                sourceModuleId: Descriptor.Id,
                provider: "relational"));
        }
    }

    private sealed class TestExecutionRuntimeContributor(
        string id,
        string displayName,
        string description,
        string executionOwnership = "runtime-managed",
        string executionTopology = "external-runtime",
        IReadOnlyList<string>? cdcCaptureIds = null) : ICdcCaptureExecutionRuntimeContributor
    {
        public void RegisterExecutionRuntimes(ICdcCaptureExecutionRuntimeRegistry executionRuntimes)
        {
            ArgumentNullException.ThrowIfNull(executionRuntimes);

            executionRuntimes.Add(new CdcCaptureExecutionRuntimeDescriptor(
                id: id,
                displayName: displayName,
                description: description,
                executionOwnership: executionOwnership,
                executionTopology: executionTopology,
                cdcCaptureIds: cdcCaptureIds));
        }
    }
}
