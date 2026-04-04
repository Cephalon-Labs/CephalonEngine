using Microsoft.Extensions.Configuration;

namespace Cephalon.Engine.Configuration;

/// <summary>
/// Describes configuration-driven messaging settings for a Cephalon app.
/// </summary>
public sealed class MessagingSettings
{
    /// <summary>
    /// Gets an empty messaging-settings instance.
    /// </summary>
    public static MessagingSettings Empty { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="MessagingSettings" /> class.
    /// </summary>
    /// <param name="provider">The selected messaging provider or runtime adapter.</param>
    public MessagingSettings(string? provider = null)
    {
        Provider = string.IsNullOrWhiteSpace(provider) ? null : provider.Trim();
    }

    /// <summary>
    /// Gets the selected messaging provider or runtime adapter.
    /// </summary>
    public string? Provider { get; }

    /// <summary>
    /// Gets a value indicating whether any messaging settings were explicitly supplied.
    /// </summary>
    public bool HasValues => Provider is not null;

    /// <summary>
    /// Reads messaging settings from configuration.
    /// </summary>
    /// <param name="configuration">The configuration source that contains the engine section.</param>
    /// <param name="sectionPath">The root configuration section path to read from.</param>
    /// <returns>The parsed messaging settings.</returns>
    public static MessagingSettings FromConfiguration(
        IConfiguration configuration,
        string sectionPath = EngineSettings.SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration
            .GetSection(sectionPath)
            .GetSection("Messaging");

        return new MessagingSettings(section["Provider"]);
    }
}
