namespace Cephalon.Abstractions.Behaviors;

/// <summary>
/// Represents a transport-neutral behavior outcome.
/// </summary>
public enum BehaviorResultStatus
{
    /// <summary>
    /// The behavior completed successfully and returned a value.
    /// </summary>
    Ok = 0,

    /// <summary>
    /// The behavior created a new resource or record.
    /// </summary>
    Created = 1,

    /// <summary>
    /// The behavior accepted the request for asynchronous work.
    /// </summary>
    Accepted = 2,

    /// <summary>
    /// The behavior completed successfully without a response payload.
    /// </summary>
    NoContent = 3,

    /// <summary>
    /// The request was invalid for the target behavior.
    /// </summary>
    Invalid = 10,

    /// <summary>
    /// The caller is not authenticated for the requested behavior.
    /// </summary>
    Unauthorized = 11,

    /// <summary>
    /// The caller is authenticated but not allowed to perform the requested action.
    /// </summary>
    Forbidden = 12,

    /// <summary>
    /// The requested resource or target was not found.
    /// </summary>
    NotFound = 13,

    /// <summary>
    /// The request conflicts with the current state of the target resource.
    /// </summary>
    Conflict = 14
}
