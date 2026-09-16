using System.Text.Json;
using Cephalon.Abstractions.Coordination;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Coordination;
using Cephalon.Engine.Runtime;
using Cephalon.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Composition;

public sealed class ReconciliationTests
{
    private static readonly DateTimeOffset Epoch = new(2026, 9, 16, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void PlanningIsDeterministicAndBindsEveryIntentDimension()
    {
        var plan = Plan();
        var copy = JsonSerializer.Deserialize<ReconciliationPlan>(JsonSerializer.Serialize(plan))!;
        Assert.Equal(plan.Fingerprint, copy.Fingerprint);
        Assert.Equal(JsonSerializer.Serialize(plan), JsonSerializer.Serialize(copy));
        Assert.Equal(plan.Fingerprint, new ReconciliationPlan(plan.Request, "v1", Epoch.ToOffset(TimeSpan.FromHours(7)), plan.ExpiresAtUtc).Fingerprint);
        for (var dimension = 0; dimension < 7; dimension++)
        {
            string[] values = ["op", "tenant", "actor", "action", "target", "v2", "v1"];
            values[dimension] += "changed";
            var request = new ReconciliationRequest(values[0], values[1], values[2], values[3], values[4], values[5], values[6]);
            Assert.NotEqual(plan.Fingerprint, new ReconciliationPlan(request, "v1", Epoch, plan.ExpiresAtUtc).Fingerprint);
        }

        Assert.NotEqual(plan.Fingerprint, new ReconciliationPlan(plan.Request, "v3", Epoch, plan.ExpiresAtUtc).Fingerprint);
        Assert.NotEqual(plan.Fingerprint, new ReconciliationPlan(plan.Request, "v1", Epoch, plan.ExpiresAtUtc.AddSeconds(1)).Fingerprint);
        Assert.Throws<ArgumentOutOfRangeException>(() => new ReconciliationPlan(plan.Request, "v1", Epoch, Epoch));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ReconciliationPlan(plan.Request, "v1", Epoch, Epoch.AddDays(2)));
        Assert.Throws<ArgumentException>(() => new ReconciliationRequest("\n", "t", "a", "b", "c", "d", "e"));
    }

    [Theory]
    [InlineData("v1", ReconciliationOutcome.Applied, 1)]
    [InlineData("v2", ReconciliationOutcome.Converged, 0)]
    [InlineData("v3", ReconciliationOutcome.Stale, 0)]
    public async Task PlanningPreconditionsPreventUnnecessaryEffects(string observed, ReconciliationOutcome expected, int calls)
    {
        var effect = new Effect();
        var result = await Executor().ExecuteAsync(Plan(observed: observed), effect);
        Assert.Equal(expected, result.Outcome);
        Assert.Equal(calls, effect.Calls);
    }

    [Theory]
    [InlineData(ReconciliationEffectOutcome.Applied, ReconciliationOutcome.Applied)]
    [InlineData(ReconciliationEffectOutcome.Rejected, ReconciliationOutcome.Rejected)]
    [InlineData(ReconciliationEffectOutcome.Stale, ReconciliationOutcome.Stale)]
    [InlineData(ReconciliationEffectOutcome.InDoubt, ReconciliationOutcome.InDoubt)]
    [InlineData((ReconciliationEffectOutcome)99, ReconciliationOutcome.InDoubt)]
    public async Task TerminalProviderOutcomesAreNeverRetried(ReconciliationEffectOutcome outcome, ReconciliationOutcome expected)
    {
        var effect = new Effect((_, _, _) => ValueTask.FromResult(outcome));
        var result = await Executor().ExecuteAsync(Plan(), effect);
        Assert.Equal(expected, result.Outcome);
        Assert.Single(result.Attempts);
        Assert.Equal(1, effect.Calls);
    }

    [Fact]
    public async Task ConcurrentDuplicatesReserveOnceAndConflictingIntentCannotReplaceReservation()
    {
        var completion = new TaskCompletionSource<ReconciliationEffectOutcome>(TaskCreationOptions.RunContinuationsAsynchronously);
        var effect = new Effect((_, _, _) => new ValueTask<ReconciliationEffectOutcome>(completion.Task));
        var executor = Executor();
        var running = executor.ExecuteAsync(Plan(), effect).AsTask();
        var duplicates = await Task.WhenAll(Enumerable.Range(0, 32).Select(_ => executor.ExecuteAsync(Plan(), effect).AsTask()));
        Assert.All(duplicates, result => Assert.Equal(ReconciliationOutcome.Running, result.Outcome));
        Assert.Equal(ReconciliationOutcome.Conflict, (await executor.ExecuteAsync(Plan(desired: "v3"), effect)).Outcome);
        Assert.Equal(1, effect.Calls);
        completion.SetResult(ReconciliationEffectOutcome.Applied);
        var completed = await running;
        Assert.Same(completed, await executor.ExecuteAsync(Plan(), effect));
        Assert.Equal(1, effect.Calls);
    }

