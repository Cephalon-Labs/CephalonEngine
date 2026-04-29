using Cephalon.Abstractions.Data;
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
        var outboxCatalog = provider.GetRequiredService<IOutboxCatalog>();
        var outboxDescriptor = Assert.Single(outboxCatalog.Outboxes);

        Assert.NotNull(messageBus);
        var eventingSurfaces = technologyCatalog.GetByTechnology("event-driven-integration");
        var adapterSurface = Assert.Single(eventingSurfaces, surface => surface.SurfaceId == "wolverine-adapter");
        var adapterEntry = Assert.Single(adapterSurface.Entries, entry => entry.Id == "wolverine-eventing");

        Assert.Equal("configured", adapterEntry.Metadata["hostWiring"]);
        Assert.Equal("consumer-managed", adapterEntry.Metadata["dispatchBridge"]);
        Assert.Equal("available", adapterEntry.Metadata["dispatchStore"]);
        Assert.Contains("EntityFrameworkEventDispatchStore", adapterEntry.Metadata["dispatchStoreTypes"], StringComparison.Ordinal);
        Assert.Equal("consumer-managed", outboxDescriptor.DispatchPolicy.PolicyId);
        Assert.Equal("consumer-managed", outboxDescriptor.DispatchPolicy.ExecutionMode);
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
        var dispatchRuntimeDescriptors = provider.GetRequiredService<IEventDispatchRuntimeDescriptorCatalog>();
        var hostedExecutions = provider.GetRequiredService<global::Cephalon.Abstractions.Execution.IHostedExecutionRuntimeCatalog>();
        var technologyCatalog = provider.GetRequiredService<global::Cephalon.Abstractions.Technologies.ITechnologyRuntimeCatalog>();
        var hostedServices = provider.GetServices<IHostedService>();
        var outboxCatalog = provider.GetRequiredService<IOutboxCatalog>();
        var eventingSurfaces = technologyCatalog.GetByTechnology("event-driven-integration");
        var hostedExecution = Assert.Single(hostedExecutions.HostedExecutions, execution => execution.Id == WolverineEventingRuntimeIds.HostedExecutionId);
        var adapterSurface = Assert.Single(
            eventingSurfaces,
            surface => surface.SurfaceId == "wolverine-adapter");
        var dispatchSurface = Assert.Single(eventingSurfaces, surface => surface.SurfaceId == "event-dispatches");
        var adapterEntry = Assert.Single(adapterSurface.Entries, entry => entry.Id == "wolverine-eventing");
        var dispatchEntry = Assert.Single(dispatchSurface.Entries, entry => entry.Id == "entity-framework-outbox");
        var dispatchRuntimeDescriptor = Assert.Single(dispatchRuntimeDescriptors.Runtimes);
        var outboxDescriptor = Assert.Single(outboxCatalog.Outboxes);

        Assert.Equal("wolverine-managed", adapterEntry.Metadata["dispatchBridge"]);
        Assert.Equal("enabled", adapterEntry.Metadata["dispatchLoop"]);
        Assert.Equal("7", adapterEntry.Metadata["dispatchBatchSize"]);
        Assert.Equal("3", adapterEntry.Metadata["dispatchPollingIntervalSeconds"]);
        Assert.Equal("45", adapterEntry.Metadata["retryDelaySeconds"]);
        Assert.Equal("3", adapterEntry.Metadata["dispatchMaxAttempts"]);
        Assert.Equal("bounded-fixed-delay", adapterEntry.Metadata["dispatchRetryPolicy"]);
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
        Assert.Equal(WolverineEventingRuntimeIds.DispatchRuntimeId, dispatchRuntimeDescriptor.Id);
        Assert.Equal("Wolverine Dispatch Loop", dispatchRuntimeDescriptor.DisplayName);
        Assert.Equal("wolverine", dispatchRuntimeDescriptor.Metadata["adapter"]);
        Assert.Equal(["entity-framework-outbox"], dispatchRuntimeDescriptor.OutboxIds);
        Assert.False(dispatchRuntimeDescriptor.Summary.HasReports);
        Assert.Equal(0, dispatchRuntimeDescriptor.Summary.TotalReports);
        Assert.Equal("wolverine-managed", outboxDescriptor.DispatchPolicy.PolicyId);
        Assert.Equal("runtime-managed", outboxDescriptor.DispatchPolicy.ExecutionMode);
        Assert.Equal(WolverineEventingRuntimeIds.DispatchRuntimeId, outboxDescriptor.DispatchPolicy.RuntimeId);
        var diagnosticsConvention = Assert.Single(diagnosticsCatalog.GetBySource("Cephalon.Eventing.Wolverine"));
        Assert.Equal(4300, diagnosticsConvention.MinimumEventId);
        Assert.Equal(4306, diagnosticsConvention.MaximumEventId);
        Assert.Contains(diagnosticsConvention.Events, entry => entry.Id == 4300 && entry.Name == "WolverineDispatchLoopStarted");
        Assert.Contains(diagnosticsConvention.Events, entry => entry.Id == 4303 && entry.Name == "WolverineDispatchObservationProjectionFailed");
        Assert.Contains(diagnosticsConvention.Events, entry => entry.Id == 4304 && entry.Name == "WolverineDispatchActivityStarted");
        Assert.Contains(diagnosticsConvention.Events, entry => entry.Id == 4305 && entry.Name == "WolverineDispatchMetricsRecorded");
        Assert.Contains(diagnosticsConvention.Events, entry => entry.Id == 4306 && entry.Name == "WolverineSubscriptionObservationProjectionFailed");
        Assert.Equal("wolverine-managed", hostedExecution.Metadata["dispatchOwnership"]);
        Assert.Equal(WolverineEventingRuntimeIds.PublisherId, hostedExecution.Metadata["publisherId"]);
        Assert.Contains(hostedServices, service => service is WolverineEventDispatchHostedService);
    }

    [Fact]
    public async Task AddWolverineEventingCanExposeManagedSubscriptionExecutionThroughCapabilitiesAndRuntimeSurface()
    {
        var databaseName = $"cephalon-eventing-wolverine-subscriptions-{Guid.NewGuid():N}";
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
            engine.AddModule(new TechnologyPackContributionModule());
            engine.AddEventing();
            engine.AddEntityFrameworkData<OutboxCatalogDbContext>(
                options => options.UseInMemoryDatabase(databaseName),
                configure: options => options.RegisterOutbox = true);
            engine.AddWolverineEventing(options =>
            {
                options.EnableDispatchLoop = true;
                options.EnableSubscriptionExecution = true;
                options.SubscriptionRetryDelaySeconds = 45;
            });
        });

        await using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<global::Cephalon.Engine.Runtime.IRuntime>();
        var technologyCatalog = provider.GetRequiredService<global::Cephalon.Abstractions.Technologies.ITechnologyRuntimeCatalog>();
        var diagnosticsCatalog = provider.GetRequiredService<IRuntimeDiagnosticsCatalog>();
        var bindingCatalog = provider.GetRequiredService<IEventSubscriptionExecutionBindingCatalog>();
        var readinessCatalog = provider.GetRequiredService<IEventSubscriptionExecutionReadinessCatalog>();
        var eventingSurfaces = technologyCatalog.GetByTechnology("event-driven-integration");
        var adapterSurface = Assert.Single(eventingSurfaces, surface => surface.SurfaceId == "wolverine-adapter");
        var subscriptionSurface = Assert.Single(eventingSurfaces, surface => surface.SurfaceId == "event-subscriptions");
        var adapterEntry = Assert.Single(adapterSurface.Entries, entry => entry.Id == "wolverine-eventing");
        var subscriptionEntry = Assert.Single(subscriptionSurface.Entries, entry => entry.Id == "audit-projector");
        var subscribeCapability = Assert.Single(runtime.Manifest.Capabilities, capability => capability.Key == "eventing.subscribe");
        var binding = Assert.Single(bindingCatalog.Bindings);
        var readiness = Assert.Single(readinessCatalog.Readiness);

        Assert.Equal("wolverine-managed", adapterEntry.Metadata["subscriptionExecution"]);
        Assert.Equal("1", adapterEntry.Metadata["managedSubscriptionCount"]);
        Assert.Equal("audit-projector", adapterEntry.Metadata["managedSubscriptionIds"]);
        Assert.Equal("45", adapterEntry.Metadata["subscriptionRetryDelaySeconds"]);
        Assert.Equal("3", adapterEntry.Metadata["subscriptionMaxAttempts"]);
        Assert.Equal("bounded-fixed-delay", adapterEntry.Metadata["subscriptionRetryPolicy"]);
        Assert.Equal(WolverineEventingRuntimeIds.SubscriptionExecutionRuntimeId, adapterEntry.Metadata["subscriptionExecutionRuntimeId"]);

        Assert.Same(binding, bindingCatalog.GetBySubscriptionId("audit-projector"));
        Assert.True(bindingCatalog.TryGet("audit-projector", out var resolvedBinding));
        Assert.Same(binding, resolvedBinding);
        Assert.False(bindingCatalog.TryGet("missing-subscription", out resolvedBinding));
        Assert.Null(resolvedBinding);
        Assert.Equal("audit-projector", binding.SubscriptionId);
        Assert.Equal(WolverineEventingRuntimeIds.SubscriptionExecutionRuntimeId, binding.ExecutionRuntimeId);
        Assert.Equal("wolverine-managed", binding.ExecutionOwnership);
        Assert.Equal("message-handler", binding.ExecutionMode);
        Assert.Equal("wolverine", binding.Metadata["adapter"]);
        Assert.Equal("bounded-fixed-delay", binding.Metadata["retryPolicy"]);
        Assert.Equal("3", binding.Metadata["retryMaxAttempts"]);
        Assert.Equal("45", binding.Metadata["retryDelaySeconds"]);
        Assert.Equal("wolverine-scheduled-message", binding.Metadata["retryDurability"]);
        Assert.Equal("provider-managed", binding.Metadata["retryScope"]);
        Assert.Equal("audit-projector", readiness.SubscriptionId);
        Assert.Equal(EventSubscriptionExecutionReadinessStates.RuntimeBound, readiness.ReadinessState);
        Assert.True(readiness.HasExecutionPath);
        Assert.Equal("wolverine-managed", readiness.ExecutionOwnership);
        Assert.Equal("message-handler", readiness.ExecutionMode);
        Assert.Equal(WolverineEventingRuntimeIds.SubscriptionExecutionRuntimeId, readiness.ExecutionRuntimeId);
        Assert.Contains("managed-binding-available", readiness.Reasons);
        Assert.Equal("wolverine", readiness.Metadata["adapter"]);

        Assert.Equal("wolverine-managed", subscriptionEntry.Metadata[EventSubscriptionRuntimeMetadataKeys.DispatchRuntime]);
        Assert.Equal("runtime-bound", subscriptionEntry.Metadata[EventSubscriptionRuntimeMetadataKeys.SubscriptionRuntime]);
        Assert.Equal(EventSubscriptionExecutionReadinessStates.RuntimeBound, subscriptionEntry.Metadata[EventSubscriptionRuntimeMetadataKeys.ExecutionReadiness]);
        Assert.Equal("observed", subscriptionEntry.Metadata[EventSubscriptionRuntimeMetadataKeys.ExecutionPath]);
        Assert.Contains("managed-binding-available", subscriptionEntry.Metadata[EventSubscriptionRuntimeMetadataKeys.ExecutionReadinessReasons], StringComparison.OrdinalIgnoreCase);
        Assert.Equal("wolverine-managed", subscriptionEntry.Metadata[EventSubscriptionRuntimeMetadataKeys.ExecutionOwnership]);
        Assert.Equal("message-handler", subscriptionEntry.Metadata[EventSubscriptionRuntimeMetadataKeys.ExecutionMode]);
        Assert.Equal(WolverineEventingRuntimeIds.SubscriptionExecutionRuntimeId, subscriptionEntry.Metadata[EventSubscriptionRuntimeMetadataKeys.ExecutionRuntimeId]);
        Assert.Equal("wolverine", subscriptionEntry.Metadata[$"{EventSubscriptionRuntimeMetadataKeys.BindingMetadataPrefix}adapter"]);
        Assert.Equal("bounded-fixed-delay", subscriptionEntry.Metadata[$"{EventSubscriptionRuntimeMetadataKeys.BindingMetadataPrefix}retryPolicy"]);
        Assert.Equal("3", subscriptionEntry.Metadata[$"{EventSubscriptionRuntimeMetadataKeys.BindingMetadataPrefix}retryMaxAttempts"]);
        Assert.Equal("45", subscriptionEntry.Metadata[$"{EventSubscriptionRuntimeMetadataKeys.BindingMetadataPrefix}retryDelaySeconds"]);
        Assert.Equal(WolverineEventingRuntimeIds.DispatchRuntimeId, subscriptionEntry.Metadata[$"{EventSubscriptionRuntimeMetadataKeys.BindingMetadataPrefix}trigger"]);

        Assert.Equal("wolverine", subscribeCapability.Metadata["adapter"]);
        Assert.Equal("wolverine-managed", subscribeCapability.Metadata["executionOwnership"]);
        Assert.Equal("message-handler", subscribeCapability.Metadata["executionMode"]);
        Assert.Equal(WolverineEventingRuntimeIds.SubscriptionExecutionRuntimeId, subscribeCapability.Metadata["executionRuntimeId"]);
        Assert.Equal(WolverineEventingRuntimeIds.DispatchRuntimeId, subscribeCapability.Metadata["triggerRuntimeId"]);
        Assert.Equal("bounded-fixed-delay", subscribeCapability.Metadata["retryPolicy"]);
        Assert.Equal("3", subscribeCapability.Metadata["retryMaxAttempts"]);
        Assert.Equal("45", subscribeCapability.Metadata["retryDelaySeconds"]);

        var diagnosticsConvention = Assert.Single(diagnosticsCatalog.GetBySource("Cephalon.Eventing.Wolverine"));
        Assert.Equal(4306, diagnosticsConvention.MaximumEventId);
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
        var dispatchRuntimeDescriptors = provider.GetRequiredService<IEventDispatchRuntimeDescriptorCatalog>();
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
                [EventDispatchRuntimeMetadataKeys.NextRetryAtUtc] = "2026-04-04T14:06:00.0000000+00:00",
                [EventDispatchRuntimeMetadataKeys.RetryPolicy] = "bounded-fixed-delay",
                [EventDispatchRuntimeMetadataKeys.RetryMaxAttempts] = "3",
                [EventDispatchRuntimeMetadataKeys.RetryDelaySeconds] = "30"
            }));

        var eventingSurfaces = technologyCatalog.GetByTechnology("event-driven-integration");
        var adapterSurface = Assert.Single(eventingSurfaces, surface => surface.SurfaceId == "wolverine-adapter");
        var dispatchSurface = Assert.Single(eventingSurfaces, surface => surface.SurfaceId == "event-dispatches");
        var adapterEntry = Assert.Single(adapterSurface.Entries, entry => entry.Id == "wolverine-eventing");
        var dispatchEntry = Assert.Single(dispatchSurface.Entries, entry => entry.Id == "entity-framework-outbox");
        var runtimeDescriptor = dispatchRuntimeDescriptors.GetById(WolverineEventingRuntimeIds.DispatchRuntimeId);
        Assert.NotNull(runtimeDescriptor);

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
        Assert.Equal("bounded-fixed-delay", dispatchEntry.Metadata["reported.retryPolicy"]);
        Assert.Equal("3", dispatchEntry.Metadata["reported.retryMaxAttempts"]);
        Assert.Equal("30", dispatchEntry.Metadata["reported.retryDelaySeconds"]);
        Assert.Equal("wolverine-managed", dispatchEntry.Metadata[$"dispatchRuntime.{WolverineEventingRuntimeIds.DispatchRuntimeId}.dispatchBridge"]);
        Assert.True(runtimeDescriptor!.Summary.HasReports);
        Assert.Equal(["entity-framework-outbox"], runtimeDescriptor.Summary.ReportedOutboxIds);
        Assert.Equal("entity-framework-outbox", runtimeDescriptor.Summary.LastOutboxId);
        Assert.Equal("retry-scheduled", runtimeDescriptor.Summary.LastOutcome);
        Assert.Equal("evt-500", runtimeDescriptor.Summary.LastMessageId);
        Assert.Equal(2, runtimeDescriptor.Summary.LastAttempt);
        Assert.Equal(2, runtimeDescriptor.Summary.TotalReports);
        Assert.Equal(1, runtimeDescriptor.Summary.RetryPendingCount);
        Assert.Equal("Retrying staged event publication.", runtimeDescriptor.Summary.LastError);
    }

    [Fact]
    public async Task WolverineManagedDispatchLoopCanExecuteManagedSubscriptionsAndReportSuccess()
    {
        var databaseName = $"cephalon-eventing-wolverine-subscription-success-{Guid.NewGuid():N}";
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
            engine.AddModule(new TechnologyPackContributionModule());
            engine.AddEventing();
            engine.AddEntityFrameworkData<OutboxCatalogDbContext>(
                options => options.UseInMemoryDatabase(databaseName),
                configure: options => options.RegisterOutbox = true);
            engine.AddWolverineEventing(options =>
            {
                options.EnableDispatchLoop = true;
                options.EnableSubscriptionExecution = true;
            });
        });

        await using var provider = services.BuildServiceProvider();
        var probe = provider.GetRequiredService<ManagedAuditProjectorProbe>();
        var subscriptionRuntimeCatalog = provider.GetRequiredService<IEventSubscriptionRuntimeCatalog>();
        var dispatchService = provider.GetServices<IHostedService>().OfType<WolverineEventDispatchHostedService>().Single();
        var technologyCatalog = provider.GetRequiredService<global::Cephalon.Abstractions.Technologies.ITechnologyRuntimeCatalog>();

        await using (var scope = provider.CreateAsyncScope())
        {
            var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(new EventPublication(
                id: "audit-msg-200",
                channelId: "audit",
                eventType: "audit.entry.recorded",
                payload: "{\"id\":\"audit-200\"}",
                occurredAtUtc: new DateTimeOffset(2026, 04, 26, 8, 30, 0, TimeSpan.Zero),
                contentType: "application/json",
                correlationId: "corr-audit-200",
                tenantId: "tenant-audit"));
        }

        await dispatchService.DispatchOnceAsync();

        Assert.Equal(1, probe.TotalAttempts);
        Assert.Equal(1, probe.SuccessfulAttempts);
        Assert.Equal("audit-msg-200", probe.LastMessageId);

        var state = Assert.Single(subscriptionRuntimeCatalog.States);
        Assert.Equal("audit-projector", state.SubscriptionId);
        Assert.Equal(EventSubscriptionExecutionOutcomes.Succeeded, state.LastOutcome);
        Assert.Equal("audit-msg-200", state.LastMessageId);
        Assert.Equal(1, state.StartedCount);
        Assert.Equal(1, state.SucceededCount);
        Assert.Equal(2, state.TotalReports);
        Assert.False(state.RetryPending);
        Assert.Equal(WolverineEventingRuntimeIds.SubscriptionExecutionRuntimeId, state.Metadata["subscriptionExecutionRuntimeId"]);
        Assert.Equal("wolverine-managed", state.Metadata["subscriptionOwnership"]);
        Assert.Equal("wolverine", state.Metadata["adapter"]);

        var adapterEntry = Assert.Single(
            technologyCatalog.GetByTechnology("event-driven-integration")
                .Single(surface => surface.SurfaceId == "wolverine-adapter")
                .Entries,
            entry => entry.Id == "wolverine-eventing");
        Assert.Equal("2", adapterEntry.Metadata["managedSubscriptionReportedCount"]);
        Assert.Equal("0", adapterEntry.Metadata["managedSubscriptionRetryPendingCount"]);
        Assert.Equal("audit-projector", adapterEntry.Metadata["managedSubscriptionLastSubscriptionId"]);
        Assert.Equal("succeeded", adapterEntry.Metadata["managedSubscriptionLastOutcome"]);
    }

    [Fact]
    public async Task WolverineManagedSubscriptionExecutionProcessorSchedulesRetryAndCanSucceedOnLaterAttempt()
    {
        var databaseName = $"cephalon-eventing-wolverine-subscription-retry-{Guid.NewGuid():N}";
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
            engine.AddModule(new TechnologyPackContributionModule());
            engine.AddEventing();
            engine.AddEntityFrameworkData<OutboxCatalogDbContext>(
                options => options.UseInMemoryDatabase(databaseName),
                configure: options => options.RegisterOutbox = true);
            engine.AddWolverineEventing(options =>
            {
                options.EnableDispatchLoop = true;
                options.EnableSubscriptionExecution = true;
                options.SubscriptionRetryDelaySeconds = 45;
            });
        });

        await using var provider = services.BuildServiceProvider();
        var probe = provider.GetRequiredService<ManagedAuditProjectorProbe>();
        var processor = provider.GetRequiredService<WolverineManagedEventSubscriptionExecutionProcessor>();
        var subscriptionRuntimeCatalog = provider.GetRequiredService<IEventSubscriptionRuntimeCatalog>();
        var messageBus = new TestMessageBus(hasDestinations: false);
        probe.FailuresRemaining = 1;

        var request = new WolverineManagedEventSubscriptionExecutionRequest(
            subscriptionId: "audit-projector",
            publication: new EventPublication(
                id: "audit-msg-300",
                channelId: "audit",
                eventType: "audit.entry.recorded",
                payload: "{\"id\":\"audit-300\"}",
                occurredAtUtc: new DateTimeOffset(2026, 04, 26, 8, 45, 0, TimeSpan.Zero),
                contentType: "application/json"));

        await processor.ProcessAsync(request, attempt: 1, messageBus);

        Assert.Equal(1, probe.TotalAttempts);
        Assert.Equal(0, probe.SuccessfulAttempts);
        var scheduledMessage = Assert.Single(messageBus.SentMessages);
        var scheduledRequest = Assert.IsType<WolverineManagedEventSubscriptionExecutionRequest>(scheduledMessage.Message);
        Assert.Equal("audit-projector", scheduledRequest.SubscriptionId);
        Assert.Equal(2, scheduledRequest.Attempt);
        Assert.NotNull(scheduledMessage.Options);
        Assert.Equal(TimeSpan.FromSeconds(45), scheduledMessage.Options!.ScheduleDelay);

        var stateAfterRetry = Assert.Single(subscriptionRuntimeCatalog.States);
        Assert.Equal(EventSubscriptionExecutionOutcomes.RetryScheduled, stateAfterRetry.LastOutcome);
        Assert.Equal(1, stateAfterRetry.StartedCount);
        Assert.Equal(1, stateAfterRetry.RetryScheduledCount);
        Assert.True(stateAfterRetry.RetryPending);
        Assert.Equal("bounded-fixed-delay", stateAfterRetry.Metadata["retryPolicy"]);
        Assert.Equal("3", stateAfterRetry.Metadata["retryMaxAttempts"]);
        Assert.Equal("45", stateAfterRetry.Metadata["retryDelaySeconds"]);
        Assert.Equal("retry-scheduled", stateAfterRetry.Metadata["retryOutcome"]);
        Assert.True(stateAfterRetry.Metadata.ContainsKey("nextRetryAtUtc"));

        await processor.ProcessAsync(scheduledRequest, scheduledRequest.Attempt, messageBus);

        Assert.Equal(2, probe.TotalAttempts);
        Assert.Equal(1, probe.SuccessfulAttempts);
        var finalState = Assert.Single(subscriptionRuntimeCatalog.States);
        Assert.Equal(EventSubscriptionExecutionOutcomes.Succeeded, finalState.LastOutcome);
        Assert.Equal(2, finalState.StartedCount);
        Assert.Equal(1, finalState.SucceededCount);
        Assert.Equal(1, finalState.RetryScheduledCount);
        Assert.Equal(4, finalState.TotalReports);
        Assert.False(finalState.RetryPending);
        Assert.Equal(2, finalState.LastAttempt);
    }

    [Fact]
    public async Task WolverineManagedSubscriptionExecutionProcessorReportsTerminalFailureWhenMaxAttemptsAreExhausted()
    {
        var databaseName = $"cephalon-eventing-wolverine-subscription-terminal-{Guid.NewGuid():N}";
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
            engine.AddModule(new TechnologyPackContributionModule());
            engine.AddEventing();
            engine.AddEntityFrameworkData<OutboxCatalogDbContext>(
                options => options.UseInMemoryDatabase(databaseName),
                configure: options => options.RegisterOutbox = true);
            engine.AddWolverineEventing(options =>
            {
                options.EnableDispatchLoop = true;
                options.EnableSubscriptionExecution = true;
                options.SubscriptionMaxAttempts = 2;
                options.SubscriptionRetryDelaySeconds = 45;
            });
        });

        await using var provider = services.BuildServiceProvider();
        var probe = provider.GetRequiredService<ManagedAuditProjectorProbe>();
        var processor = provider.GetRequiredService<WolverineManagedEventSubscriptionExecutionProcessor>();
        var subscriptionRuntimeCatalog = provider.GetRequiredService<IEventSubscriptionRuntimeCatalog>();
        var messageBus = new TestMessageBus(hasDestinations: false);
        probe.FailuresRemaining = 5;

        var request = new WolverineManagedEventSubscriptionExecutionRequest(
            subscriptionId: "audit-projector",
            publication: new EventPublication(
                id: "audit-msg-301",
                channelId: "audit",
                eventType: "audit.entry.recorded",
                payload: "{\"id\":\"audit-301\"}",
                occurredAtUtc: new DateTimeOffset(2026, 04, 26, 8, 50, 0, TimeSpan.Zero),
                contentType: "application/json"));

        await processor.ProcessAsync(request, attempt: 2, messageBus);

        Assert.Equal(1, probe.TotalAttempts);
        Assert.Equal(0, probe.SuccessfulAttempts);
        Assert.Empty(messageBus.SentMessages);

        var state = Assert.Single(subscriptionRuntimeCatalog.States);
        Assert.Equal(EventSubscriptionExecutionOutcomes.Failed, state.LastOutcome);
        Assert.Equal(1, state.StartedCount);
        Assert.Equal(1, state.FailedCount);
        Assert.Equal(0, state.RetryScheduledCount);
        Assert.Equal(2, state.TotalReports);
        Assert.False(state.RetryPending);
        Assert.Equal(2, state.LastAttempt);
        Assert.Equal("bounded-fixed-delay", state.Metadata["retryPolicy"]);
        Assert.Equal("2", state.Metadata["retryMaxAttempts"]);
        Assert.Equal("45", state.Metadata["retryDelaySeconds"]);
        Assert.Equal("max-attempts-exhausted", state.Metadata["retryOutcome"]);
        Assert.Equal("true", state.Metadata["retryExhausted"]);
        Assert.Equal("true", state.Metadata["terminalFailure"]);
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
            RetryDelaySeconds = 30,
            DispatchMaxAttempts = 3
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
                Assert.Equal("bounded-fixed-delay", report.Metadata["retryPolicy"]);
                Assert.Equal("3", report.Metadata["retryMaxAttempts"]);
                Assert.Equal("30", report.Metadata["retryDelaySeconds"]);
                Assert.Equal("dispatch-store-delayed-eligibility", report.Metadata["retryDurability"]);
                Assert.Equal("provider-managed", report.Metadata["retryScope"]);
                Assert.Equal("retry-scheduled", report.Metadata["retryOutcome"]);
                Assert.Equal("no-destinations", report.Metadata["routing"]);
                Assert.True(report.Metadata.ContainsKey("nextRetryAtUtc"));
            });
        Assert.Collection(
            runtimeReporter.Reported,
            report => Assert.Equal(EventDispatchExecutionOutcomes.Started, report.Outcome),
            report => Assert.Equal(EventDispatchExecutionOutcomes.RetryScheduled, report.Outcome));
    }

    [Fact]
    public async Task WolverineManagedDispatchLoopSchedulesRetryWhenWolverinePublishFails()
    {
        var dispatchItem = new EventDispatchItem(
            outboxId: "entity-framework-outbox",
            messageId: "evt-402",
            channelId: "catalog-events",
            eventType: "catalog.item.created",
            payload: "{\"id\":\"item-402\"}",
            occurredAtUtc: new DateTimeOffset(2026, 04, 04, 13, 10, 0, TimeSpan.Zero),
            createdAtUtc: new DateTimeOffset(2026, 04, 04, 13, 10, 1, TimeSpan.Zero),
            dispatchAttemptCount: 1,
            contentType: "application/json");
        var options = new WolverineEventingOptions
        {
            EnableDispatchLoop = true,
            DispatchBatchSize = 10,
            DispatchPollingIntervalSeconds = 60,
            RetryDelaySeconds = 30,
            DispatchMaxAttempts = 3
        };
        var dispatchStore = new TestEventDispatchStore(dispatchItem);
        var runtimeReporter = new TestEventDispatchRuntimeReporter();
        var publishException = new InvalidOperationException("Simulated Wolverine publish failure.");
        var messageBus = new TestMessageBus(hasDestinations: true, publishException);
        var service = new WolverineEventDispatchHostedService(
            options,
            dispatchStore,
            runtimeReporter,
            messageBus,
            NullLogger<WolverineEventDispatchHostedService>.Instance);

        await service.DispatchOnceAsync();

        Assert.Empty(messageBus.PublishedMessages);
        Assert.NotEmpty(await dispatchStore.ReadPendingAsync(10));
        Assert.Collection(
            dispatchStore.AppliedReports,
            report => Assert.Equal(EventDispatchExecutionOutcomes.Started, report.Outcome),
            report =>
            {
                Assert.Equal(EventDispatchExecutionOutcomes.RetryScheduled, report.Outcome);
                Assert.Equal("Simulated Wolverine publish failure.", report.Error);
                Assert.Equal("bounded-fixed-delay", report.Metadata["retryPolicy"]);
                Assert.Equal("3", report.Metadata["retryMaxAttempts"]);
                Assert.Equal("30", report.Metadata["retryDelaySeconds"]);
                Assert.Equal("dispatch-store-delayed-eligibility", report.Metadata["retryDurability"]);
                Assert.Equal("provider-managed", report.Metadata["retryScope"]);
                Assert.Equal("retry-scheduled", report.Metadata["retryOutcome"]);
                Assert.Equal("publish", report.Metadata["routing"]);
                Assert.Equal(typeof(InvalidOperationException).FullName, report.Metadata["exceptionType"]);
                Assert.True(report.Metadata.ContainsKey("nextRetryAtUtc"));
            });
        Assert.Collection(
            runtimeReporter.Reported,
            report => Assert.Equal(EventDispatchExecutionOutcomes.Started, report.Outcome),
            report => Assert.Equal(EventDispatchExecutionOutcomes.RetryScheduled, report.Outcome));
    }

    [Fact]
    public async Task WolverineManagedDispatchLoopReportsTerminalFailureWhenMaxAttemptsAreExhausted()
    {
        var dispatchItem = new EventDispatchItem(
            outboxId: "entity-framework-outbox",
            messageId: "evt-401",
            channelId: "catalog-events",
            eventType: "catalog.item.created",
            payload: "{\"id\":\"item-401\"}",
            occurredAtUtc: new DateTimeOffset(2026, 04, 04, 13, 05, 0, TimeSpan.Zero),
            createdAtUtc: new DateTimeOffset(2026, 04, 04, 13, 05, 1, TimeSpan.Zero),
            dispatchAttemptCount: 1,
            contentType: "application/json");
        var options = new WolverineEventingOptions
        {
            EnableDispatchLoop = true,
            DispatchBatchSize = 10,
            DispatchPollingIntervalSeconds = 60,
            RetryDelaySeconds = 30,
            DispatchMaxAttempts = 2
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
        Assert.Empty(await dispatchStore.ReadPendingAsync(10));
        Assert.Collection(
            dispatchStore.AppliedReports,
            report => Assert.Equal(EventDispatchExecutionOutcomes.Started, report.Outcome),
            report =>
            {
                Assert.Equal(EventDispatchExecutionOutcomes.Failed, report.Outcome);
                Assert.Equal("Wolverine does not have any configured destinations for Cephalon event publications.", report.Error);
                Assert.Equal("bounded-fixed-delay", report.Metadata["retryPolicy"]);
                Assert.Equal("2", report.Metadata["retryMaxAttempts"]);
                Assert.Equal("30", report.Metadata["retryDelaySeconds"]);
                Assert.Equal("dispatch-store-delayed-eligibility", report.Metadata["retryDurability"]);
                Assert.Equal("provider-managed", report.Metadata["retryScope"]);
                Assert.Equal("max-attempts-exhausted", report.Metadata["retryOutcome"]);
                Assert.Equal("true", report.Metadata["retryExhausted"]);
                Assert.Equal("true", report.Metadata["terminalFailure"]);
                Assert.False(report.Metadata.ContainsKey("nextRetryAtUtc"));
            });
        Assert.Collection(
            runtimeReporter.Reported,
            report => Assert.Equal(EventDispatchExecutionOutcomes.Started, report.Outcome),
            report => Assert.Equal(EventDispatchExecutionOutcomes.Failed, report.Outcome));
    }

    [Fact]
    public async Task WolverineManagedDispatchLoopReportsTerminalFailureWhenPublishFailuresExhaustMaxAttempts()
    {
        var dispatchItem = new EventDispatchItem(
            outboxId: "entity-framework-outbox",
            messageId: "evt-403",
            channelId: "catalog-events",
            eventType: "catalog.item.created",
            payload: "{\"id\":\"item-403\"}",
            occurredAtUtc: new DateTimeOffset(2026, 04, 04, 13, 15, 0, TimeSpan.Zero),
            createdAtUtc: new DateTimeOffset(2026, 04, 04, 13, 15, 1, TimeSpan.Zero),
            dispatchAttemptCount: 1,
            contentType: "application/json");
        var options = new WolverineEventingOptions
        {
            EnableDispatchLoop = true,
            DispatchBatchSize = 10,
            DispatchPollingIntervalSeconds = 60,
            RetryDelaySeconds = 30,
            DispatchMaxAttempts = 2
        };
        var dispatchStore = new TestEventDispatchStore(dispatchItem);
        var runtimeReporter = new TestEventDispatchRuntimeReporter();
        var publishException = new InvalidOperationException("Simulated Wolverine terminal publish failure.");
        var messageBus = new TestMessageBus(hasDestinations: true, publishException);
        var service = new WolverineEventDispatchHostedService(
            options,
            dispatchStore,
            runtimeReporter,
            messageBus,
            NullLogger<WolverineEventDispatchHostedService>.Instance);

        await service.DispatchOnceAsync();

        Assert.Empty(messageBus.PublishedMessages);
        Assert.Empty(await dispatchStore.ReadPendingAsync(10));
        Assert.Collection(
            dispatchStore.AppliedReports,
            report => Assert.Equal(EventDispatchExecutionOutcomes.Started, report.Outcome),
            report =>
            {
                Assert.Equal(EventDispatchExecutionOutcomes.Failed, report.Outcome);
                Assert.Equal("Simulated Wolverine terminal publish failure.", report.Error);
                Assert.Equal("bounded-fixed-delay", report.Metadata["retryPolicy"]);
                Assert.Equal("2", report.Metadata["retryMaxAttempts"]);
                Assert.Equal("30", report.Metadata["retryDelaySeconds"]);
                Assert.Equal("dispatch-store-delayed-eligibility", report.Metadata["retryDurability"]);
                Assert.Equal("provider-managed", report.Metadata["retryScope"]);
                Assert.Equal("max-attempts-exhausted", report.Metadata["retryOutcome"]);
                Assert.Equal("true", report.Metadata["retryExhausted"]);
                Assert.Equal("true", report.Metadata["terminalFailure"]);
                Assert.Equal("publish", report.Metadata["routing"]);
                Assert.Equal(typeof(InvalidOperationException).FullName, report.Metadata["exceptionType"]);
                Assert.False(report.Metadata.ContainsKey("nextRetryAtUtc"));
            });
        Assert.Collection(
            runtimeReporter.Reported,
            report => Assert.Equal(EventDispatchExecutionOutcomes.Started, report.Outcome),
            report => Assert.Equal(EventDispatchExecutionOutcomes.Failed, report.Outcome));
    }

    private sealed class TestEventDispatchStore(params EventDispatchItem[] items) : IEventDispatchStore
    {
        private readonly List<EventDispatchItem> pendingItems = [.. items];

        public IReadOnlyList<string> OutboxIds { get; } = items
            .Select(static item => item.OutboxId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static outboxId => outboxId, StringComparer.OrdinalIgnoreCase)
            .ToArray();

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
            if (report.Outcome is EventDispatchExecutionOutcomes.Succeeded or EventDispatchExecutionOutcomes.Skipped ||
                report.Outcome == EventDispatchExecutionOutcomes.Failed && EventDispatchRuntimeMetadataKeys.IsTerminalFailure(report.Metadata))
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

    private sealed class TestMessageBus(bool hasDestinations, Exception? publishException = null) : IMessageBus
    {
        private readonly IReadOnlyList<Envelope> destinations = hasDestinations ? [new Envelope(new object())] : [];

        public string? TenantId { get; set; }

        public List<EventPublication> PublishedMessages { get; } = [];
        public List<TestSentMessage> SentMessages { get; } = [];

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
            if (publishException is not null)
            {
                throw publishException;
            }

            if (message is EventPublication publication)
            {
                PublishedMessages.Add(publication);
            }

            return ValueTask.CompletedTask;
        }

        public ValueTask SendAsync<T>(T message, DeliveryOptions? options = null)
        {
            SentMessages.Add(new TestSentMessage(message!, options));
            return ValueTask.CompletedTask;
        }
    }

    private sealed record TestSentMessage(object Message, DeliveryOptions? Options);
}
