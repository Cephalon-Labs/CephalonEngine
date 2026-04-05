using Microsoft.Extensions.Configuration;

namespace Cephalon.Engine.Configuration;

/// <summary>
/// Describes configuration-driven data settings for a Cephalon app.
/// </summary>
public sealed class DataSettings
{
    /// <summary>
    /// Gets an empty data-settings instance.
    /// </summary>
    public static DataSettings Empty { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="DataSettings" /> class.
    /// </summary>
    /// <param name="provider">The selected primary data-provider family or implementation identifier.</param>
    /// <param name="readWriteSplit">Whether distinct read and write paths were explicitly selected.</param>
    /// <param name="outboxEnabled">Whether the outbox pattern was explicitly enabled.</param>
    /// <param name="idGenerator">The selected identifier-generation strategy.</param>
    public DataSettings(
        string? provider = null,
        bool? readWriteSplit = null,
        bool? outboxEnabled = null,
        string? idGenerator = null)
    {
        Provider = string.IsNullOrWhiteSpace(provider) ? null : provider.Trim();
        ReadWriteSplit = readWriteSplit;
        OutboxEnabled = outboxEnabled;
        IdGenerator = string.IsNullOrWhiteSpace(idGenerator) ? null : idGenerator.Trim();
    }

    /// <summary>
    /// Gets the selected primary data-provider family or implementation identifier.
    /// </summary>
    public string? Provider { get; }

    /// <summary>
    /// Gets a value indicating whether distinct read and write paths were explicitly selected.
    /// </summary>
    public bool? ReadWriteSplit { get; }

    /// <summary>
    /// Gets a value indicating whether the outbox pattern was explicitly enabled.
    /// </summary>
    public bool? OutboxEnabled { get; }

    /// <summary>
    /// Gets the selected identifier-generation strategy.
    /// </summary>
    public string? IdGenerator { get; }

    /// <summary>
    /// Gets a value indicating whether any data settings were explicitly supplied.
    /// </summary>
    public bool HasValues =>
        Provider is not null ||
        ReadWriteSplit.HasValue ||
        OutboxEnabled.HasValue ||
        IdGenerator is not null;

    /// <summary>
    /// Reads data settings from configuration.
    /// </summary>
    /// <param name="configuration">The configuration source that contains the engine section.</param>
    /// <param name="sectionPath">The root configuration section path to read from.</param>
    /// <returns>The parsed data settings.</returns>
    public static DataSettings FromConfiguration(
        IConfiguration configuration,
        string sectionPath = EngineSettings.SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration
            .GetSection(sectionPath)
            .GetSection("Data");

        return new DataSettings(
            provider: section["Provider"],
            readWriteSplit: TryParseBoolean(section["ReadWriteSplit"]),
            outboxEnabled: TryParseBoolean(section.GetSection("Outbox")["Enabled"]),
            idGenerator: section.GetSection("Ids")["Generator"]);
    }

    private static bool? TryParseBoolean(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return bool.TryParse(value.Trim(), out var parsed)
            ? parsed
            : null;
    }
}
