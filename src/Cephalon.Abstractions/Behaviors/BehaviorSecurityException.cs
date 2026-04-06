namespace Cephalon.Abstractions.Behaviors;

/// <summary>
/// Thrown when a behavior's resolved topology violates an allowlist constraint
/// declared via <see cref="BehaviorAllowedPatternsAttribute" /> or <see cref="BehaviorAllowedTransportsAttribute" />.
/// </summary>
public sealed class BehaviorSecurityException : Exception
{
    /// <summary>
    /// Initializes the exception with the behavior identifier and a descriptive message.
    /// </summary>
    /// <param name="behaviorId">The behavior identifier that triggered the violation.</param>
    /// <param name="message">A human-readable description of the security violation.</param>
    public BehaviorSecurityException(string behaviorId, string message)
        : base(message)
    {
        BehaviorId = behaviorId;
    }

    /// <summary>
    /// Initializes the exception with the behavior identifier, a descriptive message, and an inner exception.
    /// </summary>
    /// <param name="behaviorId">The behavior identifier that triggered the violation.</param>
    /// <param name="message">A human-readable description of the security violation.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public BehaviorSecurityException(string behaviorId, string message, Exception innerException)
        : base(message, innerException)
    {
        BehaviorId = behaviorId;
    }

    /// <summary>
    /// Gets the behavior identifier that triggered the security violation.
    /// </summary>
    public string BehaviorId { get; }
}
