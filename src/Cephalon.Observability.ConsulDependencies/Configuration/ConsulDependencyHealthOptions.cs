using Cephalon.Engine.Configuration;
using Cephalon.Observability.DependencyHealth.Core.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cephalon.Observability.ConsulDependencies.Configuration;

/// <summary>
/// Configures Consul dependency probes contributed to Cephalon runtime health.
/// </summary>
public sealed class ConsulDependencyHealthOptions : DependencyHealthOptionsBase<ConsulDependencyDefinition>
{
    /// <summary>
    /// Binds Consul dependency-health options from configuration.
    /// </summary>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="sectionPath">
    /// The configuration section path that contains the engine settings. The default is <c>Engine</c>.
    /// </param>
    /// <returns>The bound dependency-health options.</returns>
    public static ConsulDependencyHealthOptions FromConfiguration(
        IConfiguration configuration,
        string sectionPath = EngineSettings.SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration
            .GetSection(sectionPath)
            .GetSection("Observability")
            .GetSection("DependencyHealth")
            .GetSection("Consul");

        return FromSection(section);
    }

    private static ConsulDependencyHealthOptions FromSection(IConfigurationSection section)
    {
        var dependencies = section
            .GetSection("Dependencies")
            .GetChildren()
            .Select(ParseDependency)
            .ToArray();

        return new ConsulDependencyHealthOptions
        {
            RefreshIntervalSeconds = GetInt32(section["RefreshIntervalSeconds"], defaultValue: 30),
            Dependencies = dependencies
        };
    }

    private static ConsulDependencyDefinition ParseDependency(IConfigurationSection section)
    {
        return new ConsulDependencyDefinition
        {
            Id = section["Id"]?.Trim() ?? string.Empty,
            DisplayName = section["DisplayName"]?.Trim(),
            Endpoint = section["Endpoint"]?.Trim() ?? string.Empty,
            AclToken = section["AclToken"]?.Trim(),
            Datacenter = section["Datacenter"]?.Trim(),
            Required = GetBoolean(section["Required"], defaultValue: false),
            TimeoutSeconds = GetInt32(section["TimeoutSeconds"], defaultValue: 5)
        };
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsulDependencyHealthOptions" /> class.
    /// </summary>
    public ConsulDependencyHealthOptions()
    {
    }
}
