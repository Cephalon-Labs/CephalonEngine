using Microsoft.Extensions.Configuration;

namespace Cephalon.Engine.Configuration;

/// <summary>
/// Describes configuration-driven retry settings for a Cephalon app.
/// </summary>
public sealed class RetrySettings
{
    /// <summary>
    /// Gets an empty retry-settings instance.
    /// </summary>
    public static RetrySettings Empty { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="RetrySettings" /> class.
    /// </summary>
    /// <param name="enabled">Whether retry support was explicitly enabled.</param>
    /// <param name="maxAttempts">The maximum retry attempts requested for the policy.</param>
    /// <param name="backoff">The requested backoff mode, such as <c>Exponential</c> or <c>Linear</c>.</param>
    /// <param name="baseDelayMilliseconds">The base delay in milliseconds used by the retry policy.</param>
    /// <param name="maxDelayMilliseconds">The maximum delay in milliseconds the retry policy may apply.</param>
    /// <param name="useJitter">Whether jitter was explicitly requested for retry delays.</param>
    public RetrySettings(
        bool? enabled = null,
        int? maxAttempts = null,
        string? backoff = null,
        int? baseDelayMilliseconds = null,
        int? maxDelayMilliseconds = null,
        bool? useJitter = null)
    {
        Enabled = enabled;
        MaxAttempts = maxAttempts;
        Backoff = string.IsNullOrWhiteSpace(backoff) ? null : backoff.Trim();
        BaseDelayMilliseconds = baseDelayMilliseconds;
        MaxDelayMilliseconds = maxDelayMilliseconds;
        UseJitter = useJitter;
    }

    /// <summary>
    /// Gets a value indicating whether retry support was explicitly enabled.
    /// </summary>
    public bool? Enabled { get; }

    /// <summary>
    /// Gets the maximum retry attempts requested for the policy.
    /// </summary>
    public int? MaxAttempts { get; }

    /// <summary>
    /// Gets the requested backoff mode, such as <c>Exponential</c> or <c>Linear</c>.
    /// </summary>
    public string? Backoff { get; }

    /// <summary>
    /// Gets the base delay in milliseconds used by the retry policy.
    /// </summary>
    public int? BaseDelayMilliseconds { get; }

    /// <summary>
    /// Gets the maximum delay in milliseconds the retry policy may apply.
    /// </summary>
    public int? MaxDelayMilliseconds { get; }

    /// <summary>
    /// Gets a value indicating whether jitter was explicitly requested for retry delays.
    /// </summary>
    public bool? UseJitter { get; }

    /// <summary>
    /// Gets a value indicating whether any retry settings were explicitly supplied.
    /// </summary>
    public bool HasValues =>
        Enabled.HasValue ||
        MaxAttempts.HasValue ||
        Backoff is not null ||
        BaseDelayMilliseconds.HasValue ||
        MaxDelayMilliseconds.HasValue ||
        UseJitter.HasValue;

    internal static RetrySettings FromSection(IConfiguration section)
    {
        ArgumentNullException.ThrowIfNull(section);

        return new RetrySettings(
            enabled: TryParseBoolean(section["Enabled"]),
            maxAttempts: TryParseInt32(section["MaxAttempts"]),
            backoff: section["Backoff"],
            baseDelayMilliseconds: TryParseInt32(section["BaseDelayMilliseconds"]),
            maxDelayMilliseconds: TryParseInt32(section["MaxDelayMilliseconds"]),
            useJitter: TryParseBoolean(section["UseJitter"]));
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
