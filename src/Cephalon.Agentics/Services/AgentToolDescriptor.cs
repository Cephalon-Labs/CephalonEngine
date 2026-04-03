using System.Text.Json.Serialization;

namespace Cephalon.Agentics.Services;

/// <summary>
/// Describes a tool that can be surfaced through the agentic runtime pack.
/// </summary>
public sealed class AgentToolDescriptor
{
    /// <summary>
    /// Creates a new agent tool descriptor.
    /// </summary>
    /// <param name="id">The stable tool identifier.</param>
    /// <param name="displayName">The operator-facing tool name.</param>
    /// <param name="description">The human-readable description of the tool.</param>
    /// <param name="tags">Optional tags that classify the tool.</param>
    /// <param name="capabilityKeys">Optional capability keys that the tool expects to use through the active runtime.</param>
    /// <param name="executionGraphId">The related execution-graph identifier when the tool coordinates a published orchestration flow.</param>
    /// <param name="hostedExecutionId">The related hosted-execution identifier when the tool coordinates one host-managed background surface.</param>
    /// <param name="metadata">Optional operator-facing metadata that should flow through the runtime surface.</param>
    [JsonConstructor]
    public AgentToolDescriptor(
        string id,
        string displayName,
        string description,
        IReadOnlyList<string>? tags = null,
        IReadOnlyList<string>? capabilityKeys = null,
        string? executionGraphId = null,
        string? hostedExecutionId = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Tool id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Tool display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Tool description is required.", nameof(description));
        }

        Id = id.Trim();
        DisplayName = displayName.Trim();
        Description = description.Trim();
        Tags = Normalize(tags);
        CapabilityKeys = Normalize(capabilityKeys);
        ExecutionGraphId = NormalizeSingleValue(executionGraphId);
        HostedExecutionId = NormalizeSingleValue(hostedExecutionId);
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the stable tool identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the operator-facing display name for the tool.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the human-readable description of the tool.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the normalized tag set associated with the tool.
    /// </summary>
    public IReadOnlyList<string> Tags { get; }

    /// <summary>
    /// Gets the capability keys that the tool expects to use through the active runtime.
    /// </summary>
    public IReadOnlyList<string> CapabilityKeys { get; }

    /// <summary>
    /// Gets the related execution-graph identifier when one is declared.
    /// </summary>
    public string? ExecutionGraphId { get; }

    /// <summary>
    /// Gets the related hosted-execution identifier when one is declared.
    /// </summary>
    public string? HostedExecutionId { get; }

    /// <summary>
    /// Gets additional operator-facing metadata associated with the tool.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    private static string[] Normalize(IReadOnlyList<string>? values)
    {
        return values?
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }

    private static string? NormalizeSingleValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
