using Cephalon.Engine.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cephalon.Observability.MongoDbDependencies.Configuration;

/// <summary>
/// Configures MongoDB dependency probes contributed to Cephalon runtime health.
/// </summary>
public sealed class MongoDbDependencyHealthOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDbDependencyHealthOptions" /> class.
    /// </summary>
    public MongoDbDependencyHealthOptions()
    {
    }

    /// <summary>
    /// Gets or sets the interval, in seconds, between background refresh attempts.
    /// </summary>
    public int RefreshIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the configured MongoDB dependencies that should contribute to runtime health.
    /// </summary>
    public IReadOnlyList<MongoDbDependencyDefinition> Dependencies { get; set; } = Array.Empty<MongoDbDependencyDefinition>();

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

    private static bool GetBoolean(string? value, bool defaultValue) =>
        bool.TryParse(value, out var parsed) ? parsed : defaultValue;

    private static bool? GetNullableBoolean(string? value) =>
        bool.TryParse(value, out var parsed) ? parsed : null;

    private static int GetInt32(string? value, int defaultValue) =>
        int.TryParse(value, out var parsed) && parsed > 0 ? parsed : defaultValue;
}
