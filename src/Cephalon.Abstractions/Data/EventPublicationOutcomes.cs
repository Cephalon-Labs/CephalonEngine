namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines stable outcome identifiers returned by managed event-publication dispatchers.
/// </summary>
public static class EventPublicationOutcomes
{
    /// <summary>
    /// The publication was accepted by the active eventing runtime.
    /// </summary>
    public const string Accepted = "accepted";

    /// <summary>
    /// The publication was rejected or failed before it could be accepted by the active eventing runtime.
    /// </summary>
    public const string Failed = "failed";
}
