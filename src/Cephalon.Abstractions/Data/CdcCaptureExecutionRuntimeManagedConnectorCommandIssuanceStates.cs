namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable state identifiers used by managed-connector command-issuance answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStates
{
    /// <summary>
    /// The execution runtime does not currently produce a managed-connector command-issuance answer.
    /// </summary>
    public const string NotApplicable = "not-applicable";

    /// <summary>
    /// The managed connector still has one or more blockers before Cephalon can trust the shared issuance lane.
    /// </summary>
    public const string Blocked = "blocked";

    /// <summary>
    /// Cephalon can describe command issuance posture, but the write-path still remains operator-owned.
    /// </summary>
    public const string OperatorOnly = "operator-only";

    /// <summary>
    /// Cephalon can accept the command onto a future shared issuance lane, but approval still gates later execution handoff.
    /// </summary>
    public const string Accepted = "accepted";

    /// <summary>
    /// Cephalon rejects the current command issuance because shared runtime truth indicates no additional write-path changes are needed.
    /// </summary>
    public const string Rejected = "rejected";

    /// <summary>
    /// Cephalon can mark the command as issued on the shared issuance lane for a later provider-execution slice.
    /// </summary>
    public const string Issued = "issued";
}
