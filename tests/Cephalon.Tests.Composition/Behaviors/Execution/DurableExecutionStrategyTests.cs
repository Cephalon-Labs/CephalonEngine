using Cephalon.Abstractions.Behaviors;
using Cephalon.Abstractions.EventSourcing;
using Cephalon.Behaviors.Patterns.Abstractions;
using Cephalon.Behaviors.Patterns.Hosting;
using Cephalon.Behaviors.Patterns.Strategies;
using Cephalon.Behaviors.Services;
using Cephalon.Tests.Behaviors;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Behaviors.Execution;

/// <summary>Tests for <see cref="DurableExecutionStrategy" />.</summary>
public sealed class DurableExecutionStrategyTests
{
    [AppBehavior("durable.increment")]
    private sealed class IncrementWorkflow : IDurableExecution<IncrementInput, CounterState, string>
    {
        public CounterState CreateInitialState() => new(0, false);

        public string ResolveStreamId(string behaviorId, IBehaviorContext context)
            => $"{behaviorId}:{context.CorrelationId ?? throw new InvalidOperationException("CorrelationId is required.")}";

        public CounterState Apply(CounterState current, IDomainEvent evt)
        {
            return evt switch
            {
                CounterAdvancedEvent advanced => current with
                {
                    Total = current.Total + advanced.Amount,
                    IsCompleted = current.IsCompleted || advanced.IsCompleted
                },
                _ => current
            };
        }

        public Task<DurableExecutionStepResult<string>> ExecuteDurablyAsync(
            IncrementInput input,
            DurableExecutionState<CounterState> execution,
            IBehaviorContext context,
            CancellationToken cancellationToken = default)
        {
            var nextTotal = execution.State.Total + input.Amount;
            return Task.FromResult(new DurableExecutionStepResult<string>(
                output: $"total:{nextTotal}",
                events:
                [
                    new CounterAdvancedEvent(
                        execution.StreamId,
                        execution.Version + 1,
                        new DateTime(2026, 4, 19, 0, 0, 0, DateTimeKind.Utc),
                        input.Amount,
                        IsCompleted: false)
                ]));
        }
    }

    [AppBehavior("durable.queue")]
    private sealed class QueueOnlyWorkflow : IDurableExecution<IncrementInput, CounterState, string?>
    {
        public CounterState CreateInitialState() => new(0, false);

        public string ResolveStreamId(string behaviorId, IBehaviorContext context)
            => $"{behaviorId}:{context.CorrelationId ?? throw new InvalidOperationException("CorrelationId is required.")}";

        public CounterState Apply(CounterState current, IDomainEvent evt)
        {
            return evt is CounterAdvancedEvent advanced
                ? current with
                {
                    Total = current.Total + advanced.Amount,
                    IsCompleted = current.IsCompleted || advanced.IsCompleted
                }
                : current;
        }

