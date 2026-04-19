using Cephalon.Abstractions.Patterns;

namespace Cephalon.Engine.Configuration;

internal static class StranglerFigMigrationConventions
{
    private static readonly string[] SupportedProgressStates =
    [
        "not-started",
        "assessing",
        "validating",
        "cutover",
        "complete"
    ];

    public static StranglerFigTarget? ParseOptionalTarget(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "legacy" => StranglerFigTarget.Legacy,
            "modern" => StranglerFigTarget.Modern,
            _ => throw new InvalidOperationException(
                $"Strangler-fig migration target '{value}' is not supported. Expected 'legacy' or 'modern' for '{parameterName}'.")
        };
    }

    public static string? NormalizeOptionalProgressState(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim().ToLowerInvariant();
        if (!SupportedProgressStates.Contains(normalized, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Strangler-fig migration progress state '{value}' is not supported. Expected one of: {string.Join(", ", SupportedProgressStates)} for '{parameterName}'.");
        }

        return normalized;
    }

    public static int? ParseOptionalProgressPercent(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!int.TryParse(value.Trim(), out var parsed) || parsed is < 0 or > 100)
        {
            throw new InvalidOperationException(
                $"Strangler-fig migration progress percent '{value}' is not supported. Expected an integer between 0 and 100 for '{parameterName}'.");
        }

        return parsed;
    }

    public static string NormalizeProgressStateOrDefault(string? value)
    {
        return NormalizeOptionalProgressState(value, "ProgressState") ?? "not-started";
    }

    public static int NormalizeProgressPercentOrDefault(int? value)
    {
        if (value is < 0 or > 100)
        {
            throw new InvalidOperationException("Strangler-fig migration progress percent must be between 0 and 100.");
        }

        return value ?? 0;
    }
}
