using Cephalon.Observability.DependencyHealth.Core.Configuration;

namespace Cephalon.Observability.OracleDependencies.Configuration;

/// <summary>
/// Describes one Oracle dependency that should contribute to runtime health.
/// </summary>
public sealed class OracleDependencyDefinition : DependencyDefinitionBase
{
    /// <summary>Gets or sets the optional full Oracle connection string used for the probe.</summary>
    public string? ConnectionString { get; set; }

    /// <summary>Gets or sets the Oracle host name or IP address to probe when no full connection string is supplied.</summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>Gets or sets the Oracle TCP port.</summary>
    public int Port { get; set; } = 1521;

    /// <summary>Gets or sets the Oracle service name used in the Easy Connect data source when no full connection string is supplied.</summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional user name used for authentication when no full connection string is supplied.</summary>
    public string? Username { get; set; }

    /// <summary>Gets or sets the optional password used for authentication when no full connection string is supplied.</summary>
    public string? Password { get; set; }

    /// <summary>Gets or sets the SQL statement executed to verify the dependency.</summary>
    public string HealthQuery { get; set; } = "SELECT 1 FROM DUAL";
}
