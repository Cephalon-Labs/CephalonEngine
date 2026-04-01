using Microsoft.Extensions.Configuration;

namespace Cephalon.Engine.Configuration;

public sealed class FailurePolicy
{
    public static FailurePolicy Default { get; } = new();

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

    public StartupFailureBehavior StartupFailureBehavior { get; }

    public StopFailureBehavior StopFailureBehavior { get; }

    public bool AllowManualRestart { get; }

    public int MaxRestartAttempts { get; }

    public bool HasValues =>
        StartupFailureBehavior != Default.StartupFailureBehavior ||
        StopFailureBehavior != Default.StopFailureBehavior ||
        AllowManualRestart != Default.AllowManualRestart ||
        MaxRestartAttempts != Default.MaxRestartAttempts;

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
