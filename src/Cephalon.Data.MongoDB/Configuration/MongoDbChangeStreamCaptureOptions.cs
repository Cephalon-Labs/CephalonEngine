namespace Cephalon.Data.MongoDB.Configuration;

/// <summary>
/// Declares one provider-native MongoDB change-stream capture for the active runtime.
/// </summary>
public sealed class MongoDbChangeStreamCaptureOptions
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
    /// Gets or sets the logical source identifier when it should differ from the watched collection path.
    /// </summary>
    public string SourceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the database name to watch. When omitted, the pack-level database name is used.
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// Gets or sets the collection name that the provider-native change stream should watch.
    /// </summary>
    public string CollectionName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the outbox identifier that receives emitted publications.
    /// </summary>
    public string OutboxId { get; set; } = "mongodb-outbox";

    /// <summary>
    /// Gets or sets the logical outbox channel that receives emitted publications.
    /// </summary>
    public string ChannelId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the logical message type emitted for each captured change event.
    /// </summary>
    public string MessageType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the operator-facing event format projected on the CDC descriptor.
    /// </summary>
    public string EventFormat { get; set; } = "mongodb-change-stream-event";

    /// <summary>
    /// Gets or sets the MongoDB full-document mode, such as <c>update-lookup</c> or <c>when-available</c>.
    /// </summary>
    public string FullDocumentMode { get; set; } = "update-lookup";

    /// <summary>
    /// Gets or sets the maximum await time, in seconds, for one provider-native change-stream batch.
    /// </summary>
    public int MaxAwaitTimeSeconds { get; set; } = 5;

    /// <summary>
    /// Gets or sets the optional batch size for each provider-native change-stream batch.
    /// </summary>
    public int? BatchSize { get; set; }

    /// <summary>
    /// Gets the resource identifiers observed by the capture.
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
