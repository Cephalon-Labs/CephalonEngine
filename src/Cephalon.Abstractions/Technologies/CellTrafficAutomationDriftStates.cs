namespace Cephalon.Abstractions.Technologies;

/// <summary>
/// Defines the stable drift states for cell traffic automation materialization answers.
/// </summary>
public static class CellTrafficAutomationDriftStates
{
    /// <summary>
    /// The selected materializer observed the desired and actual control-plane posture in sync.
    /// </summary>
    public const string InSync = "in-sync";

    /// <summary>
    /// The selected materializer is still reconciling the desired and actual control-plane posture.
    /// </summary>
    public const string Reconciling = "reconciling";

    /// <summary>
    /// The selected materializer observed drift between desired and actual control-plane posture.
    /// </summary>
    public const string Drifted = "drifted";

    /// <summary>
    /// The drift posture could not be determined from the current runtime truth.
    /// </summary>
    public const string Unknown = "unknown";

    /// <summary>
    /// Multiple required materialization dimensions currently disagree about drift posture.
    /// </summary>
    public const string Mixed = "mixed";
}
