using System.Text.Json.Serialization;

namespace Cephalon.Abstractions.AppModel;

/// <summary>
/// Describes the circuit-breaker inputs resolved for a Cephalon app.
/// </summary>
public sealed class CircuitBreakerSelection
{
    /// <summary>
    /// Gets an empty circuit-breaker-selection instance.
    /// </summary>
    public static CircuitBreakerSelection Empty { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="CircuitBreakerSelection" /> class.
    /// </summary>
    /// <param name="enabled">Whether circuit-breaker support was explicitly enabled.</param>
    /// <param name="failureRatio">The failure ratio threshold requested for opening the breaker.</param>
    /// <param name="minimumThroughput">The minimum throughput required before the breaker evaluates failures.</param>
    /// <param name="samplingDurationSeconds">The sampling duration in seconds used by the breaker.</param>
    /// <param name="breakDurationSeconds">The break duration in seconds requested for the open state.</param>
    [JsonConstructor]
    public CircuitBreakerSelection(
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
    /// Gets a value indicating whether any circuit-breaker-selection inputs were explicitly supplied.
    /// </summary>
    public bool HasValues =>
        Enabled.HasValue ||
        FailureRatio.HasValue ||
        MinimumThroughput.HasValue ||
        SamplingDurationSeconds.HasValue ||
        BreakDurationSeconds.HasValue;
}
