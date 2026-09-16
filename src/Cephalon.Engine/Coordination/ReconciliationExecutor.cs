using System.Globalization;
using Cephalon.Abstractions.Coordination;
using Cephalon.Engine.Runtime;

namespace Cephalon.Engine.Coordination;

/// <summary>Executes bounded reconciliation with instance-local idempotency and redacted runtime readback.</summary>
/// <remarks>Register one instance per process scope. Reservations never expire or evict; a full executor rejects new operations. Restart loses all reservations. There is no distributed lease, durable recovery, or authorization implementation here. Effects must validate authority and use atomic provider preconditions. Asynchronous effects are time-bounded even if they ignore cancellation; synchronous blocking code cannot be preempted.</remarks>
public sealed class ReconciliationExecutor : IRuntimeIntrospectionSectionContributor
{
    private readonly object gate = new();
    private readonly Dictionary<(string Tenant, string Operation), ReconciliationResult> results = [];
    private readonly ReconciliationOptions options;
    private readonly TimeProvider timeProvider;

    /// <summary>Creates an isolated executor with explicit limits and a replaceable clock/timer source.</summary>
    /// <param name="options">Execution limits, or defaults.</param>
    /// <param name="timeProvider">The clock and timer source, or the system provider.</param>
    public ReconciliationExecutor(ReconciliationOptions? options = null, TimeProvider? timeProvider = null)
    {
        this.options = options ?? new ReconciliationOptions();
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>Reserves intent and executes it once within this instance; duplicates return the latest stored result.</summary>
    /// <param name="plan">The immutable plan. Replanning requires a new operation id.</param>
    /// <param name="effect">The application-bound authorized provider action.</param>
    /// <param name="cancellationToken">Cancellation of the original execution; a duplicate does not cancel it.</param>
    /// <returns>A terminal or running snapshot. Uncertain effects require external reconciliation, never blind replay.</returns>
    public async ValueTask<ReconciliationResult> ExecuteAsync(ReconciliationPlan plan, IReconciliationEffect effect,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(effect);
        var key = (plan.Request.TenantId, plan.Request.OperationId);
        lock (gate)
        {
            if (results.TryGetValue(key, out var existing))
            {
                return existing.PlanFingerprint == plan.Fingerprint ? existing : CreateResult(plan, ReconciliationOutcome.Conflict, []);
            }

            if (results.Count >= options.Capacity)
            {
                return CreateResult(plan, ReconciliationOutcome.CapacityExceeded, []);
            }

            results.Add(key, CreateResult(plan, ReconciliationOutcome.Running, []));
        }

        var result = await ExecuteReservedAsync(plan, effect, cancellationToken).ConfigureAwait(false);
        lock (gate)
        {
            results[key] = result;
        }

        return result;
    }

    /// <summary>Creates a versioned observation section with no tenant, actor, target, payload, or provider exception text.</summary>
    /// <returns>The deterministic coordination section; no executable operator actions are advertised.</returns>
    public RuntimeIntrospectionSection DescribeSection()
    {
        ReconciliationResult[] snapshot;
        lock (gate)
        {
            snapshot = results.Values.ToArray();
        }

        return new RuntimeIntrospectionSection("coordination", "1.0", "Cephalon.Engine", "Reconciliation",
            "Bounded instance-local execution; reservations are not durable and do not authorize actions.",
            snapshot.Select(static result => new RuntimeIntrospectionSectionEntry(
                result.PlanFingerprint, "Reconciliation operation", "Opaque plan binding; provider details remain with the owning companion.",
                "Applied", result.Outcome.ToString(),
                conditions: [new RuntimeOperatorCondition("OutcomeKnown", result.Outcome == ReconciliationOutcome.Running ? "unknown"
                    : result.Outcome == ReconciliationOutcome.InDoubt ? "false" : "true",
                    result.Outcome == ReconciliationOutcome.InDoubt ? "warning" : "info", result.Outcome.ToString(),
                    "Execution outcome within this executor instance.", result.ObservedAtUtc)],
                metadata: new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["ownership"] = "cephalon-managed",
                    ["scope"] = "instance-local",
                    ["durability"] = "none",
                    ["attemptCount"] = result.Attempts.Count.ToString(CultureInfo.InvariantCulture),
                })).ToArray());
    }

