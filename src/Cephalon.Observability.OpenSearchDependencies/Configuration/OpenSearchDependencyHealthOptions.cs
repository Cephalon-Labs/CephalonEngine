using Cephalon.Engine.Configuration;
using Cephalon.Observability.DependencyHealth.Core.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cephalon.Observability.OpenSearchDependencies.Configuration;

/// <summary>
/// Configures OpenSearch dependency probes contributed to Cephalon runtime health.
/// </summary>
public sealed class OpenSearchDependencyHealthOptions : DependencyHealthOptionsBase<OpenSearchDependencyDefinition>
{
    /// <summary>
    /// Binds OpenSearch dependency-health options from configuration.
    /// </summary>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="sectionPath">
    /// The configuration section path that contains the engine settings. The default is <c>Engine</c>.
    /// </param>
    /// <returns>The bound dependency-health options.</returns>
    public static OpenSearchDependencyHealthOptions FromConfiguration(
        IConfiguration configuration,
        string sectionPath = EngineSettings.SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration
            .GetSection(sectionPath)
            .GetSection("Observability")
            .GetSection("DependencyHealth")
            .GetSection("OpenSearch");

        return FromSection(section);
    }

    private static OpenSearchDependencyHealthOptions FromSection(IConfigurationSection section)
    {
        var dependencies = section
            .GetSection("Dependencies")
            .GetChildren()
            .Select(ParseDependency)
            .ToArray();

        return new OpenSearchDependencyHealthOptions
        {
            RefreshIntervalSeconds = GetInt32(section["RefreshIntervalSeconds"], defaultValue: 30),
            Dependencies = dependencies
        };
    }

    private static OpenSearchDependencyDefinition ParseDependency(IConfigurationSection section)
    {
        return new OpenSearchDependencyDefinition
        {
            Id = section["Id"]?.Trim() ?? string.Empty,
            DisplayName = section["DisplayName"]?.Trim(),
            Endpoint = section["Endpoint"]?.Trim() ?? string.Empty,
            Index = section["Index"]?.Trim(),
            BearerToken = section["BearerToken"]?.Trim(),
            Username = section["Username"]?.Trim(),
            Password = section["Password"],
            Required = GetBoolean(section["Required"], defaultValue: false),
            TimeoutSeconds = GetInt32(section["TimeoutSeconds"], defaultValue: 5)
        };
    }
}
