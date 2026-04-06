namespace Cephalon.Behaviors.Exceptions;

/// <summary>Thrown when a dispatch lookup fails because no behavior is registered with the given identifier.</summary>
public sealed class BehaviorNotFoundException : Exception
{
    /// <summary>Initializes a new instance of <see cref="BehaviorNotFoundException"/>.</summary>
    public BehaviorNotFoundException(string behaviorId)
        : base($"No behavior registered with identifier '{behaviorId}'.")
    {
        BehaviorId = behaviorId;
    }

    /// <summary>Initializes a new instance of <see cref="BehaviorNotFoundException"/> with an inner exception.</summary>
    public BehaviorNotFoundException(string behaviorId, Exception innerException)
        : base($"No behavior registered with identifier '{behaviorId}'.", innerException)
    {
        BehaviorId = behaviorId;
    }

    /// <summary>Gets the behavior identifier that was not found.</summary>
    public string BehaviorId { get; }
}