    [Fact]
    public async Task TenantIdentityIsolatedAndCapacityNeverEvictsCompletedReservations()
    {
        var executor = new ReconciliationExecutor(new ReconciliationOptions(capacity: 2), new TestClock());
        var effect = new Effect();
        Assert.Equal(ReconciliationOutcome.Applied, (await executor.ExecuteAsync(Plan(), effect)).Outcome);
        Assert.Equal(ReconciliationOutcome.Applied, (await executor.ExecuteAsync(Plan(tenant: "other"), effect)).Outcome);
        Assert.Equal(ReconciliationOutcome.CapacityExceeded, (await executor.ExecuteAsync(Plan(id: "new"), effect)).Outcome);
        Assert.Equal(ReconciliationOutcome.Applied, (await executor.ExecuteAsync(Plan(), effect)).Outcome);
        Assert.Equal(2, effect.Calls);
    }

    [Fact]
    public async Task CancellationBeforeApplyDoesNotInvokeEffect()
    {
        var effect = new Effect();
        using var source = new CancellationTokenSource();
        source.Cancel();
        var result = await Executor().ExecuteAsync(Plan(), effect, source.Token);
        Assert.Equal(ReconciliationOutcome.Canceled, result.Outcome);
        Assert.Empty(result.Attempts);
        Assert.Equal(0, effect.Calls);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(60)]
    public async Task OutsideValidityWindowDoesNotInvokeEffect(int seconds)
    {
        var effect = new Effect();
        var clock = new TestClock();
        clock.Advance(TimeSpan.FromSeconds(seconds));
        var result = await new ReconciliationExecutor(timeProvider: clock).ExecuteAsync(Plan(), effect);
        Assert.Equal(ReconciliationOutcome.Expired, result.Outcome);
        Assert.Equal(0, effect.Calls);
    }

    [Fact]
    public async Task CancellationDuringUncooperativeEffectRetainsUncertaintyEvenAfterLateSuccess()
    {
        var completion = new TaskCompletionSource<ReconciliationEffectOutcome>(TaskCreationOptions.RunContinuationsAsynchronously);
        var effect = new Effect((_, _, _) => new ValueTask<ReconciliationEffectOutcome>(completion.Task));
        var executor = Executor();
        using var source = new CancellationTokenSource();
        var running = executor.ExecuteAsync(Plan(), effect, source.Token).AsTask();
        source.Cancel();
        var result = await running.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(ReconciliationOutcome.InDoubt, result.Outcome);
        completion.SetResult(ReconciliationEffectOutcome.Applied);
        Assert.Same(result, await executor.ExecuteAsync(Plan(), effect));
        Assert.Equal(1, effect.Calls);
    }

    [Fact]
    public async Task TotalDeadlineBoundsUncooperativeAsyncEffect()
    {
        var clock = new TestClock();
        var completion = new TaskCompletionSource<ReconciliationEffectOutcome>(TaskCreationOptions.RunContinuationsAsynchronously);
        var effect = new Effect((_, _, _) => new ValueTask<ReconciliationEffectOutcome>(completion.Task));
        var executor = new ReconciliationExecutor(timeProvider: clock);
        var running = executor.ExecuteAsync(Plan(), effect).AsTask();
        clock.Advance(TimeSpan.FromMinutes(1));
        Assert.Equal(ReconciliationOutcome.InDoubt, (await running.WaitAsync(TimeSpan.FromSeconds(5))).Outcome);
        completion.SetException(new InvalidOperationException("late provider failure"));
        Assert.Equal(ReconciliationOutcome.InDoubt, (await executor.ExecuteAsync(Plan(), effect)).Outcome);
    }

