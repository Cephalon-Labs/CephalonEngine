using Microsoft.Extensions.Configuration;

namespace Cephalon.Engine.Configuration;

/// <summary>
/// Describes configuration-driven rate-limiting settings for a Cephalon app.
/// </summary>
public sealed class RateLimitingSettings
{
    /// <summary>
    /// Gets an empty rate-limiting-settings instance.
    /// </summary>
    public static RateLimitingSettings Empty { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitingSettings" /> class.
    /// </summary>
    /// <param name="enabled">Whether rate limiting was explicitly enabled.</param>
    /// <param name="algorithm">The requested rate-limiting algorithm, such as <c>FixedWindow</c> or <c>TokenBucket</c>.</param>
    /// <param name="permitLimit">The maximum permits available per limiter window or bucket.</param>
    /// <param name="queueLimit">The maximum queued requests allowed before rejection.</param>
    /// <param name="windowSeconds">The limiter window duration in seconds when the selected algorithm uses windows.</param>
    /// <param name="segmentsPerWindow">The number of segments per window when sliding windows are used.</param>
    /// <param name="overrides">The named override policies targeted at specific transports or behaviors.</param>
    public RateLimitingSettings(
        bool? enabled = null,
        string? algorithm = null,
        int? permitLimit = null,
        int? queueLimit = null,
        int? windowSeconds = null,
        int? segmentsPerWindow = null,
        IReadOnlyList<RateLimitingOverrideSettings>? overrides = null)
    {
        Enabled = enabled;
        Algorithm = string.IsNullOrWhiteSpace(algorithm) ? null : algorithm.Trim();
        PermitLimit = permitLimit;
        QueueLimit = queueLimit;
        WindowSeconds = windowSeconds;
        SegmentsPerWindow = segmentsPerWindow;
        Overrides = overrides?
            .Where(static entry => entry is not null)
            .ToArray() ?? [];
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
    /// Gets the named override policies targeted at specific transports or behaviors.
    /// </summary>
    public IReadOnlyList<RateLimitingOverrideSettings> Overrides { get; }

    /// <summary>
    /// Gets a value indicating whether any rate-limiting settings were explicitly supplied.
    /// </summary>
    public bool HasValues =>
        Enabled.HasValue ||
        Algorithm is not null ||
        PermitLimit.HasValue ||
        QueueLimit.HasValue ||
        WindowSeconds.HasValue ||
        SegmentsPerWindow.HasValue ||
        Overrides.Count > 0;

    internal static RateLimitingSettings FromSection(IConfiguration section)
    {
        ArgumentNullException.ThrowIfNull(section);

        return new RateLimitingSettings(
            enabled: TryParseBoolean(section["Enabled"]),
            algorithm: section["Algorithm"],
            permitLimit: TryParseInt32(section["PermitLimit"]),
            queueLimit: TryParseInt32(section["QueueLimit"]),
            windowSeconds: TryParseInt32(section["WindowSeconds"]),
            segmentsPerWindow: TryParseInt32(section["SegmentsPerWindow"]),
            overrides: RateLimitingOverrideSettings.FromSection(section.GetSection("Overrides")));
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
