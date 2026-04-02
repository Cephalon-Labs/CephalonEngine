namespace Cephalon.Observability.ClickHouseDependencies.Configuration;

/// <summary>
/// Describes one ClickHouse dependency that should contribute to runtime health.
/// </summary>
public sealed class ClickHouseDependencyDefinition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClickHouseDependencyDefinition" /> class.
    /// </summary>
    public ClickHouseDependencyDefinition()
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
    /// Gets or sets the optional full ClickHouse connection string used for the probe.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the ClickHouse host name or IP address to probe when no full connection string is supplied.
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ClickHouse protocol used for the probe, such as <c>http</c> or <c>https</c>.
    /// </summary>
    public string Protocol { get; set; } = "http";

    /// <summary>
    /// Gets or sets the ClickHouse TCP port used by the selected protocol.
    /// </summary>
    public int Port { get; set; } = 8123;

    /// <summary>
    /// Gets or sets the optional ClickHouse database to select for the probe session.
    /// </summary>
    public string? Database { get; set; }

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
    public string HealthQuery { get; set; } = "SELECT 1";

    /// <summary>
    /// Gets or sets a value indicating whether this dependency is required for readiness.
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// Gets or sets the per-probe timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 5;
}
