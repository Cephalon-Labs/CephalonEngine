using Cephalon.Engine.Configuration;
using Cephalon.Observability.DependencyHealth.Core.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cephalon.Observability.Neo4jDependencies.Configuration;

/// <summary>
/// Configures Neo4j dependency probes contributed to Cephalon runtime health.
/// </summary>
public sealed class Neo4jDependencyHealthOptions : DependencyHealthOptionsBase<Neo4jDependencyDefinition>
{
    /// <summary>
    /// Binds Neo4j dependency-health options from configuration.
    /// </summary>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="sectionPath">
    /// The configuration section path that contains the engine settings. The default is <c>Engine</c>.
    /// </param>
    /// <returns>The bound dependency-health options.</returns>
    public static Neo4jDependencyHealthOptions FromConfiguration(
        IConfiguration configuration,
        string sectionPath = EngineSettings.SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration
            .GetSection(sectionPath)
            .GetSection("Observability")
            .GetSection("DependencyHealth")
            .GetSection("Neo4j");

        return FromSection(section);
    }

    private static Neo4jDependencyHealthOptions FromSection(IConfigurationSection section)
    {
        var dependencies = section
            .GetSection("Dependencies")
            .GetChildren()
            .Select(ParseDependency)
            .ToArray();

        return new Neo4jDependencyHealthOptions
        {
            RefreshIntervalSeconds = GetInt32(section["RefreshIntervalSeconds"], defaultValue: 30),
            Dependencies = dependencies
        };
    }

    private static Neo4jDependencyDefinition ParseDependency(IConfigurationSection section)
    {
        return new Neo4jDependencyDefinition
        {
            Id = section["Id"]?.Trim() ?? string.Empty,
            DisplayName = section["DisplayName"]?.Trim(),
            Uri = section["Uri"]?.Trim(),
            Host = section["Host"]?.Trim() ?? string.Empty,
            Port = GetInt32(section["Port"], defaultValue: 7687),
            Scheme = section["Scheme"]?.Trim() ?? "neo4j",
            Database = section["Database"]?.Trim(),
            Username = section["Username"]?.Trim(),
            Password = section["Password"],
            HealthQuery = section["HealthQuery"]?.Trim() ?? "RETURN 1 AS health",
            Required = GetBoolean(section["Required"], defaultValue: false),
            TimeoutSeconds = GetInt32(section["TimeoutSeconds"], defaultValue: 5)
        };
    }
}
