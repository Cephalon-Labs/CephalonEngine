using System.Text.Json.Serialization;

namespace Cephalon.Abstractions.Execution;

/// <summary>
/// Describes one node within an execution graph.
/// </summary>
public sealed class ExecutionGraphNodeDescriptor
{
    /// <summary>
    /// Creates a new execution-graph node descriptor.
    /// </summary>
    /// <param name="id">The stable node identifier within the graph.</param>
    /// <param name="displayName">The operator-facing node name.</param>
    /// <param name="description">A human-readable description of the node.</param>
    /// <param name="kind">The node kind, such as <c>activity</c>, <c>decision</c>, or <c>wait</c>.</param>
    /// <param name="moduleId">The module identifier that primarily owns the node, when different from the graph source.</param>
    /// <param name="capabilityKey">The capability key the node intends to drive, when it maps to an existing capability contract.</param>
    /// <param name="tags">Optional descriptive tags associated with the node.</param>
    /// <param name="metadata">Optional operator-facing metadata associated with the node.</param>
    [JsonConstructor]
    public ExecutionGraphNodeDescriptor(
        string id,
        string displayName,
        string description,
        string kind,
        string? moduleId = null,
        string? capabilityKey = null,
        IReadOnlyList<string>? tags = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Execution graph node id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Execution graph node display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Execution graph node description is required.", nameof(description));
        }

        if (string.IsNullOrWhiteSpace(kind))
        {
            throw new ArgumentException("Execution graph node kind is required.", nameof(kind));
        }

        Id = id.Trim();
        DisplayName = displayName.Trim();
        Description = description.Trim();
        Kind = kind.Trim();
        ModuleId = string.IsNullOrWhiteSpace(moduleId) ? null : moduleId.Trim();
        CapabilityKey = string.IsNullOrWhiteSpace(capabilityKey) ? null : capabilityKey.Trim();
        Tags = Normalize(tags);
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the stable node identifier within the graph.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the operator-facing node name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the human-readable description of the node.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the node kind.
    /// </summary>
    public string Kind { get; }

    /// <summary>
    /// Gets the module identifier that primarily owns the node, when one was declared.
    /// </summary>
    public string? ModuleId { get; }

    /// <summary>
    /// Gets the capability key the node intends to drive, when one was declared.
    /// </summary>
    public string? CapabilityKey { get; }

    /// <summary>
    /// Gets descriptive tags associated with the node.
    /// </summary>
    public IReadOnlyList<string> Tags { get; }

    /// <summary>
    /// Gets operator-facing metadata associated with the node.
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
