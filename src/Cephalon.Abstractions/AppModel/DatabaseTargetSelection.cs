using System.Text.Json.Serialization;

namespace Cephalon.Abstractions.AppModel;

/// <summary>
/// Describes one database role target resolved for a Cephalon app.
/// </summary>
public sealed class DatabaseTargetSelection
{
    /// <summary>
    /// Gets an empty database-target selection instance.
    /// </summary>
    public static DatabaseTargetSelection Empty { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseTargetSelection" /> class.
    /// </summary>
    [JsonConstructor]
    public DatabaseTargetSelection(
        string? provider = null,
        string? connectionStringName = null,
        string? connectionString = null,
        string? schema = null,
        DatabaseRuntimeSelection? runtime = null)
    {
        Provider = string.IsNullOrWhiteSpace(provider) ? null : provider.Trim();
        ConnectionStringName = string.IsNullOrWhiteSpace(connectionStringName) ? null : connectionStringName.Trim();
        ConnectionString = string.IsNullOrWhiteSpace(connectionString) ? null : connectionString.Trim();
        Schema = string.IsNullOrWhiteSpace(schema) ? null : schema.Trim();
        Runtime = runtime ?? DatabaseRuntimeSelection.Empty;

        if (ConnectionStringName is not null && ConnectionString is not null)
        {
            throw new InvalidOperationException(
                "Database target selection must choose either ConnectionStringName or ConnectionString, but not both.");
        }
    }

    /// <summary>
    /// Gets the selected logical provider identifier.
    /// </summary>
    public string? Provider { get; }

    /// <summary>
    /// Gets the root connection-string name selected for this database role.
    /// </summary>
    public string? ConnectionStringName { get; }

    /// <summary>
    /// Gets the inline connection string selected for this database role.
    /// </summary>
    public string? ConnectionString { get; }

    /// <summary>
    /// Gets the schema override selected for this database role.
    /// </summary>
    public string? Schema { get; }

    /// <summary>
    /// Gets the role-specific runtime overrides for this database target.
    /// </summary>
    public DatabaseRuntimeSelection Runtime { get; }

    /// <summary>
    /// Gets a value indicating whether any target-selection inputs were explicitly supplied.
    /// </summary>
    public bool HasValues =>
        Provider is not null ||
        ConnectionStringName is not null ||
        ConnectionString is not null ||
        Schema is not null ||
        Runtime.HasValues;
}
