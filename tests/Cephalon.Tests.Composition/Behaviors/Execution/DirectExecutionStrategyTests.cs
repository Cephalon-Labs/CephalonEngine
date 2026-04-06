using Cephalon.Abstractions.Behaviors;
using Cephalon.Behaviors.Patterns.Abstractions;
using Cephalon.Behaviors.Patterns.Strategies;
using Cephalon.Behaviors.Services;

namespace Cephalon.Tests.Behaviors.Execution;

/// <summary>Tests for <see cref="DirectExecutionStrategy"/>.</summary>
public sealed class DirectExecutionStrategyTests
{
    // ─── Fixtures ────────────────────────────────────────────────────────────

    [AppBehavior("direct.query")]
    private sealed class QueryBehavior : IAppBehavior<string, string>
    {
        public Task<string> HandleAsync(string input, IBehaviorContext context, CancellationToken cancellationToken = default)
            => Task.FromResult($"value:{input}");
    }

    [AppBehavior("direct.void")]
    private sealed class VoidBehavior : IAppBehavior<string, string?>
    {
        public Task<string?> HandleAsync(string input, IBehaviorContext context, CancellationToken cancellationToken = default)
            => Task.FromResult<string?>(null);
    }

    private static BehaviorExecutionContext MakeContext<TBehavior>(TBehavior behavior, object input)
        where TBehavior : class
    {
        var descriptor = new BehaviorTopologyDescriptor(typeof(TBehavior).Name, "direct", ["in-memory"]);
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

    // ─── DIR-02: null → 204; non-null → 200 ──────────────────────────────────

    [Fact]
    public async Task DirectExecutionStrategy_NonNullOutput_Returns200()
    {
        var strategy = new DirectExecutionStrategy();
        var execCtx = MakeContext(new QueryBehavior(), "hello");

        var result = await strategy.ExecuteAsync(execCtx);

        Assert.Equal(200, result.HttpStatusCode);
        Assert.NotNull(result.Output);
        Assert.False(result.IsFireAndForget);
    }

    [Fact]
    public async Task DirectExecutionStrategy_NullOutput_Returns204()
    {
        var strategy = new DirectExecutionStrategy();
        var execCtx = MakeContext(new VoidBehavior(), "x");

        var result = await strategy.ExecuteAsync(execCtx);

        Assert.Equal(204, result.HttpStatusCode);
        Assert.Null(result.Output);
        Assert.False(result.IsFireAndForget);
    }

    // ─── DIR-03: No store dependencies ───────────────────────────────────────

    [Fact]
    public void DirectExecutionStrategy_HasNoStoreDependencies()
    {
        // Constructor takes no arguments — verifies DIR-03.
        var strategy = new DirectExecutionStrategy();
        Assert.NotNull(strategy);
    }

    // ─── STRAT-05: Pattern is compile-time literal ────────────────────────────

    [Fact]
    public void DirectExecutionStrategy_Pattern_IsDirectLiteral()
    {
        var strategy = new DirectExecutionStrategy();

        Assert.Equal("direct", strategy.Pattern);
    }
}
