using Cephalon.Engine.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cephalon.Observability.MqttDependencies.Configuration;

/// <summary>
/// Configures MQTT dependency probes contributed to Cephalon runtime health.
/// </summary>
public sealed class MqttDependencyHealthOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MqttDependencyHealthOptions" /> class.
    /// </summary>
    public MqttDependencyHealthOptions()
    {
    }

    /// <summary>
    /// Gets or sets the interval, in seconds, between background refresh attempts.
    /// </summary>
    public int RefreshIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the configured MQTT dependencies that should contribute to runtime health.
    /// </summary>
    public IReadOnlyList<MqttDependencyDefinition> Dependencies { get; set; } = Array.Empty<MqttDependencyDefinition>();

    /// <summary>
    /// Binds MQTT dependency-health options from configuration.
    /// </summary>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="sectionPath">
    /// The configuration section path that contains the engine settings. The default is <c>Engine</c>.
    /// </param>
    /// <returns>The bound dependency-health options.</returns>
    public static MqttDependencyHealthOptions FromConfiguration(
        IConfiguration configuration,
        string sectionPath = EngineSettings.SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration
            .GetSection(sectionPath)
            .GetSection("Observability")
            .GetSection("DependencyHealth")
            .GetSection("Mqtt");

        return FromSection(section);
    }

    private static MqttDependencyHealthOptions FromSection(IConfigurationSection section)
    {
        var dependencies = section
            .GetSection("Dependencies")
            .GetChildren()
            .Select(ParseDependency)
            .ToArray();

        return new MqttDependencyHealthOptions
        {
            RefreshIntervalSeconds = GetInt32(section["RefreshIntervalSeconds"], defaultValue: 30),
            Dependencies = dependencies
        };
    }

    private static MqttDependencyDefinition ParseDependency(IConfigurationSection section)
    {
        return new MqttDependencyDefinition
        {
            Id = section["Id"]?.Trim() ?? string.Empty,
            DisplayName = section["DisplayName"]?.Trim(),
            Host = section["Host"]?.Trim() ?? string.Empty,
            Port = GetInt32(section["Port"], defaultValue: 1883),
            UseTls = GetBoolean(section["UseTls"], defaultValue: false),
            TlsServerName = section["TlsServerName"]?.Trim(),
            ClientId = section["ClientId"]?.Trim(),
            Username = section["Username"]?.Trim(),
            Password = section["Password"],
            KeepAliveSeconds = GetInt32(section["KeepAliveSeconds"], defaultValue: 30),
            Required = GetBoolean(section["Required"], defaultValue: false),
            TimeoutSeconds = GetInt32(section["TimeoutSeconds"], defaultValue: 5)
        };
    }

    private static bool GetBoolean(string? value, bool defaultValue) =>
        bool.TryParse(value, out var parsed) ? parsed : defaultValue;

    private static int GetInt32(string? value, int defaultValue) =>
        int.TryParse(value, out var parsed) && parsed > 0 ? parsed : defaultValue;
}
