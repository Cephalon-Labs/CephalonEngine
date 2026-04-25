using System.Net;
using System.Net.Http.Json;
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
        var missingStateResponse = await client.GetAsync("/engine/event-dispatches/entity-framework-outbox");

        Assert.NotNull(runtimeDescriptors);
        var descriptor = Assert.Single(runtimeDescriptors);
        Assert.Equal("wolverine-dispatch-loop", descriptor.Id);
        Assert.Equal("wolverine", descriptor.Metadata["adapter"]);
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
                ["nextRetryAtUtc"] = "2026-04-11T09:45:00.0000000+00:00"
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
        Assert.Equal("fixed-delay", managedCapability.Metadata["retryPolicy"]);

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
        Assert.Equal("fixed-delay", managedSubscription.Metadata["binding.retryPolicy"]);
        Assert.Equal("45", managedSubscription.Metadata["binding.retryDelaySeconds"]);
        Assert.Equal("audit-projector-pump", managedSubscription.Metadata["hostedExecutionId"]);
        Assert.Equal("audit-subscription-flow", managedSubscription.Metadata["executionGraphId"]);

        var adapterSurface = Assert.Single(eventingSurfaces, surface => surface.SurfaceId == "wolverine-adapter");
        var adapterEntry = Assert.Single(adapterSurface.Entries);
        Assert.Equal("wolverine-managed", adapterEntry.Metadata["dispatchBridge"]);
        Assert.Equal("wolverine-managed", adapterEntry.Metadata["subscriptionExecution"]);
        Assert.Equal("wolverine-subscription-execution", adapterEntry.Metadata["subscriptionExecutionRuntimeId"]);
        Assert.Equal("1", adapterEntry.Metadata["managedSubscriptionCount"]);
        Assert.Equal("audit-projector", adapterEntry.Metadata["managedSubscriptionIds"]);
        Assert.Equal("45", adapterEntry.Metadata["subscriptionRetryDelaySeconds"]);

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
}
