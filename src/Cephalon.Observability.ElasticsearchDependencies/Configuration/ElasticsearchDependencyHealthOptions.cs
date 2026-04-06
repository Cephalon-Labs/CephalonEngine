using Cephalon.Engine.Configuration;
using Cephalon.Observability.DependencyHealth.Core.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cephalon.Observability.ElasticsearchDependencies.Configuration;

/// <summary>
/// Configures Elasticsearch dependency probes contributed to Cephalon runtime health.
/// </summary>
public sealed class ElasticsearchDependencyHealthOptions : DependencyHealthOptionsBase<ElasticsearchDependencyDefinition>
{
    /// <summary>
    /// Binds Elasticsearch dependency-health options from configuration.
    /// </summary>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="sectionPath">
    /// The configuration section path that contains the engine settings. The default is <c>Engine</c>.
    /// </param>
    /// <returns>The bound dependency-health options.</returns>
    public static ElasticsearchDependencyHealthOptions FromConfiguration(
        IConfiguration configuration,
        string sectionPath = EngineSettings.SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration
            .GetSection(sectionPath)
            .GetSection("Observability")
            .GetSection("DependencyHealth")
            .GetSection("Elasticsearch");

        return FromSection(section);
    }

    private static ElasticsearchDependencyHealthOptions FromSection(IConfigurationSection section)
    {
        var dependencies = section
            .GetSection("Dependencies")
            .GetChildren()
            .Select(ParseDependency)
            .ToArray();

        return new ElasticsearchDependencyHealthOptions
        {
            RefreshIntervalSeconds = GetInt32(section["RefreshIntervalSeconds"], defaultValue: 30),
            Dependencies = dependencies
        };
    }

    private static ElasticsearchDependencyDefinition ParseDependency(IConfigurationSection section)
    {
        return new ElasticsearchDependencyDefinition
        {
            Id = section["Id"]?.Trim() ?? string.Empty,
            DisplayName = section["DisplayName"]?.Trim(),
            Endpoint = section["Endpoint"]?.Trim() ?? string.Empty,
            ApiKey = section["ApiKey"]?.Trim(),
            BearerToken = section["BearerToken"]?.Trim(),
            Username = section["Username"]?.Trim(),
            Password = section["Password"],
            Required = GetBoolean(section["Required"], defaultValue: false),
            TimeoutSeconds = GetInt32(section["TimeoutSeconds"], defaultValue: 5)
        };
    }
}
