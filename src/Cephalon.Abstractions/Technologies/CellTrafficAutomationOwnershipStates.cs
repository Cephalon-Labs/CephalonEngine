namespace Cephalon.Abstractions.Technologies;

/// <summary>
/// Defines the stable ownership states for cell traffic automation materialization answers.
/// </summary>
public static class CellTrafficAutomationOwnershipStates
{
    /// <summary>
    /// The automation is requested for reconciliation, but ownership has not been observed yet.
    /// </summary>
    public const string Requested = "requested";

    /// <summary>
    /// The selected materializer currently owns the reconciled control-plane resources.
    /// </summary>
    public const string Owned = "owned";

    /// <summary>
    /// The selected materializer observed a conflicting owner for the target resources.
    /// </summary>
    public const string OwnershipConflict = "ownership-conflict";

    /// <summary>
    /// The selected materializer observed resources that no longer have an active desired owner.
    /// </summary>
    public const string Orphaned = "orphaned";

    /// <summary>
    /// The selected materializer pruned previously owned resources.
    /// </summary>
    public const string Pruned = "pruned";

    /// <summary>
    /// The selected materializer transferred ownership to another reconciler.
    /// </summary>
    public const string Transferred = "transferred";

    /// <summary>
    /// The ownership posture could not be determined from the current runtime truth.
    /// </summary>
    public const string Unknown = "unknown";

    /// <summary>
    /// Multiple required materialization dimensions currently disagree about ownership posture.
    /// </summary>
    public const string Mixed = "mixed";
}
