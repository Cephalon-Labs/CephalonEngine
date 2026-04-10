using Microsoft.Extensions.Configuration;

namespace Cephalon.Engine.Configuration;

/// <summary>
/// Describes configuration-driven bulkhead settings for a Cephalon app.
/// </summary>
public sealed class BulkheadSettings
{
    /// <summary>
    /// Gets an empty bulkhead-settings instance.
    /// </summary>
    public static BulkheadSettings Empty { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="BulkheadSettings" /> class.
    /// </summary>
    /// <param name="enabled">Whether bulkhead isolation was explicitly enabled.</param>
    /// <param name="maxConcurrentExecutions">The maximum concurrent executions allowed inside the bulkhead.</param>
    /// <param name="maxQueuedActions">The maximum queued actions allowed before rejection.</param>
    public BulkheadSettings(
        bool? enabled = null,
        int? maxConcurrentExecutions = null,
        int? maxQueuedActions = null)
    {
        Enabled = enabled;
        MaxConcurrentExecutions = maxConcurrentExecutions;
        MaxQueuedActions = maxQueuedActions;
    }

    /// <summary>
    /// Gets a value indicating whether bulkhead isolation was explicitly enabled.
    /// </summary>
    public bool? Enabled { get; }

    /// <summary>
    /// Gets the maximum concurrent executions allowed inside the bulkhead.
    /// </summary>
    public int? MaxConcurrentExecutions { get; }

    /// <summary>
    /// Gets the maximum queued actions allowed before rejection.
    /// </summary>
    public int? MaxQueuedActions { get; }

    /// <summary>
    /// Gets a value indicating whether any bulkhead settings were explicitly supplied.
    /// </summary>
    public bool HasValues =>
        Enabled.HasValue ||
        MaxConcurrentExecutions.HasValue ||
        MaxQueuedActions.HasValue;

    internal static BulkheadSettings FromSection(IConfiguration section)
    {
        ArgumentNullException.ThrowIfNull(section);

        return new BulkheadSettings(
            enabled: TryParseBoolean(section["Enabled"]),
            maxConcurrentExecutions: TryParseInt32(section["MaxConcurrentExecutions"]),
            maxQueuedActions: TryParseInt32(section["MaxQueuedActions"]));
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

    private static int? TryParseInt32(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return int.TryParse(value.Trim(), out var parsed)
            ? parsed
            : null;
    }
}
