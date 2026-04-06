using Cephalon.Observability.DependencyHealth.Core.Configuration;

namespace Cephalon.Observability.Neo4jDependencies.Configuration;

/// <summary>
/// Describes one Neo4j dependency that should contribute to runtime health.
/// </summary>
public sealed class Neo4jDependencyDefinition : DependencyDefinitionBase
{
    /// <summary>
    /// Gets or sets the optional full Neo4j endpoint URI such as <c>neo4j://graph.internal.example:7687</c> or <c>neo4j+s://graph.internal.example:7687</c>.
    /// </summary>
    public string? Uri { get; set; }

    /// <summary>
    /// Gets or sets the Neo4j host name or IP address to probe when no full URI is supplied.
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Neo4j Bolt port used when no full URI is supplied.
    /// </summary>
    public int Port { get; set; } = 7687;

    /// <summary>
    /// Gets or sets the URI scheme used when building a discrete endpoint, such as <c>neo4j</c>, <c>neo4j+s</c>, <c>bolt</c>, or <c>bolt+s</c>.
    /// </summary>
    public string Scheme { get; set; } = "neo4j";

    /// <summary>
    /// Gets or sets the optional Neo4j database name used when opening the probe session.
    /// </summary>
    public string? Database { get; set; }

    /// <summary>
    /// Gets or sets the optional user name used for authentication.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the optional password used for authentication.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Gets or sets the Cypher statement executed to verify the dependency.
    /// </summary>
    public string HealthQuery { get; set; } = "RETURN 1 AS health";
}
