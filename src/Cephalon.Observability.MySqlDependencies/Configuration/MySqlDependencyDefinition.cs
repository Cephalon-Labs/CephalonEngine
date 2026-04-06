using Cephalon.Observability.DependencyHealth.Core.Configuration;

namespace Cephalon.Observability.MySqlDependencies.Configuration;

/// <summary>
/// Describes one MySQL dependency that should contribute to runtime health.
/// </summary>
public sealed class MySqlDependencyDefinition : DependencyDefinitionBase
{
    /// <summary>Gets or sets the optional full MySQL connection string used for the probe.</summary>
    public string? ConnectionString { get; set; }

    /// <summary>Gets or sets the MySQL host name or IP address to probe when no full connection string is supplied.</summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>Gets or sets the MySQL TCP port.</summary>
    public int Port { get; set; } = 3306;

    /// <summary>Gets or sets the database name used for the health query.</summary>
    public string Database { get; set; } = "mysql";

    /// <summary>Gets or sets the optional user name used for authentication when no full connection string is supplied.</summary>
    public string? Username { get; set; }

    /// <summary>Gets or sets the optional password used for authentication when no full connection string is supplied.</summary>
    public string? Password { get; set; }

    /// <summary>Gets or sets the optional MySQL SSL mode such as <c>Preferred</c>, <c>Required</c>, <c>VerifyCA</c>, or <c>VerifyFull</c>.</summary>
    public string? SslMode { get; set; }

    /// <summary>Gets or sets the optional value that controls whether the server RSA public key may be requested automatically.</summary>
    public bool? AllowPublicKeyRetrieval { get; set; }

    /// <summary>Gets or sets the SQL statement executed to verify the dependency.</summary>
    public string HealthQuery { get; set; } = "SELECT 1;";
}
