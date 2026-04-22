namespace Cephalon.Data.Oracle.Configuration;

/// <summary>
/// Configuration options for the Oracle data provider (<c>Engine:Data:Oracle</c>).
/// </summary>
public sealed class OracleDataOptions
{
    /// <summary>
    /// Gets the configuration section path used by default for Oracle data settings.
    /// </summary>
    public const string SectionPath = "Engine:Data:Oracle";

    /// <summary>
    /// Gets the canonical provider identifier emitted by the pack.
    /// </summary>
    public const string ProviderId = "oracle";

    /// <summary>
    /// Gets or sets the root <c>ConnectionStrings</c> entry name to resolve for Oracle.
    /// </summary>
    /// <remarks>
    /// Use either <see cref="ConnectionStringName" /> or <see cref="ConnectionString" />.
    /// </remarks>
    public string? ConnectionStringName { get; set; }

    /// <summary>
    /// Gets or sets the inline Oracle connection string.
    /// </summary>
    /// <remarks>
    /// Use either <see cref="ConnectionStringName" /> or <see cref="ConnectionString" />.
    /// </remarks>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the operator-facing database name that owns the configured LogMiner captures.
    /// </summary>
    public string DatabaseName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Cephalon-managed checkpoint table name used for durable Oracle LogMiner progress.
    /// </summary>
    public string CheckpointTableName { get; set; } = "CEPHALON_CDC_CHECKPOINTS";

    /// <summary>
    /// Gets the provider-native Oracle LogMiner captures that should be contributed to the active runtime.
    /// </summary>
    public IList<OracleLogMinerCaptureOptions> CdcCaptures { get; } = [];
}