        public Task<DurableExecutionStepResult<string?>> ExecuteDurablyAsync(
            IncrementInput input,
            DurableExecutionState<CounterState> execution,
            IBehaviorContext context,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new DurableExecutionStepResult<string?>(
                events:
                [
                    new CounterAdvancedEvent(
                        execution.StreamId,
                        execution.Version + 1,
                        new DateTime(2026, 4, 19, 0, 1, 0, DateTimeKind.Utc),
                        input.Amount,
                        IsCompleted: false)
                ]));
        }
    }

    [AppBehavior("durable.complete")]
    private sealed class CompletionWorkflow : IDurableExecution<IncrementInput, CounterState, string?>
    {
        public CounterState CreateInitialState() => new(0, false);

        public string ResolveStreamId(string behaviorId, IBehaviorContext context)
            => $"{behaviorId}:{context.CorrelationId ?? throw new InvalidOperationException("CorrelationId is required.")}";

        public CounterState Apply(CounterState current, IDomainEvent evt)
        {
            return evt is CounterAdvancedEvent advanced
                ? current with
                {
                    Total = current.Total + advanced.Amount,
                    IsCompleted = current.IsCompleted || advanced.IsCompleted
                }
                : current;
        }

        public Task<DurableExecutionStepResult<string?>> ExecuteDurablyAsync(
            IncrementInput input,
            DurableExecutionState<CounterState> execution,
            IBehaviorContext context,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new DurableExecutionStepResult<string?>(
                output: null,
                events: [],
                isCompleted: true));
        }
    }

    [AppBehavior("durable.invalid")]
    private sealed class InvalidVersionWorkflow : IDurableExecution<IncrementInput, CounterState, string?>
    {
        public CounterState CreateInitialState() => new(0, false);

        public string ResolveStreamId(string behaviorId, IBehaviorContext context)
            => $"{behaviorId}:{context.CorrelationId ?? throw new InvalidOperationException("CorrelationId is required.")}";

        public CounterState Apply(CounterState current, IDomainEvent evt)
        {
            return evt is CounterAdvancedEvent advanced
                ? current with { Total = current.Total + advanced.Amount }
                : current;
        }

        public Task<DurableExecutionStepResult<string?>> ExecuteDurablyAsync(
            IncrementInput input,
            DurableExecutionState<CounterState> execution,
            IBehaviorContext context,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new DurableExecutionStepResult<string?>(
                events:
                [
                    new CounterAdvancedEvent(
                        execution.StreamId,
                        execution.Version + 2,
                        new DateTime(2026, 4, 19, 0, 2, 0, DateTimeKind.Utc),
                        input.Amount,
                        IsCompleted: false)
                ]));
        }
    }

    private sealed record IncrementInput(int Amount);

    private sealed record CounterState(int Total, bool IsCompleted);

    private sealed record CounterAdvancedEvent(
        string StreamId,
        long StreamVersion,
        DateTime OccurredAtUtc,
        int Amount,
        bool IsCompleted) : DomainEvent(StreamId, StreamVersion, OccurredAtUtc);

    private static BehaviorExecutionContext MakeContext<TBehavior>(
        TBehavior behavior,
        object input,
        IBehaviorContext behaviorContext)
        where TBehavior : class
    {
        var descriptor = new BehaviorTopologyDescriptor(typeof(TBehavior).Name, "durable-execution", ["in-memory"]);
        var slot = BehaviorExecutionTestSlots.For(behavior);
        return new BehaviorExecutionContext
        {
            Descriptor = descriptor,
            BehaviorInstance = behavior,
            Slot = slot,
            Input = input,
            BehaviorContext = behaviorContext
        };
    }

    private static DurableExecutionStrategy CreateStrategy<TBehavior, TInput, TState, TOutput>()
        where TBehavior : class, IDurableExecution<TInput, TState, TOutput>
    {
        return DurableExecutionStrategy.CreateWithSlots(
            runtimeStateCatalog: null,
            [DurableExecutionSlot.For<TBehavior, TInput, TState, TOutput>()]);
    }

    [Fact]
    public async Task DurableExecutionStrategy_ReplaysExistingState_AppendsEventsAndReturnsOutput()
    {
        var eventStore = new RecordingEventStore();
        await eventStore.AppendAsync(
            "IncrementWorkflow:corr-123",
            [
                new CounterAdvancedEvent(
                    "IncrementWorkflow:corr-123",
                    0,
                    new DateTime(2026, 4, 19, 0, 0, 0, DateTimeKind.Utc),
                    2,
                    IsCompleted: false)
            ],
            -1);
        var strategy = CreateStrategy<IncrementWorkflow, IncrementInput, CounterState, string>();
        var context = MakeContext(
            new IncrementWorkflow(),
            new IncrementInput(3),
            new TestBehaviorContext(
                "durable.increment",
                correlationId: "corr-123",
                eventStore: eventStore));

        var result = await strategy.ExecuteAsync(context);

        Assert.Equal(200, result.HttpStatusCode);
        Assert.Equal("total:5", result.Output);
        Assert.Equal(1, await eventStore.GetVersionAsync("IncrementWorkflow:corr-123"));

        var replayed = new List<IDomainEvent>();
        await foreach (var evt in eventStore.ReadStreamAsync("IncrementWorkflow:corr-123"))
        {
            replayed.Add(evt);
        }

        var last = Assert.IsType<CounterAdvancedEvent>(Assert.Single(replayed.Skip(1)));
        Assert.Equal(3, last.Amount);
        Assert.Equal(1, last.StreamVersion);
    }

    [Fact]
    public async Task DurableExecutionStrategy_UsesGeneratedDurableExecutionSlot()
    {
        var eventStore = new RecordingEventStore();
        var services = new ServiceCollection();
        var builder = new BehaviorCollectionBuilder(services);
        services.AddSingleton(new BehaviorImplementationDescriptor(nameof(IncrementWorkflow), typeof(IncrementWorkflow)));
        services.AddSingleton(DurableExecutionSlot.For<IncrementWorkflow, IncrementInput, CounterState, string>());
        services.AddSingleton(DurableExecutionSlot.For<IncrementWorkflow, IncrementInput, CounterState, string>());
        services.AddSingleton<IBehaviorCatalog>(
            new BehaviorCatalog(
            [
                new StaticBehaviorContributor(
                    new BehaviorTopologyDescriptor(nameof(IncrementWorkflow), "durable-execution", ["in-memory"]))
            ]));
        services.AddLogging();
        builder.AddBehaviorPatterns();
        using var provider = services.BuildServiceProvider();
        var strategy = provider.GetServices<IBehaviorExecutionStrategy>()
            .OfType<DurableExecutionStrategy>()
            .Single();
        var context = MakeContext(
            new IncrementWorkflow(),
            new IncrementInput(3),
            new TestBehaviorContext(
                "durable.increment",
                correlationId: "corr-generated",
                eventStore: eventStore));

        var result = await strategy.ExecuteAsync(context);

        Assert.Equal(200, result.HttpStatusCode);
        Assert.Equal("total:3", result.Output);
        Assert.Equal(0, await eventStore.GetVersionAsync("IncrementWorkflow:corr-generated"));
    }

    [Fact]
    public async Task DurableExecutionStrategy_WhenOnlyEventsRemain_ReturnsAccepted()
    {
        var eventStore = new RecordingEventStore();
        var strategy = CreateStrategy<QueueOnlyWorkflow, IncrementInput, CounterState, string?>();
        var context = MakeContext(
            new QueueOnlyWorkflow(),
            new IncrementInput(4),
            new TestBehaviorContext(
                "durable.queue",
                correlationId: "corr-queue",
                eventStore: eventStore));

        var result = await strategy.ExecuteAsync(context);

        Assert.Equal(202, result.HttpStatusCode);
        Assert.Null(result.Output);
        Assert.Equal(0, await eventStore.GetVersionAsync("QueueOnlyWorkflow:corr-queue"));
    }

    [Fact]
    public async Task DurableExecutionStrategy_WhenCompletedWithoutOutput_ReturnsNoContent()
    {
        var eventStore = new RecordingEventStore();
        var strategy = CreateStrategy<CompletionWorkflow, IncrementInput, CounterState, string?>();
        var context = MakeContext(
            new CompletionWorkflow(),
            new IncrementInput(0),
            new TestBehaviorContext(
                "durable.complete",
                correlationId: "corr-complete",
                eventStore: eventStore));

        var result = await strategy.ExecuteAsync(context);

        Assert.Equal(204, result.HttpStatusCode);
        Assert.Null(result.Output);
        Assert.Equal(-1, await eventStore.GetVersionAsync("CompletionWorkflow:corr-complete"));
    }

    [Fact]
    public async Task DurableExecutionStrategy_RequiresEventStore()
    {
        var strategy = CreateStrategy<IncrementWorkflow, IncrementInput, CounterState, string>();
        var context = MakeContext(
            new IncrementWorkflow(),
            new IncrementInput(1),
            new TestBehaviorContext(
                "durable.increment",
                correlationId: "corr-missing"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => strategy.ExecuteAsync(context));

        Assert.Contains("EventStore", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DurableExecutionStrategy_RequiresRegisteredDurableExecutionSlot()
    {
        var strategy = new DurableExecutionStrategy();
        var context = MakeContext(
            new IncrementWorkflow(),
            new IncrementInput(1),
            new TestBehaviorContext(
                "durable.increment",
                correlationId: "corr-missing-slot",
                eventStore: new RecordingEventStore()));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => strategy.ExecuteAsync(context));

        Assert.Contains("DurableExecutionSlot", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Behaviors.SourceGen", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DurableExecutionStrategy_RejectsUnexpectedEventVersion()
    {
        var eventStore = new RecordingEventStore();
        var strategy = CreateStrategy<InvalidVersionWorkflow, IncrementInput, CounterState, string?>();
        var context = MakeContext(
            new InvalidVersionWorkflow(),
            new IncrementInput(1),
            new TestBehaviorContext(
                "durable.invalid",
                correlationId: "corr-invalid",
                eventStore: eventStore));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => strategy.ExecuteAsync(context));

        Assert.Contains("stream version", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DurableExecutionStrategy_Pattern_IsLiteral()
    {
        var strategy = new DurableExecutionStrategy();

        Assert.Equal("durable-execution", strategy.Pattern);
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
            ArgumentException.ThrowIfNullOrWhiteSpace(streamId);
            ArgumentNullException.ThrowIfNull(events);

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
            ArgumentException.ThrowIfNullOrWhiteSpace(streamId);

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
                await Task.CompletedTask;
            }
        }
    }

    private sealed class StaticBehaviorContributor(params BehaviorTopologyDescriptor[] descriptors) : IBehaviorContributor
    {
        public IReadOnlyList<BehaviorTopologyDescriptor> Contribute()
        {
            return descriptors;
        }
    }
}
