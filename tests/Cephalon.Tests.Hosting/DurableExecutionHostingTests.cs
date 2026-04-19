using System.Net;
using System.Net.Http.Json;
using Cephalon.Abstractions.Behaviors;
using Cephalon.Abstractions.EventSourcing;
using Cephalon.Abstractions.Execution;
using Cephalon.Abstractions.Modules;
using Cephalon.AspNetCore.Hosting;
using Cephalon.Behaviors.Hosting;
using Cephalon.Behaviors.Modules;
using Cephalon.Behaviors.Patterns.Abstractions;
using Cephalon.Behaviors.Patterns.Hosting;
using Cephalon.Behaviors.Patterns.Strategies;
using Cephalon.Behaviors.Services;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Runtime;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Hosting;

public sealed class DurableExecutionHostingTests
{
    [Fact]
    public async Task MapCephalonExposesDurableExecutionCatalogAcrossRoutesAndSnapshot()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
        builder.AddCephalon(engine =>
        {
            engine.AddBehaviors(
                configureOptions: options => options.AutoRegister = false,
                configure: behaviors => behaviors.AddBehaviorPatterns());
            engine.AddModule(new DurableExecutionHostingModule());
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var durableExecutions = await client.GetFromJsonAsync<DurableExecutionRuntimeDescriptor[]>("/engine/durable-executions");
        var byModule = await client.GetFromJsonAsync<DurableExecutionRuntimeDescriptor[]>("/engine/durable-executions/modules/tests.durable-host");
        var byTransport = await client.GetFromJsonAsync<DurableExecutionRuntimeDescriptor[]>("/engine/durable-executions/transports/in-memory");
        var durableExecution = await client.GetFromJsonAsync<DurableExecutionRuntimeDescriptor>("/engine/durable-executions/tests.workflows.hosted.approvals.start");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        var listedDescriptor = Assert.Single(durableExecutions!);
        Assert.Single(byModule!);
        Assert.Single(byTransport!);
        Assert.NotNull(durableExecution);
        Assert.NotNull(snapshot);

        Assert.Equal(listedDescriptor.Id, durableExecution!.Id);
        Assert.Equal("tests.durable-host", durableExecution.SourceModuleId);
        Assert.Equal(["host.workflow-preview"], durableExecution.RequiredFeatureFlagIds);
        Assert.Equal(["in-memory"], durableExecution.TransportIds);
        Assert.Equal("event-store-replay", durableExecution.ExecutionMode);
        Assert.True(durableExecution.EventSourcingEnabled);
        Assert.True(durableExecution.RequiresEventStore);
        Assert.Equal([200, 202, 204], durableExecution.SuccessStatusCodes);
        Assert.Equal("approval", durableExecution.Metadata["lane"]);
        Assert.Equal("behaviors.durable-execution", durableExecution.Metadata["capabilityKey"]);

        var snapshotDescriptor = Assert.Single(snapshot!.DurableExecutions);
        Assert.Equal(durableExecution.Id, snapshotDescriptor.Id);
        Assert.Equal(durableExecution.SourceModuleId, snapshotDescriptor.SourceModuleId);
        Assert.Equal(durableExecution.BehaviorType, snapshotDescriptor.BehaviorType);
        Assert.Equal(durableExecution.InputType, snapshotDescriptor.InputType);
        Assert.Equal(durableExecution.StateType, snapshotDescriptor.StateType);
        Assert.Equal(durableExecution.OutputType, snapshotDescriptor.OutputType);
    }

    [Fact]
    public async Task MapCephalonExposesDurableExecutionRuntimeStateAcrossRoutesAndSnapshot()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
        builder.AddCephalon(engine =>
        {
            engine.AddBehaviors(
                configureOptions: options => options.AutoRegister = false,
                configure: behaviors => behaviors.AddBehaviorPatterns());
            engine.AddModule(new DurableExecutionHostingModule());
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();
        var initialStates = await client.GetFromJsonAsync<DurableExecutionRuntimeState[]>("/engine/durable-executions/runtime");
        var initialTimerStates = await client.GetFromJsonAsync<DurableExecutionRuntimeState[]>("/engine/durable-executions/runtime/timers");
        var initialSignalStates = await client.GetFromJsonAsync<DurableExecutionRuntimeState[]>("/engine/durable-executions/runtime/signals");
        var missingStateResponse = await client.GetAsync("/engine/durable-executions/runtime/streams/tests.workflows.hosted.approvals.start:missing");

        Assert.NotNull(initialStates);
        Assert.Empty(initialStates);
        Assert.NotNull(initialTimerStates);
        Assert.Empty(initialTimerStates);
        Assert.NotNull(initialSignalStates);
        Assert.Empty(initialSignalStates);
        Assert.Equal(HttpStatusCode.NotFound, missingStateResponse.StatusCode);

        var strategy = app.Services.GetServices<IBehaviorExecutionStrategy>()
            .OfType<DurableExecutionStrategy>()
            .Single();
        var eventStore = new HostingRecordingEventStore();
        await strategy.ExecuteAsync(MakeContext(
            behaviorId: "tests.workflows.hosted.approvals.start",
            behavior: new HostedApprovalWorkflowBehavior(),
            input: new HostedApprovalWorkflowInput("APR-42", "success"),
            behaviorContext: new HostingTestBehaviorContext(
                "tests.workflows.hosted.approvals.start",
                correlationId: "corr-hosted",
                eventStore: eventStore,
                metadata: new Dictionary<string, string>
                {
                    ["tenantId"] = "tenant-7"
                })));

        await strategy.ExecuteAsync(MakeContext(
            behaviorId: "tests.workflows.hosted.approvals.start",
            behavior: new HostedApprovalWorkflowBehavior(),
            input: new HostedApprovalWorkflowInput("APR-99", "wait"),
            behaviorContext: new HostingTestBehaviorContext(
                "tests.workflows.hosted.approvals.start",
                correlationId: "corr-wait",
                eventStore: eventStore)));

        var states = await client.GetFromJsonAsync<DurableExecutionRuntimeState[]>("/engine/durable-executions/runtime");
        var byBehavior = await client.GetFromJsonAsync<DurableExecutionRuntimeState[]>("/engine/durable-executions/runtime/behaviors/tests.workflows.hosted.approvals.start");
        var byModule = await client.GetFromJsonAsync<DurableExecutionRuntimeState[]>("/engine/durable-executions/runtime/modules/tests.durable-host");
        var byTransport = await client.GetFromJsonAsync<DurableExecutionRuntimeState[]>("/engine/durable-executions/runtime/transports/in-memory");
        var byPendingTimers = await client.GetFromJsonAsync<DurableExecutionRuntimeState[]>("/engine/durable-executions/runtime/timers");
        var byPendingTimerId = await client.GetFromJsonAsync<DurableExecutionRuntimeState[]>("/engine/durable-executions/runtime/timers/approval-timeout");
        var byPendingSignals = await client.GetFromJsonAsync<DurableExecutionRuntimeState[]>("/engine/durable-executions/runtime/signals");
        var byPendingSignalId = await client.GetFromJsonAsync<DurableExecutionRuntimeState[]>("/engine/durable-executions/runtime/signals/approval-released");
        var state = await client.GetFromJsonAsync<DurableExecutionRuntimeState>("/engine/durable-executions/runtime/streams/tests.workflows.hosted.approvals.start:corr-hosted");
        var waitingState = await client.GetFromJsonAsync<DurableExecutionRuntimeState>("/engine/durable-executions/runtime/streams/tests.workflows.hosted.approvals.start:corr-wait");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(states);
        Assert.Equal(2, states.Length);
        Assert.Equal(2, byBehavior!.Length);
        Assert.Equal(2, byModule!.Length);
        Assert.Equal(2, byTransport!.Length);
        Assert.Single(byPendingTimers!);
        Assert.Single(byPendingTimerId!);
        Assert.Single(byPendingSignals!);
        Assert.Single(byPendingSignalId!);
        Assert.NotNull(state);
        Assert.NotNull(waitingState);
        Assert.NotNull(snapshot);

        var reportedState = Assert.Single(
            states,
            static candidate => string.Equals(
                candidate.StreamId,
                "tests.workflows.hosted.approvals.start:corr-hosted",
                StringComparison.Ordinal));
        Assert.Equal(reportedState.StreamId, state!.StreamId);
        Assert.Equal("tests.workflows.hosted.approvals.start", state.BehaviorId);
        Assert.Equal("tests.durable-host", state.SourceModuleId);
        Assert.Equal(["in-memory"], state.TransportIds);
        Assert.Equal("succeeded", state.LastOutcome);
        Assert.Equal("append", state.LastStage);
        Assert.Equal(200, state.LastHttpStatusCode);
        Assert.Equal(-1, state.LastReplayedVersion);
        Assert.Equal(0, state.LastKnownVersion);
        Assert.Equal(1, state.LastAppendedEventCount);
        Assert.True(state.LastStepProducedOutput);
        Assert.False(state.LastStepCompleted);
        Assert.Equal(1, state.StartedCount);
        Assert.Equal(1, state.SucceededCount);
        Assert.Equal(0, state.ContinuationCount);
        Assert.Equal(0, state.CompletedCount);
        Assert.Equal(0, state.FailedCount);
        Assert.Equal(2, state.TotalReports);
        Assert.False(state.ContinuationPending);
        Assert.False(state.IsFailed);
        Assert.False(state.CoordinationPending);
        Assert.Equal("corr-hosted", state.Metadata["correlationId"]);
        Assert.Equal("tenant-7", state.Metadata["tenantId"]);

        Assert.Equal("waiting", waitingState!.LastOutcome);
        Assert.Equal("execute", waitingState.LastStage);
        Assert.Equal(202, waitingState.LastHttpStatusCode);
        Assert.True(waitingState.HasPendingTimers);
        Assert.True(waitingState.HasPendingSignals);
        Assert.True(waitingState.CoordinationPending);
        Assert.Equal(
            new DateTimeOffset(2026, 4, 19, 5, 0, 0, TimeSpan.Zero),
            waitingState.NextTimerDueAtUtc);
        Assert.Equal("approval-timeout", Assert.Single(waitingState.PendingTimers).Id);
        Assert.Equal("approval-released", Assert.Single(waitingState.PendingSignals).Id);

        Assert.Equal(2, snapshot!.DurableExecutionStates.Count);
        var snapshotState = Assert.Single(
            snapshot.DurableExecutionStates,
            static candidate => string.Equals(
                candidate.StreamId,
                "tests.workflows.hosted.approvals.start:corr-hosted",
                StringComparison.Ordinal));
        Assert.Equal(state.StreamId, snapshotState.StreamId);
        Assert.Equal(state.BehaviorId, snapshotState.BehaviorId);
        Assert.Equal(state.LastOutcome, snapshotState.LastOutcome);
        Assert.Equal(state.LastStage, snapshotState.LastStage);
        Assert.Equal(state.LastKnownVersion, snapshotState.LastKnownVersion);
    }

    private static BehaviorExecutionContext MakeContext<TBehavior>(
        string behaviorId,
        TBehavior behavior,
        object input,
        IBehaviorContext behaviorContext)
        where TBehavior : class
    {
        var descriptor = new BehaviorTopologyDescriptor(
            id: behaviorId,
            pattern: "durable-execution",
            transportIds: ["in-memory"],
            eventSourcingEnabled: true,
            sourceModuleId: "tests.durable-host");
        var slot = BehaviorExecutionSlot.ForType(typeof(TBehavior));
        return new BehaviorExecutionContext
        {
            Descriptor = descriptor,
            BehaviorInstance = behavior,
            Slot = slot,
            Input = input,
            BehaviorContext = behaviorContext
        };
    }

    private sealed class DurableExecutionHostingModule : BehaviorModuleBase
    {
        private static readonly ModuleDescriptor DescriptorInstance = new(
            id: "tests.durable-host",
            displayName: "Durable Host",
            description: "Owns one durable workflow for ASP.NET Core operator-route tests.",
            version: "1.0.0");

        public override ModuleDescriptor Descriptor => DescriptorInstance;

        public override void ConfigureBehaviors(IBehaviorModuleBuilder behaviors)
        {
            behaviors.Add<HostedApprovalWorkflowBehavior>(topology => topology
                .AsDurableExecution()
                .ViaInMemory()
                .RequireFeatureFlag("host.workflow-preview")
                .WithApiSurface("hosted-approvals", "start")
                .WithMetadata("lane", "approval")
                .WithOptions(options => options.EventSourcingEnabled = true));
        }
    }

    private sealed record HostedApprovalWorkflowInput(string ApprovalId, string Mode);

    private sealed record HostedApprovalWorkflowState(int ApprovedCount);

    private sealed record HostedApprovalWorkflowOutput(string Status);

    private sealed record HostedApprovalAcceptedEvent(
        string StreamId,
        long StreamVersion,
        DateTime OccurredAtUtc) : DomainEvent(StreamId, StreamVersion, OccurredAtUtc);

    [AppBehavior("tests.workflows.hosted.approvals.start")]
    private sealed class HostedApprovalWorkflowBehavior : IDurableExecution<HostedApprovalWorkflowInput, HostedApprovalWorkflowState, HostedApprovalWorkflowOutput>
    {
        public HostedApprovalWorkflowState CreateInitialState()
        {
            return new HostedApprovalWorkflowState(0);
        }

        public HostedApprovalWorkflowState Apply(
            HostedApprovalWorkflowState state,
            IDomainEvent domainEvent)
        {
            return domainEvent is HostedApprovalAcceptedEvent
                ? state with { ApprovedCount = state.ApprovedCount + 1 }
                : state;
        }

        public string ResolveStreamId(string behaviorId, IBehaviorContext context)
        {
            return $"{behaviorId}:{context.CorrelationId ?? "hosted"}";
        }

        public Task<DurableExecutionStepResult<HostedApprovalWorkflowOutput>> ExecuteDurablyAsync(
            HostedApprovalWorkflowInput input,
            DurableExecutionState<HostedApprovalWorkflowState> execution,
            IBehaviorContext context,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(input.Mode switch
            {
                "success" => new DurableExecutionStepResult<HostedApprovalWorkflowOutput>(
                    output: new HostedApprovalWorkflowOutput("accepted"),
                    events:
                    [
                        new HostedApprovalAcceptedEvent(
                            execution.StreamId,
                            execution.Version + 1,
                            new DateTime(2026, 4, 19, 2, 0, 0, DateTimeKind.Utc))
                    ],
                    isCompleted: false),
                "wait" => new DurableExecutionStepResult<HostedApprovalWorkflowOutput>(
                    output: null,
                    events: [],
                    isCompleted: false,
                    pendingTimers:
                    [
                        new DurableExecutionPendingTimer(
                            id: "approval-timeout",
                            dueAtUtc: new DateTimeOffset(2026, 4, 19, 5, 0, 0, TimeSpan.Zero),
                            displayName: "Approval Timeout")
                    ],
                    pendingSignals:
                    [
                        new DurableExecutionPendingSignal(
                            id: "approval-released",
                            displayName: "Approval Released",
                            payloadType: typeof(string).FullName)
                    ]),
                _ => throw new InvalidOperationException($"Unknown hosted durable mode '{input.Mode}'.")
            });
        }
    }

    private sealed class HostingTestBehaviorContext : IBehaviorContext
    {
        private readonly List<object> replies = [];

        public HostingTestBehaviorContext(
            string behaviorId,
            string? correlationId = null,
            IReadOnlyDictionary<string, string>? metadata = null,
            IEventStore? eventStore = null)
        {
            BehaviorId = behaviorId;
            CorrelationId = correlationId;
            Metadata = metadata ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            EventStore = eventStore;
        }

        public string BehaviorId { get; }

        public string? CorrelationId { get; }

        public IReadOnlyDictionary<string, string> Metadata { get; }

        public IEventStore? EventStore { get; }

        public IReadOnlyList<object> Replies => replies.AsReadOnly();

        public Task ReplyAsync(object reply, CancellationToken cancellationToken = default)
        {
            replies.Add(reply);
            return Task.CompletedTask;
        }
    }

    private sealed class HostingRecordingEventStore : IEventStore
    {
        private readonly Lock gate = new();
        private readonly Dictionary<string, List<IDomainEvent>> streams = new(StringComparer.Ordinal);

        public Task AppendAsync(
            string streamId,
            IReadOnlyCollection<IDomainEvent> events,
            long expectedVersion,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(streamId);
            ArgumentNullException.ThrowIfNull(events);

            lock (gate)
            {
                if (!streams.TryGetValue(streamId, out var streamEvents))
                {
                    streamEvents = [];
                    streams[streamId] = streamEvents;
                }

                var actualVersion = streamEvents.Count == 0
                    ? -1
                    : streamEvents[^1].StreamVersion;
                if (actualVersion != expectedVersion)
                {
                    throw new EventStreamConcurrencyException(streamId, expectedVersion, actualVersion);
                }

                streamEvents.AddRange(events);
            }

            return Task.CompletedTask;
        }

        public Task<long> GetVersionAsync(string streamId, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(streamId);

            lock (gate)
            {
                if (!streams.TryGetValue(streamId, out var streamEvents) || streamEvents.Count == 0)
                {
                    return Task.FromResult(-1L);
                }

                return Task.FromResult(streamEvents[^1].StreamVersion);
            }
        }

        public async IAsyncEnumerable<IDomainEvent> ReadStreamAsync(
            string streamId,
            long fromVersion = 0,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            List<IDomainEvent> snapshot;

            lock (gate)
            {
                snapshot = streams.TryGetValue(streamId, out var streamEvents)
                    ? streamEvents
                        .Where(static evt => evt.StreamVersion >= 0)
                        .OrderBy(static evt => evt.StreamVersion)
                        .ToList()
                    : [];
            }

            foreach (var domainEvent in snapshot.Where(evt => evt.StreamVersion >= fromVersion))
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return domainEvent;
                await Task.Yield();
            }
        }
    }
}
