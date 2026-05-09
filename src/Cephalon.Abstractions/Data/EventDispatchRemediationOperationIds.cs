namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines stable event-dispatch remediation operation identifiers.
/// </summary>
public static class EventDispatchRemediationOperationIds
{
    /// <summary>
    /// Makes a staged event immediately eligible for another dispatch attempt.
    /// </summary>
    public const string RetryNow = "retry-now";

    /// <summary>
    /// Makes a staged event eligible for another dispatch attempt at a later time.
    /// </summary>
    public const string RetryLater = "retry-later";

    /// <summary>
    /// Marks a staged event as intentionally skipped by an operator.
    /// </summary>
    public const string Skip = "skip";

    /// <summary>
    /// Marks a staged event as terminally failed so it stops re-entering pending-dispatch reads.
    /// </summary>
    public const string Quarantine = "quarantine";
}
