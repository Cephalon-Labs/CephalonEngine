namespace Cephalon.Abstractions.Technologies;

/// <summary>
/// Defines the stable lifecycle actions for cell traffic automation materialization answers.
/// </summary>
public static class CellTrafficAutomationLifecycleActions
{
    /// <summary>
    /// The selected materializer is projecting desired intent without applying it yet.
    /// </summary>
    public const string Project = "project";

    /// <summary>
    /// The selected materializer is observing control-plane truth without writing changes.
    /// </summary>
    public const string Observe = "observe";

    /// <summary>
    /// The selected materializer is reconciling the desired and actual control-plane posture.
    /// </summary>
    public const string Reconcile = "reconcile";

    /// <summary>
    /// The selected materializer created a new owned control-plane resource.
    /// </summary>
    public const string Create = "create";

    /// <summary>
    /// The selected materializer replaced or updated an existing owned control-plane resource.
    /// </summary>
    public const string Replace = "replace";

    /// <summary>
    /// The selected materializer deleted an owned control-plane resource.
    /// </summary>
    public const string Delete = "delete";

    /// <summary>
    /// The selected materializer pruned a no-longer-desired owned control-plane resource.
    /// </summary>
    public const string Prune = "prune";

    /// <summary>
    /// The selected materializer transferred ownership to another reconciler.
    /// </summary>
    public const string Transfer = "transfer";
}
