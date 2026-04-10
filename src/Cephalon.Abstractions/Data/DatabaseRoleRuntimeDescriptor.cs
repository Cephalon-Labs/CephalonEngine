using Cephalon.Abstractions.Health;

namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes additive runtime state projected for one logical database role.
/// </summary>
public sealed class DatabaseRoleRuntimeDescriptor
{
    /// <summary>
    /// Creates a new database-role runtime descriptor.
    /// </summary>
    /// <param name="databaseRoleId">The logical database-role identifier that this runtime state applies to.</param>
    /// <param name="healthState">The current runtime health state for the role, when known.</param>
    /// <param name="healthDescription">The operator-facing health description for the role, when known.</param>
    /// <param name="migrationState">The current migration execution state for the role, when known.</param>
    /// <param name="migrationDescription">The operator-facing migration description for the role, when known.</param>
    /// <param name="observedAtUtc">The UTC timestamp when this runtime state was last observed.</param>
    /// <param name="metadata">Optional runtime metadata associated with the role.</param>
    public DatabaseRoleRuntimeDescriptor(
        string databaseRoleId,
        HealthState? healthState = null,
        string? healthDescription = null,
        string? migrationState = null,
        string? migrationDescription = null,
        DateTimeOffset? observedAtUtc = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(databaseRoleId))
        {
            throw new ArgumentException("Database role id is required.", nameof(databaseRoleId));
        }

        DatabaseRoleId = databaseRoleId.Trim();
        HealthState = healthState;
        HealthDescription = string.IsNullOrWhiteSpace(healthDescription) ? null : healthDescription.Trim();
        MigrationState = string.IsNullOrWhiteSpace(migrationState) ? null : migrationState.Trim();
        MigrationDescription = string.IsNullOrWhiteSpace(migrationDescription) ? null : migrationDescription.Trim();
        ObservedAtUtc = observedAtUtc;
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the logical database-role identifier that this runtime state applies to.
    /// </summary>
    public string DatabaseRoleId { get; }

    /// <summary>
    /// Gets the current runtime health state for the role, when known.
    /// </summary>
    public HealthState? HealthState { get; }

    /// <summary>
    /// Gets the operator-facing health description for the role, when known.
    /// </summary>
    public string? HealthDescription { get; }

    /// <summary>
    /// Gets the current migration execution state for the role, when known.
    /// </summary>
    public string? MigrationState { get; }

    /// <summary>
    /// Gets the operator-facing migration description for the role, when known.
    /// </summary>
    public string? MigrationDescription { get; }

    /// <summary>
    /// Gets the UTC timestamp when this runtime state was last observed.
    /// </summary>
    public DateTimeOffset? ObservedAtUtc { get; }

    /// <summary>
    /// Gets optional runtime metadata associated with the role.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }
}
