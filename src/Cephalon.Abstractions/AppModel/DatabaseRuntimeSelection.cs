using System.Text.Json.Serialization;

namespace Cephalon.Abstractions.AppModel;

/// <summary>
/// Describes the active database runtime tuning inputs resolved for a Cephalon app.
/// </summary>
public sealed class DatabaseRuntimeSelection
{
    /// <summary>
    /// Gets an empty database-runtime selection instance.
    /// </summary>
    public static DatabaseRuntimeSelection Empty { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseRuntimeSelection" /> class.
    /// </summary>
    [JsonConstructor]
    public DatabaseRuntimeSelection(
        bool? enableDetailedErrors = null,
        bool? enableSensitiveDataLogging = null,
        bool? enableRetryOnFailure = null,
        int? maxRetryCount = null,
        int? maxRetryDelaySeconds = null,
        int? commandTimeoutSeconds = null,
        int? maxBatchSize = null,
        int? roleProbeFreshnessSeconds = null)
    {
        if (roleProbeFreshnessSeconds is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(roleProbeFreshnessSeconds), "Database role probe freshness must be zero or greater.");
        }

        EnableDetailedErrors = enableDetailedErrors;
        EnableSensitiveDataLogging = enableSensitiveDataLogging;
        EnableRetryOnFailure = enableRetryOnFailure;
        MaxRetryCount = maxRetryCount;
        MaxRetryDelaySeconds = maxRetryDelaySeconds;
        CommandTimeoutSeconds = commandTimeoutSeconds;
        MaxBatchSize = maxBatchSize;
        RoleProbeFreshnessSeconds = roleProbeFreshnessSeconds;
    }

    /// <summary>
    /// Gets a value indicating whether detailed provider errors were explicitly selected.
    /// </summary>
    public bool? EnableDetailedErrors { get; }

    /// <summary>
    /// Gets a value indicating whether sensitive-data logging was explicitly selected.
    /// </summary>
    public bool? EnableSensitiveDataLogging { get; }

    /// <summary>
    /// Gets a value indicating whether transient-failure retries were explicitly selected.
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
    /// Gets the freshness window in seconds for cached database-role probes when one was selected.
    /// A value of <c>0</c> disables probe-result caching.
    /// </summary>
    public int? RoleProbeFreshnessSeconds { get; }

    /// <summary>
    /// Gets a value indicating whether any database-runtime selection inputs were explicitly supplied.
    /// </summary>
    public bool HasValues =>
        EnableDetailedErrors.HasValue ||
        EnableSensitiveDataLogging.HasValue ||
        EnableRetryOnFailure.HasValue ||
        MaxRetryCount.HasValue ||
        MaxRetryDelaySeconds.HasValue ||
        CommandTimeoutSeconds.HasValue ||
        MaxBatchSize.HasValue ||
        RoleProbeFreshnessSeconds.HasValue;
}
