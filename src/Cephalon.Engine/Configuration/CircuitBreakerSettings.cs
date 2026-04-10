using Microsoft.Extensions.Configuration;

namespace Cephalon.Engine.Configuration;

/// <summary>
/// Describes configuration-driven circuit-breaker settings for a Cephalon app.
/// </summary>
public sealed class CircuitBreakerSettings
{
    /// <summary>
    /// Gets an empty circuit-breaker-settings instance.
    /// </summary>
    public static CircuitBreakerSettings Empty { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="CircuitBreakerSettings" /> class.
    /// </summary>
    /// <param name="enabled">Whether circuit-breaker support was explicitly enabled.</param>
    /// <param name="failureRatio">The failure ratio threshold requested for opening the breaker.</param>
    /// <param name="minimumThroughput">The minimum throughput required before the breaker evaluates failures.</param>
    /// <param name="samplingDurationSeconds">The sampling duration in seconds used by the breaker.</param>
    /// <param name="breakDurationSeconds">The break duration in seconds requested for the open state.</param>
    public CircuitBreakerSettings(
        bool? enabled = null,
        decimal? failureRatio = null,
        int? minimumThroughput = null,
        int? samplingDurationSeconds = null,
        int? breakDurationSeconds = null)
    {
        Enabled = enabled;
        FailureRatio = failureRatio;
        MinimumThroughput = minimumThroughput;
        SamplingDurationSeconds = samplingDurationSeconds;
        BreakDurationSeconds = breakDurationSeconds;
    }

    /// <summary>
    /// Gets a value indicating whether circuit-breaker support was explicitly enabled.
    /// </summary>
    public bool? Enabled { get; }

    /// <summary>
    /// Gets the failure ratio threshold requested for opening the breaker.
    /// </summary>
    public decimal? FailureRatio { get; }

    /// <summary>
    /// Gets the minimum throughput required before the breaker evaluates failures.
    /// </summary>
    public int? MinimumThroughput { get; }

    /// <summary>
    /// Gets the sampling duration in seconds used by the breaker.
    /// </summary>
    public int? SamplingDurationSeconds { get; }

    /// <summary>
    /// Gets the break duration in seconds requested for the open state.
    /// </summary>
    public int? BreakDurationSeconds { get; }

    /// <summary>
    /// Gets a value indicating whether any circuit-breaker settings were explicitly supplied.
    /// </summary>
    public bool HasValues =>
        Enabled.HasValue ||
        FailureRatio.HasValue ||
        MinimumThroughput.HasValue ||
        SamplingDurationSeconds.HasValue ||
        BreakDurationSeconds.HasValue;

    internal static CircuitBreakerSettings FromSection(IConfiguration section)
    {
        ArgumentNullException.ThrowIfNull(section);

        return new CircuitBreakerSettings(
            enabled: TryParseBoolean(section["Enabled"]),
            failureRatio: TryParseDecimal(section["FailureRatio"]),
            minimumThroughput: TryParseInt32(section["MinimumThroughput"]),
            samplingDurationSeconds: TryParseInt32(section["SamplingDurationSeconds"]),
            breakDurationSeconds: TryParseInt32(section["BreakDurationSeconds"]));
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

    private static decimal? TryParseDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return decimal.TryParse(value.Trim(), out var parsed)
            ? parsed
            : null;
    }
}
