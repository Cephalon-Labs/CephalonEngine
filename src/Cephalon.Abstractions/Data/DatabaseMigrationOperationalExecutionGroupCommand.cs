namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes one selected command path for a logical migration target inside an engine-owned execution group.
/// </summary>
public sealed class DatabaseMigrationOperationalExecutionGroupCommand
{
    /// <summary>
    /// Creates a new execution-group command entry.
    /// </summary>
    /// <param name="order">The positive playbook order of the logical migration target that owns this command.</param>
    /// <param name="databaseMigrationId">The logical migration target identifier that owns this command.</param>
    /// <param name="requestedRoleId">The logical requested role id represented by this command.</param>
    /// <param name="resolvedRoleId">The concrete resolved role id represented by this command.</param>
    /// <param name="command">The selected command descriptor for this execution-group entry.</param>
    public DatabaseMigrationOperationalExecutionGroupCommand(
        int order,
        string databaseMigrationId,
        string requestedRoleId,
        string resolvedRoleId,
        DatabaseMigrationCommandDescriptor command)
    {
        if (order <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(order),
                order,
                "Execution-group command order must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(databaseMigrationId))
        {
            throw new ArgumentException("Database migration id is required.", nameof(databaseMigrationId));
        }

        if (string.IsNullOrWhiteSpace(requestedRoleId))
        {
            throw new ArgumentException("Requested role id is required.", nameof(requestedRoleId));
        }

        if (string.IsNullOrWhiteSpace(resolvedRoleId))
        {
            throw new ArgumentException("Resolved role id is required.", nameof(resolvedRoleId));
        }

        ArgumentNullException.ThrowIfNull(command);

        Order = order;
        DatabaseMigrationId = databaseMigrationId.Trim();
        RequestedRoleId = requestedRoleId.Trim();
        ResolvedRoleId = resolvedRoleId.Trim();
        Command = command;
    }

    /// <summary>
    /// Gets the positive playbook order of the logical migration target that owns this command.
    /// </summary>
    public int Order { get; }

    /// <summary>
    /// Gets the logical migration target identifier that owns this command.
    /// </summary>
    public string DatabaseMigrationId { get; }

    /// <summary>
    /// Gets the logical requested role id represented by this command.
    /// </summary>
    public string RequestedRoleId { get; }

    /// <summary>
    /// Gets the concrete resolved role id represented by this command.
    /// </summary>
    public string ResolvedRoleId { get; }

    /// <summary>
    /// Gets the selected command descriptor for this execution-group entry.
    /// </summary>
    public DatabaseMigrationCommandDescriptor Command { get; }
}
