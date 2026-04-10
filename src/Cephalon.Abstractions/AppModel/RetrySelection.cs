using System.Text.Json.Serialization;

namespace Cephalon.Abstractions.AppModel;

/// <summary>
/// Describes the retry-policy inputs resolved for a Cephalon app.
/// </summary>
public sealed class RetrySelection
{
    /// <summary>
    /// Gets an empty retry-selection instance.
    /// </summary>
    public static RetrySelection Empty { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="RetrySelection" /> class.
    /// </summary>
    /// <param name="enabled">Whether retry support was explicitly enabled.</param>
    /// <param name="maxAttempts">The maximum retry attempts requested for the policy.</param>
    /// <param name="backoff">The requested backoff mode, such as <c>Exponential</c> or <c>Linear</c>.</param>
    /// <param name="baseDelayMilliseconds">The base delay in milliseconds used by the retry policy.</param>
    /// <param name="maxDelayMilliseconds">The maximum delay in milliseconds the retry policy may apply.</param>
    /// <param name="useJitter">Whether jitter was explicitly requested for retry delays.</param>
    [JsonConstructor]
    public RetrySelection(
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
    /// Gets a value indicating whether any retry-selection inputs were explicitly supplied.
    /// </summary>
    public bool HasValues =>
        Enabled.HasValue ||
        MaxAttempts.HasValue ||
        Backoff is not null ||
        BaseDelayMilliseconds.HasValue ||
        MaxDelayMilliseconds.HasValue ||
        UseJitter.HasValue;
}
