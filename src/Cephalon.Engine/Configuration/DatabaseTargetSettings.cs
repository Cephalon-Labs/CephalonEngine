using Microsoft.Extensions.Configuration;

namespace Cephalon.Engine.Configuration;

/// <summary>
/// Describes one configuration-driven database role target for a Cephalon app.
/// </summary>
public sealed class DatabaseTargetSettings
{
    /// <summary>
    /// Gets an empty database-target settings instance.
    /// </summary>
    public static DatabaseTargetSettings Empty { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseTargetSettings" /> class.
    /// </summary>
    public DatabaseTargetSettings(
        string? provider = null,
        string? connectionStringName = null,
        string? connectionString = null,
        string? useRole = null,
        string? schema = null,
        DatabaseRuntimeSettings? runtime = null)
    {
        Provider = Normalize(provider);
        ConnectionStringName = Normalize(connectionStringName);
        ConnectionString = Normalize(connectionString);
        UseRole = Normalize(useRole);
        Schema = Normalize(schema);
        Runtime = runtime ?? DatabaseRuntimeSettings.Empty;

        if (ConnectionStringName is not null && ConnectionString is not null)
        {
            throw new InvalidOperationException(
                "Database target settings must choose either ConnectionStringName or ConnectionString, but not both.");
        }

        if (UseRole is not null &&
            (Provider is not null || ConnectionStringName is not null || ConnectionString is not null))
        {
            throw new InvalidOperationException(
                "Database target settings must choose either UseRole or direct provider and connection settings, but not both.");
        }
    }

    /// <summary>
    /// Gets the selected logical provider identifier.
    /// </summary>
    public string? Provider { get; }

    /// <summary>
    /// Gets the selected root connection-string name.
    /// </summary>
    public string? ConnectionStringName { get; }

    /// <summary>
    /// Gets the selected inline connection string.
    /// </summary>
    public string? ConnectionString { get; }

    /// <summary>
    /// Gets the referenced concrete database role used to supply the physical connection target.
    /// </summary>
    public string? UseRole { get; }

    /// <summary>
    /// Gets the selected schema override.
    /// </summary>
    public string? Schema { get; }

    /// <summary>
    /// Gets the role-specific runtime overrides.
    /// </summary>
    public DatabaseRuntimeSettings Runtime { get; }

    /// <summary>
    /// Gets a value indicating whether any database-target settings were explicitly supplied.
    /// </summary>
    public bool HasValues =>
        Provider is not null ||
        ConnectionStringName is not null ||
        ConnectionString is not null ||
        UseRole is not null ||
        Schema is not null ||
        Runtime.HasValues;

    /// <summary>
    /// Reads database-target settings from the supplied configuration section.
    /// </summary>
    public static DatabaseTargetSettings FromSection(IConfigurationSection? section)
    {
        if (section is null || !section.Exists())
        {
            return Empty;
        }

        return new DatabaseTargetSettings(
            provider: section["Provider"],
            connectionStringName: section["ConnectionStringName"],
            connectionString: section["ConnectionString"],
            useRole: section["UseRole"],
            schema: section["Schema"],
            runtime: DatabaseRuntimeSettings.FromSection(section.GetSection("Runtime")));
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
