namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the recommended stable lag-state identifiers for CDC runtime reporting.
/// </summary>
public static class CdcCaptureLagStates
{
    /// <summary>
    /// Indicates that the active runtime does not yet have a lag answer.
    /// </summary>
    public const string Unknown = "unknown";

    /// <summary>
    /// Indicates that the provider reports the capture as caught up.
    /// </summary>
    public const string Current = "current";

    /// <summary>
    /// Indicates that the provider reports the capture as lagging behind the source stream.
    /// </summary>
    public const string Lagging = "lagging";

    /// <summary>
    /// Indicates that the provider reports the capture as intentionally backfilling older changes.
    /// </summary>
    public const string Backfilling = "backfilling";
}
