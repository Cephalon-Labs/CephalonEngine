using System.Text.Json.Serialization;

namespace Cephalon.Abstractions.Execution;

/// <summary>
/// Describes one directed edge within an execution graph.
/// </summary>
public sealed class ExecutionGraphEdgeDescriptor
{
    /// <summary>
    /// Creates a new execution-graph edge descriptor.
    /// </summary>
    /// <param name="fromNodeId">The source node identifier.</param>
    /// <param name="toNodeId">The destination node identifier.</param>
    /// <param name="displayName">An optional operator-facing label for the edge.</param>
    /// <param name="condition">An optional condition or routing hint associated with the edge.</param>
    /// <param name="metadata">Optional operator-facing metadata associated with the edge.</param>
    [JsonConstructor]
    public ExecutionGraphEdgeDescriptor(
        string fromNodeId,
        string toNodeId,
        string? displayName = null,
        string? condition = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(fromNodeId))
        {
            throw new ArgumentException("Execution graph edge source node id is required.", nameof(fromNodeId));
        }

        if (string.IsNullOrWhiteSpace(toNodeId))
        {
            throw new ArgumentException("Execution graph edge destination node id is required.", nameof(toNodeId));
        }

        FromNodeId = fromNodeId.Trim();
        ToNodeId = toNodeId.Trim();
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
        Condition = string.IsNullOrWhiteSpace(condition) ? null : condition.Trim();
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the source node identifier.
    /// </summary>
    public string FromNodeId { get; }

    /// <summary>
    /// Gets the destination node identifier.
    /// </summary>
    public string ToNodeId { get; }

    /// <summary>
    /// Gets the optional operator-facing label for the edge.
    /// </summary>
    public string? DisplayName { get; }

    /// <summary>
    /// Gets the optional condition or routing hint for the edge.
    /// </summary>
    public string? Condition { get; }

    /// <summary>
    /// Gets optional operator-facing metadata associated with the edge.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }
}
