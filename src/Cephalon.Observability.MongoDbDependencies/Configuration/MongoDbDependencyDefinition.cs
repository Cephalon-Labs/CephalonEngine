namespace Cephalon.Observability.MongoDbDependencies.Configuration;

/// <summary>
/// Describes one MongoDB dependency that should contribute to runtime health.
/// </summary>
public sealed class MongoDbDependencyDefinition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDbDependencyDefinition" /> class.
    /// </summary>
    public MongoDbDependencyDefinition()
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
    /// Gets or sets the optional full MongoDB connection string used for the probe.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the MongoDB host name or IP address to probe when no full connection string is supplied.
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MongoDB TCP port.
    /// </summary>
    public int Port { get; set; } = 27017;

    /// <summary>
    /// Gets or sets the database name used for the health command.
    /// </summary>
    public string Database { get; set; } = "admin";

    /// <summary>
    /// Gets or sets the optional user name used for authentication when no full connection string is supplied.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the optional password used for authentication when no full connection string is supplied.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Gets or sets the optional authentication source used when creating credentials from discrete settings.
    /// </summary>
    public string? AuthSource { get; set; }

    /// <summary>
    /// Gets or sets the optional value that controls whether TLS should be used for the probe.
    /// </summary>
    public bool? UseTls { get; set; }

    /// <summary>
    /// Gets or sets the optional value that controls whether TLS certificate validation should be relaxed.
    /// </summary>
    public bool? AllowInsecureTls { get; set; }

    /// <summary>
    /// Gets or sets the optional value that controls whether the client should connect directly to the target server.
    /// </summary>
    public bool? DirectConnection { get; set; } = true;

    /// <summary>
    /// Gets or sets the MongoDB database command executed to verify the dependency.
    /// </summary>
    public string HealthCommand { get; set; } = "ping";

    /// <summary>
    /// Gets or sets a value indicating whether this dependency is required for readiness.
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// Gets or sets the per-probe timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 5;
}
