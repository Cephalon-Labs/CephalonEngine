using Cephalon.Observability.DependencyHealth.Core.Configuration;

namespace Cephalon.Observability.PostgresDependencies.Configuration;

/// <summary>
/// Describes one Postgres dependency that should contribute to runtime health.
/// </summary>
public sealed class PostgresDependencyDefinition : DependencyDefinitionBase
{
    /// <summary>Gets or sets the optional full Postgres connection string used for the probe.</summary>
    public string? ConnectionString { get; set; }

    /// <summary>Gets or sets the Postgres host name or IP address to probe when no full connection string is supplied.</summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>Gets or sets the Postgres TCP port.</summary>
    public int Port { get; set; } = 5432;

    /// <summary>Gets or sets the database name used for the health query.</summary>
    public string Database { get; set; } = "postgres";

    /// <summary>Gets or sets the optional user name used for authentication when no full connection string is supplied.</summary>
    public string? Username { get; set; }

    /// <summary>Gets or sets the optional password used for authentication when no full connection string is supplied.</summary>
    public string? Password { get; set; }

    /// <summary>Gets or sets the optional Postgres SSL mode used when building the probe connection string.</summary>
    public string? SslMode { get; set; }

    /// <summary>Gets or sets the SQL statement executed to verify the dependency.</summary>
    public string HealthQuery { get; set; } = "SELECT 1;";

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgresDependencyDefinition" /> class.
    /// </summary>
    public PostgresDependencyDefinition()
    {
    }
}
