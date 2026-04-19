using Cephalon.Abstractions.Behaviors;
using Cephalon.Abstractions.EventSourcing;
using Cephalon.Abstractions.Execution;
using Cephalon.Abstractions.Modules;
using Cephalon.Behaviors.Hosting;
using Cephalon.Behaviors.Modules;
using Cephalon.Behaviors.Patterns.Abstractions;
using Cephalon.Behaviors.Patterns.Hosting;
using Cephalon.Behaviors.Patterns.Strategies;
using Cephalon.Behaviors.Services;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Runtime;
using Cephalon.Tests.Behaviors;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Composition;

public sealed class DurableExecutionRuntimeStateCatalogTests
{
    [Fact]
    public async Task RuntimeStateCatalogTracksDurableStreamOutcomePostureAcrossSnapshotFiltersCoordinationAndFailures()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(blueprint: "ModularMonolith"));
            engine.AddBehaviors(
                configureOptions: options => options.AutoRegister = false,
                configure: behaviors => behaviors.AddBehaviorPatterns());
            engine.AddModule(new DurableExecutionRuntimeStateModule());
        });

        using var provider = services.BuildServiceProvider();
        var strategy = provider.GetServices<IBehaviorExecutionStrategy>()
            .OfType<DurableExecutionStrategy>()
            .Single();
        var catalog = provider.GetRequiredService<IDurableExecutionRuntimeStateCatalog>();
        var snapshotProvider = provider.GetRequiredService<IRuntimeIntrospectionSnapshotProvider>();
        var eventStore = new RecordingEventStore();

        await strategy.ExecuteAsync(MakeContext(
            behaviorId: "tests.workflows.runtime.approvals.execute",
            behavior: new ObservedApprovalWorkflowBehavior(),
            input: new ObservedApprovalWorkflowInput("success", 3),
            behaviorContext: new TestBehaviorContext(
                "tests.workflows.runtime.approvals.execute",
                correlationId: "corr-success",
                eventStore: eventStore,
                metadata: new Dictionary<string, string>
                {
                    ["tenantId"] = "tenant-42"
                })));

        await strategy.ExecuteAsync(MakeContext(
            behaviorId: "tests.workflows.runtime.approvals.execute",
            behavior: new ObservedApprovalWorkflowBehavior(),
            input: new ObservedApprovalWorkflowInput("queue", 2),
            behaviorContext: new TestBehaviorContext(
                "tests.workflows.runtime.approvals.execute",
                correlationId: "corr-queue",
                eventStore: eventStore)));

        await strategy.ExecuteAsync(MakeContext(
            behaviorId: "tests.workflows.runtime.approvals.execute",
            behavior: new ObservedApprovalWorkflowBehavior(),
            input: new ObservedApprovalWorkflowInput("wait", 0),
            behaviorContext: new TestBehaviorContext(
                "tests.workflows.runtime.approvals.execute",
                correlationId: "corr-wait",
                eventStore: eventStore)));

        await strategy.ExecuteAsync(MakeContext(
            behaviorId: "tests.workflows.runtime.approvals.execute",
            behavior: new ObservedApprovalWorkflowBehavior(),
            input: new ObservedApprovalWorkflowInput("complete", 0),
            behaviorContext: new TestBehaviorContext(
                "tests.workflows.runtime.approvals.execute",
                correlationId: "corr-complete",
                eventStore: eventStore)));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => strategy.ExecuteAsync(MakeContext(
            behaviorId: "tests.workflows.runtime.approvals.execute",
            behavior: new ObservedApprovalWorkflowBehavior(),
            input: new ObservedApprovalWorkflowInput("fail", 1),
            behaviorContext: new TestBehaviorContext(
                "tests.workflows.runtime.approvals.execute",
            correlationId: "corr-fail",
            eventStore: eventStore))));

        Assert.Contains("stream version", exception.Message, StringComparison.OrdinalIgnoreCase);

        var behaviorStates = catalog.GetByBehaviorId("tests.workflows.runtime.approvals.execute");
        Assert.Equal(5, behaviorStates.Count);
        Assert.Equal(5, catalog.GetBySourceModule("tests.durable-state-owner").Count);
        Assert.Equal(5, catalog.GetByTransportId("rabbitmq").Count);
        Assert.Single(catalog.GetWithPendingTimers());
        Assert.Single(catalog.GetWithPendingSignals());
        Assert.Single(catalog.GetByPendingTimerId("approval-timeout"));
        Assert.Single(catalog.GetByPendingSignalId("approval-released"));

        Assert.True(catalog.TryGetByStreamId(
            "tests.workflows.runtime.approvals.execute:corr-success",
            out var successState));
        Assert.NotNull(successState);
        Assert.Equal("tests.workflows.runtime.approvals.execute", successState!.BehaviorId);
        Assert.Equal("tests.durable-state-owner", successState.SourceModuleId);
        Assert.Equal(["in-memory", "rabbitmq"], successState.TransportIds);
        Assert.Equal("succeeded", successState.LastOutcome);
        Assert.Equal("append", successState.LastStage);
        Assert.Equal(200, successState.LastHttpStatusCode);
        Assert.Equal(-1, successState.LastReplayedVersion);
        Assert.Equal(0, successState.LastKnownVersion);
        Assert.Equal(1, successState.LastAppendedEventCount);
        Assert.True(successState.LastStepProducedOutput);
        Assert.False(successState.LastStepCompleted);
        Assert.Equal(1, successState.StartedCount);
        Assert.Equal(1, successState.SucceededCount);
        Assert.Equal(0, successState.ContinuationCount);
        Assert.Equal(0, successState.CompletedCount);
        Assert.Equal(0, successState.FailedCount);
        Assert.Equal(2, successState.TotalReports);
        Assert.False(successState.ContinuationPending);
        Assert.False(successState.IsFailed);
        Assert.Equal("corr-success", successState.Metadata["correlationId"]);
        Assert.Equal("tenant-42", successState.Metadata["tenantId"]);

        var continuationState = catalog.GetByStreamId("tests.workflows.runtime.approvals.execute:corr-queue");
        Assert.NotNull(continuationState);
        Assert.Equal("continuation-staged", continuationState!.LastOutcome);
        Assert.Equal("append", continuationState.LastStage);
        Assert.Equal(202, continuationState.LastHttpStatusCode);
        Assert.Equal(1, continuationState.LastAppendedEventCount);
        Assert.False(continuationState.LastStepProducedOutput);
        Assert.False(continuationState.LastStepCompleted);
        Assert.Equal(1, continuationState.StartedCount);
        Assert.Equal(0, continuationState.SucceededCount);
        Assert.Equal(1, continuationState.ContinuationCount);
        Assert.True(continuationState.ContinuationPending);
        Assert.True(continuationState.CoordinationPending);

        var waitingState = catalog.GetByStreamId("tests.workflows.runtime.approvals.execute:corr-wait");
        Assert.NotNull(waitingState);
        Assert.Equal("waiting", waitingState!.LastOutcome);
        Assert.Equal("execute", waitingState.LastStage);
        Assert.Equal(202, waitingState.LastHttpStatusCode);
        Assert.Equal(0, waitingState.LastAppendedEventCount);
        Assert.False(waitingState.LastStepProducedOutput);
        Assert.False(waitingState.LastStepCompleted);
        Assert.Equal(1, waitingState.StartedCount);
        Assert.Equal(0, waitingState.SucceededCount);
        Assert.Equal(1, waitingState.ContinuationCount);
        Assert.Equal(0, waitingState.CompletedCount);
        Assert.Equal(0, waitingState.FailedCount);
        Assert.Equal(2, waitingState.TotalReports);
        Assert.False(waitingState.ContinuationPending);
        Assert.True(waitingState.HasPendingTimers);
        Assert.True(waitingState.HasPendingSignals);
        Assert.True(waitingState.CoordinationPending);
        Assert.Equal(
            new DateTimeOffset(2026, 4, 19, 4, 0, 0, TimeSpan.Zero),
            waitingState.NextTimerDueAtUtc);
        var timer = Assert.Single(waitingState.PendingTimers);
        Assert.Equal("approval-timeout", timer.Id);
        Assert.Equal("Approval Timeout", timer.DisplayName);
        Assert.Equal("approval", timer.Metadata["lane"]);
        var signal = Assert.Single(waitingState.PendingSignals);
        Assert.Equal("approval-released", signal.Id);
        Assert.Equal("Approval Released", signal.DisplayName);
        Assert.Equal("System.String", signal.PayloadType);
        Assert.Equal("approval", signal.Metadata["lane"]);

        var completedState = catalog.GetByStreamId("tests.workflows.runtime.approvals.execute:corr-complete");
        Assert.NotNull(completedState);
        Assert.Equal("completed", completedState!.LastOutcome);
        Assert.Equal("execute", completedState.LastStage);
        Assert.Equal(204, completedState.LastHttpStatusCode);
        Assert.Equal(0, completedState.LastAppendedEventCount);
        Assert.False(completedState.LastStepProducedOutput);
        Assert.True(completedState.LastStepCompleted);
        Assert.Equal(1, completedState.StartedCount);
        Assert.Equal(0, completedState.SucceededCount);
        Assert.Equal(0, completedState.ContinuationCount);
        Assert.Equal(1, completedState.CompletedCount);
        Assert.False(completedState.IsFailed);
        Assert.False(completedState.CoordinationPending);

        var failedState = catalog.GetByStreamId("tests.workflows.runtime.approvals.execute:corr-fail");
        Assert.NotNull(failedState);
        Assert.Equal("failed", failedState!.LastOutcome);
        Assert.Equal("execute", failedState.LastStage);
        Assert.Null(failedState.LastHttpStatusCode);
        Assert.Equal(-1, failedState.LastReplayedVersion);
        Assert.Equal(-1, failedState.LastKnownVersion);
        Assert.Equal(0, failedState.LastAppendedEventCount);
        Assert.False(failedState.LastStepProducedOutput);
        Assert.False(failedState.LastStepCompleted);
        Assert.Equal(1, failedState.StartedCount);
        Assert.Equal(0, failedState.SucceededCount);
        Assert.Equal(0, failedState.ContinuationCount);
        Assert.Equal(0, failedState.CompletedCount);
        Assert.Equal(1, failedState.FailedCount);
        Assert.Equal(2, failedState.TotalReports);
        Assert.True(failedState.IsFailed);
        Assert.Contains("stream version", failedState.LastError, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("corr-fail", failedState.Metadata["correlationId"]);
        Assert.Contains("InvalidOperationException", failedState.Metadata["exceptionType"], StringComparison.Ordinal);

        var snapshot = snapshotProvider.CreateSnapshot();
        Assert.Equal(5, snapshot.DurableExecutionStates.Count);
        Assert.Contains(
            snapshot.DurableExecutionStates,
            state => string.Equals(
                state.StreamId,
                "tests.workflows.runtime.approvals.execute:corr-success",
                StringComparison.Ordinal));
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
            transportIds: ["in-memory", "rabbitmq"],
            eventSourcingEnabled: true,
            sourceModuleId: "tests.durable-state-owner");
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

    private sealed class DurableExecutionRuntimeStateModule : BehaviorModuleBase
    {
        private static readonly ModuleDescriptor DescriptorInstance = new(
            id: "tests.durable-state-owner",
            displayName: "Durable State Owner",
            description: "Owns one durable workflow for runtime state catalog tests.",
            version: "1.0.0");

        public override ModuleDescriptor Descriptor => DescriptorInstance;

        public override void ConfigureBehaviors(IBehaviorModuleBuilder behaviors)
        {
            behaviors.Add<ObservedApprovalWorkflowBehavior>(topology => topology
                .AsDurableExecution()
                .ViaInMemory()
                .ViaRabbitMq()
                .RequireFeatureFlag("host.approvals-live")
                .WithApiSurface("runtime-approvals", "execute")
                .WithMetadata("lane", "approval")
                .WithOptions(options => options.EventSourcingEnabled = true));
        }
    }

    private sealed record ObservedApprovalWorkflowInput(string Mode, int Amount);

    private sealed record ObservedApprovalWorkflowState(int Total, bool IsCompleted);

    private sealed record ObservedApprovalAdvancedEvent(
        string StreamId,
        long StreamVersion,
        DateTime OccurredAtUtc,
        int Amount,
        bool IsCompleted) : DomainEvent(StreamId, StreamVersion, OccurredAtUtc);

    [AppBehavior("tests.workflows.runtime.approvals.execute")]
    private sealed class ObservedApprovalWorkflowBehavior : IDurableExecution<ObservedApprovalWorkflowInput, ObservedApprovalWorkflowState, string?>
    {
        public ObservedApprovalWorkflowState CreateInitialState() => new(0, false);

        public string ResolveStreamId(string behaviorId, IBehaviorContext context)
            => $"{behaviorId}:{context.CorrelationId ?? throw new InvalidOperationException("CorrelationId is required.")}";

        public ObservedApprovalWorkflowState Apply(
            ObservedApprovalWorkflowState current,
            IDomainEvent domainEvent)
        {
            return domainEvent is ObservedApprovalAdvancedEvent advanced
                ? current with
                {
                    Total = current.Total + advanced.Amount,
                    IsCompleted = current.IsCompleted || advanced.IsCompleted
                }
                : current;
        }

        public Task<DurableExecutionStepResult<string?>> ExecuteDurablyAsync(
            ObservedApprovalWorkflowInput input,
            DurableExecutionState<ObservedApprovalWorkflowState> execution,
            IBehaviorContext context,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(input.Mode switch
            {
                "success" => new DurableExecutionStepResult<string?>(
                    output: $"queued:{execution.State.Total + input.Amount}",
                    events:
                    [
                        new ObservedApprovalAdvancedEvent(
                            execution.StreamId,
                            execution.Version + 1,
                            new DateTime(2026, 4, 19, 1, 0, 0, DateTimeKind.Utc),
                            input.Amount,
                            IsCompleted: false)
                    ],
                    isCompleted: false),
                "queue" => new DurableExecutionStepResult<string?>(
                    events:
                    [
                        new ObservedApprovalAdvancedEvent(
                            execution.StreamId,
                            execution.Version + 1,
                            new DateTime(2026, 4, 19, 1, 1, 0, DateTimeKind.Utc),
                            input.Amount,
                            IsCompleted: false)
                    ],
                    isCompleted: false),
                "wait" => new DurableExecutionStepResult<string?>(
                    output: null,
                    events: [],
                    isCompleted: false,
                    pendingTimers:
                    [
                        new DurableExecutionPendingTimer(
                            id: "approval-timeout",
                            dueAtUtc: new DateTimeOffset(2026, 4, 19, 4, 0, 0, TimeSpan.Zero),
                            displayName: "Approval Timeout",
                            description: "Escalate the approval when the timer elapses.",
                            metadata: new Dictionary<string, string>
                            {
                                ["lane"] = "approval"
                            })
                    ],
                    pendingSignals:
                    [
                        new DurableExecutionPendingSignal(
                            id: "approval-released",
                            displayName: "Approval Released",
                            description: "Waits for the external release signal.",
                            payloadType: typeof(string).FullName,
                            metadata: new Dictionary<string, string>
                            {
                                ["lane"] = "approval"
                            })
                    ]),
                "complete" => new DurableExecutionStepResult<string?>(
                    output: null,
                    events: [],
                    isCompleted: true),
                "fail" => new DurableExecutionStepResult<string?>(
                    events:
                    [
                        new ObservedApprovalAdvancedEvent(
                            execution.StreamId,
                            execution.Version + 2,
                            new DateTime(2026, 4, 19, 1, 2, 0, DateTimeKind.Utc),
                            input.Amount,
                            IsCompleted: false)
                    ],
                    isCompleted: false),
                _ => throw new InvalidOperationException($"Unknown durable runtime mode '{input.Mode}'.")
            });
        }
    }

    private sealed class RecordingEventStore : IEventStore
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
