namespace Cephalon.Abstractions.Coordination;

/// <summary>Applies one authorized provider action under an atomic revision precondition.</summary>
/// <remarks>The implementation must bind ActionId and DesiredRevision to immutable payload, verify actor/tenant authority at apply time, check the expected revision at the protected write, and honor cancellation. A read followed by an unconditional write is insufficient. Throwing after invocation is treated as uncertain, never retried automatically.</remarks>
public interface IReconciliationEffect
{
    /// <summary>Attempts one effect; only a confirmed no-effect retry result permits another invocation.</summary>
    /// <param name="plan">The complete immutable plan.</param>
    /// <param name="attemptNumber">The one-based attempt number within this execution.</param>
    /// <param name="cancellationToken">Cancellation including the total plan deadline.</param>
    /// <returns>The provider's confirmed outcome.</returns>
    ValueTask<ReconciliationEffectOutcome> ApplyAsync(ReconciliationPlan plan, int attemptNumber,
        CancellationToken cancellationToken = default);
}
