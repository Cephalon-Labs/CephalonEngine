namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the recommended stable freshness-state identifiers for CDC runtime reporting.
/// </summary>
public static class CdcCaptureFreshnessStates
{
    /// <summary>
    /// Indicates that the active runtime does not yet have a freshness answer.
    /// </summary>
    public const string Unknown = "unknown";

    /// <summary>
    /// Indicates that the provider reports the capture as fresh.
    /// </summary>
    public const string Fresh = "fresh";

    /// <summary>
    /// Indicates that the provider reports the capture as stale.
    /// </summary>
    public const string Stale = "stale";
}
