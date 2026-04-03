using System.Text.Json.Serialization;

namespace Cephalon.Abstractions.Execution;

/// <summary>
/// Describes one operator-facing execution graph contributed by an active module.
/// </summary>
public sealed class ExecutionGraphDescriptor
{
    /// <summary>
    /// Creates a new execution graph descriptor.
    /// </summary>
    /// <param name="id">The stable execution-graph identifier.</param>
    /// <param name="displayName">The operator-facing execution-graph name.</param>
    /// <param name="description">A human-readable description of the graph.</param>
    /// <param name="sourceModuleId">The module identifier that owns the graph.</param>
    /// <param name="entryNodeId">The node identifier where execution should begin.</param>
    /// <param name="nodes">The nodes that participate in the graph.</param>
    /// <param name="edges">The directed edges that connect the graph nodes.</param>
    /// <param name="tags">Optional descriptive tags associated with the graph.</param>
    /// <param name="metadata">Optional operator-facing metadata associated with the graph.</param>
    [JsonConstructor]
    public ExecutionGraphDescriptor(
        string id,
        string displayName,
        string description,
        string sourceModuleId,
        string entryNodeId,
        IReadOnlyList<ExecutionGraphNodeDescriptor> nodes,
        IReadOnlyList<ExecutionGraphEdgeDescriptor>? edges = null,
        IReadOnlyList<string>? tags = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Execution graph id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Execution graph display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Execution graph description is required.", nameof(description));
        }

        if (string.IsNullOrWhiteSpace(sourceModuleId))
        {
            throw new ArgumentException("Execution graph source module id is required.", nameof(sourceModuleId));
        }

        if (string.IsNullOrWhiteSpace(entryNodeId))
        {
            throw new ArgumentException("Execution graph entry node id is required.", nameof(entryNodeId));
        }

        ArgumentNullException.ThrowIfNull(nodes);

        Id = id.Trim();
        DisplayName = displayName.Trim();
        Description = description.Trim();
        SourceModuleId = sourceModuleId.Trim();
        EntryNodeId = entryNodeId.Trim();
        Nodes = nodes.ToArray();
        Edges = edges?.ToArray() ?? [];
        Tags = Normalize(tags);
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the stable execution-graph identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the operator-facing execution-graph name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the human-readable description of the graph.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the identifier of the module that contributed the graph.
    /// </summary>
    public string SourceModuleId { get; }

    /// <summary>
    /// Gets the node identifier where execution should begin.
    /// </summary>
    public string EntryNodeId { get; }

    /// <summary>
    /// Gets the nodes that participate in the graph.
    /// </summary>
    public IReadOnlyList<ExecutionGraphNodeDescriptor> Nodes { get; }

    /// <summary>
    /// Gets the directed edges that connect graph nodes.
    /// </summary>
    public IReadOnlyList<ExecutionGraphEdgeDescriptor> Edges { get; }

    /// <summary>
    /// Gets descriptive tags associated with the graph.
    /// </summary>
    public IReadOnlyList<string> Tags { get; }

    /// <summary>
    /// Gets operator-facing metadata associated with the graph.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    private static string[] Normalize(IReadOnlyList<string>? values)
    {
        return values?
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }
}
