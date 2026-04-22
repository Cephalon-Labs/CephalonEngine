namespace Cephalon.Data.MySql.Configuration;

/// <summary>
/// Configuration options for the MySQL data provider (<c>Engine:Data:MySql</c>).
/// </summary>
public sealed class MySqlDataOptions
{
    /// <summary>
    /// Gets the configuration section path used by default for MySQL data settings.
    /// </summary>
    public const string SectionPath = "Engine:Data:MySql";

    /// <summary>
    /// Gets the canonical provider identifier emitted by the pack.
    /// </summary>
    public const string ProviderId = "mysql";

    /// <summary>
    /// Gets or sets the root <c>ConnectionStrings</c> entry name to resolve for MySQL.
    /// </summary>
    /// <remarks>
    /// Use either <see cref="ConnectionStringName" /> or <see cref="ConnectionString" />.
    /// </remarks>
    public string? ConnectionStringName { get; set; }

    /// <summary>
    /// Gets or sets the inline MySQL connection string.
    /// </summary>
    /// <remarks>
    /// Use either <see cref="ConnectionStringName" /> or <see cref="ConnectionString" />.
    /// </remarks>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the operator-facing database name that owns the configured binlog captures.
    /// </summary>
    public string DatabaseName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Cephalon-managed checkpoint table name used for durable MySQL binlog progress.
    /// </summary>
    public string CheckpointTableName { get; set; } = "cephalon_cdc_checkpoints";

    /// <summary>
    /// Gets the provider-native MySQL binlog captures that should be contributed to the active runtime.
    /// </summary>
    public IList<MySqlBinlogCaptureOptions> CdcCaptures { get; } = [];
}
