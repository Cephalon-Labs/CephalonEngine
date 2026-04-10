using Microsoft.Extensions.Configuration;

namespace Cephalon.Engine.Configuration;

/// <summary>
/// Describes configuration-driven timeout settings for a Cephalon app.
/// </summary>
public sealed class TimeoutSettings
{
    /// <summary>
    /// Gets an empty timeout-settings instance.
    /// </summary>
    public static TimeoutSettings Empty { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="TimeoutSettings" /> class.
    /// </summary>
    /// <param name="enabled">Whether timeout support was explicitly enabled.</param>
    /// <param name="totalTimeoutSeconds">The overall timeout in seconds requested for an execution.</param>
    /// <param name="attemptTimeoutSeconds">The per-attempt timeout in seconds requested for an execution.</param>
    public TimeoutSettings(
        bool? enabled = null,
        int? totalTimeoutSeconds = null,
        int? attemptTimeoutSeconds = null)
    {
        Enabled = enabled;
        TotalTimeoutSeconds = totalTimeoutSeconds;
        AttemptTimeoutSeconds = attemptTimeoutSeconds;
    }

    /// <summary>
    /// Gets a value indicating whether timeout support was explicitly enabled.
    /// </summary>
    public bool? Enabled { get; }

    /// <summary>
    /// Gets the overall timeout in seconds requested for an execution.
    /// </summary>
    public int? TotalTimeoutSeconds { get; }

    /// <summary>
    /// Gets the per-attempt timeout in seconds requested for an execution.
    /// </summary>
    public int? AttemptTimeoutSeconds { get; }

    /// <summary>
    /// Gets a value indicating whether any timeout settings were explicitly supplied.
    /// </summary>
    public bool HasValues =>
        Enabled.HasValue ||
        TotalTimeoutSeconds.HasValue ||
        AttemptTimeoutSeconds.HasValue;

    internal static TimeoutSettings FromSection(IConfiguration section)
    {
        ArgumentNullException.ThrowIfNull(section);

        return new TimeoutSettings(
            enabled: TryParseBoolean(section["Enabled"]),
            totalTimeoutSeconds: TryParseInt32(section["TotalTimeoutSeconds"]),
            attemptTimeoutSeconds: TryParseInt32(section["AttemptTimeoutSeconds"]));
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
