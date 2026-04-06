using Cephalon.Engine.Configuration;
using Cephalon.Observability.DependencyHealth.Core.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cephalon.Observability.NatsDependencies.Configuration;

/// <summary>
/// Configures NATS dependency probes contributed to Cephalon runtime health.
/// </summary>
public sealed class NatsDependencyHealthOptions : DependencyHealthOptionsBase<NatsDependencyDefinition>
{
    /// <summary>
    /// Binds NATS dependency-health options from configuration.
    /// </summary>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="sectionPath">
    /// The configuration section path that contains the engine settings. The default is <c>Engine</c>.
    /// </param>
    /// <returns>The bound dependency-health options.</returns>
    public static NatsDependencyHealthOptions FromConfiguration(
        IConfiguration configuration,
        string sectionPath = EngineSettings.SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration
            .GetSection(sectionPath)
            .GetSection("Observability")
            .GetSection("DependencyHealth")
            .GetSection("Nats");

        return FromSection(section);
    }

    private static NatsDependencyHealthOptions FromSection(IConfigurationSection section)
    {
        var dependencies = section
            .GetSection("Dependencies")
            .GetChildren()
            .Select(ParseDependency)
            .ToArray();

        return new NatsDependencyHealthOptions
        {
            RefreshIntervalSeconds = GetInt32(section["RefreshIntervalSeconds"], defaultValue: 30),
            Dependencies = dependencies
        };
    }

    private static NatsDependencyDefinition ParseDependency(IConfigurationSection section)
    {
        return new NatsDependencyDefinition
        {
            Id = section["Id"]?.Trim() ?? string.Empty,
            DisplayName = section["DisplayName"]?.Trim(),
            Host = section["Host"]?.Trim() ?? string.Empty,
            Port = GetInt32(section["Port"], defaultValue: 4222),
            UseTls = GetBoolean(section["UseTls"], defaultValue: false),
            TlsServerName = section["TlsServerName"]?.Trim(),
            Username = section["Username"]?.Trim(),
            Password = section["Password"],
            Token = section["Token"]?.Trim(),
            ClientName = section["ClientName"]?.Trim(),
            Required = GetBoolean(section["Required"], defaultValue: false),
            TimeoutSeconds = GetInt32(section["TimeoutSeconds"], defaultValue: 5)
        };
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NatsDependencyHealthOptions" /> class.
    /// </summary>
    public NatsDependencyHealthOptions()
    {
    }
}
