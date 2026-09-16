namespace Cephalon.Abstractions.Coordination;

/// <summary>Describes whether an effect is confirmed, safely retryable, or uncertain.</summary>
public enum ReconciliationEffectOutcome
{
    /// <summary>The outcome is unknown; do not retry until externally reconciled.</summary>
    InDoubt,
    /// <summary>The desired revision was durably applied or verified at the protected write.</summary>
    Applied,
    /// <summary>No mutation occurred and retrying this same intent is safe.</summary>
    RetryableNoEffect,
    /// <summary>No mutation occurred and this intent cannot succeed.</summary>
    Rejected,
    /// <summary>The revision precondition failed atomically without mutation.</summary>
    Stale,
}
