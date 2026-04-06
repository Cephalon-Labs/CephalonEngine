using Cephalon.Engine.Configuration;
using Cephalon.Observability.DependencyHealth.Core.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cephalon.Observability.ClickHouseDependencies.Configuration;

/// <summary>
/// Configures ClickHouse dependency probes contributed to Cephalon runtime health.
/// </summary>
public sealed class ClickHouseDependencyHealthOptions : DependencyHealthOptionsBase<ClickHouseDependencyDefinition>
{
    /// <summary>
    /// Binds ClickHouse dependency-health options from configuration.
    /// </summary>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="sectionPath">
    /// The configuration section path that contains the engine settings. The default is <c>Engine</c>.
    /// </param>
    /// <returns>The bound dependency-health options.</returns>
    public static ClickHouseDependencyHealthOptions FromConfiguration(
        IConfiguration configuration,
        string sectionPath = EngineSettings.SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration
            .GetSection(sectionPath)
            .GetSection("Observability")
            .GetSection("DependencyHealth")
            .GetSection("ClickHouse");

        return FromSection(section);
    }

    private static ClickHouseDependencyHealthOptions FromSection(IConfigurationSection section)
    {
        var dependencies = section
            .GetSection("Dependencies")
            .GetChildren()
            .Select(ParseDependency)
            .ToArray();

        return new ClickHouseDependencyHealthOptions
        {
            RefreshIntervalSeconds = GetInt32(section["RefreshIntervalSeconds"], defaultValue: 30),
            Dependencies = dependencies
        };
    }

    private static ClickHouseDependencyDefinition ParseDependency(IConfigurationSection section)
    {
        return new ClickHouseDependencyDefinition
        {
            Id = section["Id"]?.Trim() ?? string.Empty,
            DisplayName = section["DisplayName"]?.Trim(),
            ConnectionString = section["ConnectionString"]?.Trim(),
            Host = section["Host"]?.Trim() ?? string.Empty,
            Protocol = section["Protocol"]?.Trim() ?? "http",
            Port = GetInt32(section["Port"], defaultValue: 8123),
            Database = section["Database"]?.Trim(),
            Username = section["Username"]?.Trim(),
            Password = section["Password"],
            HealthQuery = section["HealthQuery"]?.Trim() ?? "SELECT 1",
            Required = GetBoolean(section["Required"], defaultValue: false),
            TimeoutSeconds = GetInt32(section["TimeoutSeconds"], defaultValue: 5)
        };
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ClickHouseDependencyHealthOptions" /> class.
    /// </summary>
    public ClickHouseDependencyHealthOptions()
    {
    }
}
