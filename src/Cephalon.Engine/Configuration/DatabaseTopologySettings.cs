using Microsoft.Extensions.Configuration;

namespace Cephalon.Engine.Configuration;

/// <summary>
/// Describes configuration-driven database topology for a Cephalon app.
/// </summary>
public sealed class DatabaseTopologySettings
{
    /// <summary>
    /// Gets an empty database-topology settings instance.
    /// </summary>
    public static DatabaseTopologySettings Empty { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseTopologySettings" /> class.
    /// </summary>
    public DatabaseTopologySettings(
        DatabaseRuntimeSettings? runtime = null,
        DatabaseTargetSettings? write = null,
        DatabaseTargetSettings? read = null,
        DatabaseTargetSettings? outbox = null,
        DatabaseTargetSettings? history = null,
        DatabaseMigrationsSettings? migrations = null)
    {
        Runtime = runtime ?? DatabaseRuntimeSettings.Empty;
        Write = write ?? DatabaseTargetSettings.Empty;
        Read = read ?? DatabaseTargetSettings.Empty;
        Outbox = outbox ?? DatabaseTargetSettings.Empty;
        History = history ?? DatabaseTargetSettings.Empty;
        Migrations = migrations ?? DatabaseMigrationsSettings.Empty;
    }

    /// <summary>
    /// Gets the shared database runtime tuning.
    /// </summary>
    public DatabaseRuntimeSettings Runtime { get; }

    /// <summary>
    /// Gets the write-side database target.
    /// </summary>
    public DatabaseTargetSettings Write { get; }

    /// <summary>
    /// Gets the read-side database target.
    /// </summary>
    public DatabaseTargetSettings Read { get; }

    /// <summary>
    /// Gets the outbox database target.
    /// </summary>
    public DatabaseTargetSettings Outbox { get; }

    /// <summary>
    /// Gets the audit-history database target.
    /// </summary>
    public DatabaseTargetSettings History { get; }

    /// <summary>
    /// Gets the database-migration settings for the active topology.
    /// </summary>
    public DatabaseMigrationsSettings Migrations { get; }

    /// <summary>
    /// Gets a value indicating whether any database-topology settings were explicitly supplied.
    /// </summary>
    public bool HasValues =>
        Runtime.HasValues ||
        Write.HasValues ||
        Read.HasValues ||
        Outbox.HasValues ||
        History.HasValues ||
        Migrations.HasValues;

    /// <summary>
    /// Reads database-topology settings from configuration.
    /// </summary>
    public static DatabaseTopologySettings FromConfiguration(
        IConfiguration configuration,
        string sectionPath = EngineSettings.SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration
            .GetSection(sectionPath)
            .GetSection("Databases");

        if (!section.Exists())
        {
            return Empty;
        }

        return new DatabaseTopologySettings(
            runtime: DatabaseRuntimeSettings.FromSection(section.GetSection("Runtime")),
            write: DatabaseTargetSettings.FromSection(section.GetSection("Write")),
            read: DatabaseTargetSettings.FromSection(section.GetSection("Read")),
            outbox: DatabaseTargetSettings.FromSection(section.GetSection("Outbox")),
            history: DatabaseTargetSettings.FromSection(section.GetSection("History")),
            migrations: DatabaseMigrationsSettings.FromSection(section.GetSection("Migrations")));
    }
}
