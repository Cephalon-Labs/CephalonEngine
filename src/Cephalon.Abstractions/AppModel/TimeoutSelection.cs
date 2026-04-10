using System.Text.Json.Serialization;

namespace Cephalon.Abstractions.AppModel;

/// <summary>
/// Describes the timeout-policy inputs resolved for a Cephalon app.
/// </summary>
public sealed class TimeoutSelection
{
    /// <summary>
    /// Gets an empty timeout-selection instance.
    /// </summary>
    public static TimeoutSelection Empty { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="TimeoutSelection" /> class.
    /// </summary>
    /// <param name="enabled">Whether timeout support was explicitly enabled.</param>
    /// <param name="totalTimeoutSeconds">The overall timeout in seconds requested for an execution.</param>
    /// <param name="attemptTimeoutSeconds">The per-attempt timeout in seconds requested for an execution.</param>
    [JsonConstructor]
    public TimeoutSelection(
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
    /// Gets a value indicating whether any timeout-selection inputs were explicitly supplied.
    /// </summary>
    public bool HasValues =>
        Enabled.HasValue ||
        TotalTimeoutSeconds.HasValue ||
        AttemptTimeoutSeconds.HasValue;
}
