namespace Cephalon.Data.MySql.Configuration;

/// <summary>
/// Declares one provider-native MySQL binlog capture for the active runtime.
/// </summary>
public sealed class MySqlBinlogCaptureOptions
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
    /// Gets or sets the MySQL schema name of the tracked table.
    /// </summary>
    /// <remarks>
    /// When this value is blank, the pack falls back to <see cref="MySqlDataOptions.DatabaseName" />.
    /// </remarks>
    public string TableSchema { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the table name of the tracked table.
    /// </summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the replication-client server identifier used for this capture connection.
    /// </summary>
    /// <remarks>
    /// MySQL expects a stable positive server id per replication client. When multiple captures run concurrently,
    /// configure a distinct value for each capture.
    /// </remarks>
    public int ServerId { get; set; } = 700001;

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
    public string EventFormat { get; set; } = "mysql-binlog-row-event";

    /// <summary>
    /// Gets or sets the initial position used when no durable checkpoint exists yet.
    /// </summary>
    /// <remarks>
    /// Supported values are <c>latest-available</c> and <c>earliest-available</c>.
    /// </remarks>
    public string InitialPosition { get; set; } = "latest-available";

    /// <summary>
    /// Gets or sets the maximum number of captured row changes to stage during one provider-native iteration.
    /// </summary>
    public int MaxChangesPerRead { get; set; } = 128;

    /// <summary>
    /// Gets or sets the maximum number of seconds to await row events during one provider-native iteration.
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
