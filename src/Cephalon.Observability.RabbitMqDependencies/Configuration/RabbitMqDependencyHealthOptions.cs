using Cephalon.Engine.Configuration;
using Cephalon.Observability.DependencyHealth.Core.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cephalon.Observability.RabbitMqDependencies.Configuration;

/// <summary>
/// Configures RabbitMQ dependency probes contributed to Cephalon runtime health.
/// </summary>
public sealed class RabbitMqDependencyHealthOptions : DependencyHealthOptionsBase<RabbitMqDependencyDefinition>
{
    /// <summary>
    /// Binds RabbitMQ dependency-health options from configuration.
    /// </summary>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="sectionPath">
    /// The configuration section path that contains the engine settings. The default is <c>Engine</c>.
    /// </param>
    /// <returns>The bound dependency-health options.</returns>
    public static RabbitMqDependencyHealthOptions FromConfiguration(
        IConfiguration configuration,
        string sectionPath = EngineSettings.SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration
            .GetSection(sectionPath)
            .GetSection("Observability")
            .GetSection("DependencyHealth")
            .GetSection("RabbitMq");

        return FromSection(section);
    }

    private static RabbitMqDependencyHealthOptions FromSection(IConfigurationSection section)
    {
        var dependencies = section
            .GetSection("Dependencies")
            .GetChildren()
            .Select(ParseDependency)
            .ToArray();

        return new RabbitMqDependencyHealthOptions
        {
            RefreshIntervalSeconds = GetInt32(section["RefreshIntervalSeconds"], defaultValue: 30),
            Dependencies = dependencies
        };
    }

    private static RabbitMqDependencyDefinition ParseDependency(IConfigurationSection section)
    {
        return new RabbitMqDependencyDefinition
        {
            Id = section["Id"]?.Trim() ?? string.Empty,
            DisplayName = section["DisplayName"]?.Trim(),
            ConnectionString = section["ConnectionString"]?.Trim(),
            Host = section["Host"]?.Trim() ?? string.Empty,
            Port = GetInt32(section["Port"], defaultValue: 5672),
            VirtualHost = section["VirtualHost"]?.Trim() ?? "/",
            Username = section["Username"]?.Trim(),
            Password = section["Password"],
            UseTls = GetBoolean(section["UseTls"], defaultValue: false),
            Required = GetBoolean(section["Required"], defaultValue: false),
            TimeoutSeconds = GetInt32(section["TimeoutSeconds"], defaultValue: 5)
        };
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RabbitMqDependencyHealthOptions" /> class.
    /// </summary>
    public RabbitMqDependencyHealthOptions()
    {
    }
}
