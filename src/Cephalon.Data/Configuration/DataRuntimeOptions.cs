namespace Cephalon.Data.Configuration;

/// <summary>
/// Describes the host-owned options for the runtime-neutral Cephalon data pack.
/// </summary>
public sealed class DataRuntimeOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataRuntimeOptions" /> class.
    /// </summary>
    public DataRuntimeOptions()
    {
    }

    /// <summary>
    /// Gets the host-defined CDC execution runtimes that should be available to the active data runtime.
    /// </summary>
    public IList<CdcCaptureExecutionRuntimeOptions> CdcExecutionRuntimes { get; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether the pack should register the default read-store dispatcher.
    /// </summary>
    public bool RegisterReadStore { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the pack should register the default write-store dispatcher.
    /// </summary>
    public bool RegisterWriteStore { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the pack should register the shared CDC hosted execution pump.
    /// </summary>
    public bool EnableCdcExecution { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the pack should accept external CDC execution-runtime reports through the shared runtime-state catalog.
    /// </summary>
    public bool EnableExternalCdcRuntimeReporting { get; set; }

    /// <summary>
    /// Gets or sets the polling interval, in seconds, used by the shared CDC hosted execution pump.
    /// </summary>
    public int CdcPollingIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets a value indicating whether the pack should run the shared automatic managed-connector background retry lane.
    /// </summary>
    public bool EnableManagedConnectorAutomaticRetryExecution { get; set; }

    /// <summary>
    /// Gets or sets the polling interval, in seconds, used by the shared automatic managed-connector background retry lane.
    /// </summary>
    public int ManagedConnectorAutomaticRetryPollingIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the host-owned local coordination owner identifier used to decide whether the current node can run automatic managed-connector retries.
    /// </summary>
    public string? ManagedConnectorAutomaticRetryCoordinationOwnerId { get; set; }

    /// <summary>
    /// Gets or sets the optional durable file path used to persist the shared managed-connector command journal across process restarts.
    /// </summary>
    public string? ManagedConnectorCommandJournalPersistencePath { get; set; }
}
