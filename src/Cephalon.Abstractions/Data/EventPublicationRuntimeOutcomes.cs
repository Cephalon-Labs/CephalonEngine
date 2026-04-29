namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines stable outcome identifiers reported by event-publication runtime-state catalogs.
/// </summary>
public static class EventPublicationRuntimeOutcomes
{
    /// <summary>
    /// The publication was accepted or staged by the active runtime, but downstream delivery may still be pending.
    /// </summary>
    public const string Accepted = "accepted";

    /// <summary>
    /// The publication completed its runtime-owned execution path successfully.
    /// </summary>
    public const string Succeeded = "succeeded";

    /// <summary>
    /// The publication failed while the active runtime processed it.
    /// </summary>
    public const string Failed = "failed";

    /// <summary>
    /// The publication was accepted but no runtime-owned work was executed.
    /// </summary>
    public const string Skipped = "skipped";
}
