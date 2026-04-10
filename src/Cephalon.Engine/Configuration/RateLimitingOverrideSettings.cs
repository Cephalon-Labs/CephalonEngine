using Microsoft.Extensions.Configuration;

namespace Cephalon.Engine.Configuration;

/// <summary>
/// Describes one named rate-limiting override configured for a subset of transports or behaviors.
/// </summary>
public sealed class RateLimitingOverrideSettings
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitingOverrideSettings" /> class.
    /// </summary>
    /// <param name="id">The stable override identifier.</param>
    /// <param name="behaviorIds">The targeted behavior identifiers.</param>
    /// <param name="transportIds">The targeted transport identifiers.</param>
    /// <param name="enabled">Whether the override explicitly enables or disables rate limiting.</param>
    /// <param name="algorithm">The requested rate-limiting algorithm, such as <c>FixedWindow</c> or <c>TokenBucket</c>.</param>
    /// <param name="permitLimit">The maximum permits available per limiter window or bucket.</param>
    /// <param name="queueLimit">The maximum queued requests allowed before rejection.</param>
    /// <param name="windowSeconds">The limiter window duration in seconds when the selected algorithm uses windows.</param>
    /// <param name="segmentsPerWindow">The number of segments per window when sliding windows are used.</param>
    public RateLimitingOverrideSettings(
        string id,
        IReadOnlyList<string>? behaviorIds = null,
        IReadOnlyList<string>? transportIds = null,
        bool? enabled = null,
        string? algorithm = null,
        int? permitLimit = null,
        int? queueLimit = null,
        int? windowSeconds = null,
        int? segmentsPerWindow = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        Id = id.Trim();
        BehaviorIds = NormalizeList(behaviorIds);
        TransportIds = NormalizeList(transportIds);
        Enabled = enabled;
        Algorithm = string.IsNullOrWhiteSpace(algorithm) ? null : algorithm.Trim();
        PermitLimit = permitLimit;
        QueueLimit = queueLimit;
        WindowSeconds = windowSeconds;
        SegmentsPerWindow = segmentsPerWindow;
    }

    /// <summary>
    /// Gets the stable override identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the targeted behavior identifiers.
    /// </summary>
    public IReadOnlyList<string> BehaviorIds { get; }

    /// <summary>
    /// Gets the targeted transport identifiers.
    /// </summary>
    public IReadOnlyList<string> TransportIds { get; }

    /// <summary>
    /// Gets a value indicating whether rate limiting was explicitly enabled or disabled.
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
    /// Gets a value indicating whether any override settings were explicitly supplied.
    /// </summary>
    public bool HasValues =>
        BehaviorIds.Count > 0 ||
        TransportIds.Count > 0 ||
        Enabled.HasValue ||
        Algorithm is not null ||
        PermitLimit.HasValue ||
        QueueLimit.HasValue ||
        WindowSeconds.HasValue ||
        SegmentsPerWindow.HasValue;

    internal static IReadOnlyList<RateLimitingOverrideSettings> FromSection(IConfiguration section)
    {
        ArgumentNullException.ThrowIfNull(section);

        return section.GetChildren()
            .Select(static child => new RateLimitingOverrideSettings(
                id: child.Key,
                behaviorIds: ReadStringArray(child.GetSection("Behaviors")),
                transportIds: ReadStringArray(child.GetSection("Transports")),
                enabled: TryParseBoolean(child["Enabled"]),
                algorithm: child["Algorithm"],
                permitLimit: TryParseInt32(child["PermitLimit"]),
                queueLimit: TryParseInt32(child["QueueLimit"]),
                windowSeconds: TryParseInt32(child["WindowSeconds"]),
                segmentsPerWindow: TryParseInt32(child["SegmentsPerWindow"])))
            .ToArray();
    }

    private static string[] ReadStringArray(IConfiguration section)
    {
        return section.GetChildren()
            .Select(static child => child.Value)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string[] NormalizeList(IReadOnlyList<string>? values)
    {
        return values?
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
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
