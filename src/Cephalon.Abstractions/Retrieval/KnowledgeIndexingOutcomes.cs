namespace Cephalon.Abstractions.Retrieval;

/// <summary>
/// Defines stable outcome identifiers emitted by managed retrieval indexing.
/// </summary>
public static class KnowledgeIndexingOutcomes
{
    /// <summary>
    /// Indexing started for a collection.
    /// </summary>
    public const string Started = "started";

    /// <summary>
    /// Indexing completed successfully.
    /// </summary>
    public const string Succeeded = "succeeded";

    /// <summary>
    /// Indexing failed before a replacement index could be published.
    /// </summary>
    public const string Failed = "failed";

    /// <summary>
    /// Indexing was skipped because required runtime inputs were not available.
    /// </summary>
    public const string Skipped = "skipped";
}
