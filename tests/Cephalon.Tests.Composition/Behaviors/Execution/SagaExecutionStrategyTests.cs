using Cephalon.Abstractions.Behaviors;
using Cephalon.Behaviors.Patterns.Abstractions;
using Cephalon.Behaviors.Patterns.Stores;
using Cephalon.Behaviors.Patterns.Strategies;
using Cephalon.Behaviors.Services;
using Cephalon.Tests.Behaviors;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cephalon.Tests.Behaviors.Execution;

/// <summary>Tests for <see cref="SagaExecutionStrategy"/>.</summary>
public sealed class SagaExecutionStrategyTests
{
    // ─── Fixtures ────────────────────────────────────────────────────────────

    [AppBehavior("saga.step")]
    private sealed class SagaStepBehavior : IAppBehavior<string, string>
    {
        public Task<string> HandleAsync(string input, IBehaviorContext context, CancellationToken cancellationToken = default)
            => Task.FromResult($"state-after:{input}");
    }

    [AppBehavior("saga.null")]
    private sealed class NullOutputSagaBehavior : IAppBehavior<string, string?>
    {
        public Task<string?> HandleAsync(string input, IBehaviorContext context, CancellationToken cancellationToken = default)
            => Task.FromResult<string?>(null);
    }

    [AppBehavior("saga.throw")]
    private sealed class ThrowingSagaBehavior : IAppBehavior<string, string>
    {
        public Task<string> HandleAsync(string input, IBehaviorContext context, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("saga-fault");
    }

    private static BehaviorExecutionContext MakeContext<TBehavior>(TBehavior behavior, object input, IBehaviorContext behaviorContext)
        where TBehavior : class
    {
        var descriptor = new BehaviorTopologyDescriptor(typeof(TBehavior).Name, "saga-step", ["in-memory"]);
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

    private static SagaExecutionStrategy MakeStrategy(ISagaStateStore store)
        => new(store, NullLogger<SagaExecutionStrategy>.Instance);

    // ─── SAGA-01: Uses CorrelationId from IBehaviorContext ────────────────────

    [Fact]
    public async Task SagaExecutionStrategy_WithCorrelationId_UsesThatAsKey()
    {
        var store = new InMemorySagaStateStore();
        var strategy = MakeStrategy(store);
        var behaviorCtx = new TestBehaviorContext("saga.step", correlationId: "corr-xyz");
        var execCtx = MakeContext(new SagaStepBehavior(), "input", behaviorCtx);

        await strategy.ExecuteAsync(execCtx);

        var saved = await store.GetAsync<string>("corr-xyz");
        Assert.NotNull(saved);
    }

    [Fact]
    public async Task SagaExecutionStrategy_WithoutCorrelationId_GeneratesNewKey()
    {
        var store = new InMemorySagaStateStore();
        var strategy = MakeStrategy(store);
        var behaviorCtx = new TestBehaviorContext("saga.step", correlationId: null);
        var execCtx = MakeContext(new SagaStepBehavior(), "input", behaviorCtx);

        // Should not throw; generates a GUID-based key.
        var result = await strategy.ExecuteAsync(execCtx);

        Assert.Equal(200, result.HttpStatusCode);
    }

    // ─── SAGA-02: GetAsync → InvokeAsync → SaveAsync order ───────────────────

    [Fact]
    public async Task SagaExecutionStrategy_Execute_SavesStateAfterSuccess()
    {
        var store = new InMemorySagaStateStore();
        var strategy = MakeStrategy(store);
        var behaviorCtx = new TestBehaviorContext("saga.step", correlationId: "saga-order");
        var execCtx = MakeContext(new SagaStepBehavior(), "step-input", behaviorCtx);

        var result = await strategy.ExecuteAsync(execCtx);

        Assert.Equal(200, result.HttpStatusCode);
        var saved = await store.GetAsync<string>("saga-order");
        Assert.NotNull(saved);
        Assert.Contains("state-after", saved);
    }

    // ─── SAGA-03: Slot throws → do NOT save state ─────────────────────────────

    [Fact]
    public async Task SagaExecutionStrategy_OnException_DoesNotSaveState()
    {
        var store = new InMemorySagaStateStore();
        var strategy = MakeStrategy(store);
        var behaviorCtx = new TestBehaviorContext("saga.throw", correlationId: "saga-ex");
        var execCtx = MakeContext(new ThrowingSagaBehavior(), "input", behaviorCtx);

        await Assert.ThrowsAsync<InvalidOperationException>(() => strategy.ExecuteAsync(execCtx));

        // Verify via GetAsync — must still be null (original value).
        var state = await store.GetAsync<string>("saga-ex");
        Assert.Null(state);
    }

    // ─── SAGA-05: null state from GetAsync → passes null to slot as-is ────────

    [Fact]
    public async Task SagaExecutionStrategy_NullExistingState_DoesNotThrow()
    {
        var store = new InMemorySagaStateStore();
        var strategy = MakeStrategy(store);
        var behaviorCtx = new TestBehaviorContext("saga.step", correlationId: "saga-fresh");
        var execCtx = MakeContext(new SagaStepBehavior(), "new-input", behaviorCtx);

        // No prior state — GetAsync returns null, should proceed without error.
        var result = await strategy.ExecuteAsync(execCtx);

        Assert.Equal(200, result.HttpStatusCode);
    }

    // ─── Null output from slot → no save ─────────────────────────────────────

    [Fact]
    public async Task SagaExecutionStrategy_NullOutput_DoesNotSaveState()
    {
        var store = new InMemorySagaStateStore();
        var strategy = MakeStrategy(store);
        var behaviorCtx = new TestBehaviorContext("saga.null", correlationId: "saga-null");
        var execCtx = MakeContext(new NullOutputSagaBehavior(), "input", behaviorCtx);

        var result = await strategy.ExecuteAsync(execCtx);

        Assert.Equal(200, result.HttpStatusCode);
        var saved = await store.GetAsync<string>("saga-null");
        Assert.Null(saved);
    }

    // ─── STRAT-05: Pattern is compile-time literal ────────────────────────────

    [Fact]
    public void SagaExecutionStrategy_Pattern_IsSagaStepLiteral()
    {
        var store = new InMemorySagaStateStore();
        var strategy = MakeStrategy(store);

        Assert.Equal("saga-step", strategy.Pattern);
    }
}
