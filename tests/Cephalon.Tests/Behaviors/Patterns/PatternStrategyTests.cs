using Cephalon.Abstractions.Behaviors;
using Cephalon.Behaviors.Patterns.Abstractions;
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

    /// <summary>A completion signal that implements <see cref="IProcessCompletion"/> to indicate process end.</summary>
    private sealed class CompletionSignal : IProcessCompletion { }

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
        var cpStore = new InMemoryProcessCheckpointStore();
        return new ExecutionStrategyRegistry(new IBehaviorExecutionStrategy[]
        {
            new CqrsExecutionStrategy(),
            new EventDrivenExecutionStrategy(NullLogger<EventDrivenExecutionStrategy>.Instance),
            new SagaExecutionStrategy(store, NullLogger<SagaExecutionStrategy>.Instance),
            new ProcessManagerExecutionStrategy(cpStore, NullLogger<ProcessManagerExecutionStrategy>.Instance),
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
        var pm = registry.GetStrategy("process-manager");
        var direct = registry.GetStrategy("direct");

        Assert.IsType<CqrsExecutionStrategy>(cqrs);
        Assert.IsType<EventDrivenExecutionStrategy>(eventDriven);
        Assert.IsType<SagaExecutionStrategy>(saga);
        Assert.IsType<ProcessManagerExecutionStrategy>(pm);
        Assert.IsType<DirectExecutionStrategy>(direct);
    }

    [Fact]
    public void Registry_AllStrategies_HaveUniquePatterns()
    {
        var registry = BuildRegistry();
        var patterns = registry.All.Select(s => s.Pattern).ToList();

        Assert.Equal(patterns.Count, patterns.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(5, patterns.Count);
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
}
