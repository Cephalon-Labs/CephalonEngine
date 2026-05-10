using System.Net;
using System.Net.Http.Json;
using System.Globalization;
using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Technologies;
using Cephalon.AspNetCore.Hosting;
using Cephalon.Data.EntityFramework.Registration;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Manifest;
using Cephalon.Eventing.Hosting;
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
        Assert.Equal("dispatch-store-ready", terminalRemediationEntry.Metadata["deadLetterCommand"]);
        Assert.Equal("not-claimed", terminalRemediationEntry.Metadata["brokerDeadLetterCommand"]);
        Assert.Equal("ready", terminalRemediationEntry.Metadata["quarantineCommand"]);
        Assert.Equal("ready", terminalRemediationEntry.Metadata["skipCommand"]);
        Assert.Equal("/engine/event-dispatches/{outboxId}/commands/{operationId}", terminalRemediationEntry.Metadata["operatorCommandRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands", terminalRemediationEntry.Metadata["operatorCommandListRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/{commandId}", terminalRemediationEntry.Metadata["operatorCommandResultRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/in-doubt", terminalRemediationEntry.Metadata["operatorCommandInDoubtRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/in-doubt/summary", terminalRemediationEntry.Metadata["operatorCommandInDoubtSummaryRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/in-doubt/oldest", terminalRemediationEntry.Metadata["operatorCommandOldestInDoubtRoute"]);
        Assert.Equal("beforeUtc", terminalRemediationEntry.Metadata["operatorCommandInDoubtQuery"]);
        Assert.Equal("inclusive-observed-utc-before-or-equal", terminalRemediationEntry.Metadata["operatorCommandInDoubtCutoffPolicy"]);
        Assert.Equal("newest-first", terminalRemediationEntry.Metadata["operatorCommandInDoubtDetailOrder"]);
        Assert.Equal("beforeUtc", terminalRemediationEntry.Metadata["operatorCommandInDoubtSummaryQuery"]);
        Assert.Equal("retained-reserved-summary-observed-utc-before-or-equal", terminalRemediationEntry.Metadata["operatorCommandInDoubtSummaryPolicy"]);
        Assert.Equal("beforeUtc", terminalRemediationEntry.Metadata["operatorCommandOldestInDoubtQuery"]);
        Assert.Equal("oldest-retained-reserved-observed-utc-before-or-equal", terminalRemediationEntry.Metadata["operatorCommandOldestInDoubtPolicy"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/outboxes/{outboxId}", terminalRemediationEntry.Metadata["operatorCommandOutboxRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/observations?fromUtc={fromUtc}&toUtc={toUtc}", terminalRemediationEntry.Metadata["operatorCommandObservationRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/observations/summary?fromUtc={fromUtc}&toUtc={toUtc}", terminalRemediationEntry.Metadata["operatorCommandObservationSummaryRoute"]);
        Assert.Equal("fromUtc,toUtc", terminalRemediationEntry.Metadata["operatorCommandObservationWindowQuery"]);
        Assert.Equal("inclusive-observed-utc", terminalRemediationEntry.Metadata["operatorCommandObservationWindowPolicy"]);
        Assert.Equal("newest-first", terminalRemediationEntry.Metadata["operatorCommandObservationWindowDetailOrder"]);
        Assert.Equal("available", terminalRemediationEntry.Metadata["operatorCommandObservationWindowSummary"]);
        Assert.Equal("reject-reversed-window", terminalRemediationEntry.Metadata["operatorCommandObservationWindowInvalidBounds"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/operations/{operationId}", terminalRemediationEntry.Metadata["operatorCommandOperationRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/actors/{actorId}", terminalRemediationEntry.Metadata["operatorCommandActorRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/correlations/{correlationId}", terminalRemediationEntry.Metadata["operatorCommandCorrelationRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/reasons/{reason}", terminalRemediationEntry.Metadata["operatorCommandReasonRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/messages/{messageId}", terminalRemediationEntry.Metadata["operatorCommandMessageRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/channels/{channelId}", terminalRemediationEntry.Metadata["operatorCommandChannelRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/dispatch-outcomes/{dispatchOutcome}", terminalRemediationEntry.Metadata["operatorCommandDispatchOutcomeRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/outcomes/{outcome}", terminalRemediationEntry.Metadata["operatorCommandOutcomeRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/outboxes/{outboxId}/summary", terminalRemediationEntry.Metadata["operatorCommandOutboxSummaryRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/outboxes/{outboxId}/retention", terminalRemediationEntry.Metadata["operatorCommandOutboxRetentionRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/messages/{messageId}/summary", terminalRemediationEntry.Metadata["operatorCommandMessageSummaryRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/messages/{messageId}/retention", terminalRemediationEntry.Metadata["operatorCommandMessageRetentionRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/dispatch-outcomes/{dispatchOutcome}/summary", terminalRemediationEntry.Metadata["operatorCommandDispatchOutcomeSummaryRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/dispatch-outcomes/{dispatchOutcome}/retention", terminalRemediationEntry.Metadata["operatorCommandDispatchOutcomeRetentionRoute"]);
        Assert.Equal("retained-filter-server-side-aggregate", terminalRemediationEntry.Metadata["operatorCommandFilterSummaryPolicy"]);
        Assert.Equal("outboxes,messages,channels,operations,actors,correlations,reasons,dispatch-outcomes,outcomes", terminalRemediationEntry.Metadata["operatorCommandFilterSummaryRoutes"]);
        Assert.Equal(nameof(EventDispatchRemediationRuntimeSummary), terminalRemediationEntry.Metadata["operatorCommandFilterSummaryResponse"]);
        Assert.Equal("not-required", terminalRemediationEntry.Metadata["operatorCommandFilterSummaryMaterialization"]);
        Assert.Equal("retained-filter-server-side-retention", terminalRemediationEntry.Metadata["operatorCommandFilterRetentionPolicy"]);
        Assert.Equal("outboxes,messages,channels,operations,actors,correlations,reasons,dispatch-outcomes,outcomes", terminalRemediationEntry.Metadata["operatorCommandFilterRetentionRoutes"]);
        Assert.Equal(nameof(EventDispatchRemediationRuntimeRetention), terminalRemediationEntry.Metadata["operatorCommandFilterRetentionResponse"]);
        Assert.Equal("not-required", terminalRemediationEntry.Metadata["operatorCommandFilterRetentionMaterialization"]);
        Assert.Equal("limit", terminalRemediationEntry.Metadata["operatorCommandReadLimitQuery"]);
        Assert.Equal("positive-integer-newest-first", terminalRemediationEntry.Metadata["operatorCommandReadLimitPolicy"]);
        Assert.Equal("list-and-filter-routes", terminalRemediationEntry.Metadata["operatorCommandReadLimitAppliesTo"]);
        Assert.Equal("all,in-doubt,observations,outboxes,messages,channels,operations,actors,correlations,reasons,dispatch-outcomes,outcomes", terminalRemediationEntry.Metadata["operatorCommandReadLimitRoutes"]);
        Assert.Equal("pageSize,continuationToken", terminalRemediationEntry.Metadata["operatorCommandPaginationQuery"]);
        Assert.Equal("opaque-signed-route-bound-continuation-token-newest-first", terminalRemediationEntry.Metadata["operatorCommandPaginationPolicy"]);
        Assert.Equal("false", terminalRemediationEntry.Metadata["wolverineRequired"]);
        Assert.Equal("unique-command-id", terminalRemediationEntry.Metadata[EventDispatchRemediationMetadataKeys.CommandIdempotencyPolicy]);
        Assert.Equal("reject-without-mutation", terminalRemediationEntry.Metadata[EventDispatchRemediationMetadataKeys.DuplicateCommandPolicy]);
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
        Assert.Equal("retry-now,retry-later,skip,quarantine,dead-letter", remediationCapability.Metadata["operationIds"]);
        Assert.Equal("dispatch-store-ready", remediationCapability.Metadata["deadLetterCommand"]);
        Assert.Equal("not-claimed", remediationCapability.Metadata["brokerDeadLetterCommand"]);
        Assert.Equal("available", remediationCapability.Metadata["commandRuntimeState"]);
        Assert.Equal("256", remediationCapability.Metadata["commandHistoryLimit"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands", remediationCapability.Metadata["commandListRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/{commandId}", remediationCapability.Metadata["commandResultRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/summary", remediationCapability.Metadata["commandSummaryRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/latest", remediationCapability.Metadata["commandLatestRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/retention", remediationCapability.Metadata["commandRetentionRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/in-doubt", remediationCapability.Metadata["commandInDoubtRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/in-doubt/summary", remediationCapability.Metadata["commandInDoubtSummaryRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/in-doubt/oldest", remediationCapability.Metadata["commandOldestInDoubtRoute"]);
        Assert.Equal("beforeUtc", remediationCapability.Metadata["commandInDoubtQuery"]);
        Assert.Equal("inclusive-observed-utc-before-or-equal", remediationCapability.Metadata["commandInDoubtCutoffPolicy"]);
        Assert.Equal("newest-first", remediationCapability.Metadata["commandInDoubtDetailOrder"]);
        Assert.Equal("beforeUtc", remediationCapability.Metadata["commandInDoubtSummaryQuery"]);
        Assert.Equal("retained-reserved-summary-observed-utc-before-or-equal", remediationCapability.Metadata["commandInDoubtSummaryPolicy"]);
        Assert.Equal("beforeUtc", remediationCapability.Metadata["commandOldestInDoubtQuery"]);
        Assert.Equal("oldest-retained-reserved-observed-utc-before-or-equal", remediationCapability.Metadata["commandOldestInDoubtPolicy"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/outboxes/{outboxId}", remediationCapability.Metadata["commandOutboxRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/observations?fromUtc={fromUtc}&toUtc={toUtc}", remediationCapability.Metadata["commandObservationRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/observations/summary?fromUtc={fromUtc}&toUtc={toUtc}", remediationCapability.Metadata["commandObservationSummaryRoute"]);
        Assert.Equal("fromUtc,toUtc", remediationCapability.Metadata["commandObservationWindowQuery"]);
        Assert.Equal("inclusive-observed-utc", remediationCapability.Metadata["commandObservationWindowPolicy"]);
        Assert.Equal("newest-first", remediationCapability.Metadata["commandObservationWindowDetailOrder"]);
        Assert.Equal("available", remediationCapability.Metadata["commandObservationWindowSummary"]);
        Assert.Equal("reject-reversed-window", remediationCapability.Metadata["commandObservationWindowInvalidBounds"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/operations/{operationId}", remediationCapability.Metadata["commandOperationRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/actors/{actorId}", remediationCapability.Metadata["commandActorRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/correlations/{correlationId}", remediationCapability.Metadata["commandCorrelationRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/reasons/{reason}", remediationCapability.Metadata["commandReasonRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/messages/{messageId}", remediationCapability.Metadata["commandMessageRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/channels/{channelId}", remediationCapability.Metadata["commandChannelRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/dispatch-outcomes/{dispatchOutcome}", remediationCapability.Metadata["commandDispatchOutcomeRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/outcomes/{outcome}", remediationCapability.Metadata["commandOutcomeRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/outboxes/{outboxId}/summary", remediationCapability.Metadata["commandOutboxSummaryRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/messages/{messageId}/summary", remediationCapability.Metadata["commandMessageSummaryRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/channels/{channelId}/summary", remediationCapability.Metadata["commandChannelSummaryRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/operations/{operationId}/summary", remediationCapability.Metadata["commandOperationSummaryRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/actors/{actorId}/summary", remediationCapability.Metadata["commandActorSummaryRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/correlations/{correlationId}/summary", remediationCapability.Metadata["commandCorrelationSummaryRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/reasons/{reason}/summary", remediationCapability.Metadata["commandReasonSummaryRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/dispatch-outcomes/{dispatchOutcome}/summary", remediationCapability.Metadata["commandDispatchOutcomeSummaryRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/outcomes/{outcome}/summary", remediationCapability.Metadata["commandOutcomeSummaryRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/outboxes/{outboxId}/retention", remediationCapability.Metadata["commandOutboxRetentionRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/messages/{messageId}/retention", remediationCapability.Metadata["commandMessageRetentionRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/channels/{channelId}/retention", remediationCapability.Metadata["commandChannelRetentionRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/operations/{operationId}/retention", remediationCapability.Metadata["commandOperationRetentionRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/actors/{actorId}/retention", remediationCapability.Metadata["commandActorRetentionRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/correlations/{correlationId}/retention", remediationCapability.Metadata["commandCorrelationRetentionRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/reasons/{reason}/retention", remediationCapability.Metadata["commandReasonRetentionRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/dispatch-outcomes/{dispatchOutcome}/retention", remediationCapability.Metadata["commandDispatchOutcomeRetentionRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/outcomes/{outcome}/retention", remediationCapability.Metadata["commandOutcomeRetentionRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/outboxes/{outboxId}/latest", remediationCapability.Metadata["commandOutboxLatestRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/messages/{messageId}/latest", remediationCapability.Metadata["commandMessageLatestRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/channels/{channelId}/latest", remediationCapability.Metadata["commandChannelLatestRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/operations/{operationId}/latest", remediationCapability.Metadata["commandOperationLatestRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/actors/{actorId}/latest", remediationCapability.Metadata["commandActorLatestRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/correlations/{correlationId}/latest", remediationCapability.Metadata["commandCorrelationLatestRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/reasons/{reason}/latest", remediationCapability.Metadata["commandReasonLatestRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/dispatch-outcomes/{dispatchOutcome}/latest", remediationCapability.Metadata["commandDispatchOutcomeLatestRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/outcomes/{outcome}/latest", remediationCapability.Metadata["commandOutcomeLatestRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/outboxes/{outboxId}/oldest", remediationCapability.Metadata["commandOutboxOldestRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/messages/{messageId}/oldest", remediationCapability.Metadata["commandMessageOldestRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/channels/{channelId}/oldest", remediationCapability.Metadata["commandChannelOldestRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/operations/{operationId}/oldest", remediationCapability.Metadata["commandOperationOldestRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/actors/{actorId}/oldest", remediationCapability.Metadata["commandActorOldestRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/correlations/{correlationId}/oldest", remediationCapability.Metadata["commandCorrelationOldestRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/reasons/{reason}/oldest", remediationCapability.Metadata["commandReasonOldestRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/dispatch-outcomes/{dispatchOutcome}/oldest", remediationCapability.Metadata["commandDispatchOutcomeOldestRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/outcomes/{outcome}/oldest", remediationCapability.Metadata["commandOutcomeOldestRoute"]);
        Assert.Equal("retained-filter-server-side-aggregate", remediationCapability.Metadata["commandFilterSummaryPolicy"]);
        Assert.Equal("outboxes,messages,channels,operations,actors,correlations,reasons,dispatch-outcomes,outcomes", remediationCapability.Metadata["commandFilterSummaryRoutes"]);
        Assert.Equal(nameof(EventDispatchRemediationRuntimeSummary), remediationCapability.Metadata["commandFilterSummaryResponse"]);
        Assert.Equal("not-required", remediationCapability.Metadata["commandFilterSummaryMaterialization"]);
        Assert.Equal("retained-filter-server-side-retention", remediationCapability.Metadata["commandFilterRetentionPolicy"]);
        Assert.Equal("outboxes,messages,channels,operations,actors,correlations,reasons,dispatch-outcomes,outcomes", remediationCapability.Metadata["commandFilterRetentionRoutes"]);
        Assert.Equal(nameof(EventDispatchRemediationRuntimeRetention), remediationCapability.Metadata["commandFilterRetentionResponse"]);
        Assert.Equal("not-required", remediationCapability.Metadata["commandFilterRetentionMaterialization"]);
        Assert.Equal("retained-filter-server-side-latest", remediationCapability.Metadata["commandFilterLatestPolicy"]);
        Assert.Equal("outboxes,messages,channels,operations,actors,correlations,reasons,dispatch-outcomes,outcomes", remediationCapability.Metadata["commandFilterLatestRoutes"]);
        Assert.Equal(nameof(EventDispatchRemediationRuntimeState), remediationCapability.Metadata["commandFilterLatestResponse"]);
        Assert.Equal("not-required", remediationCapability.Metadata["commandFilterLatestMaterialization"]);
        Assert.Equal("not-found", remediationCapability.Metadata["commandFilterLatestMissing"]);
        Assert.Equal("retained-filter-server-side-oldest", remediationCapability.Metadata["commandFilterOldestPolicy"]);
        Assert.Equal("outboxes,messages,channels,operations,actors,correlations,reasons,dispatch-outcomes,outcomes", remediationCapability.Metadata["commandFilterOldestRoutes"]);
        Assert.Equal(nameof(EventDispatchRemediationRuntimeState), remediationCapability.Metadata["commandFilterOldestResponse"]);
        Assert.Equal("not-required", remediationCapability.Metadata["commandFilterOldestMaterialization"]);
        Assert.Equal("not-found", remediationCapability.Metadata["commandFilterOldestMissing"]);
        Assert.Equal("limit", remediationCapability.Metadata["commandReadLimitQuery"]);
        Assert.Equal("positive-integer-newest-first", remediationCapability.Metadata["commandReadLimitPolicy"]);
        Assert.Equal("list-and-filter-routes", remediationCapability.Metadata["commandReadLimitAppliesTo"]);
        Assert.Equal("all,in-doubt,observations,outboxes,messages,channels,operations,actors,correlations,reasons,dispatch-outcomes,outcomes", remediationCapability.Metadata["commandReadLimitRoutes"]);
        Assert.Equal("pageSize,continuationToken", remediationCapability.Metadata["commandPaginationQuery"]);
        Assert.Equal("opaque-signed-route-bound-continuation-token-newest-first", remediationCapability.Metadata["commandPaginationPolicy"]);
        Assert.Equal("list-and-filter-routes", remediationCapability.Metadata["commandPaginationAppliesTo"]);
        Assert.Equal("50", remediationCapability.Metadata["commandPageSizeDefault"]);
        Assert.Equal("500", remediationCapability.Metadata["commandPageSizeMaximum"]);
        Assert.Equal("unique-command-id", remediationCapability.Metadata[EventDispatchRemediationMetadataKeys.CommandIdempotencyPolicy]);
        Assert.Equal("reject-without-mutation", remediationCapability.Metadata[EventDispatchRemediationMetadataKeys.DuplicateCommandPolicy]);

        var initialEventingSurfaces = await client.GetFromJsonAsync<TechnologyRuntimeSurface[]>("/engine/technology-surfaces/event-driven-integration");
        Assert.NotNull(initialEventingSurfaces);
        var initialCommandSurface = Assert.Single(initialEventingSurfaces, surface => surface.SurfaceId == "event-dispatch-remediation-commands");
        var initialCommandCatalogEntry = Assert.Single(initialCommandSurface.Entries, entry => entry.Id == "event-dispatch-remediation-commands");
        Assert.Equal("catalog", initialCommandCatalogEntry.Metadata["entryKind"]);
        Assert.Equal("0", initialCommandCatalogEntry.Metadata["commandStateCount"]);
        Assert.Equal("0", initialCommandCatalogEntry.Metadata["summaryTotalCommandCount"]);
        Assert.Equal("0", initialCommandCatalogEntry.Metadata["summaryReservedCount"]);
        Assert.Equal("false", initialCommandCatalogEntry.Metadata["summaryHasCommands"]);
        Assert.Equal("false", initialCommandCatalogEntry.Metadata["summaryHasInDoubtCommands"]);
        Assert.False(initialCommandCatalogEntry.Metadata.ContainsKey("summaryOldestReservedCommandId"));
        Assert.False(initialCommandCatalogEntry.Metadata.ContainsKey("summaryOldestReservedObservedAtUtc"));
        Assert.Equal("0", initialCommandCatalogEntry.Metadata["commandHistoryLimit"]);
        Assert.Equal("0", initialCommandCatalogEntry.Metadata["retainedCommandCount"]);
        Assert.Equal("0", initialCommandCatalogEntry.Metadata["totalRecordedCommandCount"]);
        Assert.Equal("0", initialCommandCatalogEntry.Metadata["droppedCommandCount"]);
        Assert.Equal("false", initialCommandCatalogEntry.Metadata["retentionTruncated"]);
        Assert.Equal("durable", initialCommandCatalogEntry.Metadata["commandJournalDurability"]);
        Assert.Equal("cross-node", initialCommandCatalogEntry.Metadata["commandJournalScope"]);
        Assert.Equal("Cephalon.Data.EntityFramework", initialCommandCatalogEntry.Metadata["commandJournalProvider"]);
        Assert.Equal("entity-framework-table", initialCommandCatalogEntry.Metadata["commandJournalStorage"]);
        Assert.Equal("true", initialCommandCatalogEntry.Metadata["commandCrossNodeCommandAudit"]);
        Assert.Equal("durable", initialCommandCatalogEntry.Metadata["commandJournalReplayCursor"]);
        Assert.Equal("oldest-first-observed-utc-command-id", initialCommandCatalogEntry.Metadata["commandJournalReplayCursorOrder"]);
        Assert.Equal("command-journal", initialCommandCatalogEntry.Metadata["commandJournalReplayCursorScope"]);
        Assert.Equal("false", initialCommandCatalogEntry.Metadata["hasLatestCommand"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands", initialCommandCatalogEntry.Metadata["commandListRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/{commandId}", initialCommandCatalogEntry.Metadata["commandResultRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/summary", initialCommandCatalogEntry.Metadata["commandSummaryRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/in-doubt", initialCommandCatalogEntry.Metadata["commandInDoubtRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/in-doubt/summary", initialCommandCatalogEntry.Metadata["commandInDoubtSummaryRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/in-doubt/oldest", initialCommandCatalogEntry.Metadata["commandOldestInDoubtRoute"]);
        Assert.Equal("beforeUtc", initialCommandCatalogEntry.Metadata["commandInDoubtQuery"]);
        Assert.Equal("inclusive-observed-utc-before-or-equal", initialCommandCatalogEntry.Metadata["commandInDoubtCutoffPolicy"]);
        Assert.Equal("newest-first", initialCommandCatalogEntry.Metadata["commandInDoubtDetailOrder"]);
        Assert.Equal("beforeUtc", initialCommandCatalogEntry.Metadata["commandInDoubtSummaryQuery"]);
        Assert.Equal("retained-reserved-summary-observed-utc-before-or-equal", initialCommandCatalogEntry.Metadata["commandInDoubtSummaryPolicy"]);
        Assert.Equal("beforeUtc", initialCommandCatalogEntry.Metadata["commandOldestInDoubtQuery"]);
        Assert.Equal("oldest-retained-reserved-observed-utc-before-or-equal", initialCommandCatalogEntry.Metadata["commandOldestInDoubtPolicy"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/outboxes/{outboxId}", initialCommandCatalogEntry.Metadata["commandOutboxRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/observations?fromUtc={fromUtc}&toUtc={toUtc}", initialCommandCatalogEntry.Metadata["commandObservationRoute"]);
        Assert.Equal("fromUtc,toUtc", initialCommandCatalogEntry.Metadata["commandObservationWindowQuery"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/outboxes/{outboxId}/summary", initialCommandCatalogEntry.Metadata["commandOutboxSummaryRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/outboxes/{outboxId}/retention", initialCommandCatalogEntry.Metadata["commandOutboxRetentionRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/outboxes/{outboxId}/latest", initialCommandCatalogEntry.Metadata["commandOutboxLatestRoute"]);
        Assert.Equal("/engine/event-dispatch-remediation-commands/outboxes/{outboxId}/oldest", initialCommandCatalogEntry.Metadata["commandOutboxOldestRoute"]);
        Assert.Equal("retained-filter-server-side-aggregate", initialCommandCatalogEntry.Metadata["commandFilterSummaryPolicy"]);
        Assert.Equal("outboxes,messages,channels,operations,actors,correlations,reasons,dispatch-outcomes,outcomes", initialCommandCatalogEntry.Metadata["commandFilterSummaryRoutes"]);
        Assert.Equal("retained-filter-server-side-retention", initialCommandCatalogEntry.Metadata["commandFilterRetentionPolicy"]);
        Assert.Equal("outboxes,messages,channels,operations,actors,correlations,reasons,dispatch-outcomes,outcomes", initialCommandCatalogEntry.Metadata["commandFilterRetentionRoutes"]);
        Assert.Equal(nameof(EventDispatchRemediationRuntimeRetention), initialCommandCatalogEntry.Metadata["commandFilterRetentionResponse"]);
        Assert.Equal("not-required", initialCommandCatalogEntry.Metadata["commandFilterRetentionMaterialization"]);
        Assert.Equal("retained-filter-server-side-latest", initialCommandCatalogEntry.Metadata["commandFilterLatestPolicy"]);
        Assert.Equal("outboxes,messages,channels,operations,actors,correlations,reasons,dispatch-outcomes,outcomes", initialCommandCatalogEntry.Metadata["commandFilterLatestRoutes"]);
        Assert.Equal(nameof(EventDispatchRemediationRuntimeState), initialCommandCatalogEntry.Metadata["commandFilterLatestResponse"]);
        Assert.Equal("not-required", initialCommandCatalogEntry.Metadata["commandFilterLatestMaterialization"]);
        Assert.Equal("not-found", initialCommandCatalogEntry.Metadata["commandFilterLatestMissing"]);
        Assert.Equal("retained-filter-server-side-oldest", initialCommandCatalogEntry.Metadata["commandFilterOldestPolicy"]);
        Assert.Equal("outboxes,messages,channels,operations,actors,correlations,reasons,dispatch-outcomes,outcomes", initialCommandCatalogEntry.Metadata["commandFilterOldestRoutes"]);
        Assert.Equal(nameof(EventDispatchRemediationRuntimeState), initialCommandCatalogEntry.Metadata["commandFilterOldestResponse"]);
        Assert.Equal("not-required", initialCommandCatalogEntry.Metadata["commandFilterOldestMaterialization"]);
        Assert.Equal("not-found", initialCommandCatalogEntry.Metadata["commandFilterOldestMissing"]);
        Assert.Equal("positive-integer-newest-first", initialCommandCatalogEntry.Metadata["commandReadLimitPolicy"]);
        Assert.Equal("all,in-doubt,observations,outboxes,messages,channels,operations,actors,correlations,reasons,dispatch-outcomes,outcomes", initialCommandCatalogEntry.Metadata["commandReadLimitRoutes"]);
        Assert.Equal("opaque-signed-route-bound-continuation-token-newest-first", initialCommandCatalogEntry.Metadata["commandPaginationPolicy"]);
        Assert.Equal("all,in-doubt,observations,outboxes,messages,channels,operations,actors,correlations,reasons,dispatch-outcomes,outcomes", initialCommandCatalogEntry.Metadata["commandPaginationRoutes"]);
        Assert.Equal("false", initialCommandCatalogEntry.Metadata["wolverineRequired"]);
        Assert.Equal("true", initialCommandCatalogEntry.Metadata["providerNeutral"]);
        Assert.Equal("unique-command-id", initialCommandCatalogEntry.Metadata[EventDispatchRemediationMetadataKeys.CommandIdempotencyPolicy]);
        Assert.Equal("reject-without-mutation", initialCommandCatalogEntry.Metadata[EventDispatchRemediationMetadataKeys.DuplicateCommandPolicy]);
        Assert.Equal(remediationCapability.Metadata["commandListRoute"], initialCommandCatalogEntry.Metadata["commandListRoute"]);
        Assert.Equal(remediationCapability.Metadata["commandResultRoute"], initialCommandCatalogEntry.Metadata["commandResultRoute"]);
        Assert.Equal(remediationCapability.Metadata["commandSummaryRoute"], initialCommandCatalogEntry.Metadata["commandSummaryRoute"]);
        Assert.Equal(remediationCapability.Metadata["commandInDoubtRoute"], initialCommandCatalogEntry.Metadata["commandInDoubtRoute"]);
        Assert.Equal(remediationCapability.Metadata["commandInDoubtSummaryRoute"], initialCommandCatalogEntry.Metadata["commandInDoubtSummaryRoute"]);
        Assert.Equal(remediationCapability.Metadata["commandOldestInDoubtRoute"], initialCommandCatalogEntry.Metadata["commandOldestInDoubtRoute"]);
        Assert.Equal(remediationCapability.Metadata["commandInDoubtQuery"], initialCommandCatalogEntry.Metadata["commandInDoubtQuery"]);
        Assert.Equal(remediationCapability.Metadata["commandInDoubtCutoffPolicy"], initialCommandCatalogEntry.Metadata["commandInDoubtCutoffPolicy"]);
        Assert.Equal(remediationCapability.Metadata["commandInDoubtSummaryPolicy"], initialCommandCatalogEntry.Metadata["commandInDoubtSummaryPolicy"]);
        Assert.Equal(remediationCapability.Metadata["commandOldestInDoubtPolicy"], initialCommandCatalogEntry.Metadata["commandOldestInDoubtPolicy"]);
        Assert.Equal(remediationCapability.Metadata["commandOutboxRoute"], initialCommandCatalogEntry.Metadata["commandOutboxRoute"]);
        Assert.Equal(remediationCapability.Metadata["commandObservationRoute"], initialCommandCatalogEntry.Metadata["commandObservationRoute"]);
        Assert.Equal(remediationCapability.Metadata["commandObservationWindowQuery"], initialCommandCatalogEntry.Metadata["commandObservationWindowQuery"]);
        Assert.Equal(remediationCapability.Metadata["commandOutboxRetentionRoute"], initialCommandCatalogEntry.Metadata["commandOutboxRetentionRoute"]);
        Assert.Equal(remediationCapability.Metadata["commandFilterRetentionPolicy"], initialCommandCatalogEntry.Metadata["commandFilterRetentionPolicy"]);
        Assert.Equal(remediationCapability.Metadata["commandFilterRetentionRoutes"], initialCommandCatalogEntry.Metadata["commandFilterRetentionRoutes"]);
        Assert.Equal(remediationCapability.Metadata["commandFilterRetentionResponse"], initialCommandCatalogEntry.Metadata["commandFilterRetentionResponse"]);
        Assert.Equal(remediationCapability.Metadata["commandFilterRetentionMaterialization"], initialCommandCatalogEntry.Metadata["commandFilterRetentionMaterialization"]);
        Assert.Equal(remediationCapability.Metadata["commandOutboxLatestRoute"], initialCommandCatalogEntry.Metadata["commandOutboxLatestRoute"]);
        Assert.Equal(remediationCapability.Metadata["commandOutboxOldestRoute"], initialCommandCatalogEntry.Metadata["commandOutboxOldestRoute"]);
        Assert.Equal(remediationCapability.Metadata["commandFilterLatestPolicy"], initialCommandCatalogEntry.Metadata["commandFilterLatestPolicy"]);
        Assert.Equal(remediationCapability.Metadata["commandFilterLatestRoutes"], initialCommandCatalogEntry.Metadata["commandFilterLatestRoutes"]);
        Assert.Equal(remediationCapability.Metadata["commandFilterLatestResponse"], initialCommandCatalogEntry.Metadata["commandFilterLatestResponse"]);
        Assert.Equal(remediationCapability.Metadata["commandFilterLatestMaterialization"], initialCommandCatalogEntry.Metadata["commandFilterLatestMaterialization"]);
        Assert.Equal(remediationCapability.Metadata["commandFilterLatestMissing"], initialCommandCatalogEntry.Metadata["commandFilterLatestMissing"]);
        Assert.Equal(remediationCapability.Metadata["commandFilterOldestPolicy"], initialCommandCatalogEntry.Metadata["commandFilterOldestPolicy"]);
        Assert.Equal(remediationCapability.Metadata["commandFilterOldestRoutes"], initialCommandCatalogEntry.Metadata["commandFilterOldestRoutes"]);
        Assert.Equal(remediationCapability.Metadata["commandFilterOldestResponse"], initialCommandCatalogEntry.Metadata["commandFilterOldestResponse"]);
        Assert.Equal(remediationCapability.Metadata["commandFilterOldestMaterialization"], initialCommandCatalogEntry.Metadata["commandFilterOldestMaterialization"]);
        Assert.Equal(remediationCapability.Metadata["commandFilterOldestMissing"], initialCommandCatalogEntry.Metadata["commandFilterOldestMissing"]);
        Assert.Equal(remediationCapability.Metadata["commandReadLimitPolicy"], initialCommandCatalogEntry.Metadata["commandReadLimitPolicy"]);
        Assert.Equal(remediationCapability.Metadata["commandReadLimitRoutes"], initialCommandCatalogEntry.Metadata["commandReadLimitRoutes"]);
        Assert.Equal(remediationCapability.Metadata["commandPaginationQuery"], initialCommandCatalogEntry.Metadata["commandPaginationQuery"]);
        Assert.Equal(remediationCapability.Metadata["commandPaginationPolicy"], initialCommandCatalogEntry.Metadata["commandPaginationPolicy"]);
        Assert.Equal(remediationCapability.Metadata["commandPaginationRoutes"], initialCommandCatalogEntry.Metadata["commandPaginationRoutes"]);
        Assert.Equal(remediationCapability.Metadata["commandPageSizeMaximum"], initialCommandCatalogEntry.Metadata["commandPageSizeMaximum"]);
        Assert.Equal(remediationCapability.Metadata[EventDispatchRemediationMetadataKeys.CommandIdempotencyPolicy], initialCommandCatalogEntry.Metadata[EventDispatchRemediationMetadataKeys.CommandIdempotencyPolicy]);
        Assert.False(initialCommandCatalogEntry.Metadata.ContainsKey("latestCommandId"));

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

        var retryReason = "Downstream recovered.";
        var commandResponse = await client.PostAsJsonAsync(
            "/engine/event-dispatches/entity-framework-outbox/commands/retry-now",
            new
            {
                commandId = "cmd-command-001-retry",
                messageId = "evt-command-001",
                channelId = "catalog-events",
                reason = retryReason,
                actorId = "operator-001",
                correlationId = "corr-command-operator-001"
            });
        commandResponse.EnsureSuccessStatusCode();
        var commandResult = await commandResponse.Content.ReadFromJsonAsync<EventDispatchRemediationResult>();
        var commandStates = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState[]>("/engine/event-dispatch-remediation-commands");
        var commandState = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState>("/engine/event-dispatch-remediation-commands/cmd-command-001-retry");
        var latestCommandState = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState>("/engine/event-dispatch-remediation-commands/latest");
        var commandStatesByOutbox = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState[]>("/engine/event-dispatch-remediation-commands/outboxes/entity-framework-outbox");
        var commandStatesByMessage = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState[]>("/engine/event-dispatch-remediation-commands/messages/evt-command-001");
        var commandStatesByChannel = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState[]>("/engine/event-dispatch-remediation-commands/channels/catalog-events");
        var commandStatesByRetryNowOperation = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState[]>("/engine/event-dispatch-remediation-commands/operations/retry-now");
        var commandStatesByOperator = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState[]>("/engine/event-dispatch-remediation-commands/actors/operator-001");
        var commandStatesByCorrelation = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState[]>("/engine/event-dispatch-remediation-commands/correlations/corr-command-operator-001");
        var commandStatesByReason = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState[]>($"/engine/event-dispatch-remediation-commands/reasons/{Uri.EscapeDataString(retryReason)}");
        var acceptedCommandStates = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState[]>("/engine/event-dispatch-remediation-commands/outcomes/accepted");
        var retryScheduledCommandStates = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState[]>("/engine/event-dispatch-remediation-commands/dispatch-outcomes/retry-scheduled");
        var latestCommandByOutbox = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState>("/engine/event-dispatch-remediation-commands/outboxes/entity-framework-outbox/latest");
        var latestCommandByMessage = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState>("/engine/event-dispatch-remediation-commands/messages/evt-command-001/latest");
        var latestCommandByChannel = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState>("/engine/event-dispatch-remediation-commands/channels/catalog-events/latest");
        var latestCommandByOperation = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState>("/engine/event-dispatch-remediation-commands/operations/retry-now/latest");
        var latestCommandByActor = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState>("/engine/event-dispatch-remediation-commands/actors/operator-001/latest");
        var latestCommandByCorrelation = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState>("/engine/event-dispatch-remediation-commands/correlations/corr-command-operator-001/latest");
        var latestCommandByReason = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState>($"/engine/event-dispatch-remediation-commands/reasons/{Uri.EscapeDataString(retryReason)}/latest");
        var latestCommandByOutcome = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState>("/engine/event-dispatch-remediation-commands/outcomes/accepted/latest");
        var latestCommandByDispatchOutcome = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState>("/engine/event-dispatch-remediation-commands/dispatch-outcomes/retry-scheduled/latest");
        var oldestCommandByOutbox = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState>("/engine/event-dispatch-remediation-commands/outboxes/entity-framework-outbox/oldest");
        var oldestCommandByMessage = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState>("/engine/event-dispatch-remediation-commands/messages/evt-command-001/oldest");
        var oldestCommandByChannel = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState>("/engine/event-dispatch-remediation-commands/channels/catalog-events/oldest");
        var oldestCommandByOperation = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState>("/engine/event-dispatch-remediation-commands/operations/retry-now/oldest");
        var oldestCommandByActor = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState>("/engine/event-dispatch-remediation-commands/actors/operator-001/oldest");
        var oldestCommandByCorrelation = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState>("/engine/event-dispatch-remediation-commands/correlations/corr-command-operator-001/oldest");
        var oldestCommandByReason = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState>($"/engine/event-dispatch-remediation-commands/reasons/{Uri.EscapeDataString(retryReason)}/oldest");
        var oldestCommandByOutcome = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState>("/engine/event-dispatch-remediation-commands/outcomes/accepted/oldest");
        var oldestCommandByDispatchOutcome = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState>("/engine/event-dispatch-remediation-commands/dispatch-outcomes/retry-scheduled/oldest");
        var retentionByOutbox = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeRetention>("/engine/event-dispatch-remediation-commands/outboxes/entity-framework-outbox/retention");
        var retentionByMessage = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeRetention>("/engine/event-dispatch-remediation-commands/messages/evt-command-001/retention");
        var retentionByChannel = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeRetention>("/engine/event-dispatch-remediation-commands/channels/catalog-events/retention");
        var retentionByOperation = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeRetention>("/engine/event-dispatch-remediation-commands/operations/retry-now/retention");
        var retentionByActor = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeRetention>("/engine/event-dispatch-remediation-commands/actors/operator-001/retention");
        var retentionByCorrelation = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeRetention>("/engine/event-dispatch-remediation-commands/correlations/corr-command-operator-001/retention");
        var retentionByReason = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeRetention>($"/engine/event-dispatch-remediation-commands/reasons/{Uri.EscapeDataString(retryReason)}/retention");
        var retentionByOutcome = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeRetention>("/engine/event-dispatch-remediation-commands/outcomes/accepted/retention");
        var retentionByDispatchOutcome = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeRetention>("/engine/event-dispatch-remediation-commands/dispatch-outcomes/retry-scheduled/retention");
        var missingLatestCommandByMessageResponse = await client.GetAsync("/engine/event-dispatch-remediation-commands/messages/missing-message/latest");
        var missingOldestCommandByMessageResponse = await client.GetAsync("/engine/event-dispatch-remediation-commands/messages/missing-message/oldest");
        var missingRetentionByMessage = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeRetention>("/engine/event-dispatch-remediation-commands/messages/missing-message/retention");
        var inDoubtCommandStates = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState[]>("/engine/event-dispatch-remediation-commands/in-doubt");
        var commandSummary = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeSummary>("/engine/event-dispatch-remediation-commands/summary");
        var commandRetention = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeRetention>("/engine/event-dispatch-remediation-commands/retention");
        var remediatedState = await client.GetFromJsonAsync<EventDispatchRuntimeState>("/engine/event-dispatches/entity-framework-outbox");
        await using var readScope = app.Services.CreateAsyncScope();
        var readStore = readScope.ServiceProvider.GetRequiredService<IEventDispatchStore>();
        var pending = await readStore.ReadPendingAsync(10);

        Assert.NotNull(commandResult);
        Assert.Equal(EventDispatchRemediationOutcomes.Accepted, commandResult.Outcome);
        Assert.Equal(EventDispatchExecutionOutcomes.RetryScheduled, commandResult.DispatchOutcome);
        Assert.Equal("cmd-command-001-retry", commandResult.CommandId);
        Assert.Equal("operator-001", commandResult.Metadata[EventDispatchRemediationMetadataKeys.OperatorActorId]);
        Assert.Equal("corr-command-operator-001", commandResult.Metadata[EventDispatchRemediationMetadataKeys.OperatorCorrelationId]);
        Assert.Equal(retryReason, commandResult.Metadata[EventDispatchRemediationMetadataKeys.OperatorCommandReason]);
        Assert.Equal("unique-command-id", commandResult.Metadata[EventDispatchRemediationMetadataKeys.CommandIdempotencyPolicy]);
        Assert.Equal("reject-without-mutation", commandResult.Metadata[EventDispatchRemediationMetadataKeys.DuplicateCommandPolicy]);
        Assert.NotNull(commandStates);
        var listedCommandState = Assert.Single(commandStates);
        Assert.Equal("cmd-command-001-retry", listedCommandState.CommandId);
        Assert.NotNull(commandState);
        Assert.Equal("cmd-command-001-retry", commandState.CommandId);
        Assert.Equal(EventDispatchRemediationOutcomes.Accepted, commandState.Outcome);
        Assert.Equal(EventDispatchExecutionOutcomes.RetryScheduled, commandState.DispatchOutcome);
        Assert.Equal("operator-001", commandState.Metadata[EventDispatchRemediationMetadataKeys.OperatorActorId]);
        Assert.Equal("corr-command-operator-001", commandState.Metadata[EventDispatchRemediationMetadataKeys.OperatorCorrelationId]);
        Assert.Equal(retryReason, commandState.Metadata[EventDispatchRemediationMetadataKeys.OperatorCommandReason]);
        Assert.Equal("unique-command-id", commandState.Metadata[EventDispatchRemediationMetadataKeys.CommandIdempotencyPolicy]);
        Assert.Equal("reject-without-mutation", commandState.Metadata[EventDispatchRemediationMetadataKeys.DuplicateCommandPolicy]);
        var commandStatesByObservationWindow = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState[]>(
            BuildObservationWindowRoute(
                commandState.ObservedAtUtc.AddSeconds(-1),
                commandState.ObservedAtUtc.AddSeconds(1)));
        var commandObservationSummary = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeSummary>(
            BuildObservationWindowSummaryRoute(
                commandState.ObservedAtUtc.AddSeconds(-1),
                commandState.ObservedAtUtc.AddSeconds(1)));
        var commandStatesBeforeObservationWindow = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState[]>(
            BuildObservationWindowRoute(null, commandState.ObservedAtUtc.AddSeconds(-1)));
        var commandSummaryBeforeObservationWindow = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeSummary>(
            BuildObservationWindowSummaryRoute(null, commandState.ObservedAtUtc.AddSeconds(-1)));
        var invalidObservationWindowResponse = await client.GetAsync("/engine/event-dispatch-remediation-commands/observations?fromUtc=not-a-date");
        var invalidObservationWindowSummaryResponse = await client.GetAsync("/engine/event-dispatch-remediation-commands/observations/summary?fromUtc=not-a-date");
        var invalidCommandReadLimitResponse = await client.GetAsync("/engine/event-dispatch-remediation-commands?limit=0");
        var invalidCommandFilterReadLimitResponse = await client.GetAsync("/engine/event-dispatch-remediation-commands/messages/evt-command-001?limit=not-a-number");
        var reversedObservationWindowResponse = await client.GetAsync(BuildObservationWindowRoute(
            commandState.ObservedAtUtc.AddSeconds(1),
            commandState.ObservedAtUtc.AddSeconds(-1)));
        var reversedObservationWindowSummaryResponse = await client.GetAsync(BuildObservationWindowSummaryRoute(
            commandState.ObservedAtUtc.AddSeconds(1),
            commandState.ObservedAtUtc.AddSeconds(-1)));
        Assert.NotNull(commandStatesByObservationWindow);
        Assert.Equal("cmd-command-001-retry", Assert.Single(commandStatesByObservationWindow).CommandId);
        Assert.NotNull(commandObservationSummary);
        Assert.Equal(1, commandObservationSummary.TotalCommandCount);
        Assert.Equal(1, commandObservationSummary.AcceptedCount);
        Assert.Equal(0, commandObservationSummary.RejectedCount);
        Assert.Equal(0, commandObservationSummary.ReservedCount);
        Assert.Equal("cmd-command-001-retry", commandObservationSummary.LastCommandId);
        Assert.Equal(0, commandObservationSummary.DroppedCommandCount);
        Assert.False(commandObservationSummary.RetentionTruncated);
        Assert.False(commandObservationSummary.SummaryMayBeIncomplete);
        Assert.Equal("cmd-command-001-retry", commandObservationSummary.OldestRetainedCommandId);
        Assert.Equal(commandState.ObservedAtUtc, commandObservationSummary.OldestRetainedObservedAtUtc);
        Assert.Null(commandObservationSummary.OldestReservedCommandId);
        Assert.Null(commandObservationSummary.OldestReservedObservedAtUtc);
        Assert.True(commandObservationSummary.HasCommands);
        Assert.False(commandObservationSummary.HasFailures);
        Assert.False(commandObservationSummary.HasInDoubtCommands);
        Assert.NotNull(commandStatesBeforeObservationWindow);
        Assert.Empty(commandStatesBeforeObservationWindow);
        Assert.NotNull(commandSummaryBeforeObservationWindow);
        Assert.Equal(0, commandSummaryBeforeObservationWindow.TotalCommandCount);
        Assert.Equal(0, commandSummaryBeforeObservationWindow.ReservedCount);
        Assert.Equal(0, commandSummaryBeforeObservationWindow.DroppedCommandCount);
        Assert.False(commandSummaryBeforeObservationWindow.RetentionTruncated);
        Assert.False(commandSummaryBeforeObservationWindow.SummaryMayBeIncomplete);
        Assert.Null(commandSummaryBeforeObservationWindow.OldestRetainedCommandId);
        Assert.Null(commandSummaryBeforeObservationWindow.OldestReservedCommandId);
        Assert.Null(commandSummaryBeforeObservationWindow.OldestReservedObservedAtUtc);
        Assert.False(commandSummaryBeforeObservationWindow.HasCommands);
        Assert.False(commandSummaryBeforeObservationWindow.HasInDoubtCommands);
        Assert.Equal(HttpStatusCode.BadRequest, invalidObservationWindowResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalidObservationWindowSummaryResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalidCommandReadLimitResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalidCommandFilterReadLimitResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, reversedObservationWindowResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, reversedObservationWindowSummaryResponse.StatusCode);
        Assert.NotNull(latestCommandState);
        Assert.Equal("cmd-command-001-retry", latestCommandState.CommandId);
        Assert.Equal(EventDispatchRemediationOutcomes.Accepted, latestCommandState.Outcome);
        Assert.Equal("retry-now", latestCommandState.OperationId);
        Assert.NotNull(commandStatesByOutbox);
        Assert.Equal("cmd-command-001-retry", Assert.Single(commandStatesByOutbox).CommandId);
        Assert.NotNull(commandStatesByMessage);
        Assert.Equal("cmd-command-001-retry", Assert.Single(commandStatesByMessage).CommandId);
        Assert.NotNull(commandStatesByChannel);
        Assert.Equal("cmd-command-001-retry", Assert.Single(commandStatesByChannel).CommandId);
        Assert.NotNull(commandStatesByRetryNowOperation);
        Assert.Equal("cmd-command-001-retry", Assert.Single(commandStatesByRetryNowOperation).CommandId);
        Assert.NotNull(commandStatesByOperator);
        Assert.Equal("cmd-command-001-retry", Assert.Single(commandStatesByOperator).CommandId);
        Assert.NotNull(commandStatesByCorrelation);
        Assert.Equal("cmd-command-001-retry", Assert.Single(commandStatesByCorrelation).CommandId);
        Assert.NotNull(commandStatesByReason);
        Assert.Equal("cmd-command-001-retry", Assert.Single(commandStatesByReason).CommandId);
        Assert.NotNull(acceptedCommandStates);
        Assert.Equal("cmd-command-001-retry", Assert.Single(acceptedCommandStates).CommandId);
        Assert.NotNull(retryScheduledCommandStates);
        Assert.Equal("cmd-command-001-retry", Assert.Single(retryScheduledCommandStates).CommandId);
        Assert.Equal("cmd-command-001-retry", latestCommandByOutbox?.CommandId);
        Assert.Equal("cmd-command-001-retry", latestCommandByMessage?.CommandId);
        Assert.Equal("cmd-command-001-retry", latestCommandByChannel?.CommandId);
        Assert.Equal("cmd-command-001-retry", latestCommandByOperation?.CommandId);
        Assert.Equal("cmd-command-001-retry", latestCommandByActor?.CommandId);
        Assert.Equal("cmd-command-001-retry", latestCommandByCorrelation?.CommandId);
        Assert.Equal("cmd-command-001-retry", latestCommandByReason?.CommandId);
        Assert.Equal("cmd-command-001-retry", latestCommandByOutcome?.CommandId);
        Assert.Equal("cmd-command-001-retry", latestCommandByDispatchOutcome?.CommandId);
        Assert.Equal("cmd-command-001-retry", oldestCommandByOutbox?.CommandId);
        Assert.Equal("cmd-command-001-retry", oldestCommandByMessage?.CommandId);
        Assert.Equal("cmd-command-001-retry", oldestCommandByChannel?.CommandId);
        Assert.Equal("cmd-command-001-retry", oldestCommandByOperation?.CommandId);
        Assert.Equal("cmd-command-001-retry", oldestCommandByActor?.CommandId);
        Assert.Equal("cmd-command-001-retry", oldestCommandByCorrelation?.CommandId);
        Assert.Equal("cmd-command-001-retry", oldestCommandByReason?.CommandId);
        Assert.Equal("cmd-command-001-retry", oldestCommandByOutcome?.CommandId);
        Assert.Equal("cmd-command-001-retry", oldestCommandByDispatchOutcome?.CommandId);
        AssertRetention(retentionByOutbox, 1, "cmd-command-001-retry", "cmd-command-001-retry");
        AssertRetention(retentionByMessage, 1, "cmd-command-001-retry", "cmd-command-001-retry");
        AssertRetention(retentionByChannel, 1, "cmd-command-001-retry", "cmd-command-001-retry");
        AssertRetention(retentionByOperation, 1, "cmd-command-001-retry", "cmd-command-001-retry");
        AssertRetention(retentionByActor, 1, "cmd-command-001-retry", "cmd-command-001-retry");
        AssertRetention(retentionByCorrelation, 1, "cmd-command-001-retry", "cmd-command-001-retry");
        AssertRetention(retentionByReason, 1, "cmd-command-001-retry", "cmd-command-001-retry");
        AssertRetention(retentionByOutcome, 1, "cmd-command-001-retry", "cmd-command-001-retry");
        AssertRetention(retentionByDispatchOutcome, 1, "cmd-command-001-retry", "cmd-command-001-retry");
        Assert.Equal(HttpStatusCode.NotFound, missingLatestCommandByMessageResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missingOldestCommandByMessageResponse.StatusCode);
        AssertRetention(missingRetentionByMessage, 0, null, null);
        Assert.NotNull(inDoubtCommandStates);
        Assert.Empty(inDoubtCommandStates);
        Assert.NotNull(commandSummary);
        Assert.Equal(1, commandSummary.TotalCommandCount);
        Assert.Equal(1, commandSummary.AcceptedCount);
        Assert.Equal(0, commandSummary.RejectedCount);
        Assert.Equal(0, commandSummary.ErrorCount);
        Assert.Equal(0, commandSummary.DuplicateCommandCount);
        Assert.Equal(0, commandSummary.ReservedCount);
        Assert.Equal("cmd-command-001-retry", commandSummary.LastCommandId);
        Assert.Equal("retry-now", commandSummary.LastOperationId);
        Assert.Equal(EventDispatchRemediationOutcomes.Accepted, commandSummary.LastOutcome);
        Assert.Equal(EventDispatchExecutionOutcomes.RetryScheduled, commandSummary.LastDispatchOutcome);
        Assert.Equal(0, commandSummary.DroppedCommandCount);
        Assert.False(commandSummary.RetentionTruncated);
        Assert.False(commandSummary.SummaryMayBeIncomplete);
        Assert.Equal("cmd-command-001-retry", commandSummary.OldestRetainedCommandId);
        Assert.Null(commandSummary.OldestReservedCommandId);
        Assert.Null(commandSummary.OldestReservedObservedAtUtc);
        Assert.True(commandSummary.HasCommands);
        Assert.False(commandSummary.HasFailures);
        Assert.False(commandSummary.HasInDoubtCommands);
        Assert.NotNull(commandRetention);
        Assert.Equal(0, commandRetention.HistoryLimit);
        Assert.Equal(1, commandRetention.RetainedCommandCount);
        Assert.Equal(1, commandRetention.TotalRecordedCommandCount);
        Assert.Equal(0, commandRetention.DroppedCommandCount);
        Assert.False(commandRetention.Truncated);
        Assert.Equal("cmd-command-001-retry", commandRetention.OldestRetainedCommandId);
        Assert.Equal("cmd-command-001-retry", commandRetention.LatestRetainedCommandId);
        Assert.NotNull(remediatedState);
        Assert.Equal(EventDispatchExecutionOutcomes.RetryScheduled, remediatedState.LastOutcome);
        Assert.True(remediatedState.RetryPending);
        Assert.False(remediatedState.TerminalFailure);
        Assert.Equal("retry-now", remediatedState.Metadata["operatorCommand"]);
        Assert.Equal("dispatch-store", remediatedState.Metadata[EventDispatchRuntimeMetadataKeys.RetryDurability]);
        var pendingItem = Assert.Single(pending);
        Assert.Equal("evt-command-001", pendingItem.MessageId);
        Assert.Equal(2, pendingItem.DispatchAttemptCount);

        var duplicateReason = "Accidental duplicate command id with a different operation.";
        var duplicateCommandResponse = await client.PostAsJsonAsync(
            "/engine/event-dispatches/entity-framework-outbox/commands/skip",
            new
            {
                commandId = "cmd-command-001-retry",
                messageId = "evt-command-001",
                channelId = "catalog-events",
                reason = duplicateReason,
                actorId = "operator-002",
                correlationId = "corr-command-operator-duplicate"
            });
        Assert.Equal(HttpStatusCode.Conflict, duplicateCommandResponse.StatusCode);
        var duplicateCommandResult = await duplicateCommandResponse.Content.ReadFromJsonAsync<EventDispatchRemediationResult>();
        var originalCommandStateAfterDuplicate = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState>("/engine/event-dispatch-remediation-commands/cmd-command-001-retry");
        var commandStatesAfterDuplicate = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState[]>("/engine/event-dispatch-remediation-commands");
        var commandStatesByMessageAfterDuplicate = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState[]>("/engine/event-dispatch-remediation-commands/messages/evt-command-001");
        var commandStatesByChannelAfterDuplicate = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState[]>("/engine/event-dispatch-remediation-commands/channels/catalog-events");
        var commandStatesBySkipOperation = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState[]>("/engine/event-dispatch-remediation-commands/operations/skip");
        var commandStatesByDuplicateActor = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState[]>("/engine/event-dispatch-remediation-commands/actors/operator-002");
        var commandStatesByDuplicateCorrelation = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState[]>("/engine/event-dispatch-remediation-commands/correlations/corr-command-operator-duplicate");
        var commandStatesByDuplicateReason = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState[]>($"/engine/event-dispatch-remediation-commands/reasons/{Uri.EscapeDataString(duplicateReason)}");
        var retryScheduledCommandStatesAfterDuplicate = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState[]>("/engine/event-dispatch-remediation-commands/dispatch-outcomes/retry-scheduled");
        var skippedCommandStatesAfterDuplicate = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState[]>("/engine/event-dispatch-remediation-commands/dispatch-outcomes/skipped");
        var commandSummaryAfterDuplicate = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeSummary>("/engine/event-dispatch-remediation-commands/summary");
        var latestCommandStateAfterDuplicate = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState>("/engine/event-dispatch-remediation-commands/latest");
        var commandRetentionAfterDuplicate = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeRetention>("/engine/event-dispatch-remediation-commands/retention");
        var remediatedStateAfterDuplicate = await client.GetFromJsonAsync<EventDispatchRuntimeState>("/engine/event-dispatches/entity-framework-outbox");
        pending = await readStore.ReadPendingAsync(10);

        Assert.NotNull(duplicateCommandResult);
        Assert.Equal(EventDispatchRemediationOutcomes.Rejected, duplicateCommandResult.Outcome);
        Assert.Equal("skip", duplicateCommandResult.OperationId);
        Assert.Equal("corr-command-operator-duplicate", duplicateCommandResult.Metadata[EventDispatchRemediationMetadataKeys.OperatorCorrelationId]);
        Assert.Equal(duplicateReason, duplicateCommandResult.Metadata[EventDispatchRemediationMetadataKeys.OperatorCommandReason]);
        Assert.Equal("true", duplicateCommandResult.Metadata[EventDispatchRemediationMetadataKeys.DuplicateCommand]);
        Assert.Equal(EventDispatchRemediationOutcomes.Accepted, duplicateCommandResult.Metadata[EventDispatchRemediationMetadataKeys.ExistingCommandOutcome]);
        Assert.Equal("retry-now", duplicateCommandResult.Metadata[EventDispatchRemediationMetadataKeys.ExistingCommandOperationId]);
        Assert.Equal("entity-framework-outbox", duplicateCommandResult.Metadata[EventDispatchRemediationMetadataKeys.ExistingCommandOutboxId]);
        Assert.Contains("already recorded", duplicateCommandResult.Error, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(originalCommandStateAfterDuplicate);
        Assert.Equal(EventDispatchRemediationOutcomes.Accepted, originalCommandStateAfterDuplicate.Outcome);
        Assert.Equal("retry-now", originalCommandStateAfterDuplicate.OperationId);
        Assert.NotNull(latestCommandStateAfterDuplicate);
        Assert.Equal("cmd-command-001-retry", latestCommandStateAfterDuplicate.CommandId);
        Assert.Equal(EventDispatchRemediationOutcomes.Accepted, latestCommandStateAfterDuplicate.Outcome);
        Assert.Equal("retry-now", latestCommandStateAfterDuplicate.OperationId);
        Assert.NotNull(commandStatesAfterDuplicate);
        Assert.Single(commandStatesAfterDuplicate);
        Assert.NotNull(commandStatesByMessageAfterDuplicate);
        Assert.Equal("cmd-command-001-retry", Assert.Single(commandStatesByMessageAfterDuplicate).CommandId);
        Assert.NotNull(commandStatesByChannelAfterDuplicate);
        Assert.Equal("cmd-command-001-retry", Assert.Single(commandStatesByChannelAfterDuplicate).CommandId);
        Assert.NotNull(commandStatesBySkipOperation);
        Assert.Empty(commandStatesBySkipOperation);
        Assert.NotNull(commandStatesByDuplicateActor);
        Assert.Empty(commandStatesByDuplicateActor);
        Assert.NotNull(commandStatesByDuplicateCorrelation);
        Assert.Empty(commandStatesByDuplicateCorrelation);
        Assert.NotNull(commandStatesByDuplicateReason);
        Assert.Empty(commandStatesByDuplicateReason);
        Assert.NotNull(retryScheduledCommandStatesAfterDuplicate);
        Assert.Equal("cmd-command-001-retry", Assert.Single(retryScheduledCommandStatesAfterDuplicate).CommandId);
        Assert.NotNull(skippedCommandStatesAfterDuplicate);
        Assert.Empty(skippedCommandStatesAfterDuplicate);
        Assert.NotNull(commandSummaryAfterDuplicate);
        Assert.Equal(1, commandSummaryAfterDuplicate.TotalCommandCount);
        Assert.Equal(1, commandSummaryAfterDuplicate.AcceptedCount);
        Assert.Equal(0, commandSummaryAfterDuplicate.RejectedCount);
        Assert.Equal(0, commandSummaryAfterDuplicate.DuplicateCommandCount);
        Assert.Equal(0, commandSummaryAfterDuplicate.ReservedCount);
        Assert.Null(commandSummaryAfterDuplicate.OldestReservedCommandId);
        Assert.Equal("cmd-command-001-retry", commandSummaryAfterDuplicate.LastCommandId);
        Assert.NotNull(commandRetentionAfterDuplicate);
        Assert.Equal(1, commandRetentionAfterDuplicate.RetainedCommandCount);
        Assert.Equal(1, commandRetentionAfterDuplicate.TotalRecordedCommandCount);
        Assert.Equal(0, commandRetentionAfterDuplicate.DroppedCommandCount);
        Assert.False(commandRetentionAfterDuplicate.Truncated);
        Assert.Equal("cmd-command-001-retry", commandRetentionAfterDuplicate.LatestRetainedCommandId);
        Assert.NotNull(remediatedStateAfterDuplicate);
        Assert.Equal(EventDispatchExecutionOutcomes.RetryScheduled, remediatedStateAfterDuplicate.LastOutcome);
        Assert.Equal("retry-now", remediatedStateAfterDuplicate.Metadata["operatorCommand"]);
        Assert.Single(pending);

        var deadLetterReason = "Move the poison event into dispatch-store dead-letter posture.";
        var deadLetterResponse = await client.PostAsJsonAsync(
            "/engine/event-dispatches/entity-framework-outbox/commands/dead-letter",
            new
            {
                commandId = "cmd-command-001-dead-letter",
                messageId = "evt-command-001",
                channelId = "catalog-events",
                reason = deadLetterReason,
                actorId = "operator-001",
                correlationId = "corr-command-operator-dead-letter"
            });
        deadLetterResponse.EnsureSuccessStatusCode();
        var deadLetterResult = await deadLetterResponse.Content.ReadFromJsonAsync<EventDispatchRemediationResult>();
        var deadLetterState = await client.GetFromJsonAsync<EventDispatchRuntimeState>("/engine/event-dispatches/entity-framework-outbox");
        var deadLetterCommandState = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState>("/engine/event-dispatch-remediation-commands/cmd-command-001-dead-letter");
        var latestCommandStateAfterDeadLetter = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState>("/engine/event-dispatch-remediation-commands/latest");
        var commandStatesByMessageAfterDeadLetter = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState[]>("/engine/event-dispatch-remediation-commands/messages/evt-command-001");
        var commandStatesByChannelAfterDeadLetter = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState[]>("/engine/event-dispatch-remediation-commands/channels/catalog-events");
        var commandStatesByDeadLetterOperation = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState[]>("/engine/event-dispatch-remediation-commands/operations/dead-letter");
        var commandStatesByOperatorAfterDeadLetter = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState[]>("/engine/event-dispatch-remediation-commands/actors/operator-001");
        var commandStatesByDeadLetterCorrelation = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState[]>("/engine/event-dispatch-remediation-commands/correlations/corr-command-operator-dead-letter");
        var commandStatesByDeadLetterReason = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState[]>($"/engine/event-dispatch-remediation-commands/reasons/{Uri.EscapeDataString(deadLetterReason)}");
        var failedCommandStatesAfterDeadLetter = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState[]>("/engine/event-dispatch-remediation-commands/dispatch-outcomes/failed");
        pending = await readStore.ReadPendingAsync(10);

        Assert.NotNull(deadLetterResult);
        Assert.Equal(EventDispatchRemediationOutcomes.Accepted, deadLetterResult.Outcome);
        Assert.Equal(EventDispatchExecutionOutcomes.Failed, deadLetterResult.DispatchOutcome);
        Assert.Equal("dead-letter", deadLetterResult.OperationId);
        Assert.Equal("operator-dispatch-store-dead-letter", deadLetterResult.Metadata[EventDispatchRuntimeMetadataKeys.DeadLetterOutcome]);
        Assert.Equal("dispatch-store", deadLetterResult.Metadata[EventDispatchRuntimeMetadataKeys.DeadLetterScope]);
        Assert.Equal("dispatch-store", deadLetterResult.Metadata[EventDispatchRuntimeMetadataKeys.DeadLetterDurability]);
        Assert.Equal("false", deadLetterResult.Metadata[EventDispatchRuntimeMetadataKeys.BrokerDeadLetter]);
        Assert.NotNull(deadLetterCommandState);
        Assert.Equal("cmd-command-001-dead-letter", deadLetterCommandState.CommandId);
        Assert.Equal(EventDispatchRemediationOutcomes.Accepted, deadLetterCommandState.Outcome);
        Assert.Equal(EventDispatchExecutionOutcomes.Failed, deadLetterCommandState.DispatchOutcome);
        Assert.Equal("dead-letter", deadLetterCommandState.OperationId);
        Assert.Equal("corr-command-operator-dead-letter", deadLetterCommandState.Metadata[EventDispatchRemediationMetadataKeys.OperatorCorrelationId]);
        Assert.Equal(deadLetterReason, deadLetterCommandState.Metadata[EventDispatchRemediationMetadataKeys.OperatorCommandReason]);
        Assert.NotNull(latestCommandStateAfterDeadLetter);
        Assert.Equal("cmd-command-001-dead-letter", latestCommandStateAfterDeadLetter.CommandId);
        Assert.Equal(EventDispatchRemediationOutcomes.Accepted, latestCommandStateAfterDeadLetter.Outcome);
        Assert.Equal("dead-letter", latestCommandStateAfterDeadLetter.OperationId);
        Assert.NotNull(commandStatesByMessageAfterDeadLetter);
        Assert.Equal(
            ["cmd-command-001-dead-letter", "cmd-command-001-retry"],
            commandStatesByMessageAfterDeadLetter.Select(state => state.CommandId).ToArray());
        Assert.NotNull(commandStatesByChannelAfterDeadLetter);
        Assert.Equal(
            ["cmd-command-001-dead-letter", "cmd-command-001-retry"],
            commandStatesByChannelAfterDeadLetter.Select(state => state.CommandId).ToArray());
        Assert.NotNull(commandStatesByDeadLetterOperation);
        Assert.Equal("cmd-command-001-dead-letter", Assert.Single(commandStatesByDeadLetterOperation).CommandId);
        Assert.NotNull(commandStatesByOperatorAfterDeadLetter);
        Assert.Equal(
            ["cmd-command-001-dead-letter", "cmd-command-001-retry"],
            commandStatesByOperatorAfterDeadLetter.Select(state => state.CommandId).ToArray());
        Assert.NotNull(commandStatesByDeadLetterCorrelation);
        Assert.Equal("cmd-command-001-dead-letter", Assert.Single(commandStatesByDeadLetterCorrelation).CommandId);
        Assert.NotNull(commandStatesByDeadLetterReason);
        Assert.Equal("cmd-command-001-dead-letter", Assert.Single(commandStatesByDeadLetterReason).CommandId);
        Assert.NotNull(failedCommandStatesAfterDeadLetter);
        Assert.Equal("cmd-command-001-dead-letter", Assert.Single(failedCommandStatesAfterDeadLetter).CommandId);
        Assert.NotNull(deadLetterState);
        Assert.Equal(EventDispatchExecutionOutcomes.Failed, deadLetterState.LastOutcome);
        Assert.True(deadLetterState.TerminalFailure);
        Assert.False(deadLetterState.RetryPending);
        Assert.Equal("dead-letter", deadLetterState.Metadata["operatorCommand"]);
        Assert.Equal("operator-dead-letter", deadLetterState.Metadata[EventDispatchRuntimeMetadataKeys.RetryOutcome]);
        Assert.Equal("operator-dispatch-store-dead-letter", deadLetterState.Metadata[EventDispatchRuntimeMetadataKeys.DeadLetterOutcome]);
        Assert.Equal("dispatch-store", deadLetterState.Metadata[EventDispatchRuntimeMetadataKeys.DeadLetterScope]);
        Assert.Equal("dispatch-store", deadLetterState.Metadata[EventDispatchRuntimeMetadataKeys.DeadLetterDurability]);
        Assert.Equal("false", deadLetterState.Metadata[EventDispatchRuntimeMetadataKeys.BrokerDeadLetter]);
        Assert.Empty(pending);

        var rejectedReason = "Operator asked to delay but did not pick a timestamp.";
        var rejectedResponse = await client.PostAsJsonAsync(
            "/engine/event-dispatches/entity-framework-outbox/commands/retry-later",
            new
            {
                commandId = "cmd-command-001-retry-later-rejected",
                messageId = "evt-command-001",
                channelId = "catalog-events",
                reason = rejectedReason,
                actorId = "operator-001",
                correlationId = "corr-command-operator-002"
            });
        Assert.Equal(HttpStatusCode.Conflict, rejectedResponse.StatusCode);
        var rejectedResult = await rejectedResponse.Content.ReadFromJsonAsync<EventDispatchRemediationResult>();
        var rejectedCommandState = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState>("/engine/event-dispatch-remediation-commands/cmd-command-001-retry-later-rejected");
        var rejectedCommandStates = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState[]>("/engine/event-dispatch-remediation-commands/outcomes/rejected");
        var finalCommandStatesByMessage = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState[]>("/engine/event-dispatch-remediation-commands/messages/evt-command-001");
        var finalCommandStatesByChannel = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState[]>("/engine/event-dispatch-remediation-commands/channels/catalog-events");
        var rejectedCommandStatesByOperation = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState[]>("/engine/event-dispatch-remediation-commands/operations/retry-later");
        var finalCommandStatesByOperator = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState[]>("/engine/event-dispatch-remediation-commands/actors/operator-001");
        var rejectedCommandStatesByCorrelation = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState[]>("/engine/event-dispatch-remediation-commands/correlations/corr-command-operator-002");
        var rejectedCommandStatesByReason = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState[]>($"/engine/event-dispatch-remediation-commands/reasons/{Uri.EscapeDataString(rejectedReason)}");
        var finalRetryScheduledCommandStates = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState[]>("/engine/event-dispatch-remediation-commands/dispatch-outcomes/retry-scheduled");
        var finalCommandSummaryByOutbox = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeSummary>("/engine/event-dispatch-remediation-commands/outboxes/entity-framework-outbox/summary");
        var finalCommandSummaryByMessage = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeSummary>("/engine/event-dispatch-remediation-commands/messages/evt-command-001/summary");
        var finalCommandSummaryByChannel = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeSummary>("/engine/event-dispatch-remediation-commands/channels/catalog-events/summary");
        var rejectedCommandSummaryByOperation = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeSummary>("/engine/event-dispatch-remediation-commands/operations/retry-later/summary");
        var finalCommandSummaryByOperator = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeSummary>("/engine/event-dispatch-remediation-commands/actors/operator-001/summary");
        var rejectedCommandSummaryByCorrelation = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeSummary>("/engine/event-dispatch-remediation-commands/correlations/corr-command-operator-002/summary");
        var rejectedCommandSummaryByReason = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeSummary>($"/engine/event-dispatch-remediation-commands/reasons/{Uri.EscapeDataString(rejectedReason)}/summary");
        var finalRetryScheduledCommandSummary = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeSummary>("/engine/event-dispatch-remediation-commands/dispatch-outcomes/retry-scheduled/summary");
        var rejectedOutcomeCommandSummary = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeSummary>("/engine/event-dispatch-remediation-commands/outcomes/rejected/summary");
        var finalCommandLatestByOutbox = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState>("/engine/event-dispatch-remediation-commands/outboxes/entity-framework-outbox/latest");
        var finalCommandLatestByMessage = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState>("/engine/event-dispatch-remediation-commands/messages/evt-command-001/latest");
        var finalCommandLatestByChannel = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState>("/engine/event-dispatch-remediation-commands/channels/catalog-events/latest");
        var rejectedCommandLatestByOperation = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState>("/engine/event-dispatch-remediation-commands/operations/retry-later/latest");
        var finalCommandLatestByOperator = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState>("/engine/event-dispatch-remediation-commands/actors/operator-001/latest");
        var rejectedCommandLatestByCorrelation = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState>("/engine/event-dispatch-remediation-commands/correlations/corr-command-operator-002/latest");
        var rejectedCommandLatestByReason = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState>($"/engine/event-dispatch-remediation-commands/reasons/{Uri.EscapeDataString(rejectedReason)}/latest");
        var finalRetryScheduledCommandLatest = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState>("/engine/event-dispatch-remediation-commands/dispatch-outcomes/retry-scheduled/latest");
        var rejectedOutcomeCommandLatest = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState>("/engine/event-dispatch-remediation-commands/outcomes/rejected/latest");
        var finalCommandOldestByOutbox = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState>("/engine/event-dispatch-remediation-commands/outboxes/entity-framework-outbox/oldest");
        var finalCommandOldestByMessage = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState>("/engine/event-dispatch-remediation-commands/messages/evt-command-001/oldest");
        var finalCommandOldestByChannel = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState>("/engine/event-dispatch-remediation-commands/channels/catalog-events/oldest");
        var rejectedCommandOldestByOperation = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState>("/engine/event-dispatch-remediation-commands/operations/retry-later/oldest");
        var finalCommandOldestByOperator = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState>("/engine/event-dispatch-remediation-commands/actors/operator-001/oldest");
        var rejectedCommandOldestByCorrelation = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState>("/engine/event-dispatch-remediation-commands/correlations/corr-command-operator-002/oldest");
        var rejectedCommandOldestByReason = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState>($"/engine/event-dispatch-remediation-commands/reasons/{Uri.EscapeDataString(rejectedReason)}/oldest");
        var finalRetryScheduledCommandOldest = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState>("/engine/event-dispatch-remediation-commands/dispatch-outcomes/retry-scheduled/oldest");
        var rejectedOutcomeCommandOldest = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState>("/engine/event-dispatch-remediation-commands/outcomes/rejected/oldest");
        var finalCommandRetentionByOutbox = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeRetention>("/engine/event-dispatch-remediation-commands/outboxes/entity-framework-outbox/retention");
        var finalCommandRetentionByMessage = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeRetention>("/engine/event-dispatch-remediation-commands/messages/evt-command-001/retention");
        var finalCommandRetentionByChannel = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeRetention>("/engine/event-dispatch-remediation-commands/channels/catalog-events/retention");
        var rejectedCommandRetentionByOperation = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeRetention>("/engine/event-dispatch-remediation-commands/operations/retry-later/retention");
        var finalCommandRetentionByOperator = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeRetention>("/engine/event-dispatch-remediation-commands/actors/operator-001/retention");
        var rejectedCommandRetentionByCorrelation = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeRetention>("/engine/event-dispatch-remediation-commands/correlations/corr-command-operator-002/retention");
        var rejectedCommandRetentionByReason = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeRetention>($"/engine/event-dispatch-remediation-commands/reasons/{Uri.EscapeDataString(rejectedReason)}/retention");
        var finalRetryScheduledCommandRetention = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeRetention>("/engine/event-dispatch-remediation-commands/dispatch-outcomes/retry-scheduled/retention");
        var rejectedOutcomeCommandRetention = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeRetention>("/engine/event-dispatch-remediation-commands/outcomes/rejected/retention");
        var allCommandStates = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState[]>("/engine/event-dispatch-remediation-commands");
        var limitedAllCommandStates = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState[]>("/engine/event-dispatch-remediation-commands?limit=2");
        var limitedFinalCommandStatesByMessage = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState[]>("/engine/event-dispatch-remediation-commands/messages/evt-command-001?limit=2");
        var limitedFinalCommandStatesByOperator = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState[]>("/engine/event-dispatch-remediation-commands/actors/operator-001?limit=1");
        var firstCommandPage = await client.GetFromJsonAsync<EventDispatchRemediationCommandPage>("/engine/event-dispatch-remediation-commands?pageSize=2");
        Assert.NotNull(firstCommandPage);
        var secondCommandPage = await client.GetFromJsonAsync<EventDispatchRemediationCommandPage>(
            $"/engine/event-dispatch-remediation-commands?pageSize=2&continuationToken={Uri.EscapeDataString(firstCommandPage.NextContinuationToken!)}");
        var tamperedCommandContinuationTokenResponse = await client.GetAsync(
            $"/engine/event-dispatch-remediation-commands?pageSize=2&continuationToken={Uri.EscapeDataString(TamperContinuationToken(firstCommandPage.NextContinuationToken!))}");
        var mismatchedCommandContinuationTokenResponse = await client.GetAsync(
            $"/engine/event-dispatch-remediation-commands/messages/evt-command-001?pageSize=1&continuationToken={Uri.EscapeDataString(firstCommandPage.NextContinuationToken!)}");
        var firstMessageCommandPage = await client.GetFromJsonAsync<EventDispatchRemediationCommandPage>("/engine/event-dispatch-remediation-commands/messages/evt-command-001?pageSize=1");
        var invalidCommandContinuationTokenResponse = await client.GetAsync("/engine/event-dispatch-remediation-commands?pageSize=2&continuationToken=not-a-token");
        var ambiguousCommandPagingResponse = await client.GetAsync("/engine/event-dispatch-remediation-commands?limit=1&pageSize=1");
        var finalCommandSummary = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeSummary>("/engine/event-dispatch-remediation-commands/summary");
        var finalLatestCommandState = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState>("/engine/event-dispatch-remediation-commands/latest");
        var finalCommandRetention = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeRetention>("/engine/event-dispatch-remediation-commands/retention");

        Assert.NotNull(rejectedResult);
        Assert.Equal(EventDispatchRemediationOutcomes.Rejected, rejectedResult.Outcome);
        Assert.NotNull(rejectedCommandState);
        Assert.Equal("cmd-command-001-retry-later-rejected", rejectedCommandState.CommandId);
        Assert.Equal(EventDispatchRemediationOutcomes.Rejected, rejectedCommandState.Outcome);
        Assert.Equal("corr-command-operator-002", rejectedCommandState.Metadata[EventDispatchRemediationMetadataKeys.OperatorCorrelationId]);
        Assert.Equal(rejectedReason, rejectedCommandState.Metadata[EventDispatchRemediationMetadataKeys.OperatorCommandReason]);
        Assert.Contains("next attempt", rejectedCommandState.Error, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(deadLetterCommandState);
        var finalCommandStatesByObservationWindow = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState[]>(
            BuildObservationWindowRoute(deadLetterCommandState.ObservedAtUtc, rejectedCommandState.ObservedAtUtc));
        var limitedFinalCommandStatesByObservationWindow = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState[]>(
            BuildObservationWindowRoute(deadLetterCommandState.ObservedAtUtc, rejectedCommandState.ObservedAtUtc, limit: 1));
        var firstObservationCommandPage = await client.GetFromJsonAsync<EventDispatchRemediationCommandPage>(
            BuildObservationWindowRoute(deadLetterCommandState.ObservedAtUtc, rejectedCommandState.ObservedAtUtc, pageSize: 1));
        Assert.NotNull(firstObservationCommandPage);
        var mismatchedObservationCommandContinuationTokenResponse = await client.GetAsync(
            BuildObservationWindowRoute(deadLetterCommandState.ObservedAtUtc, deadLetterCommandState.ObservedAtUtc, pageSize: 1, continuationToken: firstObservationCommandPage.NextContinuationToken));
        var finalCommandObservationSummary = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeSummary>(
            BuildObservationWindowSummaryRoute(deadLetterCommandState.ObservedAtUtc, rejectedCommandState.ObservedAtUtc));
        Assert.NotNull(finalCommandStatesByObservationWindow);
        Assert.Equal(
            ["cmd-command-001-retry-later-rejected", "cmd-command-001-dead-letter"],
            finalCommandStatesByObservationWindow.Select(state => state.CommandId).ToArray());
        Assert.NotNull(limitedFinalCommandStatesByObservationWindow);
        Assert.Equal("cmd-command-001-retry-later-rejected", Assert.Single(limitedFinalCommandStatesByObservationWindow).CommandId);
        Assert.Equal(1, firstObservationCommandPage.PageSize);
        Assert.Equal(1, firstObservationCommandPage.ReturnedCount);
        Assert.Equal(2, firstObservationCommandPage.TotalRetainedCount);
        Assert.True(firstObservationCommandPage.HasMore);
        Assert.NotNull(firstObservationCommandPage.NextContinuationToken);
        Assert.Equal("cmd-command-001-retry-later-rejected", Assert.Single(firstObservationCommandPage.Items).CommandId);
        Assert.Equal(HttpStatusCode.BadRequest, mismatchedObservationCommandContinuationTokenResponse.StatusCode);
        Assert.NotNull(finalCommandObservationSummary);
        Assert.Equal(2, finalCommandObservationSummary.TotalCommandCount);
        Assert.Equal(1, finalCommandObservationSummary.AcceptedCount);
        Assert.Equal(1, finalCommandObservationSummary.RejectedCount);
        Assert.Equal(1, finalCommandObservationSummary.ErrorCount);
        Assert.Equal(0, finalCommandObservationSummary.ReservedCount);
        Assert.Equal("cmd-command-001-retry-later-rejected", finalCommandObservationSummary.LastCommandId);
        Assert.Equal(0, finalCommandObservationSummary.DroppedCommandCount);
        Assert.False(finalCommandObservationSummary.RetentionTruncated);
        Assert.False(finalCommandObservationSummary.SummaryMayBeIncomplete);
        Assert.Null(finalCommandObservationSummary.OldestReservedCommandId);
        Assert.Null(finalCommandObservationSummary.OldestReservedObservedAtUtc);
        Assert.True(finalCommandObservationSummary.HasCommands);
        Assert.True(finalCommandObservationSummary.HasFailures);
        Assert.False(finalCommandObservationSummary.HasInDoubtCommands);
        Assert.NotNull(finalLatestCommandState);
        Assert.Equal("cmd-command-001-retry-later-rejected", finalLatestCommandState.CommandId);
        Assert.Equal(EventDispatchRemediationOutcomes.Rejected, finalLatestCommandState.Outcome);
        Assert.Equal("retry-later", finalLatestCommandState.OperationId);
        Assert.Contains("next attempt", finalLatestCommandState.Error, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(rejectedCommandStates);
        Assert.Equal("cmd-command-001-retry-later-rejected", Assert.Single(rejectedCommandStates).CommandId);
        Assert.NotNull(finalCommandStatesByMessage);
        Assert.Equal(
            ["cmd-command-001-retry-later-rejected", "cmd-command-001-dead-letter", "cmd-command-001-retry"],
            finalCommandStatesByMessage.Select(state => state.CommandId).ToArray());
        Assert.NotNull(finalCommandStatesByChannel);
        Assert.Equal(
            ["cmd-command-001-retry-later-rejected", "cmd-command-001-dead-letter", "cmd-command-001-retry"],
            finalCommandStatesByChannel.Select(state => state.CommandId).ToArray());
        Assert.NotNull(rejectedCommandStatesByOperation);
        Assert.Equal("cmd-command-001-retry-later-rejected", Assert.Single(rejectedCommandStatesByOperation).CommandId);
        Assert.NotNull(finalCommandStatesByOperator);
        Assert.Equal(
            ["cmd-command-001-retry-later-rejected", "cmd-command-001-dead-letter", "cmd-command-001-retry"],
            finalCommandStatesByOperator.Select(state => state.CommandId).ToArray());
        Assert.NotNull(rejectedCommandStatesByCorrelation);
        Assert.Equal("cmd-command-001-retry-later-rejected", Assert.Single(rejectedCommandStatesByCorrelation).CommandId);
        Assert.NotNull(rejectedCommandStatesByReason);
        Assert.Equal("cmd-command-001-retry-later-rejected", Assert.Single(rejectedCommandStatesByReason).CommandId);
        Assert.NotNull(finalRetryScheduledCommandStates);
        Assert.Equal(
            ["cmd-command-001-retry-later-rejected", "cmd-command-001-retry"],
            finalRetryScheduledCommandStates.Select(state => state.CommandId).ToArray());
        Assert.NotNull(finalCommandSummaryByOutbox);
        Assert.Equal(3, finalCommandSummaryByOutbox.TotalCommandCount);
        Assert.Equal(2, finalCommandSummaryByOutbox.AcceptedCount);
        Assert.Equal(1, finalCommandSummaryByOutbox.RejectedCount);
        Assert.Equal("cmd-command-001-retry-later-rejected", finalCommandSummaryByOutbox.LastCommandId);
        Assert.NotNull(finalCommandSummaryByMessage);
        Assert.Equal(3, finalCommandSummaryByMessage.TotalCommandCount);
        Assert.Equal(1, finalCommandSummaryByMessage.ErrorCount);
        Assert.True(finalCommandSummaryByMessage.HasFailures);
        Assert.NotNull(finalCommandSummaryByChannel);
        Assert.Equal(3, finalCommandSummaryByChannel.TotalCommandCount);
        Assert.NotNull(rejectedCommandSummaryByOperation);
        Assert.Equal(1, rejectedCommandSummaryByOperation.TotalCommandCount);
        Assert.Equal(1, rejectedCommandSummaryByOperation.RejectedCount);
        Assert.NotNull(finalCommandSummaryByOperator);
        Assert.Equal(3, finalCommandSummaryByOperator.TotalCommandCount);
        Assert.NotNull(rejectedCommandSummaryByCorrelation);
        Assert.Equal("cmd-command-001-retry-later-rejected", rejectedCommandSummaryByCorrelation.LastCommandId);
        Assert.NotNull(rejectedCommandSummaryByReason);
        Assert.Equal(1, rejectedCommandSummaryByReason.ErrorCount);
        Assert.NotNull(finalRetryScheduledCommandSummary);
        Assert.Equal(2, finalRetryScheduledCommandSummary.TotalCommandCount);
        Assert.Equal(1, finalRetryScheduledCommandSummary.AcceptedCount);
        Assert.Equal(1, finalRetryScheduledCommandSummary.RejectedCount);
        Assert.NotNull(rejectedOutcomeCommandSummary);
        Assert.Equal(1, rejectedOutcomeCommandSummary.TotalCommandCount);
        Assert.True(rejectedOutcomeCommandSummary.HasFailures);
        Assert.Equal("cmd-command-001-retry-later-rejected", finalCommandLatestByOutbox?.CommandId);
        Assert.Equal("cmd-command-001-retry-later-rejected", finalCommandLatestByMessage?.CommandId);
        Assert.Equal("cmd-command-001-retry-later-rejected", finalCommandLatestByChannel?.CommandId);
        Assert.Equal("cmd-command-001-retry-later-rejected", rejectedCommandLatestByOperation?.CommandId);
        Assert.Equal("cmd-command-001-retry-later-rejected", finalCommandLatestByOperator?.CommandId);
        Assert.Equal("cmd-command-001-retry-later-rejected", rejectedCommandLatestByCorrelation?.CommandId);
        Assert.Equal("cmd-command-001-retry-later-rejected", rejectedCommandLatestByReason?.CommandId);
        Assert.Equal("cmd-command-001-retry-later-rejected", finalRetryScheduledCommandLatest?.CommandId);
        Assert.Equal("cmd-command-001-retry-later-rejected", rejectedOutcomeCommandLatest?.CommandId);
        Assert.Equal("cmd-command-001-retry", finalCommandOldestByOutbox?.CommandId);
        Assert.Equal("cmd-command-001-retry", finalCommandOldestByMessage?.CommandId);
        Assert.Equal("cmd-command-001-retry", finalCommandOldestByChannel?.CommandId);
        Assert.Equal("cmd-command-001-retry-later-rejected", rejectedCommandOldestByOperation?.CommandId);
        Assert.Equal("cmd-command-001-retry", finalCommandOldestByOperator?.CommandId);
        Assert.Equal("cmd-command-001-retry-later-rejected", rejectedCommandOldestByCorrelation?.CommandId);
        Assert.Equal("cmd-command-001-retry-later-rejected", rejectedCommandOldestByReason?.CommandId);
        Assert.Equal("cmd-command-001-retry", finalRetryScheduledCommandOldest?.CommandId);
        Assert.Equal("cmd-command-001-retry-later-rejected", rejectedOutcomeCommandOldest?.CommandId);
        AssertRetention(finalCommandRetentionByOutbox, 3, "cmd-command-001-retry", "cmd-command-001-retry-later-rejected");
        AssertRetention(finalCommandRetentionByMessage, 3, "cmd-command-001-retry", "cmd-command-001-retry-later-rejected");
        AssertRetention(finalCommandRetentionByChannel, 3, "cmd-command-001-retry", "cmd-command-001-retry-later-rejected");
        AssertRetention(rejectedCommandRetentionByOperation, 1, "cmd-command-001-retry-later-rejected", "cmd-command-001-retry-later-rejected");
        AssertRetention(finalCommandRetentionByOperator, 3, "cmd-command-001-retry", "cmd-command-001-retry-later-rejected");
        AssertRetention(rejectedCommandRetentionByCorrelation, 1, "cmd-command-001-retry-later-rejected", "cmd-command-001-retry-later-rejected");
        AssertRetention(rejectedCommandRetentionByReason, 1, "cmd-command-001-retry-later-rejected", "cmd-command-001-retry-later-rejected");
        AssertRetention(finalRetryScheduledCommandRetention, 2, "cmd-command-001-retry", "cmd-command-001-retry-later-rejected");
        AssertRetention(rejectedOutcomeCommandRetention, 1, "cmd-command-001-retry-later-rejected", "cmd-command-001-retry-later-rejected");
        Assert.NotNull(allCommandStates);
        Assert.Equal(3, allCommandStates.Length);
        Assert.NotNull(limitedAllCommandStates);
        Assert.Equal(
            ["cmd-command-001-retry-later-rejected", "cmd-command-001-dead-letter"],
            limitedAllCommandStates.Select(state => state.CommandId).ToArray());
        Assert.NotNull(limitedFinalCommandStatesByMessage);
        Assert.Equal(
            ["cmd-command-001-retry-later-rejected", "cmd-command-001-dead-letter"],
            limitedFinalCommandStatesByMessage.Select(state => state.CommandId).ToArray());
        Assert.NotNull(limitedFinalCommandStatesByOperator);
        Assert.Equal("cmd-command-001-retry-later-rejected", Assert.Single(limitedFinalCommandStatesByOperator).CommandId);
        Assert.Equal(2, firstCommandPage.PageSize);
        Assert.Equal(2, firstCommandPage.ReturnedCount);
        Assert.Equal(3, firstCommandPage.TotalRetainedCount);
        Assert.True(firstCommandPage.HasMore);
        Assert.NotNull(firstCommandPage.NextContinuationToken);
        Assert.Equal(
            ["cmd-command-001-retry-later-rejected", "cmd-command-001-dead-letter"],
            firstCommandPage.Items.Select(state => state.CommandId).ToArray());
        Assert.NotNull(secondCommandPage);
        Assert.Equal(firstCommandPage.NextContinuationToken, secondCommandPage.ContinuationToken);
        Assert.Equal(1, secondCommandPage.ReturnedCount);
        Assert.Equal(3, secondCommandPage.TotalRetainedCount);
        Assert.False(secondCommandPage.HasMore);
        Assert.Null(secondCommandPage.NextContinuationToken);
        Assert.Equal("cmd-command-001-retry", Assert.Single(secondCommandPage.Items).CommandId);
        Assert.Equal(HttpStatusCode.BadRequest, tamperedCommandContinuationTokenResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, mismatchedCommandContinuationTokenResponse.StatusCode);
        Assert.NotNull(firstMessageCommandPage);
        Assert.Equal(1, firstMessageCommandPage.PageSize);
        Assert.Equal(1, firstMessageCommandPage.ReturnedCount);
        Assert.True(firstMessageCommandPage.HasMore);
        Assert.Equal("cmd-command-001-retry-later-rejected", Assert.Single(firstMessageCommandPage.Items).CommandId);
        Assert.Equal(HttpStatusCode.BadRequest, invalidCommandContinuationTokenResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, ambiguousCommandPagingResponse.StatusCode);
        Assert.NotNull(finalCommandSummary);
        Assert.Equal(3, finalCommandSummary.TotalCommandCount);
        Assert.Equal(2, finalCommandSummary.AcceptedCount);
        Assert.Equal(1, finalCommandSummary.RejectedCount);
        Assert.Equal(1, finalCommandSummary.ErrorCount);
        Assert.Equal(0, finalCommandSummary.DuplicateCommandCount);
        Assert.Equal(0, finalCommandSummary.ReservedCount);
        Assert.Equal("cmd-command-001-retry-later-rejected", finalCommandSummary.LastCommandId);
        Assert.Equal("retry-later", finalCommandSummary.LastOperationId);
        Assert.Equal(EventDispatchRemediationOutcomes.Rejected, finalCommandSummary.LastOutcome);
        Assert.Equal(EventDispatchExecutionOutcomes.RetryScheduled, finalCommandSummary.LastDispatchOutcome);
        Assert.Equal(0, finalCommandSummary.DroppedCommandCount);
        Assert.False(finalCommandSummary.RetentionTruncated);
        Assert.False(finalCommandSummary.SummaryMayBeIncomplete);
        Assert.Equal("cmd-command-001-retry", finalCommandSummary.OldestRetainedCommandId);
        Assert.Null(finalCommandSummary.OldestReservedCommandId);
        Assert.Null(finalCommandSummary.OldestReservedObservedAtUtc);
        Assert.True(finalCommandSummary.HasCommands);
        Assert.True(finalCommandSummary.HasFailures);
        Assert.False(finalCommandSummary.HasInDoubtCommands);
        Assert.NotNull(finalCommandRetention);
        Assert.Equal(0, finalCommandRetention.HistoryLimit);
        Assert.Equal(3, finalCommandRetention.RetainedCommandCount);
        Assert.Equal(3, finalCommandRetention.TotalRecordedCommandCount);
        Assert.Equal(0, finalCommandRetention.DroppedCommandCount);
        Assert.False(finalCommandRetention.Truncated);
        Assert.Equal("cmd-command-001-retry", finalCommandRetention.OldestRetainedCommandId);
        Assert.Equal("cmd-command-001-retry-later-rejected", finalCommandRetention.LatestRetainedCommandId);

        await using var inDoubtScope = app.Services.CreateAsyncScope();
        var commandJournal = inDoubtScope.ServiceProvider.GetRequiredService<IEventDispatchRemediationCommandJournal>();
        var inDoubtReservation = await commandJournal.ReserveAsync(new EventDispatchRemediationRequest(
            outboxId: "entity-framework-outbox-in-doubt",
            messageId: "evt-command-002",
            channelId: "catalog-events",
            operationId: EventDispatchRemediationOperationIds.Skip,
            commandId: "cmd-command-002-reserved",
            requestedAtUtc: new DateTimeOffset(2026, 04, 12, 08, 30, 0, TimeSpan.Zero),
            reason: "Operator command reserved before process exit.",
            actorId: "operator-003",
            correlationId: "corr-command-operator-in-doubt"));
        var inDoubtCutoff = new DateTimeOffset(2026, 04, 12, 08, 30, 0, TimeSpan.Zero);
        var beforeInDoubtCutoff = inDoubtCutoff.AddTicks(-1);
        var inDoubtCommandStatesAfterReservation = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState[]>("/engine/event-dispatch-remediation-commands/in-doubt");
        var limitedInDoubtCommandStates = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState[]>("/engine/event-dispatch-remediation-commands/in-doubt?limit=1");
        var pagedInDoubtCommandStates = await client.GetFromJsonAsync<EventDispatchRemediationCommandPage>("/engine/event-dispatch-remediation-commands/in-doubt?pageSize=1");
        var cutoffInDoubtCommandStates = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState[]>(BuildInDoubtRoute(inDoubtCutoff));
        var emptyCutoffInDoubtCommandStates = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState[]>(BuildInDoubtRoute(beforeInDoubtCutoff));
        var pagedCutoffInDoubtCommandStates = await client.GetFromJsonAsync<EventDispatchRemediationCommandPage>(BuildInDoubtRoute(inDoubtCutoff, pageSize: 1));
        var invalidInDoubtCutoffResponse = await client.GetAsync("/engine/event-dispatch-remediation-commands/in-doubt?beforeUtc=not-a-date");
        var inDoubtSummaryOnly = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeSummary>("/engine/event-dispatch-remediation-commands/in-doubt/summary");
        var cutoffInDoubtSummaryOnly = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeSummary>(BuildInDoubtSummaryRoute(inDoubtCutoff));
        var emptyCutoffInDoubtSummaryOnly = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeSummary>(BuildInDoubtSummaryRoute(beforeInDoubtCutoff));
        var invalidInDoubtSummaryCutoffResponse = await client.GetAsync("/engine/event-dispatch-remediation-commands/in-doubt/summary?beforeUtc=not-a-date");
        var oldestInDoubtCommandState = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState>("/engine/event-dispatch-remediation-commands/in-doubt/oldest");
        var cutoffOldestInDoubtCommandState = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState>(BuildOldestInDoubtRoute(inDoubtCutoff));
        var missingOldestInDoubtResponse = await client.GetAsync(BuildOldestInDoubtRoute(beforeInDoubtCutoff));
        var invalidOldestInDoubtCutoffResponse = await client.GetAsync("/engine/event-dispatch-remediation-commands/in-doubt/oldest?beforeUtc=not-a-date");
        var reservedOutcomeCommandStates = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState[]>("/engine/event-dispatch-remediation-commands/outcomes/reserved");
        var inDoubtCommandStateById = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeState>("/engine/event-dispatch-remediation-commands/cmd-command-002-reserved");
        var inDoubtSummary = await client.GetFromJsonAsync<EventDispatchRemediationRuntimeSummary>("/engine/event-dispatch-remediation-commands/summary");

        Assert.True(inDoubtReservation.Reserved);
        Assert.Null(inDoubtReservation.ExistingCommand);
        Assert.NotNull(inDoubtCommandStatesAfterReservation);
        var inDoubtCommandState = Assert.Single(inDoubtCommandStatesAfterReservation);
        Assert.Equal("cmd-command-002-reserved", inDoubtCommandState.CommandId);
        Assert.Equal(EventDispatchRemediationOutcomes.Reserved, inDoubtCommandState.Outcome);
        Assert.Equal("pending", inDoubtCommandState.DispatchOutcome);
        Assert.Equal("reserved", inDoubtCommandState.Metadata[EventDispatchRemediationMetadataKeys.CommandReservationState]);
        Assert.Equal("reserve-before-mutation", inDoubtCommandState.Metadata[EventDispatchRemediationMetadataKeys.CommandReservationPolicy]);
        Assert.NotNull(limitedInDoubtCommandStates);
        Assert.Equal("cmd-command-002-reserved", Assert.Single(limitedInDoubtCommandStates).CommandId);
        Assert.NotNull(pagedInDoubtCommandStates);
        Assert.Equal(1, pagedInDoubtCommandStates.PageSize);
        Assert.Equal(1, pagedInDoubtCommandStates.ReturnedCount);
        Assert.Equal(1, pagedInDoubtCommandStates.TotalRetainedCount);
        Assert.False(pagedInDoubtCommandStates.HasMore);
        Assert.Null(pagedInDoubtCommandStates.NextContinuationToken);
        Assert.Equal("cmd-command-002-reserved", Assert.Single(pagedInDoubtCommandStates.Items).CommandId);
        Assert.NotNull(cutoffInDoubtCommandStates);
        Assert.Equal("cmd-command-002-reserved", Assert.Single(cutoffInDoubtCommandStates).CommandId);
        Assert.NotNull(emptyCutoffInDoubtCommandStates);
        Assert.Empty(emptyCutoffInDoubtCommandStates);
        Assert.NotNull(pagedCutoffInDoubtCommandStates);
        Assert.Equal(1, pagedCutoffInDoubtCommandStates.PageSize);
        Assert.Equal(1, pagedCutoffInDoubtCommandStates.TotalRetainedCount);
        Assert.Equal("cmd-command-002-reserved", Assert.Single(pagedCutoffInDoubtCommandStates.Items).CommandId);
        Assert.Equal(HttpStatusCode.BadRequest, invalidInDoubtCutoffResponse.StatusCode);
        Assert.NotNull(inDoubtSummaryOnly);
        Assert.Equal(1, inDoubtSummaryOnly.TotalCommandCount);
        Assert.Equal(1, inDoubtSummaryOnly.ReservedCount);
        Assert.True(inDoubtSummaryOnly.HasInDoubtCommands);
        Assert.Equal("cmd-command-002-reserved", inDoubtSummaryOnly.LastCommandId);
        Assert.Equal("cmd-command-002-reserved", inDoubtSummaryOnly.OldestReservedCommandId);
        Assert.Equal(new DateTimeOffset(2026, 04, 12, 08, 30, 0, TimeSpan.Zero), inDoubtSummaryOnly.OldestReservedObservedAtUtc);
        Assert.NotNull(cutoffInDoubtSummaryOnly);
        Assert.Equal(1, cutoffInDoubtSummaryOnly.TotalCommandCount);
        Assert.Equal("cmd-command-002-reserved", cutoffInDoubtSummaryOnly.OldestReservedCommandId);
        Assert.NotNull(emptyCutoffInDoubtSummaryOnly);
        Assert.Equal(0, emptyCutoffInDoubtSummaryOnly.TotalCommandCount);
        Assert.Equal(0, emptyCutoffInDoubtSummaryOnly.ReservedCount);
        Assert.False(emptyCutoffInDoubtSummaryOnly.HasInDoubtCommands);
        Assert.Equal(HttpStatusCode.BadRequest, invalidInDoubtSummaryCutoffResponse.StatusCode);
        Assert.NotNull(oldestInDoubtCommandState);
        Assert.Equal("cmd-command-002-reserved", oldestInDoubtCommandState.CommandId);
        Assert.NotNull(cutoffOldestInDoubtCommandState);
        Assert.Equal("cmd-command-002-reserved", cutoffOldestInDoubtCommandState.CommandId);
        Assert.Equal(HttpStatusCode.NotFound, missingOldestInDoubtResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalidOldestInDoubtCutoffResponse.StatusCode);
        Assert.NotNull(reservedOutcomeCommandStates);
        Assert.Equal("cmd-command-002-reserved", Assert.Single(reservedOutcomeCommandStates).CommandId);
        Assert.NotNull(inDoubtCommandStateById);
        Assert.Equal("cmd-command-002-reserved", inDoubtCommandStateById.CommandId);
        Assert.NotNull(inDoubtSummary);
        Assert.Equal(4, inDoubtSummary.TotalCommandCount);
        Assert.Equal(1, inDoubtSummary.ReservedCount);
        Assert.True(inDoubtSummary.HasInDoubtCommands);
        Assert.Equal("cmd-command-002-reserved", inDoubtSummary.OldestReservedCommandId);
        Assert.Equal(new DateTimeOffset(2026, 04, 12, 08, 30, 0, TimeSpan.Zero), inDoubtSummary.OldestReservedObservedAtUtc);

        static void AssertRetention(
            EventDispatchRemediationRuntimeRetention? retention,
            int expectedCount,
            string? expectedOldestCommandId,
            string? expectedLatestCommandId)
        {
            Assert.NotNull(retention);
            Assert.Equal(0, retention.HistoryLimit);
            Assert.Equal(expectedCount, retention.RetainedCommandCount);
            Assert.Equal(expectedCount, retention.TotalRecordedCommandCount);
            Assert.Equal(0, retention.DroppedCommandCount);
            Assert.False(retention.Truncated);
            Assert.Equal(expectedOldestCommandId, retention.OldestRetainedCommandId);
            Assert.Equal(expectedLatestCommandId, retention.LatestRetainedCommandId);
        }

        static string BuildObservationWindowRoute(
            DateTimeOffset? fromUtc,
            DateTimeOffset? toUtc,
            int? limit = null,
            int? pageSize = null,
            string? continuationToken = null)
        {
            var parameters = new List<string>(capacity: 5);
            if (fromUtc is not null)
            {
                parameters.Add($"fromUtc={Uri.EscapeDataString(fromUtc.Value.ToString("O", CultureInfo.InvariantCulture))}");
            }

            if (toUtc is not null)
            {
                parameters.Add($"toUtc={Uri.EscapeDataString(toUtc.Value.ToString("O", CultureInfo.InvariantCulture))}");
            }

            if (limit is not null)
            {
                parameters.Add($"limit={limit.Value.ToString(CultureInfo.InvariantCulture)}");
            }

            if (pageSize is not null)
            {
                parameters.Add($"pageSize={pageSize.Value.ToString(CultureInfo.InvariantCulture)}");
            }

            if (!string.IsNullOrWhiteSpace(continuationToken))
            {
                parameters.Add($"continuationToken={Uri.EscapeDataString(continuationToken)}");
            }

            return parameters.Count == 0
                ? "/engine/event-dispatch-remediation-commands/observations"
                : $"/engine/event-dispatch-remediation-commands/observations?{string.Join("&", parameters)}";
        }

        static string BuildInDoubtRoute(
            DateTimeOffset? beforeUtc,
            int? limit = null,
            int? pageSize = null,
            string? continuationToken = null)
        {
            var parameters = new List<string>(capacity: 4);
            if (beforeUtc is not null)
            {
                parameters.Add($"beforeUtc={Uri.EscapeDataString(beforeUtc.Value.ToString("O", CultureInfo.InvariantCulture))}");
            }

            if (limit is not null)
            {
                parameters.Add($"limit={limit.Value.ToString(CultureInfo.InvariantCulture)}");
            }

            if (pageSize is not null)
            {
                parameters.Add($"pageSize={pageSize.Value.ToString(CultureInfo.InvariantCulture)}");
            }

            if (!string.IsNullOrWhiteSpace(continuationToken))
            {
                parameters.Add($"continuationToken={Uri.EscapeDataString(continuationToken)}");
            }

            return parameters.Count == 0
                ? "/engine/event-dispatch-remediation-commands/in-doubt"
                : $"/engine/event-dispatch-remediation-commands/in-doubt?{string.Join("&", parameters)}";
        }

        static string BuildOldestInDoubtRoute(DateTimeOffset? beforeUtc)
        {
            return beforeUtc is null
                ? "/engine/event-dispatch-remediation-commands/in-doubt/oldest"
                : $"/engine/event-dispatch-remediation-commands/in-doubt/oldest?beforeUtc={Uri.EscapeDataString(beforeUtc.Value.ToString("O", CultureInfo.InvariantCulture))}";
        }

        static string BuildInDoubtSummaryRoute(DateTimeOffset? beforeUtc)
        {
            return beforeUtc is null
                ? "/engine/event-dispatch-remediation-commands/in-doubt/summary"
                : $"/engine/event-dispatch-remediation-commands/in-doubt/summary?beforeUtc={Uri.EscapeDataString(beforeUtc.Value.ToString("O", CultureInfo.InvariantCulture))}";
        }

        static string BuildObservationWindowSummaryRoute(DateTimeOffset? fromUtc, DateTimeOffset? toUtc)
        {
            var observationsRoute = BuildObservationWindowRoute(fromUtc, toUtc);
            return observationsRoute.Replace(
                "/engine/event-dispatch-remediation-commands/observations",
                "/engine/event-dispatch-remediation-commands/observations/summary",
                StringComparison.Ordinal);
        }

        static string TamperContinuationToken(string continuationToken)
        {
            var payload = Convert.FromBase64String(continuationToken.Replace('-', '+').Replace('_', '/').PadRight(
                continuationToken.Length + ((4 - continuationToken.Length % 4) % 4),
                '='));
            payload[^1] ^= 0x01;

            return Convert.ToBase64String(payload)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }
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
    public async Task MapCephalonKeepsSubscriptionsCodeFirstWithConfiguredChannelsWithoutWolverine()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Engine:Messaging:InProcessSubscriptions:EnableExecution"] = "true",
            ["Engine:Messaging:Channels:audit:DisplayName"] = "Configured Audit",
            ["Engine:Messaging:Channels:audit:Description"] = "Audit events declared by host configuration.",
            ["Engine:Messaging:Channels:audit:Tags:0"] = "configuration",
            ["Engine:Messaging:Channels:audit:Tags:1"] = "audit"
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

        await using (var scope = app.Services.CreateAsyncScope())
        {
            var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(new EventPublication(
                id: "audit-config-discovery-001",
                channelId: "audit",
                eventType: "audit.created",
                payload: """{"id":"audit-config-discovery-001"}""",
                occurredAtUtc: new DateTimeOffset(2026, 05, 10, 16, 0, 0, TimeSpan.Zero),
                contentType: "application/json",
                correlationId: "corr-audit-config-discovery-001",
                tenantId: "tenant-config-discovery-001"));
        }

        var client = app.GetTestClient();
        var channelCatalog = app.Services.GetRequiredService<IEventChannelCatalog>();
        var subscriptionCatalog = app.Services.GetRequiredService<IEventSubscriptionCatalog>();
        var probe = app.Services.GetRequiredService<ManagedAuditProjectorProbe>();
        var eventingSurfaces = await client.GetFromJsonAsync<TechnologyRuntimeSurface[]>("/engine/technology-surfaces/event-driven-integration");
        var capabilities = await client.GetFromJsonAsync<CapabilityManifest[]>("/engine/capabilities");

        Assert.True(channelCatalog.TryGet("audit", out var channel));
        Assert.Equal("Configured Audit", channel.DisplayName);
        Assert.Equal("Audit events declared by host configuration.", channel.Description);
        Assert.Equal(["audit", "configuration"], channel.Tags);

        Assert.True(subscriptionCatalog.TryGet("audit-projector", out var subscription));
        Assert.Equal("Audit Projector", subscription.DisplayName);
        Assert.Equal("Projects audit events into a compliance read model.", subscription.Description);
        Assert.Equal("compliance-audit-projector", subscription.HandlerId);
        Assert.Equal("background-service", subscription.DeliveryMode);
        Assert.Equal(["audit", "module"], subscription.Tags);
        Assert.False(subscription.Metadata.ContainsKey("descriptorSource"));
        Assert.False(subscription.Metadata.ContainsKey("configurationPath"));

        Assert.Equal(1, probe.TotalAttempts);
        Assert.Equal(1, probe.SuccessfulAttempts);
        Assert.Equal("audit-config-discovery-001", probe.LastMessageId);

        Assert.NotNull(eventingSurfaces);
        var channelEntry = Assert.Single(eventingSurfaces.Single(surface => surface.SurfaceId == "event-channels").Entries);
        Assert.Equal("Configured Audit", channelEntry.DisplayName);
        Assert.Equal("audit,configuration", channelEntry.Metadata["tags"]);
        var subscriptionEntry = Assert.Single(
            eventingSurfaces.Single(surface => surface.SurfaceId == "event-subscriptions").Entries,
            entry => entry.Id == "audit-projector");
        Assert.Equal("Audit Projector", subscriptionEntry.DisplayName);
        Assert.False(subscriptionEntry.Metadata.ContainsKey("descriptorSource"));
        Assert.False(subscriptionEntry.Metadata.ContainsKey("configurationPath"));
        Assert.Equal("compliance-audit-projector", subscriptionEntry.Metadata["handlerId"]);
        Assert.Equal("background-service", subscriptionEntry.Metadata["deliveryMode"]);
        Assert.Equal("cephalon-managed", subscriptionEntry.Metadata["executionOwnership"]);
        Assert.Equal("runtime-bound", subscriptionEntry.Metadata["subscriptionRuntime"]);

        Assert.NotNull(capabilities);
        var subscribeCapability = Assert.Single(capabilities, capability => capability.Key == "eventing.subscribe");
        Assert.Equal("cephalon-managed", subscribeCapability.Metadata["executionOwnership"]);
    }

    [Fact]
    public void AddEventingFromConfigurationRejectsConfiguredSubscriptionsBecauseSubscriptionsAreCodeFirst()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Engine:Messaging:Subscriptions:audit-projector:ChannelId"] = "audit"
        });

        var exception = Assert.Throws<InvalidOperationException>(() => builder.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "Microservice",
                patterns: ["CQRS"],
                technologies: ["EventDrivenIntegration"],
                transports: ["RestApi"]));
            engine.AddEventingFromConfiguration(builder.Configuration);
        }));

        Assert.Contains("code-owned", exception.Message, StringComparison.Ordinal);
        Assert.Contains("IEventSubscriptionContributor", exception.Message, StringComparison.Ordinal);
        Assert.Contains("IEventSubscriptionExecutor", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddEventingFromConfigurationRejectsConfiguredSubscriptionHandlersBecauseHandlersAreCodeFirst()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Engine:Messaging:SubscriptionHandlers:audit-projector:Type"] = "MyCompany.Audit.Projector, MyCompany.Audit"
        });

        var exception = Assert.Throws<InvalidOperationException>(() => builder.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "Microservice",
                patterns: ["CQRS"],
                technologies: ["EventDrivenIntegration"],
                transports: ["RestApi"]));
            engine.AddEventingFromConfiguration(builder.Configuration);
        }));

        Assert.Contains("code-owned", exception.Message, StringComparison.Ordinal);
        Assert.Contains("IEventSubscriptionContributor", exception.Message, StringComparison.Ordinal);
        Assert.Contains("IEventSubscriptionExecutor", exception.Message, StringComparison.Ordinal);
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
        var topologyOwnershipEntry = Assert.Single(
            eventingSurfaces.Single(surface => surface.SurfaceId == "eventing-superiority-profile").Entries,
            entry => entry.Id == "broker-topology-materialization-ownership");
        var partitionOwnershipEntry = Assert.Single(
            eventingSurfaces.Single(surface => surface.SurfaceId == "eventing-superiority-profile").Entries,
            entry => entry.Id == "provider-partition-ownership");
        Assert.Equal("claimed", superiorityEntry.Metadata["status"]);
        Assert.Contains("routes=1", superiorityEntry.Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Equal("not-claimed", topologyOwnershipEntry.Metadata["status"]);
        Assert.Contains("routingPolicy=event-type-map", topologyOwnershipEntry.Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("routes=1", topologyOwnershipEntry.Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("brokerTopologyMaterialization=not-claimed", topologyOwnershipEntry.Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("queueProvisioning=not-claimed", topologyOwnershipEntry.Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("providerOwnedTopology=not-present", topologyOwnershipEntry.Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("wolverineRequired=false", topologyOwnershipEntry.Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Equal("not-claimed", partitionOwnershipEntry.Metadata["status"]);
        Assert.Contains("routingPolicy=event-type-map", partitionOwnershipEntry.Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("routes=1", partitionOwnershipEntry.Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("providerPartitionOwnership=not-claimed", partitionOwnershipEntry.Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("partitionAssignment=not-claimed", partitionOwnershipEntry.Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("partitionOrderingGuarantee=not-claimed", partitionOwnershipEntry.Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("providerOwnedPartitioning=not-present", partitionOwnershipEntry.Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("wolverineRequired=false", partitionOwnershipEntry.Metadata["runtimeEvidence"], StringComparison.Ordinal);

        Assert.NotNull(snapshot);
        var snapshotPublicationState = Assert.Single(snapshot.EventPublicationStates);
        Assert.Equal("audit", snapshotPublicationState.LastChannelId);
        var snapshotRoutingEntry = Assert.Single(
            snapshot.TechnologySurfaces.Single(surface => surface.SurfaceId == "eventing-superiority-profile").Entries,
            entry => entry.Id == "routing-and-provider-portability");
        var snapshotTopologyOwnershipEntry = Assert.Single(
            snapshot.TechnologySurfaces.Single(surface => surface.SurfaceId == "eventing-superiority-profile").Entries,
            entry => entry.Id == "broker-topology-materialization-ownership");
        var snapshotPartitionOwnershipEntry = Assert.Single(
            snapshot.TechnologySurfaces.Single(surface => surface.SurfaceId == "eventing-superiority-profile").Entries,
            entry => entry.Id == "provider-partition-ownership");
        Assert.Equal("claimed", snapshotRoutingEntry.Metadata["status"]);
        Assert.Equal("not-claimed", snapshotTopologyOwnershipEntry.Metadata["status"]);
        Assert.Contains("brokerTopologyMaterialization=not-claimed", snapshotTopologyOwnershipEntry.Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Equal("not-claimed", snapshotPartitionOwnershipEntry.Metadata["status"]);
        Assert.Contains("providerPartitionOwnership=not-claimed", snapshotPartitionOwnershipEntry.Metadata["runtimeEvidence"], StringComparison.Ordinal);
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

        var scheduledForUtc = DateTimeOffset.UtcNow.AddMilliseconds(1000);
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

        await WaitForConditionAsync(() => probe.SuccessfulAttempts == 1, timeoutMilliseconds: 5000);

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
    public async Task MapCephalonAppliesConfigDrivenInProcessEventSubscriptionRetryBackoffWithoutWolverine()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Engine:Messaging:InProcessSubscriptions:EnableExecution"] = "true",
            ["Engine:Messaging:InProcessSubscriptions:MaxAttempts"] = "3",
            ["Engine:Messaging:InProcessSubscriptions:RetryDelayMilliseconds"] = "1",
            ["Engine:Messaging:InProcessSubscriptions:RetryBackoff"] = "exponential",
            ["Engine:Messaging:InProcessSubscriptions:RetryBackoffMultiplier"] = "2",
            ["Engine:Messaging:InProcessSubscriptions:RetryMaxDelayMilliseconds"] = "5",
            ["Engine:Messaging:InProcessSubscriptions:RetryJitterPercent"] = "0"
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

        var probe = app.Services.GetRequiredService<ManagedAuditProjectorProbe>();
        probe.FailuresRemaining = 2;
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(new EventPublication(
                id: "audit-backoff-001",
                channelId: "audit",
                eventType: "audit.created",
                payload: """{"id":"audit-backoff-001"}""",
                occurredAtUtc: new DateTimeOffset(2026, 05, 10, 14, 0, 0, TimeSpan.Zero),
                contentType: "application/json",
                correlationId: "corr-audit-backoff-001",
                tenantId: "tenant-backoff-001"));
        }

        var client = app.GetTestClient();
        var runtimeCatalog = app.Services.GetRequiredService<IEventSubscriptionRuntimeCatalog>();
        var publicationRuntimeCatalog = app.Services.GetRequiredService<IEventPublicationRuntimeCatalog>();
        var bindingCatalog = app.Services.GetRequiredService<IEventSubscriptionExecutionBindingCatalog>();
        var capabilities = await client.GetFromJsonAsync<CapabilityManifest[]>("/engine/capabilities");
        var eventingSurfaces = await client.GetFromJsonAsync<TechnologyRuntimeSurface[]>("/engine/technology-surfaces/event-driven-integration");

        Assert.Equal(3, probe.TotalAttempts);
        Assert.Equal(1, probe.SuccessfulAttempts);

        var runtimeState = Assert.Single(runtimeCatalog.States);
        Assert.Equal(EventSubscriptionExecutionOutcomes.Succeeded, runtimeState.LastOutcome);
        Assert.Equal(3, runtimeState.LastAttempt);
        Assert.Equal(2, runtimeState.RetryScheduledCount);
        Assert.Equal("bounded-in-process", runtimeState.Metadata["retryPolicy"]);
        Assert.Equal("3", runtimeState.Metadata["retryMaxAttempts"]);
        Assert.Equal("1", runtimeState.Metadata["retryDelayMilliseconds"]);
        Assert.Equal("exponential", runtimeState.Metadata["retryBackoff"]);
        Assert.Equal("2", runtimeState.Metadata["retryBackoffMultiplier"]);
        Assert.Equal("5", runtimeState.Metadata["retryMaxDelayMilliseconds"]);
        Assert.Equal("0", runtimeState.Metadata["retryJitterPercent"]);

        var publicationState = Assert.Single(publicationRuntimeCatalog.States);
        Assert.Equal(EventPublicationRuntimeOutcomes.Succeeded, publicationState.LastOutcome);
        Assert.Equal(3, publicationState.StartedSubscriptionCount);
        Assert.Equal(2, publicationState.RetryScheduledSubscriptionCount);
        Assert.Equal("exponential", publicationState.Metadata["retryBackoff"]);
        Assert.Equal("5", publicationState.Metadata["retryMaxDelayMilliseconds"]);

        var binding = Assert.Single(bindingCatalog.Bindings);
        Assert.Equal("exponential", binding.Metadata["retryBackoff"]);
        Assert.Equal("2", binding.Metadata["retryBackoffMultiplier"]);
        Assert.Equal("5", binding.Metadata["retryMaxDelayMilliseconds"]);

        Assert.NotNull(capabilities);
        var publishCapability = Assert.Single(capabilities, capability => capability.Key == "eventing.publish");
        Assert.Equal("exponential", publishCapability.Metadata["retryBackoff"]);
        Assert.Equal("5", publishCapability.Metadata["retryMaxDelayMilliseconds"]);
        var subscribeCapability = Assert.Single(capabilities, capability => capability.Key == "eventing.subscribe");
        Assert.Equal("exponential", subscribeCapability.Metadata["retryBackoff"]);
        Assert.Equal("0", subscribeCapability.Metadata["retryJitterPercent"]);

        Assert.NotNull(eventingSurfaces);
        var publisherEntry = Assert.Single(eventingSurfaces.Single(surface => surface.SurfaceId == "event-publishers").Entries);
        Assert.Equal("exponential", publisherEntry.Metadata["retryBackoff"]);
        Assert.Equal("2", publisherEntry.Metadata["retryBackoffMultiplier"]);
        var subscriptionEntry = Assert.Single(
            eventingSurfaces.Single(surface => surface.SurfaceId == "event-subscriptions").Entries,
            entry => entry.Id == "audit-projector");
        Assert.Equal("exponential", subscriptionEntry.Metadata["binding.retryBackoff"]);
        Assert.Equal("exponential", subscriptionEntry.Metadata["reported.retryBackoff"]);
        Assert.Equal("2", subscriptionEntry.Metadata["retryScheduledCount"]);

        var recoverabilityEntry = Assert.Single(
            eventingSurfaces.Single(surface => surface.SurfaceId == "eventing-superiority-profile").Entries,
            entry => entry.Id == "recoverability-and-terminal-failure-posture");
        Assert.Equal("claimed", recoverabilityEntry.Metadata["status"]);
        Assert.Contains("backoff=exponential", recoverabilityEntry.Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("jitter=0", recoverabilityEntry.Metadata["runtimeEvidence"], StringComparison.Ordinal);
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

    [Fact]
    public async Task MapCephalonDiscoversInProcessSubscriptionDescriptorFromExecutorProviderWithoutWolverine()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<DescriptorProviderAuditExecutorProbe>();
        builder.Services.AddCephalonEventSubscriptionExecutor<DescriptorProviderAuditExecutor>();
        builder.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "Microservice",
                patterns: ["CQRS"],
                technologies: ["EventDrivenIntegration"],
                transports: ["RestApi"]));
            engine.AddEventing(options =>
            {
                options.EnableInProcessSubscriptionExecution = true;
                options.Channels.Add(new EventChannelDescriptor(
                    id: "audit",
                    displayName: "Audit",
                    description: "Audit integration events."));
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(new EventPublication(
                id: "audit-discovery-001",
                channelId: "audit",
                eventType: "audit.created",
                payload: """{"id":"audit-discovery-001"}""",
                occurredAtUtc: new DateTimeOffset(2026, 05, 10, 17, 0, 0, TimeSpan.Zero),
                contentType: "application/json",
                correlationId: "corr-audit-discovery-001",
                tenantId: "tenant-discovery-001"));
        }

        var client = app.GetTestClient();
        var probe = app.Services.GetRequiredService<DescriptorProviderAuditExecutorProbe>();
        var subscriptionCatalog = app.Services.GetRequiredService<IEventSubscriptionCatalog>();
        var runtimeCatalog = app.Services.GetRequiredService<IEventSubscriptionRuntimeCatalog>();
        var bindingCatalog = app.Services.GetRequiredService<IEventSubscriptionExecutionBindingCatalog>();
        var capabilities = await client.GetFromJsonAsync<CapabilityManifest[]>("/engine/capabilities");
        var eventingSurfaces = await client.GetFromJsonAsync<TechnologyRuntimeSurface[]>("/engine/technology-surfaces/event-driven-integration");
        var readiness = await client.GetFromJsonAsync<EventSubscriptionExecutionReadinessDescriptor[]>("/engine/event-subscription-readiness");

        Assert.Equal(1, probe.TotalAttempts);
        Assert.Equal("audit-discovery-001", probe.LastMessageId);

        Assert.True(subscriptionCatalog.TryGet("discovered-audit-projector", out var subscription));
        Assert.Equal("Discovered Audit Projector", subscription.DisplayName);
        Assert.Equal("descriptor-provider-audit-projector", subscription.HandlerId);
        Assert.Equal("executor-provider", subscription.Metadata["descriptorSource"]);
        Assert.Equal("code-first-executor", subscription.Metadata["descriptorDiscovery"]);
        Assert.Contains(nameof(DescriptorProviderAuditExecutor), subscription.Metadata["descriptorProvider"], StringComparison.Ordinal);

        var binding = Assert.Single(bindingCatalog.Bindings);
        Assert.Equal("discovered-audit-projector", binding.SubscriptionId);
        Assert.Equal("code-first-executor", binding.Metadata["subscriptionDescriptorDiscovery"]);

        var runtimeState = Assert.Single(runtimeCatalog.States);
        Assert.Equal(EventSubscriptionExecutionOutcomes.Succeeded, runtimeState.LastOutcome);
        Assert.Equal("code-first-executor", runtimeState.Metadata["subscriptionDescriptorDiscovery"]);

        Assert.NotNull(capabilities);
        var subscriptionsCapability = Assert.Single(capabilities, capability => capability.Key == "eventing.subscriptions");
        Assert.Equal("code-first-executor", subscriptionsCapability.Metadata["subscriptionDescriptorDiscovery"]);
        var subscribeCapability = Assert.Single(capabilities, capability => capability.Key == "eventing.subscribe");
        Assert.Equal("code-first-executor", subscribeCapability.Metadata["subscriptionDescriptorDiscovery"]);

        Assert.NotNull(eventingSurfaces);
        var publisherEntry = Assert.Single(
            eventingSurfaces.Single(surface => surface.SurfaceId == "event-publishers").Entries);
        Assert.Equal("code-first-executor", publisherEntry.Metadata["subscriptionDescriptorDiscovery"]);
        Assert.Equal("1", publisherEntry.Metadata["discoveredSubscriptionCount"]);
        Assert.Equal("discovered-audit-projector", publisherEntry.Metadata["discoveredSubscriptionIds"]);

        var subscriptionEntry = Assert.Single(
            eventingSurfaces.Single(surface => surface.SurfaceId == "event-subscriptions").Entries,
            entry => entry.Id == "discovered-audit-projector");
        Assert.Equal("executor-provider", subscriptionEntry.Metadata["descriptorSource"]);
        Assert.Equal("code-first-executor", subscriptionEntry.Metadata["descriptorDiscovery"]);
        Assert.Equal("code-first-executor", subscriptionEntry.Metadata["binding.subscriptionDescriptorDiscovery"]);
        Assert.Equal("code-first-executor", subscriptionEntry.Metadata["reported.subscriptionDescriptorDiscovery"]);

        var lowCeremonyEntry = Assert.Single(
            eventingSurfaces.Single(surface => surface.SurfaceId == "eventing-superiority-profile").Entries,
            entry => entry.Id == "mediator-style-in-process-low-ceremony");
        Assert.Equal("claimed", lowCeremonyEntry.Metadata["status"]);
        Assert.Contains("code-first descriptor discovery", lowCeremonyEntry.Metadata["runtimeEvidence"], StringComparison.Ordinal);

        Assert.NotNull(readiness);
        var discoveredReadiness = Assert.Single(readiness);
        Assert.Equal("discovered-audit-projector", discoveredReadiness.SubscriptionId);
        Assert.Equal(EventSubscriptionExecutionReadinessStates.RuntimeBound, discoveredReadiness.ReadinessState);
    }

    [Fact]
    public async Task MapCephalonDiscoversInProcessSubscriptionDescriptorFromExecutorAttributeWithoutWolverine()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<DescriptorProviderAuditExecutorProbe>();
        builder.Services.AddCephalonEventSubscriptionExecutor<AttributeAuditExecutor>();
        builder.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "Microservice",
                patterns: ["CQRS"],
                technologies: ["EventDrivenIntegration"],
                transports: ["RestApi"]));
            engine.AddEventing(options =>
            {
                options.EnableInProcessSubscriptionExecution = true;
                options.Channels.Add(new EventChannelDescriptor(
                    id: "audit",
                    displayName: "Audit",
                    description: "Audit integration events."));
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(new EventPublication(
                id: "audit-attribute-001",
                channelId: "audit",
                eventType: "audit.created",
                payload: """{"id":"audit-attribute-001"}""",
                occurredAtUtc: new DateTimeOffset(2026, 05, 10, 18, 0, 0, TimeSpan.Zero),
                contentType: "application/json",
                correlationId: "corr-audit-attribute-001",
                tenantId: "tenant-attribute-001"));
        }

        var client = app.GetTestClient();
        var probe = app.Services.GetRequiredService<DescriptorProviderAuditExecutorProbe>();
        var subscriptionCatalog = app.Services.GetRequiredService<IEventSubscriptionCatalog>();
        var runtimeCatalog = app.Services.GetRequiredService<IEventSubscriptionRuntimeCatalog>();
        var bindingCatalog = app.Services.GetRequiredService<IEventSubscriptionExecutionBindingCatalog>();
        var capabilities = await client.GetFromJsonAsync<CapabilityManifest[]>("/engine/capabilities");
        var eventingSurfaces = await client.GetFromJsonAsync<TechnologyRuntimeSurface[]>("/engine/technology-surfaces/event-driven-integration");

        Assert.Equal(1, probe.TotalAttempts);
        Assert.Equal("audit-attribute-001", probe.LastMessageId);

        Assert.True(subscriptionCatalog.TryGet("attribute-audit-projector", out var subscription));
        Assert.Equal("Attribute Audit Projector", subscription.DisplayName);
        Assert.Equal("attribute-audit-projector", subscription.HandlerId);
        Assert.Equal("executor-attribute", subscription.Metadata["descriptorSource"]);
        Assert.Equal("code-first-attribute", subscription.Metadata["descriptorDiscovery"]);
        Assert.Contains(nameof(AttributeAuditExecutor), subscription.Metadata["descriptorProvider"], StringComparison.Ordinal);
        Assert.Contains(nameof(EventSubscriptionAttribute), subscription.Metadata["descriptorAttribute"], StringComparison.Ordinal);
        Assert.Contains("attribute", subscription.Tags);

        var binding = Assert.Single(bindingCatalog.Bindings);
        Assert.Equal("attribute-audit-projector", binding.SubscriptionId);
        Assert.Equal("code-first-attribute", binding.Metadata["subscriptionDescriptorDiscovery"]);

        var runtimeState = Assert.Single(runtimeCatalog.States);
        Assert.Equal(EventSubscriptionExecutionOutcomes.Succeeded, runtimeState.LastOutcome);
        Assert.Equal("code-first-attribute", runtimeState.Metadata["subscriptionDescriptorDiscovery"]);

        Assert.NotNull(capabilities);
        var subscribeCapability = Assert.Single(capabilities, capability => capability.Key == "eventing.subscribe");
        Assert.Equal("code-first-attribute", subscribeCapability.Metadata["subscriptionDescriptorDiscovery"]);

        Assert.NotNull(eventingSurfaces);
        var publisherEntry = Assert.Single(
            eventingSurfaces.Single(surface => surface.SurfaceId == "event-publishers").Entries);
        Assert.Equal("code-first-attribute", publisherEntry.Metadata["subscriptionDescriptorDiscovery"]);
        Assert.Equal("1", publisherEntry.Metadata["discoveredSubscriptionCount"]);
        Assert.Equal("attribute-audit-projector", publisherEntry.Metadata["discoveredSubscriptionIds"]);

        var subscriptionEntry = Assert.Single(
            eventingSurfaces.Single(surface => surface.SurfaceId == "event-subscriptions").Entries,
            entry => entry.Id == "attribute-audit-projector");
        Assert.Equal("executor-attribute", subscriptionEntry.Metadata["descriptorSource"]);
        Assert.Equal("code-first-attribute", subscriptionEntry.Metadata["descriptorDiscovery"]);
        Assert.Equal("code-first-attribute", subscriptionEntry.Metadata["binding.subscriptionDescriptorDiscovery"]);
        Assert.Equal("code-first-attribute", subscriptionEntry.Metadata["reported.subscriptionDescriptorDiscovery"]);

        var lowCeremonyEntry = Assert.Single(
            eventingSurfaces.Single(surface => surface.SurfaceId == "eventing-superiority-profile").Entries,
            entry => entry.Id == "mediator-style-in-process-low-ceremony");
        Assert.Equal("claimed", lowCeremonyEntry.Metadata["status"]);
        Assert.Contains("code-first descriptor discovery", lowCeremonyEntry.Metadata["runtimeEvidence"], StringComparison.Ordinal);
    }

    [Fact]
    public async Task BuildRejectsDescriptorProviderWhenExecutorSubscriptionIdDoesNotMatchDescriptor()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddCephalonEventSubscriptionExecutor<MismatchedDescriptorProviderExecutor>();
        builder.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "Microservice",
                patterns: ["CQRS"],
                technologies: ["EventDrivenIntegration"],
                transports: ["RestApi"]));
            engine.AddEventing(options =>
            {
                options.EnableInProcessSubscriptionExecution = true;
                options.Channels.Add(new EventChannelDescriptor(
                    id: "audit",
                    displayName: "Audit",
                    description: "Audit integration events."));
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        var exception = Assert.Throws<InvalidOperationException>(
            () => app.Services.GetRequiredService<IEventSubscriptionCatalog>());
        Assert.Contains("provides descriptor", exception.Message, StringComparison.Ordinal);
        Assert.Contains("mismatched-descriptor", exception.Message, StringComparison.Ordinal);
        Assert.Contains("mismatched-executor", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task BuildRejectsDescriptorAttributeWhenExecutorSubscriptionIdDoesNotMatchDescriptor()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddCephalonEventSubscriptionExecutor<MismatchedDescriptorAttributeExecutor>();
        builder.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "Microservice",
                patterns: ["CQRS"],
                technologies: ["EventDrivenIntegration"],
                transports: ["RestApi"]));
            engine.AddEventing(options =>
            {
                options.EnableInProcessSubscriptionExecution = true;
                options.Channels.Add(new EventChannelDescriptor(
                    id: "audit",
                    displayName: "Audit",
                    description: "Audit integration events."));
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        var exception = Assert.Throws<InvalidOperationException>(
            () => app.Services.GetRequiredService<IEventSubscriptionCatalog>());
        Assert.Contains("declares attribute descriptor", exception.Message, StringComparison.Ordinal);
        Assert.Contains("mismatched-attribute-descriptor", exception.Message, StringComparison.Ordinal);
        Assert.Contains("mismatched-attribute-executor", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task BuildPreservesFailClosedUnknownSubscriptionWhenExecutorDoesNotProvideDescriptor()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddCephalonEventSubscriptionExecutor<UnknownSubscriptionExecutor>();
        builder.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "Microservice",
                patterns: ["CQRS"],
                technologies: ["EventDrivenIntegration"],
                transports: ["RestApi"]));
            engine.AddEventing(options =>
            {
                options.EnableInProcessSubscriptionExecution = true;
                options.Channels.Add(new EventChannelDescriptor(
                    id: "audit",
                    displayName: "Audit",
                    description: "Audit integration events."));
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        var exception = Assert.Throws<InvalidOperationException>(
            () => app.Services.GetRequiredService<IEventSubscriptionExecutionBindingCatalog>());
        Assert.Contains("unknown declared subscription", exception.Message, StringComparison.Ordinal);
        Assert.Contains("missing-subscription", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MapCephalonRunsCodeFirstSubscriptionExecutionMiddlewareWithoutWolverine()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<EventSubscriptionMiddlewareProbe>();
        builder.Services.AddCephalonEventSubscriptionExecutionMiddleware<RecordingEventSubscriptionExecutionMiddleware>();
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
                id: "audit-pipeline-001",
                channelId: "audit",
                eventType: "audit.created",
                payload: """{"id":"audit-pipeline-001"}""",
                occurredAtUtc: new DateTimeOffset(2026, 05, 10, 16, 0, 0, TimeSpan.Zero),
                contentType: "application/json",
                correlationId: "corr-audit-pipeline-001",
                tenantId: "tenant-pipeline-001"));
        }

        var client = app.GetTestClient();
        var projectorProbe = app.Services.GetRequiredService<ManagedAuditProjectorProbe>();
        var middlewareProbe = app.Services.GetRequiredService<EventSubscriptionMiddlewareProbe>();
        var runtimeCatalog = app.Services.GetRequiredService<IEventSubscriptionRuntimeCatalog>();
        var bindingCatalog = app.Services.GetRequiredService<IEventSubscriptionExecutionBindingCatalog>();
        var capabilities = await client.GetFromJsonAsync<CapabilityManifest[]>("/engine/capabilities");
        var eventingSurfaces = await client.GetFromJsonAsync<TechnologyRuntimeSurface[]>("/engine/technology-surfaces/event-driven-integration");

        Assert.Equal(1, projectorProbe.TotalAttempts);
        Assert.Equal(1, projectorProbe.SuccessfulAttempts);
        Assert.Equal(
            ["before:audit-projector:1", "after:audit-projector:1"],
            middlewareProbe.Events);
        Assert.Equal("code-first", middlewareProbe.LastPipeline);
        Assert.Equal("1", middlewareProbe.LastMiddlewareCount);

        var runtimeState = Assert.Single(runtimeCatalog.States);
        Assert.Equal(EventSubscriptionExecutionOutcomes.Succeeded, runtimeState.LastOutcome);
        Assert.Equal("code-first", runtimeState.Metadata["subscriptionExecutionPipeline"]);
        Assert.Equal("1", runtimeState.Metadata["subscriptionExecutionMiddlewareCount"]);

        var binding = Assert.Single(bindingCatalog.Bindings);
        Assert.Equal("code-first", binding.Metadata["subscriptionExecutionPipeline"]);
        Assert.Equal("1", binding.Metadata["subscriptionExecutionMiddlewareCount"]);

        Assert.NotNull(capabilities);
        var publishCapability = Assert.Single(capabilities, capability => capability.Key == "eventing.publish");
        Assert.Equal("code-first", publishCapability.Metadata["subscriptionExecutionPipeline"]);
        Assert.Equal("1", publishCapability.Metadata["subscriptionExecutionMiddlewareCount"]);
        var subscribeCapability = Assert.Single(capabilities, capability => capability.Key == "eventing.subscribe");
        Assert.Equal("code-first", subscribeCapability.Metadata["subscriptionExecutionPipeline"]);
        Assert.Equal("1", subscribeCapability.Metadata["subscriptionExecutionMiddlewareCount"]);

        Assert.NotNull(eventingSurfaces);
        var publisherEntry = Assert.Single(
            eventingSurfaces.Single(surface => surface.SurfaceId == "event-publishers").Entries);
        Assert.Equal("code-first", publisherEntry.Metadata["subscriptionExecutionPipeline"]);
        Assert.Equal("1", publisherEntry.Metadata["subscriptionExecutionMiddlewareCount"]);

        var subscriptionEntry = Assert.Single(
            eventingSurfaces.Single(surface => surface.SurfaceId == "event-subscriptions").Entries,
            entry => entry.Id == "audit-projector");
        Assert.Equal("code-first", subscriptionEntry.Metadata["binding.subscriptionExecutionPipeline"]);
        Assert.Equal("1", subscriptionEntry.Metadata["binding.subscriptionExecutionMiddlewareCount"]);
        Assert.Equal("code-first", subscriptionEntry.Metadata["reported.subscriptionExecutionPipeline"]);
        Assert.Equal("1", subscriptionEntry.Metadata["reported.subscriptionExecutionMiddlewareCount"]);

        var superiorityEntry = Assert.Single(
            eventingSurfaces.Single(surface => surface.SurfaceId == "eventing-superiority-profile").Entries,
            entry => entry.Id == "code-first-subscription-execution-pipeline");
        Assert.Equal("claimed", superiorityEntry.Metadata["status"]);
        Assert.Contains("middlewareCount=1", superiorityEntry.Metadata["runtimeEvidence"], StringComparison.Ordinal);
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

    private sealed class DescriptorProviderAuditExecutorProbe
    {
        private readonly Lock gate = new();
        private int totalAttempts;
        private string? lastMessageId;

        public int TotalAttempts
        {
            get
            {
                lock (gate)
                {
                    return totalAttempts;
                }
            }
        }

        public string? LastMessageId
        {
            get
            {
                lock (gate)
                {
                    return lastMessageId;
                }
            }
        }

        public void Record(EventSubscriptionExecutionContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            lock (gate)
            {
                totalAttempts++;
                lastMessageId = context.Publication.Id;
            }
        }
    }

    private sealed class DescriptorProviderAuditExecutor(
        DescriptorProviderAuditExecutorProbe probe) : IEventSubscriptionExecutor, IEventSubscriptionDescriptorProvider
    {
        public string SubscriptionId => "discovered-audit-projector";

        public EventSubscriptionDescriptor SubscriptionDescriptor { get; } = new(
            id: "discovered-audit-projector",
            displayName: "Discovered Audit Projector",
            description: "Projects audit events through a descriptor-bearing executor.",
            channelId: "audit",
            handlerId: "descriptor-provider-audit-projector",
            deliveryMode: "in-process-direct",
            tags: ["audit", "discovered"]);

        public ValueTask ExecuteAsync(
            EventSubscriptionExecutionContext context,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            probe.Record(context);
            return ValueTask.CompletedTask;
        }
    }

    [EventSubscription(
        id: "attribute-audit-projector",
        displayName: "Attribute Audit Projector",
        description: "Projects audit events through an attribute-bearing executor.",
        channelId: "audit",
        handlerId: "attribute-audit-projector",
        deliveryMode: "in-process-direct",
        Tags = new[] { "audit", "attribute" })]
    private sealed class AttributeAuditExecutor(
        DescriptorProviderAuditExecutorProbe probe) : IEventSubscriptionExecutor
    {
        public string SubscriptionId => "attribute-audit-projector";

        public ValueTask ExecuteAsync(
            EventSubscriptionExecutionContext context,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            probe.Record(context);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class MismatchedDescriptorProviderExecutor : IEventSubscriptionExecutor, IEventSubscriptionDescriptorProvider
    {
        public string SubscriptionId => "mismatched-executor";

        public EventSubscriptionDescriptor SubscriptionDescriptor { get; } = new(
            id: "mismatched-descriptor",
            displayName: "Mismatched Descriptor",
            description: "Intentionally mismatches the executor id.",
            channelId: "audit",
            handlerId: "mismatched-handler",
            deliveryMode: "in-process-direct");

        public ValueTask ExecuteAsync(
            EventSubscriptionExecutionContext context,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.CompletedTask;
        }
    }

    [EventSubscription(
        id: "mismatched-attribute-descriptor",
        displayName: "Mismatched Attribute Descriptor",
        description: "Intentionally mismatches the executor id from an attribute.",
        channelId: "audit",
        handlerId: "mismatched-attribute-handler",
        deliveryMode: "in-process-direct")]
    private sealed class MismatchedDescriptorAttributeExecutor : IEventSubscriptionExecutor
    {
        public string SubscriptionId => "mismatched-attribute-executor";

        public ValueTask ExecuteAsync(
            EventSubscriptionExecutionContext context,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.CompletedTask;
        }
    }

    private sealed class UnknownSubscriptionExecutor : IEventSubscriptionExecutor
    {
        public string SubscriptionId => "missing-subscription";

        public ValueTask ExecuteAsync(
            EventSubscriptionExecutionContext context,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.CompletedTask;
        }
    }

    private sealed class EventSubscriptionMiddlewareProbe
    {
        private readonly Lock gate = new();
        private readonly List<string> events = [];
        private string? lastMiddlewareCount;
        private string? lastPipeline;

        public IReadOnlyList<string> Events
        {
            get
            {
                lock (gate)
                {
                    return events.ToArray();
                }
            }
        }

        public string? LastMiddlewareCount
        {
            get
            {
                lock (gate)
                {
                    return lastMiddlewareCount;
                }
            }
        }

        public string? LastPipeline
        {
            get
            {
                lock (gate)
                {
                    return lastPipeline;
                }
            }
        }

        public void Record(string stage, EventSubscriptionExecutionContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            lock (gate)
            {
                events.Add($"{stage}:{context.Subscription.Id}:{context.Attempt.ToString(CultureInfo.InvariantCulture)}");
                lastPipeline = context.Metadata["subscriptionExecutionPipeline"];
                lastMiddlewareCount = context.Metadata["subscriptionExecutionMiddlewareCount"];
            }
        }
    }

    private sealed class RecordingEventSubscriptionExecutionMiddleware(
        EventSubscriptionMiddlewareProbe probe) : IEventSubscriptionExecutionMiddleware
    {
        public async ValueTask InvokeAsync(
            EventSubscriptionExecutionContext context,
            EventSubscriptionExecutionStep nextStep,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(nextStep);
            cancellationToken.ThrowIfCancellationRequested();

            probe.Record("before", context);
            await nextStep(context, cancellationToken).ConfigureAwait(false);
            probe.Record("after", context);
        }
    }
}
