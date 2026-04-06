using Cephalon.Observability.DependencyHealth.Core.Configuration;

namespace Cephalon.Observability.SqlServerDependencies.Configuration;

/// <summary>
/// Describes one SQL Server dependency that should contribute to runtime health.
/// </summary>
public sealed class SqlServerDependencyDefinition : DependencyDefinitionBase
{
    /// <summary>Gets or sets the optional full SQL Server connection string used for the probe.</summary>
    public string? ConnectionString { get; set; }

    /// <summary>Gets or sets the SQL Server host name or IP address to probe when no full connection string is supplied.</summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>Gets or sets the SQL Server TCP port.</summary>
    public int Port { get; set; } = 1433;

    /// <summary>Gets or sets the database name used for the health query.</summary>
    public string Database { get; set; } = "master";

    /// <summary>Gets or sets the optional user name used for authentication when no full connection string is supplied.</summary>
    public string? Username { get; set; }

    /// <summary>Gets or sets the optional password used for authentication when no full connection string is supplied.</summary>
    public string? Password { get; set; }

    /// <summary>Gets or sets the optional SQL Server encryption mode such as <c>Optional</c>, <c>Mandatory</c>, or <c>Strict</c>.</summary>
    public string? Encrypt { get; set; }

    /// <summary>Gets or sets the optional value that controls whether server certificate validation should be bypassed.</summary>
    public bool? TrustServerCertificate { get; set; }

    /// <summary>Gets or sets the SQL statement executed to verify the dependency.</summary>
    public string HealthQuery { get; set; } = "SELECT 1;";
}
