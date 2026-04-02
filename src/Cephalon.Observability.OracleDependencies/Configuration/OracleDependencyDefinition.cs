namespace Cephalon.Observability.OracleDependencies.Configuration;

/// <summary>
/// Describes one Oracle dependency that should contribute to runtime health.
/// </summary>
public sealed class OracleDependencyDefinition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OracleDependencyDefinition" /> class.
    /// </summary>
    public OracleDependencyDefinition()
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
    /// Gets or sets the optional full Oracle connection string used for the probe.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the Oracle host name or IP address to probe when no full connection string is supplied.
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Oracle TCP port.
    /// </summary>
    public int Port { get; set; } = 1521;

    /// <summary>
    /// Gets or sets the Oracle service name used in the Easy Connect data source when no full connection string is supplied.
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional user name used for authentication when no full connection string is supplied.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the optional password used for authentication when no full connection string is supplied.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Gets or sets the SQL statement executed to verify the dependency.
    /// </summary>
    public string HealthQuery { get; set; } = "SELECT 1 FROM DUAL";

    /// <summary>
    /// Gets or sets a value indicating whether this dependency is required for readiness.
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// Gets or sets the per-probe timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 5;
}
