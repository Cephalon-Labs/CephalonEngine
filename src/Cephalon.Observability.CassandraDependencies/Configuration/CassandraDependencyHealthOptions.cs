using Cephalon.Engine.Configuration;
using Cephalon.Observability.DependencyHealth.Core.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cephalon.Observability.CassandraDependencies.Configuration;

/// <summary>
/// Configures Cassandra dependency probes contributed to Cephalon runtime health.
/// </summary>
public sealed class CassandraDependencyHealthOptions : DependencyHealthOptionsBase<CassandraDependencyDefinition>
{
    /// <summary>
    /// Binds Cassandra dependency-health options from configuration.
    /// </summary>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="sectionPath">
    /// The configuration section path that contains the engine settings. The default is <c>Engine</c>.
    /// </param>
    /// <returns>The bound dependency-health options.</returns>
    public static CassandraDependencyHealthOptions FromConfiguration(
        IConfiguration configuration,
        string sectionPath = EngineSettings.SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration
            .GetSection(sectionPath)
            .GetSection("Observability")
            .GetSection("DependencyHealth")
            .GetSection("Cassandra");

        return FromSection(section);
    }

    private static CassandraDependencyHealthOptions FromSection(IConfigurationSection section)
    {
        var dependencies = section
            .GetSection("Dependencies")
            .GetChildren()
            .Select(ParseDependency)
            .ToArray();

        return new CassandraDependencyHealthOptions
        {
            RefreshIntervalSeconds = GetInt32(section["RefreshIntervalSeconds"], defaultValue: 30),
            Dependencies = dependencies
        };
    }

    private static CassandraDependencyDefinition ParseDependency(IConfigurationSection section)
    {
        return new CassandraDependencyDefinition
        {
            Id = section["Id"]?.Trim() ?? string.Empty,
            DisplayName = section["DisplayName"]?.Trim(),
            ContactPoints = ParseContactPoints(section),
            Port = GetInt32(section["Port"], defaultValue: 9042),
            Keyspace = section["Keyspace"]?.Trim(),
            Username = section["Username"]?.Trim(),
            Password = section["Password"],
            HealthQuery = section["HealthQuery"]?.Trim() ?? "SELECT release_version FROM system.local;",
            Required = GetBoolean(section["Required"], defaultValue: false),
            TimeoutSeconds = GetInt32(section["TimeoutSeconds"], defaultValue: 5)
        };
    }

    private static string[] ParseContactPoints(IConfigurationSection section)
    {
        var configuredChildren = section
            .GetSection("ContactPoints")
            .GetChildren()
            .Select(static child => child.Value?.Trim())
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Cast<string>()
            .ToArray();

        if (configuredChildren.Length > 0)
        {
            return configuredChildren;
        }

        return SplitContactPoints(section["ContactPoints"]);
    }

    private static string[] SplitContactPoints(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Array.Empty<string>();
        }

        return value
            .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(static entry => !string.IsNullOrWhiteSpace(entry))
            .ToArray();
    }
}
