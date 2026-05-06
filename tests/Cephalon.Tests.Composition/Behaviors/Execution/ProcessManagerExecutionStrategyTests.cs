using Cephalon.Abstractions.Behaviors;
using Cephalon.Behaviors.Patterns.Abstractions;
using Cephalon.Behaviors.Patterns.Stores;
using Cephalon.Behaviors.Patterns.Strategies;
using Cephalon.Behaviors.Services;
using Cephalon.Tests.Behaviors;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cephalon.Tests.Behaviors.Execution;

/// <summary>Tests for <see cref="ProcessManagerExecutionStrategy"/>.</summary>
public sealed class ProcessManagerExecutionStrategyTests
{
    // ─── Fixtures ────────────────────────────────────────────────────────────

    /// <summary>A normal step — returns a <see cref="ProcessCheckpoint"/> without <see cref="IProcessCompletion"/>.</summary>
    [AppBehavior("pm.step")]
    private sealed class StepBehavior : IAppBehavior<string, ProcessCheckpoint>
    {
        public Task<ProcessCheckpoint> HandleAsync(string input, IBehaviorContext context, CancellationToken cancellationToken = default)
            => Task.FromResult(new ProcessCheckpoint
            {
                ProcessId = context.CorrelationId ?? "unknown",
                CurrentStep = $"after-{input}",
                CreatedAt = DateTimeOffset.UtcNow
            });
    }

    /// <summary>A completion step — returns a <see cref="IProcessCompletion"/> signal.</summary>
    [AppBehavior("pm.complete")]
    private sealed class CompletingBehavior : IAppBehavior<string, CompletionSignal>
    {
        public Task<CompletionSignal> HandleAsync(string input, IBehaviorContext context, CancellationToken cancellationToken = default)
            => Task.FromResult(new CompletionSignal());
    }

    /// <summary>A behavior that returns null output.</summary>
    [AppBehavior("pm.null")]
    private sealed class NullOutputBehavior : IAppBehavior<string, ProcessCheckpoint?>
    {
        public Task<ProcessCheckpoint?> HandleAsync(string input, IBehaviorContext context, CancellationToken cancellationToken = default)
            => Task.FromResult<ProcessCheckpoint?>(null);
    }

    /// <summary>A dedicated completion signal that implements <see cref="IProcessCompletion"/>.</summary>
    private sealed class CompletionSignal : IProcessCompletion { }

    private static BehaviorExecutionContext MakeContext<TBehavior>(TBehavior behavior, object input, IBehaviorContext behaviorContext)
        where TBehavior : class
    {
        var descriptor = new BehaviorTopologyDescriptor(typeof(TBehavior).Name, "process-manager", ["in-memory"]);
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

    private static ProcessManagerExecutionStrategy MakeStrategy(IProcessCheckpointStore store)
        => new(store, NullLogger<ProcessManagerExecutionStrategy>.Instance);

    // ─── PM-01: CorrelationId required ───────────────────────────────────────

    [Fact]
    public async Task ProcessManagerExecutionStrategy_NullCorrelationId_ThrowsInvalidOperation()
    {
        var store = new InMemoryProcessCheckpointStore();
        var strategy = MakeStrategy(store);
        var behaviorCtx = new TestBehaviorContext("pm.step", correlationId: null);
        var execCtx = MakeContext(new StepBehavior(), "start", behaviorCtx);

        await Assert.ThrowsAsync<InvalidOperationException>(() => strategy.ExecuteAsync(execCtx));
    }

    // ─── PM-02: InvokeAsync → SaveAsync order ────────────────────────────────

    [Fact]
    public async Task ProcessManagerExecutionStrategy_AfterStep_SavesCheckpoint()
    {
        var store = new InMemoryProcessCheckpointStore();
        var strategy = MakeStrategy(store);
        var behaviorCtx = new TestBehaviorContext("pm.step", correlationId: "proc-save");
        var execCtx = MakeContext(new StepBehavior(), "begin", behaviorCtx);

        var result = await strategy.ExecuteAsync(execCtx);

        Assert.Equal(200, result.HttpStatusCode);
        var saved = await store.GetAsync("proc-save");
        Assert.NotNull(saved);
        Assert.Equal("after-begin", saved.CurrentStep);
    }

    // ─── PM-03: IProcessCompletion → delete checkpoint ───────────────────────

    [Fact]
    public async Task ProcessManagerExecutionStrategy_IProcessCompletion_DeletesCheckpoint()
    {
        var store = new InMemoryProcessCheckpointStore();
        // Pre-seed a checkpoint.
        await store.SaveAsync("proc-complete", new ProcessCheckpoint
        {
            ProcessId = "proc-complete",
            CurrentStep = "step-1",
            CreatedAt = DateTimeOffset.UtcNow
        });

        var strategy = MakeStrategy(store);
        var behaviorCtx = new TestBehaviorContext("pm.complete", correlationId: "proc-complete");
        var execCtx = MakeContext(new CompletingBehavior(), "finish", behaviorCtx);

        await strategy.ExecuteAsync(execCtx);

        var deleted = await store.GetAsync("proc-complete");
        Assert.Null(deleted);
    }

    // ─── PM-03: null output → log warning + preserve checkpoint ──────────────

    [Fact]
    public async Task ProcessManagerExecutionStrategy_NullOutput_PreservesCheckpoint()
    {
        var store = new InMemoryProcessCheckpointStore();
        var existing = new ProcessCheckpoint
        {
            ProcessId = "proc-null",
            CurrentStep = "existing-step",
            CreatedAt = DateTimeOffset.UtcNow
        };
        await store.SaveAsync("proc-null", existing);

        var strategy = MakeStrategy(store);
        var behaviorCtx = new TestBehaviorContext("pm.null", correlationId: "proc-null");
        var execCtx = MakeContext(new NullOutputBehavior(), "x", behaviorCtx);

        var result = await strategy.ExecuteAsync(execCtx);

        Assert.Equal(200, result.HttpStatusCode);
        // Checkpoint must still be there (not deleted).
        var preserved = await store.GetAsync("proc-null");
        Assert.NotNull(preserved);
        Assert.Equal("existing-step", preserved.CurrentStep);
    }

    // ─── STRAT-05: Pattern is compile-time literal ────────────────────────────

    [Fact]
    public void ProcessManagerExecutionStrategy_Pattern_IsProcessManagerLiteral()
    {
        var store = new InMemoryProcessCheckpointStore();
        var strategy = MakeStrategy(store);

        Assert.Equal("process-manager", strategy.Pattern);
    }
}
