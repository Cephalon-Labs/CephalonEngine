namespace Cephalon.Abstractions.Coordination;

/// <summary>Describes a pure planning decision.</summary>
public enum ReconciliationPlanState
{
    /// <summary>The supplied observation matches the precondition.</summary>
    Ready,
    /// <summary>The desired revision was already observed; no effect is needed.</summary>
    Converged,
    /// <summary>The observation contradicts the precondition; a new plan is needed.</summary>
    Stale,
}
