using System.Text.Json.Serialization;

namespace Cephalon.Abstractions.AppModel;

/// <summary>
/// Describes the active database-migration inputs resolved for a Cephalon app.
/// </summary>
public sealed class DatabaseMigrationsSelection
{
    /// <summary>
    /// Gets an empty database-migrations selection instance.
    /// </summary>
    public static DatabaseMigrationsSelection Empty { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseMigrationsSelection" /> class.
    /// </summary>
    [JsonConstructor]
    public DatabaseMigrationsSelection(
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
    /// Gets a value indicating whether any migration-selection inputs were explicitly supplied.
    /// </summary>
    public bool HasValues =>
        ApplyOnStartup.HasValue ||
        ExitAfterApply.HasValue ||
        Targets.Count > 0;
}
