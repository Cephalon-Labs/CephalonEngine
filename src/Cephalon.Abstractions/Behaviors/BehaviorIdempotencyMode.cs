namespace Cephalon.Abstractions.Behaviors;

/// <summary>
/// Describes whether a behavior execution is safe to replay automatically.
/// </summary>
public enum BehaviorIdempotencyMode
{
    /// <summary>
    /// No explicit idempotency contract was declared for the behavior.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Replaying the same logical behavior execution is expected to be safe.
    /// </summary>
    Idempotent = 1,

    /// <summary>
    /// Replaying the same logical behavior execution is not expected to be safe.
    /// </summary>
    NonIdempotent = 2
}