    [Fact]
    public async Task ExceptionAfterMutationNeverTriggersAutomaticReplayAndDoesNotLeakSecrets()
    {
        var mutations = 0;
        var effect = new Effect((_, _, _) =>
        {
            mutations++;
            throw new InvalidOperationException("secret-tenant-payload");
        });
        var executor = Executor();
        var result = await executor.ExecuteAsync(Plan(), effect);
        Assert.Equal(ReconciliationOutcome.InDoubt, result.Outcome);
        await executor.ExecuteAsync(Plan(), effect);
        Assert.Equal(1, mutations);
        Assert.DoesNotContain("secret-tenant-payload", JsonSerializer.Serialize(executor.DescribeSection()), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    public async Task SafeNoEffectRetriesStopAtAttemptBudgetWithBoundedJitter(int maxAttempts)
    {
        var clock = new TestClock(autoAdvanceDelays: true);
        var effect = new Effect((_, _, _) => ValueTask.FromResult(ReconciliationEffectOutcome.RetryableNoEffect));
        var executor = new ReconciliationExecutor(new ReconciliationOptions(maxAttempts), clock);
        var result = await executor.ExecuteAsync(Plan(), effect);
        Assert.Equal(ReconciliationOutcome.Exhausted, result.Outcome);
        Assert.Equal(maxAttempts, effect.Calls);
        Assert.Equal(Enumerable.Range(1, maxAttempts), result.Attempts.Select(attempt => attempt.Number));
        Assert.All(clock.Delays, delay => Assert.InRange(delay, TimeSpan.FromMilliseconds(50), TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public async Task CancellationBetweenSafeRetriesHasNoUncertainEffect()
    {
        using var source = new CancellationTokenSource();
        var effect = new Effect((_, _, _) =>
        {
            source.Cancel();
            return ValueTask.FromResult(ReconciliationEffectOutcome.RetryableNoEffect);
        });
        var result = await Executor().ExecuteAsync(Plan(), effect, source.Token);
        Assert.Equal(ReconciliationOutcome.Canceled, result.Outcome);
        Assert.Equal(1, effect.Calls);
    }

    [Fact]
    public async Task SuccessAfterSafeRetryAndDeadlineDuringBackoffAreDistinct()
    {
        var clock = new TestClock(autoAdvanceDelays: true);
        var effect = new Effect((_, number, _) => ValueTask.FromResult(number == 1
            ? ReconciliationEffectOutcome.RetryableNoEffect : ReconciliationEffectOutcome.Applied));
        Assert.Equal(ReconciliationOutcome.Applied, (await new ReconciliationExecutor(timeProvider: clock).ExecuteAsync(Plan(), effect)).Outcome);
        var otherClock = new TestClock();
        var retry = new Effect((_, _, _) => ValueTask.FromResult(ReconciliationEffectOutcome.RetryableNoEffect));
        var running = new ReconciliationExecutor(timeProvider: otherClock).ExecuteAsync(Plan(), retry).AsTask();
        otherClock.Advance(TimeSpan.FromMinutes(1));
        Assert.Equal(ReconciliationOutcome.Expired, (await running.WaitAsync(TimeSpan.FromSeconds(5))).Outcome);
        Assert.Equal(1, retry.Calls);
    }

    [Fact]
    public async Task SnapshotConsumerIsOptInIdempotentlyRegisteredDeterministicAndRedacted()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(new TestClock());
        services.AddCephalonReconciliation().AddCephalonReconciliation();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(blueprint: "ModularMonolith"));
            engine.AddModule(new PlatformTestModule());
        });
        using var provider = services.BuildServiceProvider();
        var executor = provider.GetRequiredService<ReconciliationExecutor>();
        await executor.ExecuteAsync(Plan(id: "z", tenant: "private-tenant"), new Effect());
        await executor.ExecuteAsync(Plan(id: "a"), new Effect());
        var snapshot = provider.GetRequiredService<IRuntimeIntrospectionSnapshotProvider>().CreateSnapshot();
        var section = Assert.Single(snapshot.ExtensionSections, section => section.Id == "coordination");
        Assert.Equal(2, section.Entries.Count);
        Assert.Equal(section.Entries.Select(entry => entry.Id).Order(StringComparer.Ordinal), section.Entries.Select(entry => entry.Id));
        Assert.All(section.Entries, entry => Assert.Empty(entry.Actions));
        Assert.DoesNotContain("private-tenant", JsonSerializer.Serialize(section), StringComparison.Ordinal);
        Assert.NotNull(snapshot.Manifest);
        Assert.Equal(JsonSerializer.Serialize(section), JsonSerializer.Serialize(executor.DescribeSection()));
    }

    [Fact]
    public async Task FreshExecutorDoesNotPretendToRecoverPreviousReservations()
    {
        var effect = new Effect();
        await Executor().ExecuteAsync(Plan(), effect);
        await Executor().ExecuteAsync(Plan(), effect);
        Assert.Equal(2, effect.Calls);
        Assert.Throws<ArgumentOutOfRangeException>(() => new ReconciliationOptions(maxAttempts: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ReconciliationOptions(capacity: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ReconciliationOptions(retryDelay: TimeSpan.Zero));
    }

    [Fact]
    public async Task AtomicProviderPreconditionRejectsPlanThatBecameStaleAfterPlanning()
    {
        var revision = "v1";
        var writes = 0;
        var providerGate = new object();
        var effect = new Effect((plan, _, _) =>
        {
            lock (providerGate)
            {
                if (revision != plan.Request.ExpectedRevision)
                {
                    return ValueTask.FromResult(ReconciliationEffectOutcome.Stale);
                }

                revision = plan.Request.DesiredRevision;
                writes++;
                return ValueTask.FromResult(ReconciliationEffectOutcome.Applied);
            }
        });
        var first = Plan(id: "first");
        var staleAfterPlanning = Plan(id: "second", desired: "v3");
        var executor = Executor();
        Assert.Equal(ReconciliationOutcome.Applied, (await executor.ExecuteAsync(first, effect)).Outcome);
        Assert.Equal(ReconciliationOutcome.Stale, (await executor.ExecuteAsync(staleAfterPlanning, effect)).Outcome);
        Assert.Equal("v2", revision);
        Assert.Equal(1, writes);
    }

    [Fact]
    public async Task GeneratedIntentCasesKeepIdentityAndTerminalReplayInvariant()
    {
        var executor = Executor();
        var fingerprints = new HashSet<string>(StringComparer.Ordinal);
        var effect = new Effect();
        for (var index = 0; index < 200; index++)
        {
            var plan = Plan(id: index.ToString(System.Globalization.CultureInfo.InvariantCulture), desired: $"revision-{index}");
            Assert.True(fingerprints.Add(plan.Fingerprint));
            var result = await executor.ExecuteAsync(plan, effect);
            Assert.Same(result, await executor.ExecuteAsync(plan, effect));
            Assert.Equal(ReconciliationOutcome.Conflict, (await executor.ExecuteAsync(
                Plan(id: plan.Request.OperationId, desired: "conflicting"), effect)).Outcome);
        }

        Assert.Equal(200, effect.Calls);
        Assert.Throws<System.Text.EncoderFallbackException>(() => Plan(id: "\ud800"));
        Assert.NotEqual(Plan(id: "a|b", tenant: "c").Fingerprint, Plan(id: "a", tenant: "b|c").Fingerprint);
    }

    private static ReconciliationExecutor Executor() => new(timeProvider: new TestClock());
    private static ReconciliationPlan Plan(string id = "op", string tenant = "tenant", string desired = "v2", string observed = "v1") =>
        new(new ReconciliationRequest(id, tenant, "actor", "action", "target", desired, "v1"), observed, Epoch, Epoch.AddMinutes(1));

    private sealed class Effect(Func<ReconciliationPlan, int, CancellationToken, ValueTask<ReconciliationEffectOutcome>>? apply = null) : IReconciliationEffect
    {
        public int Calls { get; private set; }
        public ValueTask<ReconciliationEffectOutcome> ApplyAsync(ReconciliationPlan plan, int attemptNumber, CancellationToken cancellationToken = default)
        {
            Calls++;
            return apply?.Invoke(plan, attemptNumber, cancellationToken) ?? ValueTask.FromResult(ReconciliationEffectOutcome.Applied);
        }
    }

    // These tests advance timers explicitly, never depending on scheduler-speed assumptions.
    private sealed class TestClock(bool autoAdvanceDelays = false) : TimeProvider
    {
        private DateTimeOffset now = Epoch;
        private readonly List<TestTimer> timers = [];
        public List<TimeSpan> Delays { get; } = [];
        public override DateTimeOffset GetUtcNow() => now;
        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = new TestTimer(this, callback, state, now + dueTime);
            timers.Add(timer);
            if (autoAdvanceDelays && dueTime < TimeSpan.FromSeconds(10))
            {
                Delays.Add(dueTime);
                Advance(dueTime);
            }

            return timer;
        }
        public void Advance(TimeSpan elapsed)
        {
            now += elapsed;
            foreach (var timer in timers.ToArray())
            {
                timer.Fire(now);
            }
        }
        private sealed class TestTimer(TestClock owner, TimerCallback callback, object? state, DateTimeOffset due) : ITimer
        {
            private bool disposed;
            public void Fire(DateTimeOffset current)
            {
                if (!disposed && current >= due)
                {
                    disposed = true;
                    callback(state);
                }
            }
            public bool Change(TimeSpan dueTime, TimeSpan period)
            {
                due = owner.now + dueTime;
                return !disposed;
            }
            public void Dispose() => disposed = true;
            public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
        }
    }
}
