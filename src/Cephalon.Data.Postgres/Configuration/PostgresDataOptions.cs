namespace Cephalon.Data.Postgres.Configuration;

/// <summary>
/// Configuration options for the PostgreSQL data provider (<c>Engine:Data:Postgres</c>).
/// </summary>
public sealed class PostgresDataOptions
{
    /// <summary>
    /// Gets the configuration section path used by default for PostgreSQL data settings.
    /// </summary>
    public const string SectionPath = "Engine:Data:Postgres";

    /// <summary>
    /// Gets the canonical provider identifier emitted by the pack.
    /// </summary>
    public const string ProviderId = "postgresql";

    /// <summary>
    /// Gets or sets the root <c>ConnectionStrings</c> entry name to resolve for PostgreSQL.
    /// </summary>
    /// <remarks>
    /// Use either <see cref="ConnectionStringName" /> or <see cref="ConnectionString" />.
    /// </remarks>
    public string? ConnectionStringName { get; set; }

    /// <summary>
    /// Gets or sets the inline PostgreSQL connection string.
    /// </summary>
    /// <remarks>
    /// Use either <see cref="ConnectionStringName" /> or <see cref="ConnectionString" />.
    /// </remarks>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the operator-facing database name that owns the configured logical-replication captures.
    /// </summary>
    public string DatabaseName { get; set; } = string.Empty;

    /// <summary>
    /// Gets the provider-native PostgreSQL logical-replication captures that should be contributed to the active runtime.
    /// </summary>
    public IList<PostgresLogicalReplicationCaptureOptions> CdcCaptures { get; } = [];
}
