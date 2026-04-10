namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes one logical database-migration target visible to the active Cephalon runtime.
/// </summary>
public sealed class DatabaseMigrationDescriptor
{
    /// <summary>
    /// Creates a new database-migration descriptor.
    /// </summary>
    /// <param name="id">The stable logical migration-target identifier.</param>
    /// <param name="displayName">The operator-facing migration-target name.</param>
    /// <param name="description">The human-readable migration-target description.</param>
    /// <param name="requestedRoleId">The logical database role requested by migration policy.</param>
    /// <param name="resolvedRoleId">The concrete database role that backs the target.</param>
    /// <param name="executionMode">The execution mode such as <c>startup-hosted-service</c> or <c>manual-or-deploy-time</c>.</param>
    /// <param name="status">The current execution status of the migration target.</param>
    /// <param name="applyOnStartup">Whether startup execution is enabled for this target.</param>
    /// <param name="exitAfterApply">Whether the host exits after startup execution completes.</param>
    /// <param name="provider">The effective provider identifier when known.</param>
    /// <param name="dbContextType">The DbContext type that can execute the target when known.</param>
    /// <param name="mechanism">The execution mechanism such as <c>migrate</c> or <c>ensure-created</c>.</param>
    /// <param name="startedAtUtc">The latest start time observed for this target.</param>
    /// <param name="completedAtUtc">The latest completion time observed for this target.</param>
    /// <param name="lastError">The latest error observed for this target.</param>
    /// <param name="commands">Optional operator-facing command templates for executing this target outside startup apply.</param>
    /// <param name="metadata">Optional operator-facing metadata associated with the migration target.</param>
    public DatabaseMigrationDescriptor(
        string id,
        string displayName,
        string description,
        string requestedRoleId,
        string resolvedRoleId,
        string executionMode,
        DatabaseMigrationStatus status,
        bool applyOnStartup,
        bool exitAfterApply,
        string? provider = null,
        string? dbContextType = null,
        string? mechanism = null,
        DateTimeOffset? startedAtUtc = null,
        DateTimeOffset? completedAtUtc = null,
        string? lastError = null,
        IReadOnlyList<DatabaseMigrationCommandDescriptor>? commands = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Database migration id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Database migration display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Database migration description is required.", nameof(description));
        }

        if (string.IsNullOrWhiteSpace(requestedRoleId))
        {
            throw new ArgumentException("Requested database role id is required.", nameof(requestedRoleId));
        }

        if (string.IsNullOrWhiteSpace(resolvedRoleId))
        {
            throw new ArgumentException("Resolved database role id is required.", nameof(resolvedRoleId));
        }

        if (string.IsNullOrWhiteSpace(executionMode))
        {
            throw new ArgumentException("Database migration execution mode is required.", nameof(executionMode));
        }

        Id = id.Trim();
        DisplayName = displayName.Trim();
        Description = description.Trim();
        RequestedRoleId = requestedRoleId.Trim();
        ResolvedRoleId = resolvedRoleId.Trim();
        ExecutionMode = executionMode.Trim();
        Status = status;
        ApplyOnStartup = applyOnStartup;
        ExitAfterApply = exitAfterApply;
        Provider = string.IsNullOrWhiteSpace(provider) ? null : provider.Trim();
        DbContextType = string.IsNullOrWhiteSpace(dbContextType) ? null : dbContextType.Trim();
        Mechanism = string.IsNullOrWhiteSpace(mechanism) ? null : mechanism.Trim();
        StartedAtUtc = startedAtUtc;
        CompletedAtUtc = completedAtUtc;
        LastError = string.IsNullOrWhiteSpace(lastError) ? null : lastError.Trim();
        Commands = commands?.ToArray() ?? [];
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the stable logical migration-target identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the operator-facing migration-target name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the human-readable migration-target description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the logical database role requested by migration policy.
    /// </summary>
    public string RequestedRoleId { get; }

    /// <summary>
    /// Gets the concrete database role that backs the target.
    /// </summary>
    public string ResolvedRoleId { get; }

    /// <summary>
    /// Gets the runtime execution mode for this target.
    /// </summary>
    public string ExecutionMode { get; }

    /// <summary>
    /// Gets the current execution status of the migration target.
    /// </summary>
    public DatabaseMigrationStatus Status { get; }

    /// <summary>
    /// Gets a value indicating whether startup execution is enabled for this target.
    /// </summary>
    public bool ApplyOnStartup { get; }

    /// <summary>
    /// Gets a value indicating whether the host exits after startup execution completes.
    /// </summary>
    public bool ExitAfterApply { get; }

    /// <summary>
    /// Gets the effective provider identifier when known.
    /// </summary>
    public string? Provider { get; }

    /// <summary>
    /// Gets the DbContext type that can execute the target when known.
    /// </summary>
    public string? DbContextType { get; }

    /// <summary>
    /// Gets the execution mechanism such as <c>migrate</c> or <c>ensure-created</c>.
    /// </summary>
    public string? Mechanism { get; }

    /// <summary>
    /// Gets the latest start time observed for this target.
    /// </summary>
    public DateTimeOffset? StartedAtUtc { get; }

    /// <summary>
    /// Gets the latest completion time observed for this target.
    /// </summary>
    public DateTimeOffset? CompletedAtUtc { get; }

    /// <summary>
    /// Gets the latest error observed for this target.
    /// </summary>
    public string? LastError { get; }

    /// <summary>
    /// Gets optional operator-facing command templates for executing this target outside startup apply.
    /// </summary>
    public IReadOnlyList<DatabaseMigrationCommandDescriptor> Commands { get; }

    /// <summary>
    /// Gets optional operator-facing metadata associated with the migration target.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }
}
