using Cephalon.Engine.Configuration;
using Cephalon.Observability.DependencyHealth.Core.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cephalon.Observability.PostgresDependencies.Configuration;

/// <summary>
/// Configures Postgres dependency probes contributed to Cephalon runtime health.
/// </summary>
public sealed class PostgresDependencyHealthOptions : DependencyHealthOptionsBase<PostgresDependencyDefinition>
{
    /// <summary>
    /// Binds Postgres dependency-health options from configuration.
    /// </summary>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="sectionPath">
    /// The configuration section path that contains the engine settings. The default is <c>Engine</c>.
    /// </param>
    /// <returns>The bound dependency-health options.</returns>
    public static PostgresDependencyHealthOptions FromConfiguration(
        IConfiguration configuration,
        string sectionPath = EngineSettings.SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration
            .GetSection(sectionPath)
            .GetSection("Observability")
            .GetSection("DependencyHealth")
            .GetSection("Postgres");

        return FromSection(section);
    }

    private static PostgresDependencyHealthOptions FromSection(IConfigurationSection section)
    {
        var dependencies = section
            .GetSection("Dependencies")
            .GetChildren()
            .Select(ParseDependency)
            .ToArray();

        return new PostgresDependencyHealthOptions
        {
            RefreshIntervalSeconds = GetInt32(section["RefreshIntervalSeconds"], defaultValue: 30),
            Dependencies = dependencies
        };
    }

    private static PostgresDependencyDefinition ParseDependency(IConfigurationSection section)
    {
        return new PostgresDependencyDefinition
        {
            Id = section["Id"]?.Trim() ?? string.Empty,
            DisplayName = section["DisplayName"]?.Trim(),
            ConnectionString = section["ConnectionString"]?.Trim(),
            Host = section["Host"]?.Trim() ?? string.Empty,
            Port = GetInt32(section["Port"], defaultValue: 5432),
            Database = section["Database"]?.Trim() ?? "postgres",
            Username = section["Username"]?.Trim(),
            Password = section["Password"],
            SslMode = section["SslMode"]?.Trim(),
            HealthQuery = section["HealthQuery"]?.Trim() ?? "SELECT 1;",
            Required = GetBoolean(section["Required"], defaultValue: false),
            TimeoutSeconds = GetInt32(section["TimeoutSeconds"], defaultValue: 5)
        };
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgresDependencyHealthOptions" /> class.
    /// </summary>
    public PostgresDependencyHealthOptions()
    {
    }
}
