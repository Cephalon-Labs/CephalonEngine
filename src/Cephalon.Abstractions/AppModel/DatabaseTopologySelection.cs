using System.Text.Json.Serialization;

namespace Cephalon.Abstractions.AppModel;

/// <summary>
/// Describes the active database topology inputs resolved for a Cephalon app.
/// </summary>
public sealed class DatabaseTopologySelection
{
    /// <summary>
    /// Gets an empty database-topology selection instance.
    /// </summary>
    public static DatabaseTopologySelection Empty { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseTopologySelection" /> class.
    /// </summary>
    [JsonConstructor]
    public DatabaseTopologySelection(
        DatabaseRuntimeSelection? runtime = null,
        DatabaseTargetSelection? write = null,
        DatabaseTargetSelection? read = null,
        DatabaseTargetSelection? outbox = null,
        DatabaseTargetSelection? history = null,
        DatabaseMigrationsSelection? migrations = null)
    {
        Runtime = runtime ?? DatabaseRuntimeSelection.Empty;
        Write = write ?? DatabaseTargetSelection.Empty;
        Read = read ?? DatabaseTargetSelection.Empty;
        Outbox = outbox ?? DatabaseTargetSelection.Empty;
        History = history ?? DatabaseTargetSelection.Empty;
        Migrations = migrations ?? DatabaseMigrationsSelection.Empty;
    }

    /// <summary>
    /// Gets the shared runtime tuning selected for database roles.
    /// </summary>
    public DatabaseRuntimeSelection Runtime { get; }

    /// <summary>
    /// Gets the write-side database target selection.
    /// </summary>
    public DatabaseTargetSelection Write { get; }

    /// <summary>
    /// Gets the read-side database target selection.
    /// </summary>
    public DatabaseTargetSelection Read { get; }

    /// <summary>
    /// Gets the outbox database target selection.
    /// </summary>
    public DatabaseTargetSelection Outbox { get; }

    /// <summary>
    /// Gets the audit-history database target selection.
    /// </summary>
    public DatabaseTargetSelection History { get; }

    /// <summary>
    /// Gets the database-migration selection.
    /// </summary>
    public DatabaseMigrationsSelection Migrations { get; }

    /// <summary>
    /// Gets a value indicating whether any database-topology inputs were explicitly supplied.
    /// </summary>
    public bool HasValues =>
        Runtime.HasValues ||
        Write.HasValues ||
        Read.HasValues ||
        Outbox.HasValues ||
        History.HasValues ||
        Migrations.HasValues;
}
