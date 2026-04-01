using Microsoft.Extensions.Configuration;

namespace Cephalon.Engine.Configuration;

/// <summary>
/// Describes how the runtime reacts to startup, stop, and restart failures.
/// </summary>
public sealed class FailurePolicy
{
    /// <summary>
    /// Gets the default failure policy used when no explicit configuration is supplied.
    /// </summary>
    public static FailurePolicy Default { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="FailurePolicy" /> class.
    /// </summary>
    /// <param name="startupFailureBehavior">How startup failures are handled.</param>
    /// <param name="stopFailureBehavior">How stop failures are handled.</param>
    /// <param name="allowManualRestart">Whether operators can manually restart the runtime after supported failures.</param>
    /// <param name="maxRestartAttempts">The maximum number of manual restarts, where <c>-1</c> allows unlimited restarts.</param>
    public FailurePolicy(
        StartupFailureBehavior startupFailureBehavior = StartupFailureBehavior.FailFast,
        StopFailureBehavior stopFailureBehavior = StopFailureBehavior.BestEffortContinue,
        bool allowManualRestart = true,
        int maxRestartAttempts = 3)
    {
        if (maxRestartAttempts < -1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxRestartAttempts),
                "Max restart attempts must be -1 for unlimited, 0 to disable, or a positive number.");
        }

        StartupFailureBehavior = startupFailureBehavior;
        StopFailureBehavior = stopFailureBehavior;
        AllowManualRestart = allowManualRestart;
        MaxRestartAttempts = maxRestartAttempts;
    }

    /// <summary>
    /// Gets how startup failures are handled.
    /// </summary>
    public StartupFailureBehavior StartupFailureBehavior { get; }

    /// <summary>
    /// Gets how stop failures are handled.
    /// </summary>
    public StopFailureBehavior StopFailureBehavior { get; }

    /// <summary>
    /// Gets a value indicating whether manual restart is allowed after supported failures.
    /// </summary>
    public bool AllowManualRestart { get; }

    /// <summary>
    /// Gets the maximum number of manual restarts.
    /// </summary>
    public int MaxRestartAttempts { get; }

    /// <summary>
    /// Gets a value indicating whether this policy differs from <see cref="Default" />.
    /// </summary>
    public bool HasValues =>
        StartupFailureBehavior != Default.StartupFailureBehavior ||
        StopFailureBehavior != Default.StopFailureBehavior ||
        AllowManualRestart != Default.AllowManualRestart ||
        MaxRestartAttempts != Default.MaxRestartAttempts;

    /// <summary>
    /// Reads the failure policy from configuration.
    /// </summary>
    /// <param name="configuration">The configuration source that contains the engine section.</param>
    /// <param name="sectionPath">The root configuration section path to read from.</param>
    /// <returns>The parsed failure policy.</returns>
    public static FailurePolicy FromConfiguration(
        IConfiguration configuration,
        string sectionPath = EngineSettings.SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration
            .GetSection(sectionPath)
            .GetSection("FailurePolicy");

        var startupFailureBehavior = ParseEnum(
            section["StartupFailureBehavior"],
            Default.StartupFailureBehavior);
        var stopFailureBehavior = ParseEnum(
            section["StopFailureBehavior"],
            Default.StopFailureBehavior);
        var allowManualRestart = TryParseBoolean(section["AllowManualRestart"], out var parsedRestart)
            ? parsedRestart
            : Default.AllowManualRestart;
        var maxRestartAttempts = TryParseInt32(section["MaxRestartAttempts"], out var parsedAttempts)
            ? parsedAttempts
            : Default.MaxRestartAttempts;

        return new FailurePolicy(
            startupFailureBehavior: startupFailureBehavior,
            stopFailureBehavior: stopFailureBehavior,
            allowManualRestart: allowManualRestart,
            maxRestartAttempts: maxRestartAttempts);
    }

    private static TEnum ParseEnum<TEnum>(string? value, TEnum fallback)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        return Enum.TryParse<TEnum>(value.Trim(), ignoreCase: true, out var parsed)
            ? parsed
            : fallback;
    }

    private static bool TryParseBoolean(string? value, out bool enabled)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            enabled = default;
            return false;
        }

        return bool.TryParse(value.Trim(), out enabled);
    }

    private static bool TryParseInt32(string? value, out int parsed)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            parsed = default;
            return false;
        }

        return int.TryParse(value.Trim(), out parsed);
    }
}
