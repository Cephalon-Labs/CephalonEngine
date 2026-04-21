using System.Text.Json.Serialization;

namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes one change-data-capture surface contributed to the active runtime.
/// </summary>
public sealed class CdcCaptureDescriptor
{
    /// <summary>
    /// Creates a new CDC capture descriptor.
    /// </summary>
    /// <param name="id">The stable CDC capture identifier.</param>
    /// <param name="displayName">The operator-facing CDC capture name.</param>
    /// <param name="description">The human-readable CDC capture description.</param>
    /// <param name="sourceModuleId">The module identifier that owns the CDC capture.</param>
    /// <param name="provider">The logical provider identifier that supplies the change feed.</param>
    /// <param name="sourceId">The logical source stream, database, or feed identifier.</param>
    /// <param name="outboxId">The outbox identifier that receives captured publications.</param>
    /// <param name="mode">The capture mode such as <c>wal</c>, <c>change-stream</c>, or <c>table-tail</c>.</param>
    /// <param name="eventFormat">The emitted change-event format such as <c>debezium-envelope</c>.</param>
    /// <param name="resourceIds">Optional resource identifiers such as tables, collections, or topics observed by the capture.</param>
    /// <param name="tags">Optional descriptive tags associated with the CDC capture.</param>
    /// <param name="metadata">Optional operator-facing metadata associated with the CDC capture.</param>
    [JsonConstructor]
    public CdcCaptureDescriptor(
        string id,
        string displayName,
        string description,
        string sourceModuleId,
        string provider,
        string sourceId,
        string outboxId,
        string mode = "log-based",
        string eventFormat = "debezium-envelope",
        IReadOnlyList<string>? resourceIds = null,
        IReadOnlyList<string>? tags = null,
        IReadOnlyDictionary<string, string>? metadata = null)
        : this(
            id,
            displayName,
            description,
            sourceModuleId,
            provider,
            sourceId,
            outboxId,
            CdcCaptureExecutionBindingDescriptor.Unbound(id),
            mode,
            eventFormat,
            resourceIds,
            tags,
            metadata)
    {
    }

    /// <summary>
    /// Creates a new CDC capture descriptor.
    /// </summary>
    /// <param name="id">The stable CDC capture identifier.</param>
    /// <param name="displayName">The operator-facing CDC capture name.</param>
    /// <param name="description">The human-readable CDC capture description.</param>
    /// <param name="sourceModuleId">The module identifier that owns the CDC capture.</param>
    /// <param name="provider">The logical provider identifier that supplies the change feed.</param>
    /// <param name="sourceId">The logical source stream, database, or feed identifier.</param>
    /// <param name="outboxId">The outbox identifier that receives captured publications.</param>
    /// <param name="executionBinding">
    /// The authored or effective execution-binding answer for the CDC capture. When omitted, the capture starts unbound.
    /// </param>
    /// <param name="mode">The capture mode such as <c>wal</c>, <c>change-stream</c>, or <c>table-tail</c>.</param>
    /// <param name="eventFormat">The emitted change-event format such as <c>debezium-envelope</c>.</param>
    /// <param name="resourceIds">Optional resource identifiers such as tables, collections, or topics observed by the capture.</param>
    /// <param name="tags">Optional descriptive tags associated with the CDC capture.</param>
    /// <param name="metadata">Optional operator-facing metadata associated with the CDC capture.</param>
    public CdcCaptureDescriptor(
        string id,
        string displayName,
        string description,
        string sourceModuleId,
        string provider,
        string sourceId,
        string outboxId,
        CdcCaptureExecutionBindingDescriptor executionBinding,
        string mode = "log-based",
        string eventFormat = "debezium-envelope",
        IReadOnlyList<string>? resourceIds = null,
        IReadOnlyList<string>? tags = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("CDC capture id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("CDC capture display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("CDC capture description is required.", nameof(description));
        }

        if (string.IsNullOrWhiteSpace(sourceModuleId))
        {
            throw new ArgumentException("CDC capture source module id is required.", nameof(sourceModuleId));
        }

        if (string.IsNullOrWhiteSpace(provider))
        {
            throw new ArgumentException("CDC capture provider is required.", nameof(provider));
        }

        if (string.IsNullOrWhiteSpace(sourceId))
        {
            throw new ArgumentException("CDC capture source id is required.", nameof(sourceId));
        }

        if (string.IsNullOrWhiteSpace(outboxId))
        {
            throw new ArgumentException("CDC capture outbox id is required.", nameof(outboxId));
        }

        if (string.IsNullOrWhiteSpace(mode))
        {
            throw new ArgumentException("CDC capture mode is required.", nameof(mode));
        }

        if (string.IsNullOrWhiteSpace(eventFormat))
        {
            throw new ArgumentException("CDC capture event format is required.", nameof(eventFormat));
        }

        Id = id.Trim();
        DisplayName = displayName.Trim();
        Description = description.Trim();
        SourceModuleId = sourceModuleId.Trim();
        Provider = provider.Trim();
        SourceId = sourceId.Trim();
        OutboxId = outboxId.Trim();
        Mode = mode.Trim();
        EventFormat = eventFormat.Trim();
        ResourceIds = Normalize(resourceIds);
        Tags = Normalize(tags);
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
        ExecutionBinding = ValidateExecutionBinding(executionBinding, Id);
    }

