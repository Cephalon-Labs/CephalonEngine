using System.Text.Json.Serialization;

namespace Cephalon.Abstractions.AppModel;

/// <summary>
/// Describes the rate-limiting inputs resolved for a Cephalon app.
/// </summary>
public sealed class RateLimitingSelection
{
    /// <summary>
    /// Gets an empty rate-limiting-selection instance.
    /// </summary>
    public static RateLimitingSelection Empty { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitingSelection" /> class.
    /// </summary>
    /// <param name="enabled">Whether rate limiting was explicitly enabled.</param>
    /// <param name="algorithm">The requested rate-limiting algorithm, such as <c>FixedWindow</c> or <c>TokenBucket</c>.</param>
    /// <param name="permitLimit">The maximum permits available per limiter window or bucket.</param>
    /// <param name="queueLimit">The maximum queued requests allowed before rejection.</param>
    /// <param name="windowSeconds">The limiter window duration in seconds when the selected algorithm uses windows.</param>
    /// <param name="segmentsPerWindow">The number of segments per window when sliding windows are used.</param>
    [JsonConstructor]
    public RateLimitingSelection(
        bool? enabled = null,
        string? algorithm = null,
        int? permitLimit = null,
        int? queueLimit = null,
        int? windowSeconds = null,
        int? segmentsPerWindow = null)
    {
        Enabled = enabled;
        Algorithm = string.IsNullOrWhiteSpace(algorithm) ? null : algorithm.Trim();
        PermitLimit = permitLimit;
        QueueLimit = queueLimit;
        WindowSeconds = windowSeconds;
        SegmentsPerWindow = segmentsPerWindow;
    }

    /// <summary>
    /// Gets a value indicating whether rate limiting was explicitly enabled.
    /// </summary>
    public bool? Enabled { get; }

    /// <summary>
    /// Gets the requested rate-limiting algorithm, such as <c>FixedWindow</c> or <c>TokenBucket</c>.
    /// </summary>
    public string? Algorithm { get; }

    /// <summary>
    /// Gets the maximum permits available per limiter window or bucket.
    /// </summary>
    public int? PermitLimit { get; }

    /// <summary>
    /// Gets the maximum queued requests allowed before rejection.
    /// </summary>
    public int? QueueLimit { get; }

    /// <summary>
    /// Gets the limiter window duration in seconds when the selected algorithm uses windows.
    /// </summary>
    public int? WindowSeconds { get; }

    /// <summary>
    /// Gets the number of segments per window when sliding windows are used.
    /// </summary>
    public int? SegmentsPerWindow { get; }

    /// <summary>
    /// Gets a value indicating whether any rate-limiting-selection inputs were explicitly supplied.
    /// </summary>
    public bool HasValues =>
        Enabled.HasValue ||
        Algorithm is not null ||
        PermitLimit.HasValue ||
        QueueLimit.HasValue ||
        WindowSeconds.HasValue ||
        SegmentsPerWindow.HasValue;
}
