using Cephalon.Engine.Configuration;
using Cephalon.Observability.DependencyHealth.Core.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cephalon.Observability.KafkaDependencies.Configuration;

/// <summary>
/// Configures Kafka dependency probes contributed to Cephalon runtime health.
/// </summary>
public sealed class KafkaDependencyHealthOptions : DependencyHealthOptionsBase<KafkaDependencyDefinition>
{
    /// <summary>
    /// Binds Kafka dependency-health options from configuration.
    /// </summary>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="sectionPath">
    /// The configuration section path that contains the engine settings. The default is <c>Engine</c>.
    /// </param>
    /// <returns>The bound dependency-health options.</returns>
    public static KafkaDependencyHealthOptions FromConfiguration(
        IConfiguration configuration,
        string sectionPath = EngineSettings.SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration
            .GetSection(sectionPath)
            .GetSection("Observability")
            .GetSection("DependencyHealth")
            .GetSection("Kafka");

        return FromSection(section);
    }

    private static KafkaDependencyHealthOptions FromSection(IConfigurationSection section)
    {
        var dependencies = section
            .GetSection("Dependencies")
            .GetChildren()
            .Select(ParseDependency)
            .ToArray();

        return new KafkaDependencyHealthOptions
        {
            RefreshIntervalSeconds = GetInt32(section["RefreshIntervalSeconds"], defaultValue: 30),
            Dependencies = dependencies
        };
    }

    private static KafkaDependencyDefinition ParseDependency(IConfigurationSection section)
    {
        return new KafkaDependencyDefinition
        {
            Id = section["Id"]?.Trim() ?? string.Empty,
            DisplayName = section["DisplayName"]?.Trim(),
            BootstrapServers = section["BootstrapServers"]?.Trim() ?? string.Empty,
            ClientId = section["ClientId"]?.Trim(),
            Topic = section["Topic"]?.Trim(),
            SecurityProtocol = section["SecurityProtocol"]?.Trim(),
            SaslMechanism = section["SaslMechanism"]?.Trim(),
            Username = section["Username"]?.Trim(),
            Password = section["Password"],
            Required = GetBoolean(section["Required"], defaultValue: false),
            TimeoutSeconds = GetInt32(section["TimeoutSeconds"], defaultValue: 5)
        };
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="KafkaDependencyHealthOptions" /> class.
    /// </summary>
    public KafkaDependencyHealthOptions()
    {
    }
}