    private async ValueTask<ReconciliationResult> ExecuteReservedAsync(ReconciliationPlan plan,
        IReconciliationEffect effect, CancellationToken cancellationToken)
    {
        List<ReconciliationAttempt> attempts = [];
        if (cancellationToken.IsCancellationRequested)
        {
            return CreateResult(plan, ReconciliationOutcome.Canceled, attempts);
        }

        var now = timeProvider.GetUtcNow();
        if (now < plan.CreatedAtUtc || now >= plan.ExpiresAtUtc)
        {
            return CreateResult(plan, ReconciliationOutcome.Expired, attempts);
        }

        if (plan.State != ReconciliationPlanState.Ready)
        {
            return CreateResult(plan, plan.State == ReconciliationPlanState.Converged
                ? ReconciliationOutcome.Converged : ReconciliationOutcome.Stale, attempts);
        }

        using var deadline = new CancellationTokenSource(plan.ExpiresAtUtc - now, timeProvider);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, deadline.Token);
        for (var number = 1; number <= options.MaxAttempts; number++)
        {
            if (linked.IsCancellationRequested || timeProvider.GetUtcNow() >= plan.ExpiresAtUtc)
            {
                return CreateResult(plan, cancellationToken.IsCancellationRequested
                    ? ReconciliationOutcome.Canceled : ReconciliationOutcome.Expired, attempts);
            }

            var startedAt = timeProvider.GetUtcNow();
            ReconciliationEffectOutcome outcome;
            Task<ReconciliationEffectOutcome>? pendingEffect = null;
            try
            {
                // WaitAsync bounds the caller's wait without assuming cancellation stopped the provider.
                // The reservation remains InDoubt if an ignored cancellation permits a late effect.
                pendingEffect = effect.ApplyAsync(plan, number, linked.Token).AsTask();
                outcome = await pendingEffect.WaitAsync(linked.Token).ConfigureAwait(false);
                if (!Enum.IsDefined(outcome))
                {
                    outcome = ReconciliationEffectOutcome.InDoubt;
                }
            }
            catch (Exception)
            {
                // Exception details may contain tenant payloads or secrets; they are never projected.
                outcome = ReconciliationEffectOutcome.InDoubt;
                if (pendingEffect is not null)
                {
                    _ = pendingEffect.ContinueWith(static task => { _ = task.Exception; },
                        CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                        TaskScheduler.Default);
                }
            }

            attempts.Add(new ReconciliationAttempt(number, startedAt, timeProvider.GetUtcNow(), outcome));
            if (outcome != ReconciliationEffectOutcome.RetryableNoEffect)
            {
                return CreateResult(plan, outcome switch
                {
                    ReconciliationEffectOutcome.Applied => ReconciliationOutcome.Applied,
                    ReconciliationEffectOutcome.Rejected => ReconciliationOutcome.Rejected,
                    ReconciliationEffectOutcome.Stale => ReconciliationOutcome.Stale,
                    _ => ReconciliationOutcome.InDoubt,
                }, attempts);
            }

            if (number < options.MaxAttempts)
            {
                // Stable per-plan jitter spreads unrelated plans without process-seeded randomness.
                var seed = Convert.ToInt32(plan.Fingerprint.Substring((number % 16) * 2, 2), 16);
                var delay = TimeSpan.FromTicks((long)Math.Min(options.MaxRetryDelay.Ticks,
                    options.RetryDelay.Ticks * Math.Pow(2, number - 1) * (0.5 + seed / 255.0)));
                try
                {
                    await Task.Delay(delay, timeProvider, linked.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return CreateResult(plan, cancellationToken.IsCancellationRequested
                        ? ReconciliationOutcome.Canceled : ReconciliationOutcome.Expired, attempts);
                }
            }
        }

        return CreateResult(plan, ReconciliationOutcome.Exhausted, attempts);
    }

    private ReconciliationResult CreateResult(ReconciliationPlan plan, ReconciliationOutcome outcome,
        IReadOnlyList<ReconciliationAttempt> attempts) => new(plan.Fingerprint, outcome, attempts, timeProvider.GetUtcNow());
}
