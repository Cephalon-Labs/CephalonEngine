using Cephalon.Abstractions.Behaviors;

namespace Cephalon.Behaviors.Services;

/// <summary>
/// Describes one behavior implementation registered through source generation or explicit host code.
/// </summary>
/// <remarks>
/// The descriptor is a compile-time or explicitly authored registration record. Runtime components
/// consume these descriptors instead of a mutable behavior type registry.
/// </remarks>
public sealed class BehaviorImplementationDescriptor
{
    /// <summary>
    /// Initializes a new <see cref="BehaviorImplementationDescriptor" />.
    /// </summary>
    /// <param name="id">The stable behavior identifier.</param>
    /// <param name="behaviorType">The concrete behavior implementation type.</param>
    /// <param name="idempotencyMode">The declared idempotency mode for behavior execution.</param>
    public BehaviorImplementationDescriptor(
        string id,
        Type behaviorType,
        BehaviorIdempotencyMode idempotencyMode = BehaviorIdempotencyMode.Unknown)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        Id = id.Trim();
        BehaviorType = behaviorType ?? throw new ArgumentNullException(nameof(behaviorType));
        IdempotencyMode = idempotencyMode;
    }

    /// <summary>
    /// Gets the stable behavior identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the concrete behavior implementation type.
    /// </summary>
    public Type BehaviorType { get; }

    /// <summary>
    /// Gets the declared idempotency mode for behavior execution.
    /// </summary>
    public BehaviorIdempotencyMode IdempotencyMode { get; }
}
