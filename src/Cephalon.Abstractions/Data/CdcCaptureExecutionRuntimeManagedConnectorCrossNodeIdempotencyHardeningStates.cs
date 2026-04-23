namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable state identifiers used by managed-connector cross-node idempotency-hardening answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorCrossNodeIdempotencyHardeningStates
{
    /// <summary>
    /// Cross-node idempotency hardening does not currently apply to the execution runtime.
    /// </summary>
    public const string NotApplicable = "not-applicable";

    /// <summary>
    /// Cross-node idempotency hardening still remains operator-owned outside Cephalon.
    /// </summary>
    public const string OperatorOnly = "operator-only";

    /// <summary>
    /// Cross-node idempotency evidence currently looks safe for the current retry posture.
    /// </summary>
    public const string IdempotentSafe = "idempotent-safe";

    /// <summary>
    /// Cross-node idempotency remains risky because ownership or reporter-lease truth still looks stale or conflicted.
    /// </summary>
    public const string StaleOwnerRisk = "stale-owner-risk";

    /// <summary>
    /// Cross-node idempotency remains risky because retained command lineage already looks duplicated for the current retry posture.
    /// </summary>
    public const string DuplicateLineageRisk = "duplicate-lineage-risk";

    /// <summary>
    /// Cross-node idempotency remains risky because the durable replay window still lacks enough retained evidence.
    /// </summary>
    public const string ReplayWindowRisk = "replay-window-risk";
}