    /// <summary>
    /// Gets the stable CDC capture identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the operator-facing CDC capture name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the human-readable CDC capture description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the identifier of the module that owns the CDC capture.
    /// </summary>
    public string SourceModuleId { get; }

    /// <summary>
    /// Gets the logical provider identifier that supplies the change feed.
    /// </summary>
    public string Provider { get; }

    /// <summary>
    /// Gets the logical source stream, database, or feed identifier.
    /// </summary>
    public string SourceId { get; }

    /// <summary>
    /// Gets the outbox identifier that receives captured publications.
    /// </summary>
    public string OutboxId { get; }

    /// <summary>
    /// Gets the capture mode.
    /// </summary>
    public string Mode { get; }

    /// <summary>
    /// Gets the emitted change-event format.
    /// </summary>
    public string EventFormat { get; }

    /// <summary>
    /// Gets the resource identifiers observed by the capture.
    /// </summary>
    public IReadOnlyList<string> ResourceIds { get; }

    /// <summary>
    /// Gets descriptive tags associated with the CDC capture.
    /// </summary>
    public IReadOnlyList<string> Tags { get; }

    /// <summary>
    /// Gets operator-facing metadata associated with the CDC capture.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    /// <summary>
    /// Gets the authored or effective execution-binding answer for the CDC capture.
    /// </summary>
    public CdcCaptureExecutionBindingDescriptor ExecutionBinding { get; init; }

    /// <summary>
    /// Creates a copy of the CDC capture descriptor with a different execution-binding answer.
    /// </summary>
    /// <param name="executionBinding">The execution-binding answer to apply.</param>
    /// <returns>A new CDC capture descriptor with the requested execution binding.</returns>
    public CdcCaptureDescriptor WithExecutionBinding(CdcCaptureExecutionBindingDescriptor executionBinding)
    {
        return new CdcCaptureDescriptor(
            id: Id,
            displayName: DisplayName,
            description: Description,
            sourceModuleId: SourceModuleId,
            provider: Provider,
            sourceId: SourceId,
            outboxId: OutboxId,
            executionBinding: executionBinding,
            mode: Mode,
            eventFormat: EventFormat,
            resourceIds: ResourceIds,
            tags: Tags,
            metadata: Metadata);
    }

    private static string[] Normalize(IReadOnlyList<string>? values)
    {
        return values?
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }

    private static CdcCaptureExecutionBindingDescriptor ValidateExecutionBinding(
        CdcCaptureExecutionBindingDescriptor? executionBinding,
        string cdcCaptureId)
    {
        var binding = executionBinding ?? CdcCaptureExecutionBindingDescriptor.Unbound(cdcCaptureId);
        if (!string.Equals(binding.CdcCaptureId, cdcCaptureId, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                $"CDC capture execution binding '{binding.CdcCaptureId}' does not match capture '{cdcCaptureId}'.",
                nameof(executionBinding));
        }

        return binding;
    }
}
