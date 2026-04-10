using Microsoft.Extensions.Configuration;

namespace Cephalon.Engine.Configuration;

/// <summary>
/// Describes configuration-driven database migration behavior for a Cephalon app.
/// </summary>
public sealed class DatabaseMigrationsSettings
{
    /// <summary>
    /// Gets an empty database-migrations settings instance.
    /// </summary>
    public static DatabaseMigrationsSettings Empty { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseMigrationsSettings" /> class.
    /// </summary>
    public DatabaseMigrationsSettings(
        bool? applyOnStartup = null,
        bool? exitAfterApply = null,
        IReadOnlyList<string>? targets = null)
    {
        ApplyOnStartup = applyOnStartup;
        ExitAfterApply = exitAfterApply;
        Targets = targets?
            .Where(static target => !string.IsNullOrWhiteSpace(target))
            .Select(static target => target.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static target => target, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }

    /// <summary>
    /// Gets a value indicating whether migrations should be applied during host startup.
    /// </summary>
    public bool? ApplyOnStartup { get; }

    /// <summary>
    /// Gets a value indicating whether the host should exit after applying migrations.
    /// </summary>
    public bool? ExitAfterApply { get; }

    /// <summary>
    /// Gets the logical migration targets selected for the app.
    /// </summary>
    public IReadOnlyList<string> Targets { get; }

    /// <summary>
    /// Gets a value indicating whether any migration settings were explicitly supplied.
    /// </summary>
    public bool HasValues =>
        ApplyOnStartup.HasValue ||
        ExitAfterApply.HasValue ||
        Targets.Count > 0;

    /// <summary>
    /// Reads database-migration settings from the supplied configuration section.
    /// </summary>
    public static DatabaseMigrationsSettings FromSection(IConfigurationSection? section)
    {
        if (section is null || !section.Exists())
        {
            return Empty;
        }

        return new DatabaseMigrationsSettings(
            applyOnStartup: TryParseBoolean(section["ApplyOnStartup"]),
            exitAfterApply: TryParseBoolean(section["ExitAfterApply"]),
            targets: section.GetSection("Targets")
                .GetChildren()
                .Select(static child => child.Value)
                .Where(static value => !string.IsNullOrWhiteSpace(value))
                .Select(static value => value!)
                .ToArray());
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
}
