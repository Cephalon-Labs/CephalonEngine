namespace Cephalon.Observability.CassandraDependencies.Configuration;

/// <summary>
/// Describes one Cassandra dependency that should contribute to runtime health.
/// </summary>
public sealed class CassandraDependencyDefinition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CassandraDependencyDefinition" /> class.
    /// </summary>
    public CassandraDependencyDefinition()
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
    /// Gets or sets the Cassandra contact points used to establish the probe session.
    /// </summary>
    public IReadOnlyList<string> ContactPoints { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the Cassandra native-protocol TCP port.
    /// </summary>
    public int Port { get; set; } = 9042;

    /// <summary>
    /// Gets or sets the optional Cassandra keyspace used when opening the probe session.
    /// </summary>
    public string? Keyspace { get; set; }

    /// <summary>
    /// Gets or sets the optional user name used for authentication.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the optional password used for authentication.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Gets or sets the CQL statement executed to verify the dependency.
    /// </summary>
    public string HealthQuery { get; set; } = "SELECT release_version FROM system.local;";

    /// <summary>
    /// Gets or sets a value indicating whether this dependency is required for readiness.
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// Gets or sets the per-probe timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 5;
}
