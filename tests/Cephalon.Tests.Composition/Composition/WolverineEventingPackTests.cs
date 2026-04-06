using Cephalon.Data.EntityFramework.Registration;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Eventing.Registration;
using Cephalon.Eventing.Services;
using Cephalon.Eventing.Wolverine.Configuration;
using Cephalon.Eventing.Wolverine.Registration;
using Cephalon.Eventing.Wolverine.Services;
using Cephalon.Engine.Diagnostics;
using Cephalon.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Wolverine;

namespace Cephalon.Tests.Composition;

public sealed class WolverineEventingPackTests
{
    [Fact]
    public async Task AddWolverineEventingRegistersOfficialAdapterCapabilityAndRuntimeSurface()
    {
        var databaseName = $"cephalon-eventing-wolverine-{Guid.NewGuid():N}";
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS", "Outbox"],
                technologies: ["EventDrivenIntegration"],
                transports: ["RestApi"],
                data: new DataSettings(
                    provider: "EntityFramework",
                    outboxEnabled: true),
                messaging: new MessagingSettings(provider: "Wolverine")));
            engine.AddModule(new PlatformTestModule());
            engine.AddEventing(options =>
            {
                options.Channels.Add(new EventChannelDescriptor(
                    id: "catalog-events",
                    displayName: "Catalog Events",
                    description: "Catalog integration events."));
            });
            engine.AddEntityFrameworkData<OutboxCatalogDbContext>(
                options => options.UseInMemoryDatabase(databaseName),
                configure: options => options.RegisterOutbox = true);
            engine.AddWolverineEventing();
        });

        await using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<global::Cephalon.Engine.Runtime.IRuntime>();
        var technologyCatalog = provider.GetRequiredService<global::Cephalon.Abstractions.Technologies.ITechnologyRuntimeCatalog>();
        var diagnosticsCatalog = provider.GetRequiredService<IRuntimeDiagnosticsCatalog>();
        var messageBus = provider.GetService<IMessageBus>();

        Assert.NotNull(messageBus);
        var eventingSurfaces = technologyCatalog.GetByTechnology("event-driven-integration");
        var adapterSurface = Assert.Single(eventingSurfaces, surface => surface.SurfaceId == "wolverine-adapter");
        var adapterEntry = Assert.Single(adapterSurface.Entries, entry => entry.Id == "wolverine-eventing");

        Assert.Equal("configured", adapterEntry.Metadata["hostWiring"]);
        Assert.Equal("consumer-managed", adapterEntry.Metadata["dispatchBridge"]);
        Assert.Equal("available", adapterEntry.Metadata["dispatchStore"]);
        Assert.Contains("EntityFrameworkEventDispatchStore", adapterEntry.Metadata["dispatchStoreTypes"], StringComparison.Ordinal);
        Assert.Empty(diagnosticsCatalog.GetBySource("Cephalon.Eventing.Wolverine"));
        Assert.Contains(
            runtime.Manifest.Capabilities,
            capability => capability.Key == "eventing.wolverine" &&
                capability.Metadata["adapter"] == "wolverine" &&
                capability.Metadata["dispatchStore"] == "available");
    }

    [Fact]
    public async Task AddWolverineEventingCanExposeManagedDispatchLoopThroughHostedExecutionAndRuntimeSurface()
    {
        var databaseName = $"cephalon-eventing-wolverine-managed-{Guid.NewGuid():N}";
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "Microservice",
                patterns: ["CQRS", "Outbox"],
                technologies: ["EventDrivenIntegration"],
                transports: ["RestApi"],
                data: new DataSettings(
                    provider: "EntityFramework",
                    outboxEnabled: true),
                messaging: new MessagingSettings(provider: "Wolverine")));
            engine.AddModule(new PlatformTestModule());
            engine.AddEventing(options =>
            {
                options.Channels.Add(new EventChannelDescriptor(
                    id: "catalog-events",
                    displayName: "Catalog Events",
                    description: "Catalog integration events."));
            });
            engine.AddEntityFrameworkData<OutboxCatalogDbContext>(
                options => options.UseInMemoryDatabase(databaseName),
                configure: options => options.RegisterOutbox = true);
            engine.AddWolverineEventing(options =>
            {
                options.EnableDispatchLoop = true;
                options.DispatchBatchSize = 7;
                options.DispatchPollingIntervalSeconds = 3;
                options.RetryDelaySeconds = 45;
            });
        });

        await using var provider = services.BuildServiceProvider();
        var diagnosticsCatalog = provider.GetRequiredService<IRuntimeDiagnosticsCatalog>();
        var hostedExecutions = provider.GetRequiredService<global::Cephalon.Abstractions.Execution.IHostedExecutionRuntimeCatalog>();
        var technologyCatalog = provider.GetRequiredService<global::Cephalon.Abstractions.Technologies.ITechnologyRuntimeCatalog>();
        var hostedServices = provider.GetServices<IHostedService>();
        var eventingSurfaces = technologyCatalog.GetByTechnology("event-driven-integration");
        var hostedExecution = Assert.Single(hostedExecutions.HostedExecutions, execution => execution.Id == WolverineEventingRuntimeIds.HostedExecutionId);
        var adapterSurface = Assert.Single(
            eventingSurfaces,
            surface => surface.SurfaceId == "wolverine-adapter");
        var dispatchSurface = Assert.Single(eventingSurfaces, surface => surface.SurfaceId == "event-dispatches");
        var adapterEntry = Assert.Single(adapterSurface.Entries, entry => entry.Id == "wolverine-eventing");
        var dispatchEntry = Assert.Single(dispatchSurface.Entries, entry => entry.Id == "entity-framework-outbox");

        Assert.Equal("wolverine-managed", adapterEntry.Metadata["dispatchBridge"]);
        Assert.Equal("enabled", adapterEntry.Metadata["dispatchLoop"]);
        Assert.Equal("7", adapterEntry.Metadata["dispatchBatchSize"]);
        Assert.Equal("3", adapterEntry.Metadata["dispatchPollingIntervalSeconds"]);
        Assert.Equal("45", adapterEntry.Metadata["retryDelaySeconds"]);
        Assert.Equal("0", adapterEntry.Metadata["reportedTotalCount"]);
        Assert.Equal("0", adapterEntry.Metadata["reportedRetryPendingCount"]);
        Assert.Equal("not-reported", adapterEntry.Metadata["lastOutcome"]);
        Assert.Equal(WolverineEventingRuntimeIds.HostedExecutionId, adapterEntry.Metadata["hostedExecutionId"]);
        Assert.Equal("1", dispatchEntry.Metadata["dispatchRuntimeCount"]);
        Assert.Equal(WolverineEventingRuntimeIds.DispatchRuntimeId, dispatchEntry.Metadata["dispatchRuntimeIds"]);
        Assert.Equal("wolverine", dispatchEntry.Metadata[$"dispatchRuntime.{WolverineEventingRuntimeIds.DispatchRuntimeId}.adapter"]);
        Assert.Equal("wolverine-managed", dispatchEntry.Metadata[$"dispatchRuntime.{WolverineEventingRuntimeIds.DispatchRuntimeId}.dispatchBridge"]);
        Assert.Equal(
            WolverineEventingRuntimeIds.HostedExecutionId,
            dispatchEntry.Metadata[$"dispatchRuntime.{WolverineEventingRuntimeIds.DispatchRuntimeId}.hostedExecutionId"]);
        var diagnosticsConvention = Assert.Single(diagnosticsCatalog.GetBySource("Cephalon.Eventing.Wolverine"));
        Assert.Equal(4300, diagnosticsConvention.MinimumEventId);
        Assert.Equal(4305, diagnosticsConvention.MaximumEventId);
        Assert.Contains(diagnosticsConvention.Events, entry => entry.Id == 4300 && entry.Name == "WolverineDispatchLoopStarted");
        Assert.Contains(diagnosticsConvention.Events, entry => entry.Id == 4303 && entry.Name == "WolverineDispatchObservationProjectionFailed");
        Assert.Contains(diagnosticsConvention.Events, entry => entry.Id == 4304 && entry.Name == "WolverineDispatchActivityStarted");
        Assert.Contains(diagnosticsConvention.Events, entry => entry.Id == 4305 && entry.Name == "WolverineDispatchMetricsRecorded");
        Assert.Equal("wolverine-managed", hostedExecution.Metadata["dispatchOwnership"]);
        Assert.Equal(WolverineEventingRuntimeIds.PublisherId, hostedExecution.Metadata["publisherId"]);
        Assert.Contains(hostedServices, service => service is WolverineEventDispatchHostedService);
    }

    [Fact]
    public async Task AddWolverineEventingProjectsManagedDispatchReportsIntoAdapterAndDispatchSurfaces()
    {
        var databaseName = $"cephalon-eventing-wolverine-runtime-report-{Guid.NewGuid():N}";
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "Microservice",
                patterns: ["CQRS", "Outbox"],
                technologies: ["EventDrivenIntegration"],
                transports: ["RestApi"],
                data: new DataSettings(
                    provider: "EntityFramework",
                    outboxEnabled: true),
                messaging: new MessagingSettings(provider: "Wolverine")));
            engine.AddModule(new PlatformTestModule());
            engine.AddEventing(options =>
            {
                options.Channels.Add(new EventChannelDescriptor(
                    id: "catalog-events",
                    displayName: "Catalog Events",
                    description: "Catalog integration events."));
            });
            engine.AddEntityFrameworkData<OutboxCatalogDbContext>(
                options => options.UseInMemoryDatabase(databaseName),
                configure: options => options.RegisterOutbox = true);
            engine.AddWolverineEventing(options => options.EnableDispatchLoop = true);
        });

        await using var provider = services.BuildServiceProvider();
        var runtimeReporter = provider.GetRequiredService<IEventDispatchRuntimeReporter>();
        var technologyCatalog = provider.GetRequiredService<global::Cephalon.Abstractions.Technologies.ITechnologyRuntimeCatalog>();

        await runtimeReporter.ReportAsync(new EventDispatchExecutionReport(
            outboxId: "entity-framework-outbox",
            channelId: "catalog-events",
            outcome: EventDispatchExecutionOutcomes.Started,
            observedAtUtc: new DateTimeOffset(2026, 04, 04, 14, 00, 0, TimeSpan.Zero),
            messageId: "evt-500",
            attempt: 1,
            metadata: new Dictionary<string, string>
            {
                ["publisherId"] = WolverineEventingRuntimeIds.PublisherId,
                ["dispatchBridge"] = "wolverine-managed"
            }));
        await runtimeReporter.ReportAsync(new EventDispatchExecutionReport(
            outboxId: "entity-framework-outbox",
            channelId: "catalog-events",
            outcome: EventDispatchExecutionOutcomes.RetryScheduled,
            observedAtUtc: new DateTimeOffset(2026, 04, 04, 14, 01, 0, TimeSpan.Zero),
            messageId: "evt-500",
            attempt: 2,
            error: "Retrying staged event publication.",
            metadata: new Dictionary<string, string>
            {
                ["publisherId"] = WolverineEventingRuntimeIds.PublisherId,
                ["dispatchBridge"] = "wolverine-managed",
                ["nextRetryAtUtc"] = "2026-04-04T14:06:00.0000000+00:00",
                ["retryPolicy"] = "fixed-delay"
            }));

        var eventingSurfaces = technologyCatalog.GetByTechnology("event-driven-integration");
        var adapterSurface = Assert.Single(eventingSurfaces, surface => surface.SurfaceId == "wolverine-adapter");
        var dispatchSurface = Assert.Single(eventingSurfaces, surface => surface.SurfaceId == "event-dispatches");
        var adapterEntry = Assert.Single(adapterSurface.Entries, entry => entry.Id == "wolverine-eventing");
        var dispatchEntry = Assert.Single(dispatchSurface.Entries, entry => entry.Id == "entity-framework-outbox");

        Assert.Equal("2", adapterEntry.Metadata["reportedTotalCount"]);
        Assert.Equal("1", adapterEntry.Metadata["reportedStartedCount"]);
        Assert.Equal("1", adapterEntry.Metadata["reportedRetryScheduledCount"]);
        Assert.Equal("1", adapterEntry.Metadata["reportedRetryPendingCount"]);
        Assert.Equal("retry-scheduled", adapterEntry.Metadata["lastOutcome"]);
        Assert.Equal("entity-framework-outbox", adapterEntry.Metadata["lastOutboxId"]);
        Assert.Equal("evt-500", adapterEntry.Metadata["lastMessageId"]);
        Assert.Equal("catalog-events", adapterEntry.Metadata["lastChannelId"]);
        Assert.Equal("Retrying staged event publication.", adapterEntry.Metadata["lastError"]);

        Assert.Equal("reported", dispatchEntry.Metadata["runtimeState"]);
        Assert.Equal("retry-scheduled", dispatchEntry.Metadata["lastOutcome"]);
        Assert.Equal("true", dispatchEntry.Metadata["retryPending"]);
        Assert.Equal("evt-500", dispatchEntry.Metadata["lastMessageId"]);
        Assert.Equal("2026-04-04T14:06:00.0000000+00:00", dispatchEntry.Metadata["reported.nextRetryAtUtc"]);
        Assert.Equal("fixed-delay", dispatchEntry.Metadata["reported.retryPolicy"]);
        Assert.Equal("wolverine-managed", dispatchEntry.Metadata[$"dispatchRuntime.{WolverineEventingRuntimeIds.DispatchRuntimeId}.dispatchBridge"]);
    }

    [Fact]
    public async Task WolverineManagedDispatchLoopPublishesPendingItemsAndReportsSuccess()
    {
        var dispatchItem = new EventDispatchItem(
            outboxId: "entity-framework-outbox",
            messageId: "evt-300",
            channelId: "catalog-events",
            eventType: "catalog.item.created",
            payload: "{\"id\":\"item-300\"}",
            occurredAtUtc: new DateTimeOffset(2026, 04, 04, 12, 30, 0, TimeSpan.Zero),
            createdAtUtc: new DateTimeOffset(2026, 04, 04, 12, 30, 1, TimeSpan.Zero),
            dispatchAttemptCount: 0,
            contentType: "application/json",
            correlationId: "corr-300",
            tenantId: "tenant-300");
        var options = new WolverineEventingOptions
        {
            EnableDispatchLoop = true,
            DispatchBatchSize = 10,
            DispatchPollingIntervalSeconds = 60,
            RetryDelaySeconds = 30
        };
        var dispatchStore = new TestEventDispatchStore(dispatchItem);
        var runtimeReporter = new TestEventDispatchRuntimeReporter();
        var messageBus = new TestMessageBus(hasDestinations: true);
        var service = new WolverineEventDispatchHostedService(
            options,
            dispatchStore,
            runtimeReporter,
            messageBus,
            NullLogger<WolverineEventDispatchHostedService>.Instance);

        await service.DispatchOnceAsync();

        var publication = Assert.Single(messageBus.PublishedMessages);
        Assert.Equal("evt-300", publication.Id);
        Assert.Equal("catalog-events", publication.ChannelId);
        Assert.Equal("catalog.item.created", publication.EventType);
        Assert.Equal("corr-300", publication.CorrelationId);
        Assert.Equal("tenant-300", publication.TenantId);

        Assert.Collection(
            dispatchStore.AppliedReports,
            report =>
            {
                Assert.Equal(EventDispatchExecutionOutcomes.Started, report.Outcome);
                Assert.Equal("evt-300", report.MessageId);
                Assert.Equal("wolverine-managed", report.Metadata["dispatchOwnership"]);
            },
            report =>
            {
                Assert.Equal(EventDispatchExecutionOutcomes.Succeeded, report.Outcome);
                Assert.Equal("evt-300", report.MessageId);
                Assert.Equal(WolverineEventingRuntimeIds.PublisherId, report.Metadata["publisherId"]);
                Assert.Equal("publish", report.Metadata["deliveryMode"]);
            });

        Assert.Collection(
            runtimeReporter.Reported,
            report => Assert.Equal(EventDispatchExecutionOutcomes.Started, report.Outcome),
            report => Assert.Equal(EventDispatchExecutionOutcomes.Succeeded, report.Outcome));
    }

    [Fact]
    public async Task WolverineManagedDispatchLoopSchedulesRetryWhenWolverineHasNoDestinations()
    {
        var dispatchItem = new EventDispatchItem(
            outboxId: "entity-framework-outbox",
            messageId: "evt-400",
            channelId: "catalog-events",
            eventType: "catalog.item.created",
            payload: "{\"id\":\"item-400\"}",
            occurredAtUtc: new DateTimeOffset(2026, 04, 04, 13, 00, 0, TimeSpan.Zero),
            createdAtUtc: new DateTimeOffset(2026, 04, 04, 13, 00, 1, TimeSpan.Zero),
            dispatchAttemptCount: 1,
            contentType: "application/json");
        var options = new WolverineEventingOptions
        {
            EnableDispatchLoop = true,
            DispatchBatchSize = 10,
            DispatchPollingIntervalSeconds = 60,
            RetryDelaySeconds = 30
        };
        var dispatchStore = new TestEventDispatchStore(dispatchItem);
        var runtimeReporter = new TestEventDispatchRuntimeReporter();
        var messageBus = new TestMessageBus(hasDestinations: false);
        var service = new WolverineEventDispatchHostedService(
            options,
            dispatchStore,
            runtimeReporter,
            messageBus,
            NullLogger<WolverineEventDispatchHostedService>.Instance);

        await service.DispatchOnceAsync();

        Assert.Empty(messageBus.PublishedMessages);
        Assert.Collection(
            dispatchStore.AppliedReports,
            report => Assert.Equal(EventDispatchExecutionOutcomes.Started, report.Outcome),
            report =>
            {
                Assert.Equal(EventDispatchExecutionOutcomes.RetryScheduled, report.Outcome);
                Assert.Equal("Wolverine does not have any configured destinations for Cephalon event publications.", report.Error);
                Assert.Equal("fixed-delay", report.Metadata["retryPolicy"]);
                Assert.Equal("no-destinations", report.Metadata["routing"]);
                Assert.True(report.Metadata.ContainsKey("nextRetryAtUtc"));
            });
        Assert.Collection(
            runtimeReporter.Reported,
            report => Assert.Equal(EventDispatchExecutionOutcomes.Started, report.Outcome),
            report => Assert.Equal(EventDispatchExecutionOutcomes.RetryScheduled, report.Outcome));
    }

    private sealed class TestEventDispatchStore(params EventDispatchItem[] items) : IEventDispatchStore
    {
        private readonly List<EventDispatchItem> pendingItems = [.. items];

        public List<EventDispatchExecutionReport> AppliedReports { get; } = [];

        public ValueTask<IReadOnlyList<EventDispatchItem>> ReadPendingAsync(int maximumCount, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult<IReadOnlyList<EventDispatchItem>>(pendingItems.Take(maximumCount).ToArray());
        }

        public ValueTask ApplyReportAsync(EventDispatchExecutionReport report, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AppliedReports.Add(report);
            if (report.Outcome is EventDispatchExecutionOutcomes.Succeeded or EventDispatchExecutionOutcomes.Skipped)
            {
                pendingItems.RemoveAll(item => string.Equals(item.MessageId, report.MessageId, StringComparison.OrdinalIgnoreCase));
            }

            return ValueTask.CompletedTask;
        }
    }

    private sealed class TestEventDispatchRuntimeReporter : IEventDispatchRuntimeReporter
    {
        public List<EventDispatchExecutionReport> Reported { get; } = [];

        public ValueTask ReportAsync(EventDispatchExecutionReport report, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Reported.Add(report);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class TestMessageBus(bool hasDestinations) : IMessageBus
    {
        private readonly IReadOnlyList<Envelope> destinations = hasDestinations ? [new Envelope(new object())] : [];

        public string? TenantId { get; set; }

        public List<EventPublication> PublishedMessages { get; } = [];

        public ValueTask BroadcastToTopicAsync(string topicName, object message, DeliveryOptions? options = null) => ValueTask.CompletedTask;

        public IDestinationEndpoint EndpointFor(string endpointName) => throw new NotSupportedException();

        public IDestinationEndpoint EndpointFor(Uri uri) => throw new NotSupportedException();

        public Task InvokeAsync(object message, CancellationToken cancellation = default, TimeSpan? timeout = null) =>
            throw new NotSupportedException();

        public Task InvokeAsync(object message, DeliveryOptions options, CancellationToken cancellation = default, TimeSpan? timeout = null) =>
            throw new NotSupportedException();

        public Task<T> InvokeAsync<T>(object message, CancellationToken cancellation = default, TimeSpan? timeout = null) =>
            throw new NotSupportedException();

        public Task<T> InvokeAsync<T>(object message, DeliveryOptions options, CancellationToken cancellation = default, TimeSpan? timeout = null) =>
            throw new NotSupportedException();

        public Task InvokeForTenantAsync(string tenantId, object message, CancellationToken cancellation, TimeSpan? timeout) =>
            throw new NotSupportedException();

        public Task<T> InvokeForTenantAsync<T>(string tenantId, object message, CancellationToken cancellation, TimeSpan? timeout) =>
            throw new NotSupportedException();

        public IReadOnlyList<Envelope> PreviewSubscriptions(object message) => destinations;

        public IReadOnlyList<Envelope> PreviewSubscriptions(object message, DeliveryOptions options) => destinations;

        public ValueTask PublishAsync<T>(T message, DeliveryOptions? options = null)
        {
            if (message is EventPublication publication)
            {
                PublishedMessages.Add(publication);
            }

            return ValueTask.CompletedTask;
        }

        public ValueTask SendAsync<T>(T message, DeliveryOptions? options = null) => ValueTask.CompletedTask;
    }
}
