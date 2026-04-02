namespace Cephalon.Observability.SqlServerDependencies.Configuration;

/// <summary>
/// Describes one SQL Server dependency that should contribute to runtime health.
/// </summary>
public sealed class SqlServerDependencyDefinition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerDependencyDefinition" /> class.
    /// </summary>
    public SqlServerDependencyDefinition()
    {
    }

    /// <summary>
    /// Gets or sets the stable dependency identifier surfaced through runtime health endpoints.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the human-readable dependency name shown to operators.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the optional full SQL Server connection string used for the probe.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the SQL Server host name or IP address to probe when no full connection string is supplied.
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the SQL Server TCP port.
    /// </summary>
    public int Port { get; set; } = 1433;

    /// <summary>
    /// Gets or sets the database name used for the health query.
    /// </summary>
    public string Database { get; set; } = "master";

    /// <summary>
    /// Gets or sets the optional user name used for authentication when no full connection string is supplied.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the optional password used for authentication when no full connection string is supplied.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Gets or sets the optional SQL Server encryption mode such as <c>Optional</c>, <c>Mandatory</c>, or <c>Strict</c>.
    /// </summary>
    public string? Encrypt { get; set; }

    /// <summary>
    /// Gets or sets the optional value that controls whether server certificate validation should be bypassed.
    /// </summary>
    public bool? TrustServerCertificate { get; set; }

    /// <summary>
    /// Gets or sets the SQL statement executed to verify the dependency.
    /// </summary>
    public string HealthQuery { get; set; } = "SELECT 1;";

    /// <summary>
    /// Gets or sets a value indicating whether this dependency is required for readiness.
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// Gets or sets the per-probe timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 5;
}
