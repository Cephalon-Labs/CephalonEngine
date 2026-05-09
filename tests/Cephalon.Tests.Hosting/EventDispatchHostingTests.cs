using System.Net;
using System.Net.Http.Json;
using System.Globalization;
using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Technologies;
using Cephalon.AspNetCore.Hosting;
using Cephalon.Data.EntityFramework.Registration;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Manifest;
using Cephalon.Eventing.Registration;
using Cephalon.Eventing.Services;
using Cephalon.Eventing.Wolverine.Registration;
using Cephalon.Tests.Support;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Hosting;

public sealed class EventDispatchHostingTests
{
    [Fact]
    public async Task MapCephalonExposesEventDispatchRuntimeAndStateEndpoints()
    {
        var databaseName = $"cephalon-hosting-event-dispatch-{Guid.NewGuid():N}";
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.AddCephalon(engine =>
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
                options.DispatchBatchSize = 5;
                options.DispatchPollingIntervalSeconds = 2;
                options.RetryDelaySeconds = 20;
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();
        var runtimeDescriptors = await client.GetFromJsonAsync<EventDispatchRuntimeDescriptor[]>("/engine/event-dispatch-runtimes");
        var runtimeDescriptor = await client.GetFromJsonAsync<EventDispatchRuntimeDescriptor>("/engine/event-dispatch-runtimes/wolverine-dispatch-loop");
        var outboxes = await client.GetFromJsonAsync<OutboxDescriptor[]>("/engine/outboxes");
        var initialStates = await client.GetFromJsonAsync<EventDispatchRuntimeState[]>("/engine/event-dispatches");
        var initialTerminalFailures = await client.GetFromJsonAsync<EventDispatchRuntimeState[]>("/engine/event-dispatches/terminal-failures");
        var missingStateResponse = await client.GetAsync("/engine/event-dispatches/entity-framework-outbox");

        Assert.NotNull(runtimeDescriptors);
        var descriptor = Assert.Single(runtimeDescriptors);
        Assert.Equal("wolverine-dispatch-loop", descriptor.Id);
        Assert.Equal("wolverine", descriptor.Metadata["adapter"]);
        Assert.Equal("bounded-fixed-delay", descriptor.Metadata["retryPolicy"]);
        Assert.Equal("3", descriptor.Metadata["retryMaxAttempts"]);
        Assert.Equal("20", descriptor.Metadata["retryDelaySeconds"]);
        Assert.Equal("dispatch-store-delayed-eligibility", descriptor.Metadata["retryDurability"]);
        Assert.Equal("provider-managed", descriptor.Metadata["retryScope"]);
        Assert.Equal(["entity-framework-outbox"], descriptor.OutboxIds);
        Assert.NotNull(runtimeDescriptor);
        Assert.Equal("wolverine-dispatch-loop", runtimeDescriptor.Id);
        Assert.NotNull(outboxes);
        var outbox = Assert.Single(outboxes);
        Assert.Equal("wolverine-managed", outbox.DispatchPolicy.PolicyId);
        Assert.Equal("runtime-managed", outbox.DispatchPolicy.ExecutionMode);
        Assert.Equal("wolverine-dispatch-loop", outbox.DispatchPolicy.RuntimeId);
        Assert.NotNull(initialStates);
        Assert.Empty(initialStates);
        Assert.NotNull(initialTerminalFailures);
        Assert.Empty(initialTerminalFailures);
        Assert.Equal(HttpStatusCode.NotFound, missingStateResponse.StatusCode);

        var reporter = app.Services.GetRequiredService<IEventDispatchRuntimeReporter>();
        await reporter.ReportAsync(new EventDispatchExecutionReport(
            outboxId: "entity-framework-outbox",
            channelId: "catalog-events",
            outcome: EventDispatchExecutionOutcomes.RetryScheduled,
            observedAtUtc: new DateTimeOffset(2026, 04, 11, 09, 30, 00, TimeSpan.Zero),
            messageId: "evt-900",
            attempt: 2,
            error: "Dispatch retry is pending.",
            metadata: new Dictionary<string, string>
            {
                ["publisherId"] = "wolverine-dispatch-loop",
                ["dispatchBridge"] = "wolverine-managed",
                [EventDispatchRuntimeMetadataKeys.NextRetryAtUtc] = "2026-04-11T09:45:00.0000000+00:00",
                [EventDispatchRuntimeMetadataKeys.RetryPolicy] = "bounded-fixed-delay",
                [EventDispatchRuntimeMetadataKeys.RetryMaxAttempts] = "3",
                [EventDispatchRuntimeMetadataKeys.RetryDelaySeconds] = "20"
            }));

        var states = await client.GetFromJsonAsync<EventDispatchRuntimeState[]>("/engine/event-dispatches");
        var state = await client.GetFromJsonAsync<EventDispatchRuntimeState>("/engine/event-dispatches/entity-framework-outbox");
        var enrichedRuntimeDescriptor = await client.GetFromJsonAsync<EventDispatchRuntimeDescriptor>("/engine/event-dispatch-runtimes/wolverine-dispatch-loop");
        var snapshot = await client.GetFromJsonAsync<Cephalon.Engine.Runtime.RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(states);
        var reportedState = Assert.Single(states);
        Assert.Equal("entity-framework-outbox", reportedState.OutboxId);
        Assert.Equal("retry-scheduled", reportedState.LastOutcome);
        Assert.True(reportedState.RetryPending);
        Assert.Equal("evt-900", reportedState.LastMessageId);
        Assert.Equal("bounded-fixed-delay", reportedState.Metadata["retryPolicy"]);
        Assert.Equal("3", reportedState.Metadata["retryMaxAttempts"]);
        Assert.Equal("20", reportedState.Metadata["retryDelaySeconds"]);
        Assert.NotNull(state);
        Assert.Equal("entity-framework-outbox", state.OutboxId);
        Assert.NotNull(enrichedRuntimeDescriptor);
        Assert.True(enrichedRuntimeDescriptor.Summary.HasReports);
        Assert.Equal(["entity-framework-outbox"], enrichedRuntimeDescriptor.Summary.ReportedOutboxIds);
        Assert.Equal("entity-framework-outbox", enrichedRuntimeDescriptor.Summary.LastOutboxId);
        Assert.Equal("retry-scheduled", enrichedRuntimeDescriptor.Summary.LastOutcome);
        Assert.Equal("evt-900", enrichedRuntimeDescriptor.Summary.LastMessageId);
        Assert.Equal(2, enrichedRuntimeDescriptor.Summary.LastAttempt);
        Assert.Equal(1, enrichedRuntimeDescriptor.Summary.TotalReports);
        Assert.Equal(1, enrichedRuntimeDescriptor.Summary.RetryPendingCount);
        Assert.NotNull(snapshot);
        Assert.Single(snapshot.EventDispatchRuntimes);
        Assert.Single(snapshot.EventDispatchStates);
        Assert.Equal("wolverine-dispatch-loop", snapshot.EventDispatchRuntimes[0].Id);
        Assert.Equal(1, snapshot.EventDispatchRuntimes[0].Summary.TotalReports);
        Assert.Equal("entity-framework-outbox", snapshot.EventDispatchStates[0].OutboxId);

        await reporter.ReportAsync(new EventDispatchExecutionReport(
            outboxId: "entity-framework-outbox",
            channelId: "catalog-events",
            outcome: EventDispatchExecutionOutcomes.Failed,
            observedAtUtc: new DateTimeOffset(2026, 04, 11, 09, 50, 00, TimeSpan.Zero),
            messageId: "evt-900",
            attempt: 3,
            error: "Dispatch retry budget exhausted.",
            metadata: new Dictionary<string, string>
            {
                ["publisherId"] = "wolverine-dispatch-loop",
                ["dispatchBridge"] = "wolverine-managed",
                [EventDispatchRuntimeMetadataKeys.RetryPolicy] = "bounded-fixed-delay",
                [EventDispatchRuntimeMetadataKeys.RetryMaxAttempts] = "3",
                [EventDispatchRuntimeMetadataKeys.RetryDelaySeconds] = "20",
                [EventDispatchRuntimeMetadataKeys.RetryOutcome] = "max-attempts-exhausted",
                [EventDispatchRuntimeMetadataKeys.RetryExhausted] = "true",
                [EventDispatchRuntimeMetadataKeys.TerminalFailure] = "true"
            }));

        var terminalFailures = await client.GetFromJsonAsync<EventDispatchRuntimeState[]>("/engine/event-dispatches/terminal-failures");
        var terminalState = await client.GetFromJsonAsync<EventDispatchRuntimeState>("/engine/event-dispatches/entity-framework-outbox");
        var terminalRuntimeDescriptor = await client.GetFromJsonAsync<EventDispatchRuntimeDescriptor>("/engine/event-dispatch-runtimes/wolverine-dispatch-loop");
        var terminalEventingSurfaces = await client.GetFromJsonAsync<TechnologyRuntimeSurface[]>("/engine/technology-surfaces/event-driven-integration");
        var terminalSnapshot = await client.GetFromJsonAsync<Cephalon.Engine.Runtime.RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(terminalFailures);
        var terminalFailure = Assert.Single(terminalFailures);
        Assert.Equal("entity-framework-outbox", terminalFailure.OutboxId);
        Assert.True(terminalFailure.TerminalFailure);
        Assert.Equal(1, terminalFailure.TerminalFailureCount);
        Assert.False(terminalFailure.RetryPending);
        Assert.NotNull(terminalState);
        Assert.True(terminalState.TerminalFailure);
        Assert.Equal("max-attempts-exhausted", terminalState.Metadata["retryOutcome"]);
        Assert.NotNull(terminalRuntimeDescriptor);
        Assert.Equal(2, terminalRuntimeDescriptor.Summary.TotalReports);
        Assert.Equal(1, terminalRuntimeDescriptor.Summary.TerminalFailureCount);
        Assert.Equal(1, terminalRuntimeDescriptor.Summary.TerminalOutboxCount);
        Assert.True(terminalRuntimeDescriptor.Summary.HasTerminalFailures);
        Assert.Equal(0, terminalRuntimeDescriptor.Summary.RetryPendingCount);
        Assert.NotNull(terminalEventingSurfaces);
        var terminalDispatchSurface = Assert.Single(terminalEventingSurfaces, surface => surface.SurfaceId == "event-dispatches");
        var terminalDispatchEntry = Assert.Single(terminalDispatchSurface.Entries, entry => entry.Id == "entity-framework-outbox");
        Assert.Equal("true", terminalDispatchEntry.Metadata["terminalFailure"]);
        Assert.Equal("1", terminalDispatchEntry.Metadata["terminalFailureCount"]);
        var terminalRemediationSurface = Assert.Single(terminalEventingSurfaces, surface => surface.SurfaceId == "event-dispatch-remediations");
        var terminalRemediationEntry = Assert.Single(terminalRemediationSurface.Entries, entry => entry.Id == "entity-framework-outbox:evt-900");
        Assert.Equal("terminal-failure", terminalRemediationEntry.Metadata["remediationState"]);
        Assert.Equal("inspect-terminal-failure-before-replay", terminalRemediationEntry.Metadata["recommendedAction"]);
        Assert.Equal("bounded-dispatch-store-command-ready", terminalRemediationEntry.Metadata["operatorCommandState"]);
        Assert.Equal("retry-now-ready", terminalRemediationEntry.Metadata["replayCommand"]);
        Assert.Equal("ready", terminalRemediationEntry.Metadata["retryLaterCommand"]);
        Assert.Equal("ready", terminalRemediationEntry.Metadata["quarantineCommand"]);
        Assert.Equal("ready", terminalRemediationEntry.Metadata["skipCommand"]);
        Assert.Equal("/engine/event-dispatches/{outboxId}/commands/{operationId}", terminalRemediationEntry.Metadata["operatorCommandRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/{commandId}", terminalRemediationEntry.Metadata["operatorCommandResultRoute"]);
        Assert.Equal("false", terminalRemediationEntry.Metadata["wolverineRequired"]);
        var terminalRuntimeSurface = Assert.Single(terminalEventingSurfaces, surface => surface.SurfaceId == "event-dispatch-runtimes");
        var terminalRuntimeEntry = Assert.Single(terminalRuntimeSurface.Entries, entry => entry.Id == "wolverine-dispatch-loop");
        Assert.Equal("1", terminalRuntimeEntry.Metadata["reportedTerminalFailureCount"]);
        Assert.Equal("1", terminalRuntimeEntry.Metadata["reportedTerminalOutboxCount"]);
        Assert.Equal("true", terminalRuntimeEntry.Metadata["reportedHasTerminalFailures"]);
        Assert.NotNull(terminalSnapshot);
        Assert.Equal(1, terminalSnapshot.EventDispatchRuntimes[0].Summary.TerminalFailureCount);
        Assert.Equal(1, terminalSnapshot.EventDispatchRuntimes[0].Summary.TerminalOutboxCount);
        Assert.True(terminalSnapshot.EventDispatchStates[0].TerminalFailure);
    }

    [Fact]
    public async Task MapCephalonRunsEventDispatchRemediationCommandWithoutWolverine()
    {
        var databaseName = $"cephalon-hosting-event-dispatch-command-{Guid.NewGuid():N}";
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "Microservice",
                patterns: ["CQRS", "Outbox"],
                technologies: ["EventDrivenIntegration"],
                transports: ["RestApi"],
                data: new DataSettings(
                    provider: "EntityFramework",
                    outboxEnabled: true),
                messaging: new MessagingSettings(provider: "InMemoryChannels")));
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
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();
        var capabilities = await client.GetFromJsonAsync<CapabilityManifest[]>("/engine/capabilities");
        Assert.NotNull(capabilities);
        var remediationCapability = Assert.Single(capabilities, capability => capability.Key == "eventing.dispatch-remediation");
        Assert.Equal("false", remediationCapability.Metadata["wolverineRequired"]);
        Assert.Equal("retry-now,retry-later,skip,quarantine", remediationCapability.Metadata["operationIds"]);
        Assert.Equal("available", remediationCapability.Metadata["commandRuntimeState"]);
        Assert.Equal("256", remediationCapability.Metadata["commandHistoryLimit"]);

        var publicationResponse = await client.PostAsJsonAsync("/engine/event-publications", new
        {
            id = "evt-command-001",
            channelId = "catalog-events",
            eventType = "catalog.item.changed",
            payload = new { id = "item-command-001" },
            occurredAtUtc = new DateTimeOffset(2026, 04, 12, 08, 0, 0, TimeSpan.Zero),
            correlationId = "corr-command-001"
        });
        publicationResponse.EnsureSuccessStatusCode();

        await using var setupScope = app.Services.CreateAsyncScope();
        var dispatchStore = setupScope.ServiceProvider.GetRequiredService<IEventDispatchStore>();
        var reporter = app.Services.GetRequiredService<IEventDispatchRuntimeReporter>();
        var terminalReport = new EventDispatchExecutionReport(
            outboxId: "entity-framework-outbox",
            channelId: "catalog-events",
            outcome: EventDispatchExecutionOutcomes.Failed,
            observedAtUtc: new DateTimeOffset(2026, 04, 12, 08, 5, 0, TimeSpan.Zero),
            messageId: "evt-command-001",
            attempt: 1,
            error: "Dispatch exhausted before operator recovery.",
            metadata: new Dictionary<string, string>
            {
                [EventDispatchRuntimeMetadataKeys.RetryOutcome] = "max-attempts-exhausted",
                [EventDispatchRuntimeMetadataKeys.RetryExhausted] = "true",
                [EventDispatchRuntimeMetadataKeys.TerminalFailure] = "true"
            });
        await dispatchStore.ApplyReportAsync(terminalReport);
        await reporter.ReportAsync(terminalReport);

        var terminalPending = await dispatchStore.ReadPendingAsync(10);
        Assert.Empty(terminalPending);

        var commandResponse = await client.PostAsJsonAsync(
            "/engine/event-dispatches/entity-framework-outbox/commands/retry-now",
            new
            {
                commandId = "cmd-command-001-retry",
                messageId = "evt-command-001",
                channelId = "catalog-events",
                reason = "Downstream recovered.",
                actorId = "operator-001",
                correlationId = "corr-command-operator-001"
            });
        commandResponse.EnsureSuccessStatusCode();
        var commandResult = await commandResponse.Content.ReadFromJsonAsync<EventDispatchRemediationResult>();
        var commandStates = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState[]>("/engine/event-dispatch-remediation-commands");
        var commandState = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState>("/engine/event-dispatch-remediation-commands/cmd-command-001-retry");
        var commandStatesByOutbox = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState[]>("/engine/event-dispatch-remediation-commands/outboxes/entity-framework-outbox");
        var acceptedCommandStates = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState[]>("/engine/event-dispatch-remediation-commands/outcomes/accepted");
        var remediatedState = await client.GetFromJsonAsync<EventDispatchRuntimeState>("/engine/event-dispatches/entity-framework-outbox");
        await using var readScope = app.Services.CreateAsyncScope();
        var readStore = readScope.ServiceProvider.GetRequiredService<IEventDispatchStore>();
        var pending = await readStore.ReadPendingAsync(10);

        Assert.NotNull(commandResult);
        Assert.Equal(EventDispatchRemediationOutcomes.Accepted, commandResult.Outcome);
        Assert.Equal(EventDispatchExecutionOutcomes.RetryScheduled, commandResult.DispatchOutcome);
        Assert.Equal("cmd-command-001-retry", commandResult.CommandId);
        Assert.Equal("operator-001", commandResult.Metadata["operatorActorId"]);
        Assert.NotNull(commandStates);
        var listedCommandState = Assert.Single(commandStates);
        Assert.Equal("cmd-command-001-retry", listedCommandState.CommandId);
        Assert.NotNull(commandState);
        Assert.Equal("cmd-command-001-retry", commandState.CommandId);
        Assert.Equal(EventDispatchRemediationOutcomes.Accepted, commandState.Outcome);
        Assert.Equal(EventDispatchExecutionOutcomes.RetryScheduled, commandState.DispatchOutcome);
        Assert.Equal("operator-001", commandState.Metadata["operatorActorId"]);
        Assert.NotNull(commandStatesByOutbox);
        Assert.Equal("cmd-command-001-retry", Assert.Single(commandStatesByOutbox).CommandId);
        Assert.NotNull(acceptedCommandStates);
        Assert.Equal("cmd-command-001-retry", Assert.Single(acceptedCommandStates).CommandId);
        Assert.NotNull(remediatedState);
        Assert.Equal(EventDispatchExecutionOutcomes.RetryScheduled, remediatedState.LastOutcome);
        Assert.True(remediatedState.RetryPending);
        Assert.False(remediatedState.TerminalFailure);
        Assert.Equal("retry-now", remediatedState.Metadata["operatorCommand"]);
        Assert.Equal("dispatch-store", remediatedState.Metadata[EventDispatchRuntimeMetadataKeys.RetryDurability]);
        var pendingItem = Assert.Single(pending);
        Assert.Equal("evt-command-001", pendingItem.MessageId);
        Assert.Equal(2, pendingItem.DispatchAttemptCount);

        var rejectedResponse = await client.PostAsJsonAsync(
            "/engine/event-dispatches/entity-framework-outbox/commands/retry-later",
            new
            {
                commandId = "cmd-command-001-retry-later-rejected",
                messageId = "evt-command-001",
                channelId = "catalog-events",
                reason = "Operator asked to delay but did not pick a timestamp.",
                actorId = "operator-001",
                correlationId = "corr-command-operator-002"
            });
        Assert.Equal(HttpStatusCode.Conflict, rejectedResponse.StatusCode);
        var rejectedResult = await rejectedResponse.Content.ReadFromJsonAsync<EventDispatchRemediationResult>();
        var rejectedCommandState = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState>("/engine/event-dispatch-remediation-commands/cmd-command-001-retry-later-rejected");
        var rejectedCommandStates = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState[]>("/engine/event-dispatch-remediation-commands/outcomes/rejected");
        var allCommandStates = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState[]>("/engine/event-dispatch-remediation-commands");

        Assert.NotNull(rejectedResult);
        Assert.Equal(EventDispatchRemediationOutcomes.Rejected, rejectedResult.Outcome);
        Assert.NotNull(rejectedCommandState);
        Assert.Equal("cmd-command-001-retry-later-rejected", rejectedCommandState.CommandId);
        Assert.Equal(EventDispatchRemediationOutcomes.Rejected, rejectedCommandState.Outcome);
        Assert.Contains("next attempt", rejectedCommandState.Error, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(rejectedCommandStates);
        Assert.Equal("cmd-command-001-retry-later-rejected", Assert.Single(rejectedCommandStates).CommandId);
        Assert.NotNull(allCommandStates);
        Assert.Equal(2, allCommandStates.Length);
    }

    [Fact]
    public async Task MapCephalonExposesManagedWolverineSubscriptionExecutionCapabilityAndRuntimeSurfaces()
    {
        var databaseName = $"cephalon-hosting-event-subscriptions-{Guid.NewGuid():N}";
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.AddCephalon(engine =>
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
                options.EnableSubscriptionExecution = true;
                options.DispatchBatchSize = 5;
                options.DispatchPollingIntervalSeconds = 2;
                options.RetryDelaySeconds = 20;
                options.SubscriptionRetryDelaySeconds = 45;
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();
        var capabilities = await client.GetFromJsonAsync<CapabilityManifest[]>("/engine/capabilities");
        var eventingSurfaces = await client.GetFromJsonAsync<TechnologyRuntimeSurface[]>("/engine/technology-surfaces/event-driven-integration");
        var snapshot = await client.GetFromJsonAsync<Cephalon.Engine.Runtime.RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(capabilities);
        var managedCapability = Assert.Single(capabilities, capability => capability.Key == "eventing.subscribe");
        Assert.Equal("wolverine-managed", managedCapability.Metadata["executionOwnership"]);
        Assert.Equal("message-handler", managedCapability.Metadata["executionMode"]);
        Assert.Equal("wolverine-subscription-execution", managedCapability.Metadata["executionRuntimeId"]);
        Assert.Equal("wolverine-dispatch-loop", managedCapability.Metadata["triggerRuntimeId"]);
        Assert.Equal("bounded-fixed-delay", managedCapability.Metadata["retryPolicy"]);
        Assert.Equal("3", managedCapability.Metadata["retryMaxAttempts"]);
        Assert.Equal("45", managedCapability.Metadata["retryDelaySeconds"]);

        Assert.NotNull(eventingSurfaces);
        var subscriptionSurface = Assert.Single(eventingSurfaces, surface => surface.SurfaceId == "event-subscriptions");
        var managedSubscription = Assert.Single(subscriptionSurface.Entries, entry => entry.Id == "audit-projector");
        Assert.Equal("audit", managedSubscription.Metadata["channelId"]);
        Assert.Equal("wolverine-managed", managedSubscription.Metadata["dispatchRuntime"]);
        Assert.Equal("runtime-bound", managedSubscription.Metadata["subscriptionRuntime"]);
        Assert.Equal("wolverine-subscription-execution", managedSubscription.Metadata["executionRuntimeId"]);
        Assert.Equal("wolverine-managed", managedSubscription.Metadata["executionOwnership"]);
        Assert.Equal("message-handler", managedSubscription.Metadata["executionMode"]);
        Assert.Equal("wolverine", managedSubscription.Metadata["binding.adapter"]);
        Assert.Equal("wolverine-dispatch-loop", managedSubscription.Metadata["binding.trigger"]);
        Assert.Equal("bounded-fixed-delay", managedSubscription.Metadata["binding.retryPolicy"]);
        Assert.Equal("3", managedSubscription.Metadata["binding.retryMaxAttempts"]);
        Assert.Equal("45", managedSubscription.Metadata["binding.retryDelaySeconds"]);
        Assert.Equal("audit-projector-pump", managedSubscription.Metadata["hostedExecutionId"]);
        Assert.Equal("audit-subscription-flow", managedSubscription.Metadata["executionGraphId"]);

        var adapterSurface = Assert.Single(eventingSurfaces, surface => surface.SurfaceId == "wolverine-adapter");
        var adapterEntry = Assert.Single(adapterSurface.Entries);
        Assert.Equal("wolverine-managed", adapterEntry.Metadata["dispatchBridge"]);
        Assert.Equal("bounded-fixed-delay", adapterEntry.Metadata["dispatchRetryPolicy"]);
        Assert.Equal("3", adapterEntry.Metadata["dispatchMaxAttempts"]);
        Assert.Equal("20", adapterEntry.Metadata["retryDelaySeconds"]);
        Assert.Equal("wolverine-managed", adapterEntry.Metadata["subscriptionExecution"]);
        Assert.Equal("wolverine-subscription-execution", adapterEntry.Metadata["subscriptionExecutionRuntimeId"]);
        Assert.Equal("1", adapterEntry.Metadata["managedSubscriptionCount"]);
        Assert.Equal("audit-projector", adapterEntry.Metadata["managedSubscriptionIds"]);
        Assert.Equal("45", adapterEntry.Metadata["subscriptionRetryDelaySeconds"]);
        Assert.Equal("3", adapterEntry.Metadata["subscriptionMaxAttempts"]);
        Assert.Equal("bounded-fixed-delay", adapterEntry.Metadata["subscriptionRetryPolicy"]);

        Assert.NotNull(snapshot);
        var snapshotSubscription = Assert.Single(
            snapshot.TechnologySurfaces.Single(surface => surface.SurfaceId == "event-subscriptions").Entries,
            entry => entry.Id == "audit-projector");
        Assert.Equal("wolverine-managed", snapshotSubscription.Metadata["dispatchRuntime"]);
        Assert.Equal("runtime-bound", snapshotSubscription.Metadata["subscriptionRuntime"]);
        var snapshotAdapter = Assert.Single(
            snapshot.TechnologySurfaces.Single(surface => surface.SurfaceId == "wolverine-adapter").Entries);
        Assert.Equal("wolverine-managed", snapshotAdapter.Metadata["subscriptionExecution"]);
        Assert.Equal("1", snapshotAdapter.Metadata["managedSubscriptionCount"]);
    }

    [Fact]
    public async Task MapCephalonPublishesCoreInProcessEventPublicationThroughOperatorRoute()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "Microservice",
                patterns: ["CQRS"],
                technologies: ["EventDrivenIntegration"],
                transports: ["RestApi"]));
            engine.AddModule(new TechnologyPackContributionModule());
            engine.AddEventing(options =>
            {
                options.EnableInProcessSubscriptionExecution = true;
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();

        var client = app.GetTestClient();
        var response = await client.PostAsJsonAsync(
            "/engine/event-publications",
            new
            {
                id = "audit-route-001",
                channelId = "audit",
                eventType = "audit.created",
                payload = new
                {
                    id = "audit-route-001"
                },
                occurredAtUtc = new DateTimeOffset(2026, 04, 29, 9, 0, 0, TimeSpan.Zero),
                contentType = "application/json",
                correlationId = "corr-audit-route-001",
                tenantId = "tenant-operator-001",
                actorId = "hosting-operator",
                headers = new Dictionary<string, string>
                {
                    ["x-test"] = "operator-route"
                },
                metadata = new Dictionary<string, string>
                {
                    ["requestedBy"] = "hosting-test"
                }
            });
        var result = await response.Content.ReadFromJsonAsync<EventPublicationResult>();
        var missingChannelResponse = await client.PostAsJsonAsync(
            "/engine/event-publications",
            new
            {
                id = "audit-route-missing",
                channelId = "missing",
                eventType = "audit.created",
                payload = new
                {
                    id = "audit-route-missing"
                }
            });

        var probe = app.Services.GetRequiredService<ManagedAuditProjectorProbe>();
        var runtimeCatalog = app.Services.GetRequiredService<IEventSubscriptionRuntimeCatalog>();
        var publicationRuntimeCatalog = app.Services.GetRequiredService<IEventPublicationRuntimeCatalog>();
        var capabilities = await client.GetFromJsonAsync<CapabilityManifest[]>("/engine/capabilities");
        var eventingSurfaces = await client.GetFromJsonAsync<TechnologyRuntimeSurface[]>("/engine/technology-surfaces/event-driven-integration");
        var publicationStates = await client.GetFromJsonAsync<EventPublicationRuntimeState[]>("/engine/event-publications/runtime");
        var publicationState = await client.GetFromJsonAsync<EventPublicationRuntimeState>("/engine/event-publications/runtime/audit-route-001");
        var channelPublicationStates = await client.GetFromJsonAsync<EventPublicationRuntimeState[]>("/engine/event-publications/runtime/channels/audit");
        var missingPublicationStateResponse = await client.GetAsync("/engine/event-publications/runtime/missing-publication");
        var snapshot = await client.GetFromJsonAsync<Cephalon.Engine.Runtime.RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.Equal("audit-route-001", result.PublicationId);
        Assert.Equal("audit", result.ChannelId);
        Assert.Equal("audit.created", result.EventType);
        Assert.Equal(EventPublicationOutcomes.Accepted, result.Outcome);
        Assert.Equal("aspnetcore-operator-route", result.Metadata["trigger"]);
        Assert.Equal("/engine/event-publications", result.Metadata["route"]);
        Assert.Equal("hosting-operator", result.Metadata["actorId"]);
        Assert.Equal("hosting-test", result.Metadata["requestedBy"]);
        Assert.Equal("cephalon-eventing", result.Metadata["publicationDispatcher"]);
        Assert.Equal("available", result.Metadata["publicationRuntimeState"]);
        Assert.Equal(HttpStatusCode.NotFound, missingChannelResponse.StatusCode);

        Assert.Equal(1, probe.TotalAttempts);
        Assert.Equal(1, probe.SuccessfulAttempts);
        Assert.Equal("audit-route-001", probe.LastMessageId);

        var runtimeState = Assert.Single(runtimeCatalog.States);
        Assert.Equal("audit-projector", runtimeState.SubscriptionId);
        Assert.Equal(EventSubscriptionExecutionOutcomes.Succeeded, runtimeState.LastOutcome);
        Assert.Equal("audit-route-001", runtimeState.LastMessageId);
        Assert.Equal("in-process-direct", runtimeState.Metadata["executionMode"]);
        Assert.Equal("cephalon-managed", runtimeState.Metadata["executionOwnership"]);
        Assert.Equal("aspnetcore-operator-route", runtimeState.Metadata["publicationMetadata.trigger"]);
        Assert.Equal("/engine/event-publications", runtimeState.Metadata["publicationMetadata.route"]);
        Assert.Equal("hosting-operator", runtimeState.Metadata["publicationMetadata.actorId"]);

        var reportedPublicationState = Assert.Single(publicationRuntimeCatalog.States);
        Assert.Equal("audit-route-001", reportedPublicationState.PublicationId);
        Assert.Equal("audit", reportedPublicationState.LastChannelId);
        Assert.Equal("audit.created", reportedPublicationState.LastEventType);
        Assert.Equal(EventPublicationRuntimeOutcomes.Succeeded, reportedPublicationState.LastOutcome);
        Assert.Equal(1, reportedPublicationState.SucceededCount);
        Assert.Equal(0, reportedPublicationState.FailedCount);
        Assert.Equal(0, reportedPublicationState.SkippedCount);
        Assert.Equal(1, reportedPublicationState.MatchedSubscriptionCount);
        Assert.Equal(1, reportedPublicationState.StartedSubscriptionCount);
        Assert.Equal(1, reportedPublicationState.SucceededSubscriptionCount);
        Assert.Equal(0, reportedPublicationState.FailedSubscriptionCount);
        Assert.Equal(0, reportedPublicationState.RetryScheduledSubscriptionCount);
        Assert.Equal(0, reportedPublicationState.SkippedSubscriptionCount);
        Assert.Equal("reported", reportedPublicationState.Metadata["publicationRuntimeState"]);
        Assert.Equal("aspnetcore-operator-route", reportedPublicationState.Metadata["publicationMetadata.trigger"]);
        Assert.Equal("/engine/event-publications", reportedPublicationState.Metadata["publicationMetadata.route"]);
        Assert.Equal("hosting-operator", reportedPublicationState.Metadata["publicationMetadata.actorId"]);
        Assert.Equal("audit-projector", reportedPublicationState.Metadata["subscriptionIds"]);

        Assert.NotNull(publicationStates);
        var routePublicationState = Assert.Single(publicationStates);
        Assert.Equal("audit-route-001", routePublicationState.PublicationId);
        Assert.NotNull(publicationState);
        Assert.Equal(EventPublicationRuntimeOutcomes.Succeeded, publicationState.LastOutcome);
        Assert.NotNull(channelPublicationStates);
        Assert.Single(channelPublicationStates);
        Assert.Equal(HttpStatusCode.NotFound, missingPublicationStateResponse.StatusCode);

        Assert.NotNull(capabilities);
        var publishCapability = Assert.Single(capabilities, capability => capability.Key == "eventing.publish");
        Assert.Equal("available", publishCapability.Metadata["publicationDispatcher"]);
        Assert.Equal("available", publishCapability.Metadata["publicationRuntimeState"]);

        Assert.NotNull(eventingSurfaces);
        var publisherSurface = Assert.Single(eventingSurfaces, surface => surface.SurfaceId == "event-publishers");
        var publisherEntry = Assert.Single(publisherSurface.Entries);
        Assert.Equal("in-process-event-publisher", publisherEntry.Id);
        Assert.Equal("available", publisherEntry.Metadata["publicationDispatcher"]);
        Assert.Equal("reported", publisherEntry.Metadata["publicationRuntimeState"]);
        Assert.Equal("1", publisherEntry.Metadata["publicationStateCount"]);
        Assert.Equal("1", publisherEntry.Metadata["publicationSucceededCount"]);
        Assert.Equal("audit-route-001", publisherEntry.Metadata["lastPublicationId"]);
        Assert.Equal("succeeded", publisherEntry.Metadata["lastPublicationOutcome"]);

        Assert.NotNull(snapshot);
        var snapshotPublicationState = Assert.Single(snapshot.EventPublicationStates);
        Assert.Equal("audit-route-001", snapshotPublicationState.PublicationId);
    }

    [Fact]
    public async Task MapCephalonRoutesCoreInProcessEventPublicationFromConfigurationWithoutWolverine()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Engine:Messaging:InProcessSubscriptions:EnableExecution"] = "true",
            ["Engine:Messaging:Publications:Routing:Enabled"] = "true",
            ["Engine:Messaging:Publications:Routing:AutoChannelId"] = "auto",
            ["Engine:Messaging:Publications:Routing:RejectMismatchedExplicitChannel"] = "true",
            ["Engine:Messaging:Publications:Routing:Routes:audit.created"] = "audit"
        });
        builder.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "Microservice",
                patterns: ["CQRS"],
                technologies: ["EventDrivenIntegration"],
                transports: ["RestApi"]));
            engine.AddModule(new TechnologyPackContributionModule());
            engine.AddEventingFromConfiguration(builder.Configuration);
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();

        var client = app.GetTestClient();
        var response = await client.PostAsJsonAsync(
            "/engine/event-publications",
            new
            {
                id = "audit-routed-001",
                channelId = "auto",
                eventType = "audit.created",
                payload = new
                {
                    id = "audit-routed-001"
                },
                occurredAtUtc = new DateTimeOffset(2026, 05, 10, 12, 0, 0, TimeSpan.Zero),
                correlationId = "corr-audit-routed-001",
                metadata = new Dictionary<string, string>
                {
                    ["requestedBy"] = "routing-hosting-test"
                }
            });

        var result = await response.Content.ReadFromJsonAsync<EventPublicationResult>();
        var probe = app.Services.GetRequiredService<ManagedAuditProjectorProbe>();
        var publicationRuntimeCatalog = app.Services.GetRequiredService<IEventPublicationRuntimeCatalog>();
        var capabilities = await client.GetFromJsonAsync<CapabilityManifest[]>("/engine/capabilities");
        var eventingSurfaces = await client.GetFromJsonAsync<TechnologyRuntimeSurface[]>("/engine/technology-surfaces/event-driven-integration");
        var channelPublicationStates = await client.GetFromJsonAsync<EventPublicationRuntimeState[]>("/engine/event-publications/runtime/channels/audit");
        var snapshot = await client.GetFromJsonAsync<Cephalon.Engine.Runtime.RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.Equal("audit-routed-001", result.PublicationId);
        Assert.Equal("audit", result.ChannelId);
        Assert.Equal("auto", result.Metadata["requestedChannelId"]);
        Assert.Equal("audit", result.Metadata["channelId"]);
        Assert.Equal("event-type-map", result.Metadata["routingPolicy"]);
        Assert.Equal("routed", result.Metadata["routingState"]);
        Assert.Equal("auto-channel", result.Metadata["routingSource"]);
        Assert.Equal("auto", result.Metadata["routingRequestedChannelId"]);
        Assert.Equal("audit", result.Metadata["routingEffectiveChannelId"]);
        Assert.Equal("audit.created", result.Metadata["routingRule"]);
        Assert.Equal("audit", result.Metadata["routingRuleChannelId"]);
        Assert.Equal("routing-hosting-test", result.Metadata["requestedBy"]);

        Assert.Equal(1, probe.TotalAttempts);
        Assert.Equal(1, probe.SuccessfulAttempts);
        Assert.Equal("audit-routed-001", probe.LastMessageId);

        var publicationState = Assert.Single(publicationRuntimeCatalog.States);
        Assert.Equal("audit", publicationState.LastChannelId);
        Assert.Equal(EventPublicationRuntimeOutcomes.Succeeded, publicationState.LastOutcome);
        Assert.Equal("routed", publicationState.Metadata["publicationMetadata.routingState"]);
        Assert.Equal("auto", publicationState.Metadata["publicationMetadata.routingRequestedChannelId"]);
        Assert.Equal("audit", publicationState.Metadata["publicationMetadata.routingEffectiveChannelId"]);
        Assert.NotNull(channelPublicationStates);
        Assert.Single(channelPublicationStates);

        Assert.NotNull(capabilities);
        var publishCapability = Assert.Single(capabilities, capability => capability.Key == "eventing.publish");
        Assert.Equal("event-type-map", publishCapability.Metadata["publicationRoutingPolicy"]);
        Assert.Equal("1", publishCapability.Metadata["publicationRoutingRouteCount"]);
        Assert.Equal("auto", publishCapability.Metadata["publicationRoutingAutoChannelId"]);
        Assert.Equal("true", publishCapability.Metadata["publicationRoutingRejectMismatchedExplicitChannel"]);

        Assert.NotNull(eventingSurfaces);
        var publisherEntry = Assert.Single(eventingSurfaces.Single(surface => surface.SurfaceId == "event-publishers").Entries);
        Assert.Equal("event-type-map", publisherEntry.Metadata["publicationRoutingPolicy"]);
        Assert.Equal("1", publisherEntry.Metadata["publicationRoutingRouteCount"]);
        Assert.Equal("auto", publisherEntry.Metadata["publicationRoutingAutoChannelId"]);

        var superiorityEntry = Assert.Single(
            eventingSurfaces.Single(surface => surface.SurfaceId == "eventing-superiority-profile").Entries,
            entry => entry.Id == "routing-and-provider-portability");
        Assert.Equal("claimed", superiorityEntry.Metadata["status"]);
        Assert.Contains("routes=1", superiorityEntry.Metadata["runtimeEvidence"], StringComparison.Ordinal);

        Assert.NotNull(snapshot);
        var snapshotPublicationState = Assert.Single(snapshot.EventPublicationStates);
        Assert.Equal("audit", snapshotPublicationState.LastChannelId);
        var snapshotRoutingEntry = Assert.Single(
            snapshot.TechnologySurfaces.Single(surface => surface.SurfaceId == "eventing-superiority-profile").Entries,
            entry => entry.Id == "routing-and-provider-portability");
        Assert.Equal("claimed", snapshotRoutingEntry.Metadata["status"]);
    }

    [Fact]
    public async Task MapCephalonRejectsMismatchedExplicitEventPublicationRouteFromConfigurationWithoutWolverine()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Engine:Messaging:InProcessSubscriptions:EnableExecution"] = "true",
            ["Engine:Messaging:Publications:Routing:Enabled"] = "true",
            ["Engine:Messaging:Publications:Routing:RejectMismatchedExplicitChannel"] = "true",
            ["Engine:Messaging:Publications:Routing:Routes:audit.created"] = "audit"
        });
        builder.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "Microservice",
                patterns: ["CQRS"],
                technologies: ["EventDrivenIntegration"],
                transports: ["RestApi"]));
            engine.AddModule(new TechnologyPackContributionModule());
            engine.AddEventingFromConfiguration(builder.Configuration);
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();

        var client = app.GetTestClient();
        var response = await client.PostAsJsonAsync(
            "/engine/event-publications",
            new
            {
                id = "audit-route-mismatch-001",
                channelId = "catalog-events",
                eventType = "audit.created",
                payload = new
                {
                    id = "audit-route-mismatch-001"
                }
            });
        var body = await response.Content.ReadAsStringAsync();
        var probe = app.Services.GetRequiredService<ManagedAuditProjectorProbe>();
        var publicationRuntimeCatalog = app.Services.GetRequiredService<IEventPublicationRuntimeCatalog>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("does not match configured route", body, StringComparison.Ordinal);
        Assert.Equal(0, probe.TotalAttempts);
        Assert.Empty(publicationRuntimeCatalog.States);
    }

    [Fact]
    public async Task MapCephalonSchedulesCoreInProcessEventPublicationWithoutWolverine()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Engine:Messaging:InProcessSubscriptions:EnableExecution"] = "true",
            ["Engine:Messaging:Publications:Scheduling:Enabled"] = "true",
            ["Engine:Messaging:Publications:Scheduling:MaxDelayMilliseconds"] = "5000",
            ["Engine:Messaging:Publications:Scheduling:MaxPendingCount"] = "4"
        });
        builder.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "Microservice",
                patterns: ["CQRS"],
                technologies: ["EventDrivenIntegration"],
                transports: ["RestApi"]));
            engine.AddModule(new TechnologyPackContributionModule());
            engine.AddEventingFromConfiguration(builder.Configuration);
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();

        var scheduledForUtc = DateTimeOffset.UtcNow.AddMilliseconds(200);
        var client = app.GetTestClient();
        var response = await client.PostAsJsonAsync(
            "/engine/event-publications",
            new
            {
                id = "audit-scheduled-001",
                channelId = "audit",
                eventType = "audit.created",
                payload = new
                {
                    id = "audit-scheduled-001"
                },
                occurredAtUtc = new DateTimeOffset(2026, 05, 10, 10, 0, 0, TimeSpan.Zero),
                correlationId = "corr-audit-scheduled-001",
                metadata = new Dictionary<string, string>
                {
                    ["scheduledForUtc"] = scheduledForUtc.ToString("O", CultureInfo.InvariantCulture),
                    ["requestedBy"] = "scheduled-hosting-test"
                }
            });
        var result = await response.Content.ReadFromJsonAsync<EventPublicationResult>();
        var probe = app.Services.GetRequiredService<ManagedAuditProjectorProbe>();
        var publicationRuntimeCatalog = app.Services.GetRequiredService<IEventPublicationRuntimeCatalog>();
        var pendingSurfaces = await client.GetFromJsonAsync<TechnologyRuntimeSurface[]>("/engine/technology-surfaces/event-driven-integration");
        var acceptedState = Assert.Single(publicationRuntimeCatalog.States);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.Equal(EventPublicationOutcomes.Accepted, result.Outcome);
        Assert.Equal("bounded-process-local", result.Metadata["schedulePolicy"]);
        Assert.Equal("scheduled", result.Metadata["scheduleState"]);
        Assert.Equal("process-local", result.Metadata["scheduleScope"]);
        Assert.Equal("none", result.Metadata["scheduleDurability"]);
        Assert.Equal("pending", result.Metadata["scheduleDispatch"]);
        Assert.Equal("scheduled-hosting-test", result.Metadata["requestedBy"]);
        Assert.Equal(0, probe.TotalAttempts);
        Assert.Equal(EventPublicationRuntimeOutcomes.Accepted, acceptedState.LastOutcome);
        Assert.Equal("scheduled", acceptedState.Metadata["scheduleState"]);
        Assert.Equal("pending", acceptedState.Metadata["scheduleDispatch"]);

        Assert.NotNull(pendingSurfaces);
        var pendingPublisherEntry = Assert.Single(
            pendingSurfaces.Single(surface => surface.SurfaceId == "event-publishers").Entries);
        Assert.Equal("bounded-process-local", pendingPublisherEntry.Metadata["publicationSchedulingPolicy"]);
        Assert.Equal("process-local", pendingPublisherEntry.Metadata["publicationSchedulingScope"]);
        Assert.Equal("none", pendingPublisherEntry.Metadata["publicationSchedulingDurability"]);
        Assert.Equal("4", pendingPublisherEntry.Metadata["publicationSchedulingMaxPendingCount"]);
        Assert.Equal("1", pendingPublisherEntry.Metadata["scheduledPublicationPendingCount"]);

        await WaitForConditionAsync(() => probe.SuccessfulAttempts == 1);

        var completedState = Assert.Single(publicationRuntimeCatalog.States);
        var runtimeState = Assert.Single(app.Services.GetRequiredService<IEventSubscriptionRuntimeCatalog>().States);
        var completedSurfaces = await client.GetFromJsonAsync<TechnologyRuntimeSurface[]>("/engine/technology-surfaces/event-driven-integration");

        Assert.Equal(1, probe.TotalAttempts);
        Assert.Equal("audit-scheduled-001", probe.LastMessageId);
        Assert.Equal(EventPublicationRuntimeOutcomes.Succeeded, completedState.LastOutcome);
        Assert.Equal(1, completedState.AcceptedCount);
        Assert.Equal(1, completedState.SucceededCount);
        Assert.Equal("due", completedState.Metadata["publicationMetadata.scheduleState"]);
        Assert.Equal("started", completedState.Metadata["publicationMetadata.scheduleDispatch"]);
        Assert.Equal("bounded-process-local", completedState.Metadata["publicationMetadata.schedulePolicy"]);
        Assert.Equal("process-local", completedState.Metadata["publicationMetadata.scheduleScope"]);
        Assert.Equal("none", completedState.Metadata["publicationMetadata.scheduleDurability"]);
        Assert.Equal("due", runtimeState.Metadata["publicationMetadata.scheduleState"]);
        Assert.Equal("scheduled-hosting-test", runtimeState.Metadata["publicationMetadata.requestedBy"]);

        Assert.NotNull(completedSurfaces);
        var completedPublisherEntry = Assert.Single(
            completedSurfaces.Single(surface => surface.SurfaceId == "event-publishers").Entries);
        Assert.Equal("0", completedPublisherEntry.Metadata["scheduledPublicationPendingCount"]);
        Assert.Equal("succeeded", completedPublisherEntry.Metadata["lastPublicationOutcome"]);
    }

    [Fact]
    public async Task MapCephalonRejectsScheduledEventPublicationWhenSchedulingIsDisabled()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "Microservice",
                patterns: ["CQRS"],
                technologies: ["EventDrivenIntegration"],
                transports: ["RestApi"]));
            engine.AddModule(new TechnologyPackContributionModule());
            engine.AddEventing(options =>
            {
                options.EnableInProcessSubscriptionExecution = true;
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();

        var client = app.GetTestClient();
        var response = await client.PostAsJsonAsync(
            "/engine/event-publications",
            new
            {
                id = "audit-scheduled-disabled-001",
                channelId = "audit",
                eventType = "audit.created",
                payload = new
                {
                    id = "audit-scheduled-disabled-001"
                },
                metadata = new Dictionary<string, string>
                {
                    ["delayMilliseconds"] = "100"
                }
            });

        var probe = app.Services.GetRequiredService<ManagedAuditProjectorProbe>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, probe.TotalAttempts);
    }

    [Fact]
    public async Task MapCephalonRetriesCoreInProcessEventSubscriptionFailuresWithinConfiguredBound()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "Microservice",
                patterns: ["CQRS"],
                technologies: ["EventDrivenIntegration"],
                transports: ["RestApi"]));
            engine.AddModule(new TechnologyPackContributionModule());
            engine.AddEventing(options =>
            {
                options.EnableInProcessSubscriptionExecution = true;
                options.InProcessSubscriptionMaxAttempts = 2;
                options.InProcessSubscriptionRetryDelayMilliseconds = 0;
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();

        var probe = app.Services.GetRequiredService<ManagedAuditProjectorProbe>();
        probe.FailuresRemaining = 1;
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(new EventPublication(
                id: "audit-retry-001",
                channelId: "audit",
                eventType: "audit.created",
                payload: """{"id":"audit-retry-001"}""",
                occurredAtUtc: new DateTimeOffset(2026, 04, 29, 10, 0, 0, TimeSpan.Zero),
                contentType: "application/json",
                correlationId: "corr-audit-retry-001",
                tenantId: "tenant-retry-001"));
        }

        var client = app.GetTestClient();
        var runtimeCatalog = app.Services.GetRequiredService<IEventSubscriptionRuntimeCatalog>();
        var bindingCatalog = app.Services.GetRequiredService<IEventSubscriptionExecutionBindingCatalog>();
        var capabilities = await client.GetFromJsonAsync<CapabilityManifest[]>("/engine/capabilities");
        var eventingSurfaces = await client.GetFromJsonAsync<TechnologyRuntimeSurface[]>("/engine/technology-surfaces/event-driven-integration");

        Assert.Equal(2, probe.TotalAttempts);
        Assert.Equal(1, probe.SuccessfulAttempts);
        Assert.Equal("audit-retry-001", probe.LastMessageId);

        var runtimeState = Assert.Single(runtimeCatalog.States);
        Assert.Equal(EventSubscriptionExecutionOutcomes.Succeeded, runtimeState.LastOutcome);
        Assert.Equal("audit-retry-001", runtimeState.LastMessageId);
        Assert.Equal(2, runtimeState.LastAttempt);
        Assert.Equal(2, runtimeState.StartedCount);
        Assert.Equal(1, runtimeState.SucceededCount);
        Assert.Equal(0, runtimeState.FailedCount);
        Assert.Equal(1, runtimeState.RetryScheduledCount);
        Assert.False(runtimeState.RetryPending);
        Assert.Equal("2", runtimeState.Metadata["attempt"]);
        Assert.Equal("bounded-in-process", runtimeState.Metadata["retryPolicy"]);
        Assert.Equal("2", runtimeState.Metadata["retryMaxAttempts"]);
        Assert.Equal("0", runtimeState.Metadata["retryDelayMilliseconds"]);
        Assert.Equal("none", runtimeState.Metadata["retryDurability"]);
        Assert.Equal("process-local", runtimeState.Metadata["retryScope"]);

        var binding = Assert.Single(bindingCatalog.Bindings);
        Assert.Equal("bounded-in-process", binding.Metadata["retryPolicy"]);
        Assert.Equal("2", binding.Metadata["retryMaxAttempts"]);
        Assert.Equal("0", binding.Metadata["retryDelayMilliseconds"]);

        Assert.NotNull(capabilities);
        var publishCapability = Assert.Single(capabilities, capability => capability.Key == "eventing.publish");
        Assert.Equal("bounded-in-process", publishCapability.Metadata["retryPolicy"]);
        Assert.Equal("2", publishCapability.Metadata["retryMaxAttempts"]);
        var subscribeCapability = Assert.Single(capabilities, capability => capability.Key == "eventing.subscribe");
        Assert.Equal("bounded-in-process", subscribeCapability.Metadata["retryPolicy"]);
        Assert.Equal("process-local", subscribeCapability.Metadata["retryScope"]);

        Assert.NotNull(eventingSurfaces);
        var publisherSurface = Assert.Single(eventingSurfaces, surface => surface.SurfaceId == "event-publishers");
        var publisherEntry = Assert.Single(publisherSurface.Entries);
        Assert.Equal("bounded-in-process", publisherEntry.Metadata["retryPolicy"]);
        Assert.Equal("2", publisherEntry.Metadata["retryMaxAttempts"]);
        Assert.Equal("none", publisherEntry.Metadata["retryDurability"]);

        var subscriptionSurface = Assert.Single(eventingSurfaces, surface => surface.SurfaceId == "event-subscriptions");
        var subscriptionEntry = Assert.Single(subscriptionSurface.Entries, entry => entry.Id == "audit-projector");
        Assert.Equal("bounded-in-process", subscriptionEntry.Metadata["binding.retryPolicy"]);
        Assert.Equal("2", subscriptionEntry.Metadata["binding.retryMaxAttempts"]);
        Assert.Equal("succeeded", subscriptionEntry.Metadata["lastOutcome"]);
        Assert.Equal("2", subscriptionEntry.Metadata["lastAttempt"]);
        Assert.Equal("1", subscriptionEntry.Metadata["retryScheduledCount"]);
        Assert.Equal("bounded-in-process", subscriptionEntry.Metadata["reported.retryPolicy"]);
        Assert.Equal("process-local", subscriptionEntry.Metadata["reported.retryScope"]);
    }

    [Fact]
    public async Task MapCephalonSkipsDuplicateCoreInProcessEventSubscriptionExecutionsWithinProcess()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "Microservice",
                patterns: ["CQRS"],
                technologies: ["EventDrivenIntegration"],
                transports: ["RestApi"]));
            engine.AddModule(new TechnologyPackContributionModule());
            engine.AddEventing(options =>
            {
                options.EnableInProcessSubscriptionExecution = true;
                options.EnableInProcessSubscriptionIdempotency = true;
                options.InProcessSubscriptionIdempotencyRetentionMinutes = 30;
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();

        await using (var scope = app.Services.CreateAsyncScope())
        {
            var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();
            var publication = new EventPublication(
                id: "audit-idempotency-001",
                channelId: "audit",
                eventType: "audit.created",
                payload: """{"id":"audit-idempotency-001"}""",
                occurredAtUtc: new DateTimeOffset(2026, 04, 29, 11, 0, 0, TimeSpan.Zero),
                contentType: "application/json",
                correlationId: "corr-audit-idempotency-001",
                tenantId: "tenant-idempotency-001");

            await publisher.PublishAsync(publication);
            await publisher.PublishAsync(publication);
        }

        var client = app.GetTestClient();
        var probe = app.Services.GetRequiredService<ManagedAuditProjectorProbe>();
        var runtimeCatalog = app.Services.GetRequiredService<IEventSubscriptionRuntimeCatalog>();
        var publicationRuntimeCatalog = app.Services.GetRequiredService<IEventPublicationRuntimeCatalog>();
        var bindingCatalog = app.Services.GetRequiredService<IEventSubscriptionExecutionBindingCatalog>();
        var capabilities = await client.GetFromJsonAsync<CapabilityManifest[]>("/engine/capabilities");
        var eventingSurfaces = await client.GetFromJsonAsync<TechnologyRuntimeSurface[]>("/engine/technology-surfaces/event-driven-integration");

        Assert.Equal(1, probe.TotalAttempts);
        Assert.Equal(1, probe.SuccessfulAttempts);
        Assert.Equal("audit-idempotency-001", probe.LastMessageId);

        var runtimeState = Assert.Single(runtimeCatalog.States);
        Assert.Equal(EventSubscriptionExecutionOutcomes.Skipped, runtimeState.LastOutcome);
        Assert.Equal("audit-idempotency-001", runtimeState.LastMessageId);
        Assert.Equal(1, runtimeState.LastAttempt);
        Assert.Equal(1, runtimeState.StartedCount);
        Assert.Equal(1, runtimeState.SucceededCount);
        Assert.Equal(1, runtimeState.SkippedCount);
        Assert.Equal(3, runtimeState.TotalReports);
        Assert.Equal("completed-publication", runtimeState.Metadata["idempotencyPolicy"]);
        Assert.Equal("subscription-publication", runtimeState.Metadata["idempotencyKey"]);
        Assert.Equal("30", runtimeState.Metadata["idempotencyRetentionMinutes"]);
        Assert.Equal("none", runtimeState.Metadata["idempotencyDurability"]);
        Assert.Equal("process-local", runtimeState.Metadata["idempotencyScope"]);
        Assert.Equal("duplicate-skipped", runtimeState.Metadata["idempotencyOutcome"]);
        Assert.True(runtimeState.Metadata.ContainsKey("idempotencyCompletedAtUtc"));

        var publicationState = Assert.Single(publicationRuntimeCatalog.States);
        Assert.Equal("audit-idempotency-001", publicationState.PublicationId);
        Assert.Equal(EventPublicationRuntimeOutcomes.Skipped, publicationState.LastOutcome);
        Assert.Equal(1, publicationState.SucceededCount);
        Assert.Equal(1, publicationState.SkippedCount);
        Assert.Equal(2, publicationState.TotalReports);
        Assert.Equal(1, publicationState.MatchedSubscriptionCount);
        Assert.Equal(0, publicationState.StartedSubscriptionCount);
        Assert.Equal(0, publicationState.SucceededSubscriptionCount);
        Assert.Equal(1, publicationState.SkippedSubscriptionCount);
        Assert.Equal("duplicate-completed-subscriptions", publicationState.Metadata["skipReason"]);
        Assert.Equal("completed-publication", publicationState.Metadata["idempotencyPolicy"]);
        Assert.Equal("subscription-publication", publicationState.Metadata["idempotencyKey"]);

        var binding = Assert.Single(bindingCatalog.Bindings);
        Assert.Equal("completed-publication", binding.Metadata["idempotencyPolicy"]);
        Assert.Equal("subscription-publication", binding.Metadata["idempotencyKey"]);
        Assert.Equal("30", binding.Metadata["idempotencyRetentionMinutes"]);
        Assert.Equal("none", binding.Metadata["idempotencyDurability"]);
        Assert.Equal("process-local", binding.Metadata["idempotencyScope"]);

        Assert.NotNull(capabilities);
        var publishCapability = Assert.Single(capabilities, capability => capability.Key == "eventing.publish");
        Assert.Equal("completed-publication", publishCapability.Metadata["idempotencyPolicy"]);
        Assert.Equal("subscription-publication", publishCapability.Metadata["idempotencyKey"]);
        var subscribeCapability = Assert.Single(capabilities, capability => capability.Key == "eventing.subscribe");
        Assert.Equal("completed-publication", subscribeCapability.Metadata["idempotencyPolicy"]);
        Assert.Equal("process-local", subscribeCapability.Metadata["idempotencyScope"]);

        Assert.NotNull(eventingSurfaces);
        var publisherSurface = Assert.Single(eventingSurfaces, surface => surface.SurfaceId == "event-publishers");
        var publisherEntry = Assert.Single(publisherSurface.Entries);
        Assert.Equal("completed-publication", publisherEntry.Metadata["idempotencyPolicy"]);
        Assert.Equal("30", publisherEntry.Metadata["idempotencyRetentionMinutes"]);

        var subscriptionSurface = Assert.Single(eventingSurfaces, surface => surface.SurfaceId == "event-subscriptions");
        var subscriptionEntry = Assert.Single(subscriptionSurface.Entries, entry => entry.Id == "audit-projector");
        Assert.Equal("completed-publication", subscriptionEntry.Metadata["binding.idempotencyPolicy"]);
        Assert.Equal("subscription-publication", subscriptionEntry.Metadata["binding.idempotencyKey"]);
        Assert.Equal("skipped", subscriptionEntry.Metadata["lastOutcome"]);
        Assert.Equal("1", subscriptionEntry.Metadata["skippedCount"]);
        Assert.Equal("completed-publication", subscriptionEntry.Metadata["reported.idempotencyPolicy"]);
        Assert.Equal("duplicate-skipped", subscriptionEntry.Metadata["reported.idempotencyOutcome"]);
    }

    [Fact]
    public async Task MapCephalonSkipsDuplicateCoreInProcessEventSubscriptionExecutionsWithInboxStore()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Engine:Messaging:InProcessSubscriptions:EnableExecution"] = "true",
            ["Engine:Messaging:InProcessSubscriptions:Idempotency:Enabled"] = "true",
            ["Engine:Messaging:InProcessSubscriptions:Idempotency:Store"] = "inbox",
            ["Engine:Messaging:InProcessSubscriptions:Idempotency:RetentionMinutes"] = "45"
        });
        builder.Services.AddSingleton<RecordingInbox>();
        builder.Services.AddSingleton<IInbox>(static serviceProvider =>
            serviceProvider.GetRequiredService<RecordingInbox>());
        builder.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "Microservice",
                patterns: ["CQRS"],
                technologies: ["EventDrivenIntegration"],
                transports: ["RestApi"]));
            engine.AddModule(new TechnologyPackContributionModule());
            engine.AddEventingFromConfiguration(builder.Configuration);
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();

        await using (var scope = app.Services.CreateAsyncScope())
        {
            var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();
            var publication = new EventPublication(
                id: "audit-inbox-idempotency-001",
                channelId: "audit",
                eventType: "audit.created",
                payload: """{"id":"audit-inbox-idempotency-001"}""",
                occurredAtUtc: new DateTimeOffset(2026, 05, 10, 9, 0, 0, TimeSpan.Zero),
                contentType: "application/json",
                correlationId: "corr-audit-inbox-idempotency-001",
                tenantId: "tenant-inbox-idempotency-001");

            await publisher.PublishAsync(publication);
            await publisher.PublishAsync(publication);
        }

        var client = app.GetTestClient();
        var probe = app.Services.GetRequiredService<ManagedAuditProjectorProbe>();
        var inbox = app.Services.GetRequiredService<RecordingInbox>();
        var runtimeCatalog = app.Services.GetRequiredService<IEventSubscriptionRuntimeCatalog>();
        var publicationRuntimeCatalog = app.Services.GetRequiredService<IEventPublicationRuntimeCatalog>();
        var bindingCatalog = app.Services.GetRequiredService<IEventSubscriptionExecutionBindingCatalog>();
        var capabilities = await client.GetFromJsonAsync<CapabilityManifest[]>("/engine/capabilities");
        var eventingSurfaces = await client.GetFromJsonAsync<TechnologyRuntimeSurface[]>("/engine/technology-surfaces/event-driven-integration");

        Assert.Equal(1, probe.TotalAttempts);
        Assert.Equal(1, probe.SuccessfulAttempts);
        const string inboxMessageId = "cephalon:eventing:subscription:audit-projector:publication:audit-inbox-idempotency-001";
        Assert.Equal(inboxMessageId, Assert.Single(inbox.MarkedMessageIds));
        Assert.Collection(
            inbox.HasProcessedMessageIds,
            messageId => Assert.Equal(inboxMessageId, messageId),
            messageId => Assert.Equal(inboxMessageId, messageId));
        var recordedMessage = Assert.Single(inbox.MarkedMessages);
        Assert.Equal("audit", recordedMessage.ChannelId);
        Assert.Equal("audit.created", recordedMessage.MessageType);
        Assert.Equal("inbox", recordedMessage.Metadata["idempotencyStore"]);
        Assert.Equal("completed-publication", recordedMessage.Metadata["idempotencyPolicy"]);

        var runtimeState = Assert.Single(runtimeCatalog.States);
        Assert.Equal(EventSubscriptionExecutionOutcomes.Skipped, runtimeState.LastOutcome);
        Assert.Equal("completed-publication", runtimeState.Metadata["idempotencyPolicy"]);
        Assert.Equal("subscription-publication", runtimeState.Metadata["idempotencyKey"]);
        Assert.Equal("inbox", runtimeState.Metadata["idempotencyStore"]);
        Assert.Equal("45", runtimeState.Metadata["idempotencyRetentionMinutes"]);
        Assert.Equal("inbox", runtimeState.Metadata["idempotencyDurability"]);
        Assert.Equal("durable-store", runtimeState.Metadata["idempotencyScope"]);
        Assert.Equal("available", runtimeState.Metadata["inbox"]);
        Assert.Equal("duplicate-skipped", runtimeState.Metadata["idempotencyOutcome"]);
        Assert.Equal("recorded", runtimeState.Metadata["idempotencyCompletionState"]);
        Assert.True(runtimeState.Metadata.ContainsKey("idempotencyCheckedAtUtc"));

        var publicationState = Assert.Single(publicationRuntimeCatalog.States);
        Assert.Equal(EventPublicationRuntimeOutcomes.Skipped, publicationState.LastOutcome);
        Assert.Equal(1, publicationState.SucceededCount);
        Assert.Equal(1, publicationState.SkippedCount);
        Assert.Equal("duplicate-completed-subscriptions", publicationState.Metadata["skipReason"]);
        Assert.Equal("inbox", publicationState.Metadata["idempotencyStore"]);
        Assert.Equal("inbox", publicationState.Metadata["idempotencyDurability"]);
        Assert.Equal("durable-store", publicationState.Metadata["idempotencyScope"]);
        Assert.Equal("available", publicationState.Metadata["inbox"]);

        var binding = Assert.Single(bindingCatalog.Bindings);
        Assert.Equal("inbox", binding.Metadata["idempotencyStore"]);
        Assert.Equal("inbox", binding.Metadata["idempotencyDurability"]);
        Assert.Equal("durable-store", binding.Metadata["idempotencyScope"]);

        Assert.NotNull(capabilities);
        var publishCapability = Assert.Single(capabilities, capability => capability.Key == "eventing.publish");
        Assert.Equal("inbox", publishCapability.Metadata["idempotencyStore"]);
        Assert.Equal("inbox", publishCapability.Metadata["idempotencyDurability"]);
        Assert.Equal("durable-store", publishCapability.Metadata["idempotencyScope"]);
        Assert.Equal("available", publishCapability.Metadata["inbox"]);
        var subscribeCapability = Assert.Single(capabilities, capability => capability.Key == "eventing.subscribe");
        Assert.Equal("inbox", subscribeCapability.Metadata["idempotencyStore"]);
        Assert.Equal("inbox", subscribeCapability.Metadata["idempotencyDurability"]);
        Assert.Equal("durable-store", subscribeCapability.Metadata["idempotencyScope"]);
        Assert.Equal("available", subscribeCapability.Metadata["inbox"]);

        Assert.NotNull(eventingSurfaces);
        var publisherSurface = Assert.Single(eventingSurfaces, surface => surface.SurfaceId == "event-publishers");
        var publisherEntry = Assert.Single(publisherSurface.Entries);
        Assert.Equal("inbox", publisherEntry.Metadata["idempotencyStore"]);
        Assert.Equal("inbox", publisherEntry.Metadata["idempotencyDurability"]);
        Assert.Equal("durable-store", publisherEntry.Metadata["idempotencyScope"]);
        Assert.Equal("available", publisherEntry.Metadata["inbox"]);

        var subscriptionSurface = Assert.Single(eventingSurfaces, surface => surface.SurfaceId == "event-subscriptions");
        var subscriptionEntry = Assert.Single(subscriptionSurface.Entries, entry => entry.Id == "audit-projector");
        Assert.Equal("inbox", subscriptionEntry.Metadata["binding.idempotencyStore"]);
        Assert.Equal("inbox", subscriptionEntry.Metadata["binding.idempotencyDurability"]);
        Assert.Equal("durable-store", subscriptionEntry.Metadata["binding.idempotencyScope"]);
        Assert.Equal("inbox", subscriptionEntry.Metadata["reported.idempotencyStore"]);
        Assert.Equal("inbox", subscriptionEntry.Metadata["reported.idempotencyDurability"]);
        Assert.Equal("durable-store", subscriptionEntry.Metadata["reported.idempotencyScope"]);
        Assert.Equal("duplicate-skipped", subscriptionEntry.Metadata["reported.idempotencyOutcome"]);
    }

    [Fact]
    public void BuildRejectsInboxBackedCoreInProcessIdempotencyWithoutInboxStore()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        var exception = Assert.Throws<InvalidOperationException>(() => builder.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "Microservice",
                patterns: ["CQRS"],
                technologies: ["EventDrivenIntegration"],
                transports: ["RestApi"]));
            engine.AddModule(new TechnologyPackContributionModule());
            engine.AddEventing(options =>
            {
                options.EnableInProcessSubscriptionExecution = true;
                options.EnableInProcessSubscriptionIdempotency = true;
                options.InProcessSubscriptionIdempotencyStore = "inbox";
            });
        }));

        Assert.Contains("IInbox", exception.Message, StringComparison.Ordinal);
        Assert.Contains("exactly one", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MapCephalonExecutesCoreInProcessEventSubscriptionsWithoutWolverine()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "Microservice",
                patterns: ["CQRS"],
                technologies: ["EventDrivenIntegration"],
                transports: ["RestApi"]));
            engine.AddModule(new TechnologyPackContributionModule());
            engine.AddEventing(options =>
            {
                options.EnableInProcessSubscriptionExecution = true;
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(new EventPublication(
                id: "audit-001",
                channelId: "audit",
                eventType: "audit.created",
                payload: """{"id":"audit-001"}""",
                occurredAtUtc: new DateTimeOffset(2026, 04, 29, 8, 0, 0, TimeSpan.Zero),
                contentType: "application/json",
                correlationId: "corr-audit-001",
                tenantId: "tenant-001",
                headers: new Dictionary<string, string>
                {
                    ["x-test"] = "core-in-process"
                }));
        }

        var client = app.GetTestClient();
        var probe = app.Services.GetRequiredService<ManagedAuditProjectorProbe>();
        var runtimeCatalog = app.Services.GetRequiredService<IEventSubscriptionRuntimeCatalog>();
        var bindingCatalog = app.Services.GetRequiredService<IEventSubscriptionExecutionBindingCatalog>();
        var capabilities = await client.GetFromJsonAsync<CapabilityManifest[]>("/engine/capabilities");
        var eventingSurfaces = await client.GetFromJsonAsync<TechnologyRuntimeSurface[]>("/engine/technology-surfaces/event-driven-integration");
        var readiness = await client.GetFromJsonAsync<EventSubscriptionExecutionReadinessDescriptor[]>("/engine/event-subscription-readiness");
        var snapshot = await client.GetFromJsonAsync<Cephalon.Engine.Runtime.RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.Equal(1, probe.TotalAttempts);
        Assert.Equal(1, probe.SuccessfulAttempts);
        Assert.Equal("audit-001", probe.LastMessageId);

        var runtimeState = Assert.Single(runtimeCatalog.States);
        Assert.Equal("audit-projector", runtimeState.SubscriptionId);
        Assert.Equal(EventSubscriptionExecutionOutcomes.Succeeded, runtimeState.LastOutcome);
        Assert.Equal("audit-001", runtimeState.LastMessageId);
        Assert.Equal(1, runtimeState.StartedCount);
        Assert.Equal(1, runtimeState.SucceededCount);
        Assert.Equal("in-process-direct", runtimeState.Metadata["executionMode"]);
        Assert.Equal("cephalon-managed", runtimeState.Metadata["executionOwnership"]);

        var binding = Assert.Single(bindingCatalog.Bindings);
        Assert.Equal("audit-projector", binding.SubscriptionId);
        Assert.Equal("cephalon-eventing-in-process-subscriptions", binding.ExecutionRuntimeId);
        Assert.Equal("cephalon-managed", binding.ExecutionOwnership);
        Assert.Equal("in-process-direct", binding.ExecutionMode);
        Assert.Equal("in-process-event-publisher", binding.Metadata["trigger"]);
        Assert.Equal("none", binding.Metadata["retryPolicy"]);

        Assert.NotNull(capabilities);
        var publishCapability = Assert.Single(capabilities, capability => capability.Key == "eventing.publish");
        Assert.Equal("in-process", publishCapability.Metadata["handoff"]);
        Assert.Equal("cephalon-managed", publishCapability.Metadata["subscriptionExecution"]);
        Assert.Equal("available", publishCapability.Metadata["publicationDispatcher"]);
        Assert.Equal("none", publishCapability.Metadata["retryPolicy"]);
        var subscribeCapability = Assert.Single(capabilities, capability => capability.Key == "eventing.subscribe");
        Assert.Equal("cephalon-managed", subscribeCapability.Metadata["executionOwnership"]);
        Assert.Equal("in-process-direct", subscribeCapability.Metadata["executionMode"]);
        Assert.Equal("cephalon-eventing-in-process-subscriptions", subscribeCapability.Metadata["executionRuntimeId"]);
        Assert.Equal("in-process-event-publisher", subscribeCapability.Metadata["triggerRuntimeId"]);

        Assert.NotNull(eventingSurfaces);
        var publisherSurface = Assert.Single(eventingSurfaces, surface => surface.SurfaceId == "event-publishers");
        var publisherEntry = Assert.Single(publisherSurface.Entries);
        Assert.Equal("in-process-event-publisher", publisherEntry.Id);
        Assert.Equal("in-process", publisherEntry.Metadata["handoff"]);
        Assert.Equal("cephalon-managed", publisherEntry.Metadata["subscriptionExecution"]);
        Assert.Equal("available", publisherEntry.Metadata["publicationDispatcher"]);
        Assert.Equal("1", publisherEntry.Metadata["subscriptionExecutorCount"]);

        var subscriptionSurface = Assert.Single(eventingSurfaces, surface => surface.SurfaceId == "event-subscriptions");
        var subscriptionEntry = Assert.Single(subscriptionSurface.Entries, entry => entry.Id == "audit-projector");
        Assert.Equal("cephalon-managed", subscriptionEntry.Metadata["dispatchRuntime"]);
        Assert.Equal("runtime-bound", subscriptionEntry.Metadata["subscriptionRuntime"]);
        Assert.Equal("cephalon-eventing-in-process-subscriptions", subscriptionEntry.Metadata["executionRuntimeId"]);
        Assert.Equal("cephalon-managed", subscriptionEntry.Metadata["executionOwnership"]);
        Assert.Equal("in-process-direct", subscriptionEntry.Metadata["executionMode"]);
        Assert.Equal("reported", subscriptionEntry.Metadata["runtimeState"]);
        Assert.Equal("succeeded", subscriptionEntry.Metadata["lastOutcome"]);
        Assert.Equal("audit-001", subscriptionEntry.Metadata["lastMessageId"]);
        Assert.Equal("in-process-event-publisher", subscriptionEntry.Metadata["binding.trigger"]);
        Assert.Equal("none", subscriptionEntry.Metadata["binding.retryPolicy"]);
        Assert.Equal("in-process-direct", subscriptionEntry.Metadata["reported.executionMode"]);

        Assert.NotNull(readiness);
        var subscriptionReadiness = Assert.Single(readiness);
        Assert.Equal("audit-projector", subscriptionReadiness.SubscriptionId);
        Assert.Equal(EventSubscriptionExecutionReadinessStates.RuntimeBound, subscriptionReadiness.ReadinessState);
        Assert.Equal("cephalon-managed", subscriptionReadiness.ExecutionOwnership);
        Assert.Equal("in-process-direct", subscriptionReadiness.ExecutionMode);
        Assert.Equal("cephalon-eventing-in-process-subscriptions", subscriptionReadiness.ExecutionRuntimeId);

        Assert.NotNull(snapshot);
        Assert.Single(snapshot.EventSubscriptionExecutionReadiness);
        var snapshotSubscription = Assert.Single(
            snapshot.TechnologySurfaces.Single(surface => surface.SurfaceId == "event-subscriptions").Entries,
            entry => entry.Id == "audit-projector");
        Assert.Equal("cephalon-managed", snapshotSubscription.Metadata["dispatchRuntime"]);
        Assert.Equal("succeeded", snapshotSubscription.Metadata["lastOutcome"]);
    }

    private static async Task WaitForConditionAsync(
        Func<bool> condition,
        int timeoutMilliseconds = 3000)
    {
        var deadline = DateTimeOffset.UtcNow.AddMilliseconds(timeoutMilliseconds);
        while (DateTimeOffset.UtcNow < deadline)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(25);
        }

        Assert.True(condition(), "The expected condition was not reached before the timeout.");
    }

    private sealed class RecordingInbox : IInbox
    {
        private readonly Lock gate = new();
        private readonly List<string> hasProcessedMessageIds = [];
        private readonly List<InboxMessage> markedMessages = [];
        private readonly Dictionary<string, InboxMessage> messages = new(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyList<string> HasProcessedMessageIds
        {
            get
            {
                lock (gate)
                {
                    return hasProcessedMessageIds.ToArray();
                }
            }
        }

        public IReadOnlyList<string> MarkedMessageIds
        {
            get
            {
                lock (gate)
                {
                    return markedMessages
                        .Select(static message => message.Id)
                        .ToArray();
                }
            }
        }

        public IReadOnlyList<InboxMessage> MarkedMessages
        {
            get
            {
                lock (gate)
                {
                    return markedMessages.ToArray();
                }
            }
        }

        public ValueTask<bool> HasProcessedAsync(
            string messageId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            lock (gate)
            {
                hasProcessedMessageIds.Add(messageId);
                return ValueTask.FromResult(messages.ContainsKey(messageId));
            }
        }

        public ValueTask MarkProcessedAsync(
            InboxMessage message,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(message);
            cancellationToken.ThrowIfCancellationRequested();

            lock (gate)
            {
                messages[message.Id] = message;
                markedMessages.Add(message);
            }

            return ValueTask.CompletedTask;
        }
    }
}
