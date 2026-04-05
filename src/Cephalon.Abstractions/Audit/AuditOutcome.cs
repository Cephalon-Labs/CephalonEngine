namespace Cephalon.Abstractions.Audit;

/// <summary>
/// Identifies the outcome recorded for an audit entry.
/// </summary>
public enum AuditOutcome
{
    /// <summary>
    /// Indicates the operation outcome was not explicitly classified.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Indicates the operation completed successfully.
    /// </summary>
    Succeeded = 1,

    /// <summary>
    /// Indicates the operation failed.
    /// </summary>
    Failed = 2
}
