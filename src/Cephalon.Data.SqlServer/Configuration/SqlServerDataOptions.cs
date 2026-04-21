namespace Cephalon.Data.SqlServer.Configuration;

/// <summary>
/// Configuration options for the SQL Server data provider (<c>Engine:Data:SqlServer</c>).
/// </summary>
public sealed class SqlServerDataOptions
{
    /// <summary>
    /// Gets the configuration section path used by default for SQL Server data settings.
    /// </summary>
    public const string SectionPath = "Engine:Data:SqlServer";

    /// <summary>
    /// Gets the canonical provider identifier emitted by the pack.
    /// </summary>
    public const string ProviderId = "sqlserver";

    /// <summary>
    /// Gets or sets the root <c>ConnectionStrings</c> entry name to resolve for SQL Server.
    /// </summary>
    /// <remarks>
    /// Use either <see cref="ConnectionStringName" /> or <see cref="ConnectionString" />.
    /// </remarks>
    public string? ConnectionStringName { get; set; }

    /// <summary>
    /// Gets or sets the inline SQL Server connection string.
    /// </summary>
    /// <remarks>
    /// Use either <see cref="ConnectionStringName" /> or <see cref="ConnectionString" />.
    /// </remarks>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the operator-facing database name that owns the configured CDC captures.
    /// </summary>
    public string DatabaseName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the schema that stores Cephalon-managed SQL Server CDC checkpoints.
    /// </summary>
    public string CheckpointTableSchema { get; set; } = "dbo";

    /// <summary>
    /// Gets or sets the table name that stores Cephalon-managed SQL Server CDC checkpoints.
    /// </summary>
    public string CheckpointTableName { get; set; } = "cephalon_cdc_checkpoints";

    /// <summary>
    /// Gets the provider-native SQL Server CDC captures that should be contributed to the active runtime.
    /// </summary>
    public IList<SqlServerCdcCaptureOptions> CdcCaptures { get; } = [];
}
