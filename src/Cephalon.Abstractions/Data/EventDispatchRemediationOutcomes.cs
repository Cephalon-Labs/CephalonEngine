namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines stable outcomes returned by event-dispatch remediation commands.
/// </summary>
public static class EventDispatchRemediationOutcomes
{
    /// <summary>
    /// The command was accepted and applied to the active dispatch store.
    /// </summary>
    public const string Accepted = "accepted";

    /// <summary>
    /// The command was rejected before it could be applied to the active dispatch store.
    /// </summary>
    public const string Rejected = "rejected";
}
