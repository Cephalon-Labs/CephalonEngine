namespace Cephalon.Data.Debezium.Configuration;

/// <summary>
/// Declares one external Debezium-managed connector runtime for the active Cephalon data runtime.
/// </summary>
public sealed class DebeziumConnectorOptions
{
    /// <summary>
    /// Gets or sets the stable execution-runtime identifier for the managed connector.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the operator-facing connector name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the human-readable connector description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the operator-facing Kafka Connect or Debezium cluster identifier that owns the connector.
    /// </summary>
    public string ConnectClusterId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Debezium connector-class identifier when the runtime should publish it on shared operator surfaces.
    /// </summary>
    public string ConnectorClass { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the upstream provider identifier behind the managed connector, such as <c>postgresql</c> or <c>sqlserver</c>.
    /// </summary>
    public string SourceProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Debezium topic prefix when the connector fans out into one or more topics.
    /// </summary>
    public string TopicPrefix { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the execution-ownership mode published for the connector runtime.
    /// </summary>
    public string ExecutionOwnership { get; set; } = "external-managed";

    /// <summary>
    /// Gets or sets the execution-topology classification published for the connector runtime.
    /// </summary>
    public string ExecutionTopology { get; set; } = "managed-connector";

    /// <summary>
    /// Gets or sets the acknowledgement mode published for the connector runtime.
    /// </summary>
    public string AcknowledgementMode { get; set; } = "connector-offset-commit";

    /// <summary>
    /// Gets or sets the report-freshness window, in seconds, used to mark connector observations stale.
    /// </summary>
    public int? ObservationStaleAfterSeconds { get; set; } = 300;

    /// <summary>
    /// Gets or sets a value indicating whether the connector rejects out-of-order external reports.
    /// </summary>
    public bool RejectOutOfOrderReports { get; set; }

    /// <summary>
    /// Gets or sets the reporter-lease window, in seconds, used to keep one reporter authoritative for the connector.
    /// </summary>
    public int? ReporterLeaseSeconds { get; set; } = 120;

    /// <summary>
    /// Gets or sets a value indicating whether the connector rejects conflicting reporter identities while an active lease still exists.
    /// </summary>
    public bool RejectConflictingReporterIds { get; set; }

    /// <summary>
    /// Gets the declared task identifiers that belong to the managed connector runtime.
    /// </summary>
    public IList<string> TaskIds { get; } = [];

    /// <summary>
    /// Gets the declared edge-node identifiers that can originate observations for the managed connector runtime.
    /// </summary>
    public IList<string> EdgeNodeIds { get; } = [];

    /// <summary>
    /// Gets the Debezium-managed CDC captures that should bind to this connector runtime.
    /// </summary>
    public IList<DebeziumCaptureOptions> CdcCaptures { get; } = [];

    /// <summary>
    /// Gets arbitrary operator-facing metadata that should flow through the execution-runtime descriptor.
    /// </summary>
    public IDictionary<string, string> Metadata { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
