namespace Cephalon.Abstractions.Behaviors;

/// <summary>
/// Declares whether a behavior execution is safe to replay automatically.
/// </summary>
/// <remarks>
/// Cephalon uses this behavior-authored contract when resilience features need to decide whether
/// transient failures should stay fail-fast only or can later participate in automatic retry.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class BehaviorIdempotencyAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BehaviorIdempotencyAttribute" /> class and
    /// marks the behavior as idempotent.
    /// </summary>
    public BehaviorIdempotencyAttribute()
        : this(BehaviorIdempotencyMode.Idempotent)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BehaviorIdempotencyAttribute" /> class.
    /// </summary>
    /// <param name="mode">The declared idempotency mode.</param>
    public BehaviorIdempotencyAttribute(BehaviorIdempotencyMode mode)
    {
        Mode = mode;
    }

    /// <summary>
    /// Gets the declared idempotency mode.
    /// </summary>
    public BehaviorIdempotencyMode Mode { get; }
}
