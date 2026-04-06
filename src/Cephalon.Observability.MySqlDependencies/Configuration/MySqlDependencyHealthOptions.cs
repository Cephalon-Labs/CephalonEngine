using Cephalon.Engine.Configuration;
using Cephalon.Observability.DependencyHealth.Core.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cephalon.Observability.MySqlDependencies.Configuration;

/// <summary>
/// Configures MySQL dependency probes contributed to Cephalon runtime health.
/// </summary>
public sealed class MySqlDependencyHealthOptions : DependencyHealthOptionsBase<MySqlDependencyDefinition>
{
    /// <summary>
    /// Binds MySQL dependency-health options from configuration.
    /// </summary>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="sectionPath">
    /// The configuration section path that contains the engine settings. The default is <c>Engine</c>.
    /// </param>
    /// <returns>The bound dependency-health options.</returns>
    public static MySqlDependencyHealthOptions FromConfiguration(
        IConfiguration configuration,
        string sectionPath = EngineSettings.SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration
            .GetSection(sectionPath)
            .GetSection("Observability")
            .GetSection("DependencyHealth")
            .GetSection("MySql");

        return FromSection(section);
    }

    private static MySqlDependencyHealthOptions FromSection(IConfigurationSection section)
    {
        var dependencies = section
            .GetSection("Dependencies")
            .GetChildren()
            .Select(ParseDependency)
            .ToArray();

        return new MySqlDependencyHealthOptions
        {
            RefreshIntervalSeconds = GetInt32(section["RefreshIntervalSeconds"], defaultValue: 30),
            Dependencies = dependencies
        };
    }

    private static MySqlDependencyDefinition ParseDependency(IConfigurationSection section)
    {
        return new MySqlDependencyDefinition
        {
            Id = section["Id"]?.Trim() ?? string.Empty,
            DisplayName = section["DisplayName"]?.Trim(),
            ConnectionString = section["ConnectionString"]?.Trim(),
            Host = section["Host"]?.Trim() ?? string.Empty,
            Port = GetInt32(section["Port"], defaultValue: 3306),
            Database = section["Database"]?.Trim() ?? "mysql",
            Username = section["Username"]?.Trim(),
            Password = section["Password"],
            SslMode = section["SslMode"]?.Trim(),
            AllowPublicKeyRetrieval = GetNullableBoolean(section["AllowPublicKeyRetrieval"]),
            HealthQuery = section["HealthQuery"]?.Trim() ?? "SELECT 1;",
            Required = GetBoolean(section["Required"], defaultValue: false),
            TimeoutSeconds = GetInt32(section["TimeoutSeconds"], defaultValue: 5)
        };
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MySqlDependencyHealthOptions" /> class.
    /// </summary>
    public MySqlDependencyHealthOptions()
    {
    }
}
