namespace Cephalon.AspNetCore.Hosting;

/// <summary>
/// Represents the operator HTTP request body used to query a knowledge collection.
/// </summary>
public sealed class KnowledgeQueryHttpRequest
{
    /// <summary>
    /// Gets or initializes the query text.
    /// </summary>
    public string? QueryText { get; init; }

    /// <summary>
    /// Gets or initializes the maximum number of results to return.
    /// </summary>
    public int? MaxResults { get; init; }

    /// <summary>
    /// Gets or initializes the actor identifier responsible for the query.
    /// </summary>
    public string? ActorId { get; init; }

    /// <summary>
    /// Gets or initializes the correlation identifier for the query.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Gets or initializes metadata to attach to the query.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
