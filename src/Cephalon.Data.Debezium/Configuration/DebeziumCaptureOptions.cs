namespace Cephalon.Data.Debezium.Configuration;

/// <summary>
/// Declares one Debezium-managed CDC capture that should publish truth through the shared Cephalon CDC runtime surfaces.
/// </summary>
public sealed class DebeziumCaptureOptions
{
    /// <summary>
    /// Gets or sets the stable CDC capture identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the operator-facing CDC capture name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the human-readable CDC capture description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the module identifier that owns the capture surface.
    /// </summary>
    public string SourceModuleId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the logical source identifier when it should differ from the derived connector or topic path.
    /// </summary>
    public string SourceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the outbox identifier that the external managed connector logically feeds.
    /// </summary>
    public string OutboxId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the external topic name that carries the Debezium envelope for this capture.
    /// </summary>
    public string TopicName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the operator-facing capture mode projected on the shared descriptor.
    /// </summary>
    public string Mode { get; set; } = "managed-connector";

    /// <summary>
    /// Gets or sets the operator-facing event format projected on the shared descriptor.
    /// </summary>
    public string EventFormat { get; set; } = "debezium-envelope";

    /// <summary>
    /// Gets or sets the Debezium snapshot mode when the pack should publish it as operator-facing metadata.
    /// </summary>
    public string SnapshotMode { get; set; } = "connector-default";

    /// <summary>
    /// Gets the resource identifiers observed by the capture, such as tables, topics, or collections.
    /// </summary>
    public IList<string> ResourceIds { get; } = [];

    /// <summary>
    /// Gets the descriptive tags associated with the capture.
    /// </summary>
    public IList<string> Tags { get; } = [];

    /// <summary>
    /// Gets arbitrary operator-facing metadata that should flow through the capture descriptor.
    /// </summary>
    public IDictionary<string, string> Metadata { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
