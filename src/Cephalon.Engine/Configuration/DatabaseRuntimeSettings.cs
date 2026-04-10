using Microsoft.Extensions.Configuration;

namespace Cephalon.Engine.Configuration;

/// <summary>
/// Describes configuration-driven database runtime tuning for Cephalon apps.
/// </summary>
public sealed class DatabaseRuntimeSettings
{
    /// <summary>
    /// Gets an empty database-runtime settings instance.
    /// </summary>
    public static DatabaseRuntimeSettings Empty { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseRuntimeSettings" /> class.
    /// </summary>
    public DatabaseRuntimeSettings(
        bool? enableDetailedErrors = null,
        bool? enableSensitiveDataLogging = null,
        bool? enableRetryOnFailure = null,
        int? maxRetryCount = null,
        int? maxRetryDelaySeconds = null,
        int? commandTimeoutSeconds = null,
        int? maxBatchSize = null)
    {
        EnableDetailedErrors = enableDetailedErrors;
        EnableSensitiveDataLogging = enableSensitiveDataLogging;
        EnableRetryOnFailure = enableRetryOnFailure;
        MaxRetryCount = maxRetryCount;
        MaxRetryDelaySeconds = maxRetryDelaySeconds;
        CommandTimeoutSeconds = commandTimeoutSeconds;
        MaxBatchSize = maxBatchSize;
    }

    /// <summary>
    /// Gets a value indicating whether detailed provider errors were explicitly enabled.
    /// </summary>
    public bool? EnableDetailedErrors { get; }

    /// <summary>
    /// Gets a value indicating whether sensitive-data logging was explicitly enabled.
    /// </summary>
    public bool? EnableSensitiveDataLogging { get; }

    /// <summary>
    /// Gets a value indicating whether transient-failure retries were explicitly enabled.
    /// </summary>
    public bool? EnableRetryOnFailure { get; }

    /// <summary>
    /// Gets the maximum retry count when transient-failure retries were configured.
    /// </summary>
    public int? MaxRetryCount { get; }

    /// <summary>
    /// Gets the maximum retry delay in seconds when transient-failure retries were configured.
    /// </summary>
    public int? MaxRetryDelaySeconds { get; }

    /// <summary>
    /// Gets the command timeout in seconds when one was configured.
    /// </summary>
    public int? CommandTimeoutSeconds { get; }

    /// <summary>
    /// Gets the maximum provider batch size when one was configured.
    /// </summary>
    public int? MaxBatchSize { get; }

    /// <summary>
    /// Gets a value indicating whether any database runtime settings were explicitly supplied.
    /// </summary>
    public bool HasValues =>
        EnableDetailedErrors.HasValue ||
        EnableSensitiveDataLogging.HasValue ||
        EnableRetryOnFailure.HasValue ||
        MaxRetryCount.HasValue ||
        MaxRetryDelaySeconds.HasValue ||
        CommandTimeoutSeconds.HasValue ||
        MaxBatchSize.HasValue;

    /// <summary>
    /// Reads database runtime settings from the supplied configuration section.
    /// </summary>
    public static DatabaseRuntimeSettings FromSection(IConfigurationSection? section)
    {
        if (section is null || !section.Exists())
        {
            return Empty;
        }

        return new DatabaseRuntimeSettings(
            enableDetailedErrors: TryParseBoolean(section["EnableDetailedErrors"]),
            enableSensitiveDataLogging: TryParseBoolean(section["EnableSensitiveDataLogging"]),
            enableRetryOnFailure: TryParseBoolean(section["EnableRetryOnFailure"]),
            maxRetryCount: TryParsePositiveInt(section["MaxRetryCount"]),
            maxRetryDelaySeconds: TryParsePositiveInt(section["MaxRetryDelaySeconds"]),
            commandTimeoutSeconds: TryParsePositiveInt(section["CommandTimeoutSeconds"]),
            maxBatchSize: TryParsePositiveInt(section["MaxBatchSize"]));
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

    private static int? TryParsePositiveInt(string? value)
    {
        return int.TryParse(value, out var parsed) && parsed > 0
            ? parsed
            : null;
    }
}
