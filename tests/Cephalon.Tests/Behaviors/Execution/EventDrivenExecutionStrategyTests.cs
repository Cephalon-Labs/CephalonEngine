using Cephalon.Abstractions.Behaviors;
using Cephalon.Behaviors.Patterns.Abstractions;
using Cephalon.Behaviors.Patterns.Strategies;
using Cephalon.Behaviors.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cephalon.Tests.Behaviors.Execution;

/// <summary>Tests for <see cref="EventDrivenExecutionStrategy"/>.</summary>
public sealed class EventDrivenExecutionStrategyTests
{
    // ─── Fixtures ────────────────────────────────────────────────────────────

    [AppBehavior("ed.work")]
    private sealed class WorkBehavior : IAppBehavior<string, string>
    {
        public Task<string> HandleAsync(string input, IBehaviorContext context, CancellationToken cancellationToken = default)
            => Task.FromResult($"done:{input}");
    }

    [AppBehavior("ed.slow")]
    private sealed class SlowBehavior : IAppBehavior<string, string>
    {
        private readonly TaskCompletionSource<string> _tcs = new();
        internal Task SlowTask => _tcs.Task;

        public async Task<string> HandleAsync(string input, IBehaviorContext context, CancellationToken cancellationToken = default)
        {
            await Task.Delay(50, cancellationToken);
            return "slow-done";
        }
    }

    private static BehaviorExecutionContext MakeContext<TBehavior>(TBehavior behavior, object input)
        where TBehavior : class
    {
        var descriptor = new BehaviorTopologyDescriptor(typeof(TBehavior).Name, "event-driven", ["in-memory"]);
        var slot = BehaviorExecutionSlot.ForType(typeof(TBehavior));
        var ctx = new TestBehaviorContext(typeof(TBehavior).Name);
        return new BehaviorExecutionContext
        {
            Descriptor = descriptor,
            BehaviorInstance = behavior,
            Slot = slot,
            Input = input,
            BehaviorContext = ctx
        };
    }

    // ─── ED-01: Returns 202 + IsFireAndForget = true immediately ─────────────

    [Fact]
    public async Task EventDrivenExecutionStrategy_Returns202_Immediately()
    {
        var strategy = new EventDrivenExecutionStrategy(NullLogger<EventDrivenExecutionStrategy>.Instance);
        var execCtx = MakeContext(new WorkBehavior(), "payload");

        var result = await strategy.ExecuteAsync(execCtx);

        Assert.Equal(202, result.HttpStatusCode);
        Assert.True(result.IsFireAndForget);
        Assert.Null(result.Output);
    }

    // ─── ED-02: Background runs via Task.Run (verified by non-blocking return) ─

    [Fact]
    public async Task EventDrivenExecutionStrategy_ReturnsBeforeBackgroundCompletes()
    {
        var strategy = new EventDrivenExecutionStrategy(NullLogger<EventDrivenExecutionStrategy>.Instance);
        var execCtx = MakeContext(new SlowBehavior(), "x");

        // Should return well before the 50ms delay finishes.
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = await strategy.ExecuteAsync(execCtx);
        sw.Stop();

        Assert.Equal(202, result.HttpStatusCode);
        Assert.True(sw.ElapsedMilliseconds < 45, $"Expected < 45ms but was {sw.ElapsedMilliseconds}ms");
    }

    // ─── STRAT-05: Pattern is compile-time literal ────────────────────────────

    [Fact]
    public void EventDrivenExecutionStrategy_Pattern_IsEventDrivenLiteral()
    {
        var strategy = new EventDrivenExecutionStrategy(NullLogger<EventDrivenExecutionStrategy>.Instance);

        Assert.Equal("event-driven", strategy.Pattern);
    }
}
