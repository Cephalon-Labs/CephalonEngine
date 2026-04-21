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
