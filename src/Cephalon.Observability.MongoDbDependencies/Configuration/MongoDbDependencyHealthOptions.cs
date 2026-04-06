using Cephalon.Engine.Configuration;
using Cephalon.Observability.DependencyHealth.Core.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cephalon.Observability.MongoDbDependencies.Configuration;

/// <summary>
/// Configures MongoDB dependency probes contributed to Cephalon runtime health.
/// </summary>
public sealed class MongoDbDependencyHealthOptions : DependencyHealthOptionsBase<MongoDbDependencyDefinition>
{
    /// <summary>
    /// Binds MongoDB dependency-health options from configuration.
    /// </summary>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="sectionPath">
    /// The configuration section path that contains the engine settings. The default is <c>Engine</c>.
    /// </param>
    /// <returns>The bound dependency-health options.</returns>
    public static MongoDbDependencyHealthOptions FromConfiguration(
        IConfiguration configuration,
        string sectionPath = EngineSettings.SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration
            .GetSection(sectionPath)
            .GetSection("Observability")
            .GetSection("DependencyHealth")
            .GetSection("MongoDb");

        return FromSection(section);
    }

    private static MongoDbDependencyHealthOptions FromSection(IConfigurationSection section)
    {
        var dependencies = section
            .GetSection("Dependencies")
            .GetChildren()
            .Select(ParseDependency)
            .ToArray();

        return new MongoDbDependencyHealthOptions
        {
            RefreshIntervalSeconds = GetInt32(section["RefreshIntervalSeconds"], defaultValue: 30),
            Dependencies = dependencies
        };
    }

    private static MongoDbDependencyDefinition ParseDependency(IConfigurationSection section)
    {
        return new MongoDbDependencyDefinition
        {
            Id = section["Id"]?.Trim() ?? string.Empty,
            DisplayName = section["DisplayName"]?.Trim(),
            ConnectionString = section["ConnectionString"]?.Trim(),
            Host = section["Host"]?.Trim() ?? string.Empty,
            Port = GetInt32(section["Port"], defaultValue: 27017),
            Database = section["Database"]?.Trim() ?? "admin",
            Username = section["Username"]?.Trim(),
            Password = section["Password"],
            AuthSource = section["AuthSource"]?.Trim(),
            UseTls = GetNullableBoolean(section["UseTls"]),
            AllowInsecureTls = GetNullableBoolean(section["AllowInsecureTls"]),
            DirectConnection = GetNullableBoolean(section["DirectConnection"]),
            HealthCommand = section["HealthCommand"]?.Trim() ?? "ping",
            Required = GetBoolean(section["Required"], defaultValue: false),
            TimeoutSeconds = GetInt32(section["TimeoutSeconds"], defaultValue: 5)
        };
    }
}
