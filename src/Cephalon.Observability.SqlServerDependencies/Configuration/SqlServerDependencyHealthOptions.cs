using Cephalon.Engine.Configuration;
using Cephalon.Observability.DependencyHealth.Core.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cephalon.Observability.SqlServerDependencies.Configuration;

/// <summary>
/// Configures SQL Server dependency probes contributed to Cephalon runtime health.
/// </summary>
public sealed class SqlServerDependencyHealthOptions : DependencyHealthOptionsBase<SqlServerDependencyDefinition>
{
    /// <summary>
    /// Binds SQL Server dependency-health options from configuration.
    /// </summary>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="sectionPath">
    /// The configuration section path that contains the engine settings. The default is <c>Engine</c>.
    /// </param>
    /// <returns>The bound dependency-health options.</returns>
    public static SqlServerDependencyHealthOptions FromConfiguration(
        IConfiguration configuration,
        string sectionPath = EngineSettings.SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration
            .GetSection(sectionPath)
            .GetSection("Observability")
            .GetSection("DependencyHealth")
            .GetSection("SqlServer");

        return FromSection(section);
    }

    private static SqlServerDependencyHealthOptions FromSection(IConfigurationSection section)
    {
        var dependencies = section
            .GetSection("Dependencies")
            .GetChildren()
            .Select(ParseDependency)
            .ToArray();

        return new SqlServerDependencyHealthOptions
        {
            RefreshIntervalSeconds = GetInt32(section["RefreshIntervalSeconds"], defaultValue: 30),
            Dependencies = dependencies
        };
    }

    private static SqlServerDependencyDefinition ParseDependency(IConfigurationSection section)
    {
        return new SqlServerDependencyDefinition
        {
            Id = section["Id"]?.Trim() ?? string.Empty,
            DisplayName = section["DisplayName"]?.Trim(),
            ConnectionString = section["ConnectionString"]?.Trim(),
            Host = section["Host"]?.Trim() ?? string.Empty,
            Port = GetInt32(section["Port"], defaultValue: 1433),
            Database = section["Database"]?.Trim() ?? "master",
            Username = section["Username"]?.Trim(),
            Password = section["Password"],
            Encrypt = section["Encrypt"]?.Trim(),
            TrustServerCertificate = GetNullableBoolean(section["TrustServerCertificate"]),
            HealthQuery = section["HealthQuery"]?.Trim() ?? "SELECT 1;",
            Required = GetBoolean(section["Required"], defaultValue: false),
            TimeoutSeconds = GetInt32(section["TimeoutSeconds"], defaultValue: 5)
        };
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerDependencyHealthOptions" /> class.
    /// </summary>
    public SqlServerDependencyHealthOptions()
    {
    }
}
