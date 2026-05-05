using Cephalon.Abstractions.Behaviors;
using Cephalon.Abstractions.EventSourcing;
using Cephalon.Behaviors.Patterns.Abstractions;
using Cephalon.Behaviors.Patterns.Publishers;
using Cephalon.Behaviors.Patterns.Registry;
using Cephalon.Behaviors.Patterns.Stores;
using Cephalon.Behaviors.Patterns.Strategies;
using Cephalon.Behaviors.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cephalon.Tests.Behaviors.Patterns;

public sealed class PatternStrategyTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // Fixtures
    // ─────────────────────────────────────────────────────────────────────────

    [AppBehavior("test.query")]
    private sealed class QueryBehavior : IAppBehavior<string, string>
    {
        public Task<string> HandleAsync(string input, IBehaviorContext context, CancellationToken cancellationToken = default)
            => Task.FromResult($"result:{input}");
    }

    [AppBehavior("test.command")]
    private sealed class CommandBehavior : IAppBehavior<string, string?>
    {
        public Task<string?> HandleAsync(string input, IBehaviorContext context, CancellationToken cancellationToken = default)
            => Task.FromResult<string?>(null);
    }

    [AppBehavior("test.saga")]
    private sealed class SagaBehavior : IAppBehavior<string, string>
    {
        public Task<string> HandleAsync(string input, IBehaviorContext context, CancellationToken cancellationToken = default)
            => Task.FromResult($"saga-state:{input}");
    }

    [AppBehavior("test.saga.throw")]
    private sealed class ThrowingSagaBehavior : IAppBehavior<string, string>
    {
        public Task<string> HandleAsync(string input, IBehaviorContext context, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("saga-fault");
    }

    [AppBehavior("test.process")]
    private sealed class ProcessBehavior : IAppBehavior<string, ProcessCheckpoint>
    {
        private readonly bool _complete;

        internal ProcessBehavior(bool complete = false) => _complete = complete;

        public Task<ProcessCheckpoint> HandleAsync(string input, IBehaviorContext context, CancellationToken cancellationToken = default)
        {
            var cp = new ProcessCheckpoint
            {
                ProcessId = "proc-1",
                CurrentStep = "step-2",
                CreatedAt = DateTimeOffset.UtcNow
            };
            return Task.FromResult(cp);
        }
    }

    [AppBehavior("test.process.complete")]
    private sealed class CompletingProcessBehavior : IAppBehavior<string, CompletionSignal>
    {
        public Task<CompletionSignal> HandleAsync(string input, IBehaviorContext context, CancellationToken cancellationToken = default)
            => Task.FromResult(new CompletionSignal());
    }

    [AppBehavior("test.choreography")]
    private sealed class ChoreographyBehavior : IAppBehavior<string, SagaChoreographyPublication>
    {
        public Task<SagaChoreographyPublication> HandleAsync(string input, IBehaviorContext context, CancellationToken cancellationToken = default)
            => Task.FromResult(new SagaChoreographyPublication(
                id: "choreography-1",
                channelId: "orders.events",
                eventType: "order-submitted",
                payload: $"{{\"value\":\"{input}\"}}",
                occurredAtUtc: DateTimeOffset.UtcNow));
    }

    [AppBehavior("test.durable")]
    private sealed class DurableBehavior : IDurableExecution<string, DurableState, string>
    {
        public DurableState CreateInitialState() => new(0);

        public string ResolveStreamId(string behaviorId, IBehaviorContext context)
            => $"{behaviorId}:{context.CorrelationId ?? "missing"}";

        public DurableState Apply(DurableState current, IDomainEvent evt)
        {
            return evt is DurableStateAdvancedEvent advanced
                ? new DurableState(current.Total + advanced.Amount)
                : current;
        }

        public Task<DurableExecutionStepResult<string>> ExecuteDurablyAsync(
            string input,
            DurableExecutionState<DurableState> execution,
            IBehaviorContext context,
            CancellationToken cancellationToken = default)
        {
            var amount = int.Parse(input, System.Globalization.CultureInfo.InvariantCulture);
            var nextTotal = execution.State.Total + amount;
            return Task.FromResult(new DurableExecutionStepResult<string>(
                output: $"durable:{nextTotal}",
                events:
                [
                    new DurableStateAdvancedEvent(
                        execution.StreamId,
                        execution.Version + 1,
                        new DateTime(2026, 4, 19, 0, 0, 0, DateTimeKind.Utc),
                        amount)
                ]));
        }
    }

    /// <summary>A completion signal that implements <see cref="IProcessCompletion"/> to indicate process end.</summary>
    private sealed class CompletionSignal : IProcessCompletion { }
    private sealed record DurableState(int Total);
    private sealed record DurableStateAdvancedEvent(
        string StreamId,
        long StreamVersion,
        DateTime OccurredAtUtc,
        int Amount) : DomainEvent(StreamId, StreamVersion, OccurredAtUtc);

    private static BehaviorTopologyDescriptor MakeDescriptor(string id, string pattern = "direct") =>
        new(id, pattern, ["in-memory"]);

    private static BehaviorExecutionContext MakeContext<TBehavior>(
        TBehavior behavior,
        object input,
        IBehaviorContext behaviorContext,
        string pattern = "direct")
        where TBehavior : class
    {
        var descriptor = MakeDescriptor(typeof(TBehavior).Name, pattern);
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

    private static TestBehaviorContext MakeCtx(
        string behaviorId = "test",
        Dictionary<string, string>? metadata = null,
        string? correlationId = null)
        => new(behaviorId, metadata: metadata, correlationId: correlationId);

    // ─────────────────────────────────────────────────────────────────────────
    // CqrsExecutionStrategy
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CqrsStrategy_WithOutput_Returns200()
    {
        var strategy = new CqrsExecutionStrategy();
        var ctx = MakeContext(new QueryBehavior(), "world", MakeCtx(), "cqrs");

        var result = await strategy.ExecuteAsync(ctx);

        Assert.Equal(200, result.HttpStatusCode);
        Assert.NotNull(result.Output);
        Assert.False(result.IsFireAndForget);
    }

    [Fact]
    public async Task CqrsStrategy_WithNullOutput_Returns202()
    {
        var strategy = new CqrsExecutionStrategy();
        var ctx = MakeContext(new CommandBehavior(), "cmd", MakeCtx(), "cqrs");

        var result = await strategy.ExecuteAsync(ctx);

        Assert.Equal(202, result.HttpStatusCode);
        Assert.Null(result.Output);
        Assert.False(result.IsFireAndForget);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // EventDrivenExecutionStrategy
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task EventDrivenStrategy_AlwaysReturns202_IsFireAndForget()
    {
        var strategy = new EventDrivenExecutionStrategy(NullLogger<EventDrivenExecutionStrategy>.Instance);
        var ctx = MakeContext(new QueryBehavior(), "event", MakeCtx(), "event-driven");

        var result = await strategy.ExecuteAsync(ctx);

        Assert.Equal(202, result.HttpStatusCode);
        Assert.True(result.IsFireAndForget);
        Assert.Null(result.Output);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SagaExecutionStrategy
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SagaStrategy_LoadsSavesState_ViaStore()
    {
        var store = new InMemorySagaStateStore();
        var strategy = new SagaExecutionStrategy(store, NullLogger<SagaExecutionStrategy>.Instance);
        var ctx = MakeContext(new SagaBehavior(), "input", MakeCtx("test.saga", correlationId: "saga-abc"), "saga-step");

        var result = await strategy.ExecuteAsync(ctx);

        Assert.Equal(200, result.HttpStatusCode);
        Assert.NotNull(result.Output);

        // State should have been saved.
        var saved = await store.GetAsync<string>("saga-abc");
        Assert.NotNull(saved);
        Assert.Contains("saga-state", saved);
    }

    [Fact]
    public async Task SagaStrategy_OnException_DoesNotSaveState()
    {
        var store = new InMemorySagaStateStore();
        var strategy = new SagaExecutionStrategy(store, NullLogger<SagaExecutionStrategy>.Instance);
        var ctx = MakeContext(new ThrowingSagaBehavior(), "input", MakeCtx("test.saga.throw", correlationId: "saga-fault"), "saga-step");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => strategy.ExecuteAsync(ctx));

        // State must NOT have been saved.
        var saved = await store.GetAsync<string>("saga-fault");
        Assert.Null(saved);
    }

    [Fact]
    public async Task ChoreographySagaStrategy_PublishesReturnedPublication()
    {
        var publisher = new InMemorySagaChoreographyPublisher();
        var strategy = new ChoreographySagaExecutionStrategy(
            publisher,
            NullLogger<ChoreographySagaExecutionStrategy>.Instance);
        var ctx = MakeContext(new ChoreographyBehavior(), "input", MakeCtx("test.choreography", correlationId: "choreo-1"), "saga-choreography");

        var result = await strategy.ExecuteAsync(ctx);

        Assert.Equal(202, result.HttpStatusCode);
        var published = Assert.Single(publisher.PublishedPublications);
        Assert.Equal("choreo-1", published.CorrelationId);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ProcessManagerExecutionStrategy
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ProcessManagerStrategy_SavesCheckpoint()
    {
        var store = new InMemoryProcessCheckpointStore();
        var strategy = new ProcessManagerExecutionStrategy(store, NullLogger<ProcessManagerExecutionStrategy>.Instance);
        var ctx = MakeContext(new ProcessBehavior(), "start", MakeCtx("test.process", correlationId: "proc-1"), "process-manager");

        var result = await strategy.ExecuteAsync(ctx);

        Assert.Equal(200, result.HttpStatusCode);

        var saved = await store.GetAsync("proc-1");
        Assert.NotNull(saved);
        Assert.Equal("step-2", saved.CurrentStep);
    }

    [Fact]
    public async Task ProcessManagerStrategy_OnCompletion_DeletesCheckpoint()
    {
        var store = new InMemoryProcessCheckpointStore();
        // Pre-seed a checkpoint.
        await store.SaveAsync("proc-complete", new ProcessCheckpoint
        {
            ProcessId = "proc-complete",
            CurrentStep = "step-1",
            CreatedAt = DateTimeOffset.UtcNow
        });

        var strategy = new ProcessManagerExecutionStrategy(store, NullLogger<ProcessManagerExecutionStrategy>.Instance);
        var ctx = MakeContext(new CompletingProcessBehavior(), "finish", MakeCtx("test.process.complete", correlationId: "proc-complete"), "process-manager");

        await strategy.ExecuteAsync(ctx);

        var deleted = await store.GetAsync("proc-complete");
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DurableExecutionStrategy_AppendsEventsAndReturnsReplayAwareOutput()
    {
        var eventStore = new RecordingEventStore();
        await eventStore.AppendAsync(
            "DurableBehavior:durable-1",
            [
                new DurableStateAdvancedEvent(
                    "DurableBehavior:durable-1",
                    0,
                    new DateTime(2026, 4, 19, 0, 0, 0, DateTimeKind.Utc),
                    2)
            ],
            -1);
        var strategy = DurableExecutionStrategy.CreateWithSlots(
            runtimeStateCatalog: null,
            [DurableExecutionSlot.For<DurableBehavior, string, DurableState, string>()]);
        var ctx = MakeContext(
            new DurableBehavior(),
            "3",
            new TestBehaviorContext("test.durable", correlationId: "durable-1", eventStore: eventStore),
            "durable-execution");

        var result = await strategy.ExecuteAsync(ctx);

        Assert.Equal(200, result.HttpStatusCode);
        Assert.Equal("durable:5", result.Output);
        Assert.Equal(1, await eventStore.GetVersionAsync("DurableBehavior:durable-1"));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DirectExecutionStrategy
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DirectStrategy_WithOutput_Returns200()
    {
        var strategy = new DirectExecutionStrategy();
        var ctx = MakeContext(new QueryBehavior(), "direct", MakeCtx(), "direct");

        var result = await strategy.ExecuteAsync(ctx);

        Assert.Equal(200, result.HttpStatusCode);
        Assert.NotNull(result.Output);
        Assert.False(result.IsFireAndForget);
    }

    [Fact]
    public async Task DirectStrategy_WithNullOutput_Returns204()
    {
        var strategy = new DirectExecutionStrategy();
        var ctx = MakeContext(new CommandBehavior(), "cmd", MakeCtx(), "direct");

        var result = await strategy.ExecuteAsync(ctx);

        Assert.Equal(204, result.HttpStatusCode);
        Assert.Null(result.Output);
        Assert.False(result.IsFireAndForget);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ExecutionStrategyRegistry
    // ─────────────────────────────────────────────────────────────────────────

    private static ExecutionStrategyRegistry BuildRegistry()
    {
        var store = new InMemorySagaStateStore();
        var choreographyPublisher = new InMemorySagaChoreographyPublisher();
        var cpStore = new InMemoryProcessCheckpointStore();
        return new ExecutionStrategyRegistry(new IBehaviorExecutionStrategy[]
        {
            new CqrsExecutionStrategy(),
            new EventDrivenExecutionStrategy(NullLogger<EventDrivenExecutionStrategy>.Instance),
            new SagaExecutionStrategy(store, NullLogger<SagaExecutionStrategy>.Instance),
            new ChoreographySagaExecutionStrategy(choreographyPublisher, NullLogger<ChoreographySagaExecutionStrategy>.Instance),
            new ProcessManagerExecutionStrategy(cpStore, NullLogger<ProcessManagerExecutionStrategy>.Instance),
            new DurableExecutionStrategy(),
            new DirectExecutionStrategy()
        });
    }

    [Fact]
    public void Registry_GetStrategy_ByPattern_ReturnsCorrect()
    {
        var registry = BuildRegistry();

        var cqrs = registry.GetStrategy("cqrs");
        var eventDriven = registry.GetStrategy("event-driven");
        var saga = registry.GetStrategy("saga-step");
        var choreography = registry.GetStrategy("saga-choreography");
        var pm = registry.GetStrategy("process-manager");
        var durable = registry.GetStrategy("durable-execution");
        var direct = registry.GetStrategy("direct");

        Assert.IsType<CqrsExecutionStrategy>(cqrs);
        Assert.IsType<EventDrivenExecutionStrategy>(eventDriven);
        Assert.IsType<SagaExecutionStrategy>(saga);
        Assert.IsType<ChoreographySagaExecutionStrategy>(choreography);
        Assert.IsType<ProcessManagerExecutionStrategy>(pm);
        Assert.IsType<DurableExecutionStrategy>(durable);
        Assert.IsType<DirectExecutionStrategy>(direct);
    }

    [Fact]
    public void Registry_AllStrategies_HaveUniquePatterns()
    {
        var registry = BuildRegistry();
        var patterns = registry.All.Select(s => s.Pattern).ToList();

        Assert.Equal(patterns.Count, patterns.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(7, patterns.Count);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // InMemorySagaStateStore
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task InMemorySagaStore_GetSaveDelete_RoundTrip()
    {
        var store = new InMemorySagaStateStore();

        // Get non-existent.
        var notFound = await store.GetAsync<string>("s1");
        Assert.Null(notFound);

        // Save and retrieve.
        await store.SaveAsync("s1", "my-state");
        var found = await store.GetAsync<string>("s1");
        Assert.Equal("my-state", found);

        // Delete and verify gone.
        await store.DeleteAsync("s1");
        var afterDelete = await store.GetAsync<string>("s1");
        Assert.Null(afterDelete);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // InMemoryProcessCheckpointStore
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task InMemoryCheckpointStore_GetSaveDelete_RoundTrip()
    {
        var store = new InMemoryProcessCheckpointStore();

        // Get non-existent.
        var notFound = await store.GetAsync("p1");
        Assert.Null(notFound);

        var cp = new ProcessCheckpoint
        {
            ProcessId = "p1",
            CurrentStep = "step-A",
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Save and retrieve.
        await store.SaveAsync("p1", cp);
        var found = await store.GetAsync("p1");
        Assert.NotNull(found);
        Assert.Equal("step-A", found.CurrentStep);

        // Delete and verify gone.
        await store.DeleteAsync("p1");
        var afterDelete = await store.GetAsync("p1");
        Assert.Null(afterDelete);
    }

    private sealed class RecordingEventStore : IEventStore
    {
        private readonly Lock _gate = new();
        private readonly Dictionary<string, List<IDomainEvent>> _streams = new(StringComparer.Ordinal);

        public Task AppendAsync(
            string streamId,
            IReadOnlyCollection<IDomainEvent> events,
            long expectedVersion,
            CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                if (!_streams.TryGetValue(streamId, out var streamEvents))
                {
                    streamEvents = [];
                    _streams[streamId] = streamEvents;
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
            lock (_gate)
            {
                if (!_streams.TryGetValue(streamId, out var streamEvents) || streamEvents.Count == 0)
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

            lock (_gate)
            {
                snapshot = _streams.TryGetValue(streamId, out var streamEvents)
                    ? streamEvents.OrderBy(static evt => evt.StreamVersion).ToList()
                    : [];
            }

            foreach (var domainEvent in snapshot.Where(evt => evt.StreamVersion >= fromVersion))
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return domainEvent;
                await Task.CompletedTask;
            }
        }
    }
}
