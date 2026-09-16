namespace Cephalon.Abstractions.Coordination;

/// <summary>An immutable local execution snapshot; it is not a durable command journal.</summary>
public sealed class ReconciliationResult
{
    /// <summary>Creates a result with a defensive copy of attempt history.</summary>
    /// <param name="planFingerprint">The immutable plan binding.</param>
    /// <param name="outcome">The local outcome.</param>
    /// <param name="attempts">The ordered completed attempts.</param>
    /// <param name="observedAtUtc">The snapshot timestamp.</param>
    public ReconciliationResult(string planFingerprint, ReconciliationOutcome outcome,
        IReadOnlyList<ReconciliationAttempt> attempts, DateTimeOffset observedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(planFingerprint);
        ArgumentNullException.ThrowIfNull(attempts);
        PlanFingerprint = planFingerprint;
        Outcome = outcome;
        Attempts = Array.AsReadOnly(attempts.ToArray());
        ObservedAtUtc = observedAtUtc;
    }

    /// <summary>Gets the immutable plan binding.</summary>
    public string PlanFingerprint { get; }
    /// <summary>Gets the local outcome.</summary>
    public ReconciliationOutcome Outcome { get; }
    /// <summary>Gets the ordered, immutable completed attempt history.</summary>
    public IReadOnlyList<ReconciliationAttempt> Attempts { get; }
    /// <summary>Gets the snapshot timestamp.</summary>
    public DateTimeOffset ObservedAtUtc { get; }
}
