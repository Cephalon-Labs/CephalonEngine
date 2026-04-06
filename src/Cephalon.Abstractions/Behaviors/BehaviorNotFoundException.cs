namespace Cephalon.Abstractions.Behaviors;

/// <summary>
/// Thrown when a behavior cannot be located in the active runtime's behavior catalog.
/// </summary>
public sealed class BehaviorNotFoundException : Exception
{
    /// <summary>
    /// Initializes the exception for the given behavior identifier.
    /// </summary>
    /// <param name="behaviorId">The behavior identifier that could not be resolved.</param>
    public BehaviorNotFoundException(string behaviorId)
        : base($"No behavior with id '{behaviorId}' is registered in the active runtime.")
    {
        BehaviorId = behaviorId;
    }

    /// <summary>
    /// Initializes the exception for the given behavior identifier with an inner exception.
    /// </summary>
    /// <param name="behaviorId">The behavior identifier that could not be resolved.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public BehaviorNotFoundException(string behaviorId, Exception innerException)
        : base($"No behavior with id '{behaviorId}' is registered in the active runtime.", innerException)
    {
        BehaviorId = behaviorId;
    }

    /// <summary>
    /// Gets the behavior identifier that could not be resolved.
    /// </summary>
    public string BehaviorId { get; }
}
