namespace Cephalon.Data.Postgres.Configuration;

/// <summary>
/// Declares one provider-native PostgreSQL logical-replication capture for the active runtime.
/// </summary>
public sealed class PostgresLogicalReplicationCaptureOptions
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
    /// Gets or sets the logical source identifier when it should differ from the watched table path.
    /// </summary>
    public string SourceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the PostgreSQL publication that should emit the tracked table changes.
    /// </summary>
    public string PublicationName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the PostgreSQL logical replication slot used for durable progress.
    /// </summary>
    public string SlotName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the schema name of the tracked table.
    /// </summary>
    public string TableSchema { get; set; } = "public";

    /// <summary>
    /// Gets or sets the table name of the tracked table.
    /// </summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the outbox identifier that receives emitted publications.
    /// </summary>
    public string OutboxId { get; set; } = string.Empty;

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
    public string EventFormat { get; set; } = "postgresql-logical-replication-event";

    /// <summary>
    /// Gets or sets the initial position used when the logical replication slot must be created.
    /// </summary>
    /// <remarks>
    /// Supported values are <c>slot-consistent-point</c> and <c>latest-available</c>.
    /// </remarks>
    public string InitialPosition { get; set; } = "slot-consistent-point";

    /// <summary>
    /// Gets or sets a value indicating whether the pack should create the logical replication slot when it does not exist yet.
    /// </summary>
    public bool CreateSlotIfMissing { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of committed logical-replication changes to stage during one iteration.
    /// </summary>
    public int MaxChangesPerRead { get; set; } = 128;

    /// <summary>
    /// Gets or sets the maximum number of seconds to await committed WAL messages during one provider-native iteration.
    /// </summary>
    public int MaxAwaitTimeSeconds { get; set; } = 5;

    /// <summary>
    /// Gets or sets the polling interval, in seconds, between hosted-service iterations.
    /// </summary>
    public int PollingIntervalSeconds { get; set; } = 5;

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
