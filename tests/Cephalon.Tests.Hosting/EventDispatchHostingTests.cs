using System.Net;
using System.Net.Http.Json;
using Cephalon.Abstractions.Data;
using Cephalon.AspNetCore.Hosting;
using Cephalon.Data.EntityFramework.Registration;
using Cephalon.Engine.Configuration;
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
        var snapshot = await client.GetFromJsonAsync<Cephalon.Engine.Runtime.RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(states);
        var reportedState = Assert.Single(states);
        Assert.Equal("entity-framework-outbox", reportedState.OutboxId);
        Assert.Equal("retry-scheduled", reportedState.LastOutcome);
        Assert.True(reportedState.RetryPending);
        Assert.Equal("evt-900", reportedState.LastMessageId);
        Assert.NotNull(state);
        Assert.Equal("entity-framework-outbox", state.OutboxId);
        Assert.NotNull(snapshot);
        Assert.Single(snapshot.EventDispatchRuntimes);
        Assert.Single(snapshot.EventDispatchStates);
        Assert.Equal("wolverine-dispatch-loop", snapshot.EventDispatchRuntimes[0].Id);
        Assert.Equal("entity-framework-outbox", snapshot.EventDispatchStates[0].OutboxId);
    }
}
