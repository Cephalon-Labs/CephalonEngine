using Cephalon.Abstractions.Behaviors;
using Cephalon.Behaviors.Patterns.Abstractions;
using Cephalon.Behaviors.Patterns.Strategies;
using Cephalon.Behaviors.Services;

namespace Cephalon.Tests.Behaviors.Execution;

/// <summary>Tests for <see cref="CqrsExecutionStrategy"/>.</summary>
public sealed class CqrsExecutionStrategyTests
{
    // ─── Fixtures ────────────────────────────────────────────────────────────

    [AppBehavior("cqrs.query")]
    private sealed class QueryBehavior : IAppBehavior<string, string>
    {
        public Task<string> HandleAsync(string input, IBehaviorContext context, CancellationToken cancellationToken = default)
            => Task.FromResult($"echo:{input}");
    }

    [AppBehavior("cqrs.command")]
    private sealed class CommandBehavior : IAppBehavior<string, string?>
    {
        public Task<string?> HandleAsync(string input, IBehaviorContext context, CancellationToken cancellationToken = default)
            => Task.FromResult<string?>(null);
    }

    [AppBehavior("cqrs.throwing")]
    private sealed class ThrowingBehavior : IAppBehavior<string, string>
    {
        public Task<string> HandleAsync(string input, IBehaviorContext context, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("slot-fault");
    }

    private static BehaviorExecutionContext MakeContext<TBehavior>(TBehavior behavior, object input)
        where TBehavior : class
    {
        var descriptor = new BehaviorTopologyDescriptor(typeof(TBehavior).Name, "cqrs", ["in-memory"]);
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

    // ─── CQRS-01: output == null → 202; != null → 200 ────────────────────────

    [Fact]
    public async Task CqrsExecutionStrategy_NonNullOutput_Returns200()
    {
        var strategy = new CqrsExecutionStrategy();
        var execCtx = MakeContext(new QueryBehavior(), "ping");

        var result = await strategy.ExecuteAsync(execCtx);

        Assert.Equal(200, result.HttpStatusCode);
        Assert.NotNull(result.Output);
    }

    [Fact]
    public async Task CqrsExecutionStrategy_NullOutput_Returns202()
    {
        var strategy = new CqrsExecutionStrategy();
        var execCtx = MakeContext(new CommandBehavior(), "do-it");

        var result = await strategy.ExecuteAsync(execCtx);

        Assert.Equal(202, result.HttpStatusCode);
        Assert.Null(result.Output);
    }

    // ─── CQRS-02: slot exceptions propagate raw ───────────────────────────────

    [Fact]
    public async Task CqrsExecutionStrategy_SlotException_PropagatesRaw()
    {
        var strategy = new CqrsExecutionStrategy();
        var execCtx = MakeContext(new ThrowingBehavior(), "x");

        await Assert.ThrowsAsync<InvalidOperationException>(() => strategy.ExecuteAsync(execCtx));
    }

    // ─── CQRS-03: IsFireAndForget always false ────────────────────────────────

    [Fact]
    public async Task CqrsExecutionStrategy_IsFireAndForget_AlwaysFalse()
    {
        var strategy = new CqrsExecutionStrategy();
        var execCtx = MakeContext(new QueryBehavior(), "q");

        var result = await strategy.ExecuteAsync(execCtx);

        Assert.False(result.IsFireAndForget);
    }

    // ─── STRAT-05: Pattern is compile-time literal ────────────────────────────

    [Fact]
    public void CqrsExecutionStrategy_Pattern_IsCqrsLiteral()
    {
        var strategy = new CqrsExecutionStrategy();

        Assert.Equal("cqrs", strategy.Pattern);
    }
}
