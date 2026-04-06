namespace Cephalon.Behaviors.Exceptions;

/// <summary>Thrown when a resolved topology violates the allowlist declared by <c>[BehaviorAllowedPatterns]</c> or <c>[BehaviorAllowedTransports]</c>.</summary>
public sealed class BehaviorSecurityException : Exception
{
    /// <summary>Initializes a new instance of <see cref="BehaviorSecurityException"/>.</summary>
    public BehaviorSecurityException(string behaviorId, string message)
        : base(message)
    {
        BehaviorId = behaviorId;
    }

    /// <summary>Initializes a new instance of <see cref="BehaviorSecurityException"/> with an inner exception.</summary>
    public BehaviorSecurityException(string behaviorId, string message, Exception innerException)
        : base(message, innerException)
    {
        BehaviorId = behaviorId;
    }

    /// <summary>Gets the behavior identifier that triggered the allowlist violation.</summary>
    public string BehaviorId { get; }
}
