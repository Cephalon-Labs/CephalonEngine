namespace Cephalon.Data.Oracle.Configuration;

/// <summary>
/// Declares one provider-native Oracle LogMiner capture for the active runtime.
/// </summary>
public sealed class OracleLogMinerCaptureOptions
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
    /// Gets or sets the Oracle schema name of the tracked table.
    /// </summary>
    public string TableSchema { get; set; } = string.Empty;

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
    public string EventFormat { get; set; } = "oracle-logminer-redo-event";

    /// <summary>
    /// Gets or sets the initial position used when no durable checkpoint exists yet.
    /// </summary>
    /// <remarks>
    /// Supported values are <c>latest-available</c> and <c>earliest-available</c>.
    /// </remarks>
    public string InitialPosition { get; set; } = "latest-available";

    /// <summary>
    /// Gets or sets the expected Oracle database identifier when the capture should fail fast if the runtime connects to a different upstream.
    /// </summary>
    /// <remarks>
    /// Leave this unset when the capture should observe Oracle database identity for diagnostics only. When set, the provider-native runner
    /// validates the live <c>DBID</c> before it starts or resumes LogMiner execution.
    /// </remarks>
    public decimal? ExpectedDatabaseId { get; set; }

    /// <summary>
    /// Gets or sets the expected Oracle database unique name when the capture should fail fast if the runtime connects to a different upstream.
    /// </summary>
    /// <remarks>
    /// Leave this blank when the capture should observe Oracle database identity for diagnostics only. When set, the provider-native runner
    /// validates the live <c>DB_UNIQUE_NAME</c> before it starts or resumes LogMiner execution.
    /// </remarks>
    public string ExpectedDatabaseUniqueName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the provider-native runner should reseed from the earliest retained SCN when a durable checkpoint is older than the retained archive-log window.
    /// </summary>
    /// <remarks>
    /// The default is <see langword="false" /> so Oracle LogMiner fails fast instead of silently skipping the gap between the durable checkpoint and the earliest retained archive-log SCN.
    /// </remarks>
    public bool ResumeFromEarliestAvailableScnIfCheckpointUnavailable { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of captured row changes to stage during one provider-native iteration.
    /// </summary>
    public int MaxChangesPerRead { get; set; } = 128;

    /// <summary>
    /// Gets or sets the maximum number of seconds to await committed redo during one provider-native iteration.
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
