using System.Text.Json.Serialization;

namespace Cephalon.Abstractions.Execution;

/// <summary>
/// Describes one operator-facing hosted or background execution surface contributed by an active module.
/// </summary>
public sealed class HostedExecutionDescriptor
{
    /// <summary>
    /// Creates a new hosted execution descriptor.
    /// </summary>
    /// <param name="id">The stable hosted-execution identifier.</param>
    /// <param name="displayName">The operator-facing hosted-execution name.</param>
    /// <param name="description">A human-readable description of the hosted execution.</param>
    /// <param name="sourceModuleId">The module identifier that owns the hosted execution.</param>
    /// <param name="kind">The operator-facing hosted-execution kind such as <c>background-service</c>, <c>timer</c>, or <c>listener</c>.</param>
    /// <param name="executionGraphId">The related execution-graph identifier when this hosted execution drives one graph directly.</param>
    /// <param name="startsWithHost">
    /// A value indicating whether the hosted execution is expected to become active when the runtime host starts.
    /// </param>
    /// <param name="tags">Optional descriptive tags associated with the hosted execution.</param>
    /// <param name="metadata">Optional operator-facing metadata associated with the hosted execution.</param>
    [JsonConstructor]
    public HostedExecutionDescriptor(
        string id,
        string displayName,
        string description,
        string sourceModuleId,
        string kind,
        string? executionGraphId = null,
        bool startsWithHost = true,
        IReadOnlyList<string>? tags = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Hosted execution id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Hosted execution display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Hosted execution description is required.", nameof(description));
        }

        if (string.IsNullOrWhiteSpace(sourceModuleId))
        {
            throw new ArgumentException("Hosted execution source module id is required.", nameof(sourceModuleId));
        }

        if (string.IsNullOrWhiteSpace(kind))
        {
            throw new ArgumentException("Hosted execution kind is required.", nameof(kind));
        }

        Id = id.Trim();
        DisplayName = displayName.Trim();
        Description = description.Trim();
        SourceModuleId = sourceModuleId.Trim();
        Kind = kind.Trim();
        ExecutionGraphId = string.IsNullOrWhiteSpace(executionGraphId)
            ? null
            : executionGraphId.Trim();
        StartsWithHost = startsWithHost;
        Tags = Normalize(tags);
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the stable hosted-execution identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the operator-facing hosted-execution name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the human-readable description of the hosted execution.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the identifier of the module that contributed the hosted execution.
    /// </summary>
    public string SourceModuleId { get; }

    /// <summary>
    /// Gets the operator-facing hosted-execution kind.
    /// </summary>
    public string Kind { get; }

    /// <summary>
    /// Gets the related execution-graph identifier when one is declared.
    /// </summary>
    public string? ExecutionGraphId { get; }

    /// <summary>
    /// Gets a value indicating whether the hosted execution is expected to become active when the runtime host starts.
    /// </summary>
    public bool StartsWithHost { get; }

    /// <summary>
    /// Gets descriptive tags associated with the hosted execution.
    /// </summary>
    public IReadOnlyList<string> Tags { get; }

    /// <summary>
    /// Gets operator-facing metadata associated with the hosted execution.
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
