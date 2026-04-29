namespace Cephalon.Abstractions.Retrieval;

/// <summary>
/// Defines stable freshness state identifiers for managed retrieval indexes.
/// </summary>
public static class KnowledgeIndexFreshnessStates
{
    /// <summary>
    /// The collection has not been indexed.
    /// </summary>
    public const string NotIndexed = "not-indexed";

    /// <summary>
    /// The latest successful index is within the configured freshness window.
    /// </summary>
    public const string Fresh = "fresh";

    /// <summary>
    /// The latest successful index is older than the configured freshness window.
    /// </summary>
    public const string Stale = "stale";

    /// <summary>
    /// The latest indexing run failed.
    /// </summary>
    public const string Failed = "failed";

    /// <summary>
    /// The latest indexing run was skipped.
    /// </summary>
    public const string Skipped = "skipped";
}
