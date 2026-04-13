namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes one ordered operator step in the engine-owned database-migration playbook.
/// </summary>
public sealed class DatabaseMigrationOperationalStep
{
    /// <summary>
    /// Creates a new database-migration operational step.
    /// </summary>
    /// <param name="order">The positive playbook order for this step.</param>
    /// <param name="databaseMigrationId">The logical database-migration target identifier for this step.</param>
    /// <param name="requestedRoleId">The logical database role requested by migration policy.</param>
    /// <param name="resolvedRoleId">The concrete database role that backs this step.</param>
    /// <param name="status">The current execution status for this step.</param>
    /// <param name="executionMode">The execution mode such as <c>startup-hosted-service</c> or <c>manual-or-deploy-time</c>.</param>
    /// <param name="applyOnStartup">Whether startup execution is enabled for this step.</param>
    /// <param name="productionCommand">The primary production-recommended command selected for this step when available.</param>
    /// <param name="manualCommand">The primary direct or manual command selected for this step when available.</param>
    public DatabaseMigrationOperationalStep(
        int order,
        string databaseMigrationId,
        string requestedRoleId,
        string resolvedRoleId,
        DatabaseMigrationStatus status,
        string executionMode,
        bool applyOnStartup,
        DatabaseMigrationCommandDescriptor? productionCommand = null,
        DatabaseMigrationCommandDescriptor? manualCommand = null)
    {
        if (order <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(order),
                order,
                "Database-migration playbook order must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(databaseMigrationId))
        {
            throw new ArgumentException("Database-migration playbook step id is required.", nameof(databaseMigrationId));
        }

        if (string.IsNullOrWhiteSpace(requestedRoleId))
        {
            throw new ArgumentException("Requested role id is required.", nameof(requestedRoleId));
        }

        if (string.IsNullOrWhiteSpace(resolvedRoleId))
        {
            throw new ArgumentException("Resolved role id is required.", nameof(resolvedRoleId));
        }

        if (string.IsNullOrWhiteSpace(executionMode))
        {
            throw new ArgumentException("Execution mode is required.", nameof(executionMode));
        }

        Order = order;
        DatabaseMigrationId = databaseMigrationId.Trim();
        RequestedRoleId = requestedRoleId.Trim();
        ResolvedRoleId = resolvedRoleId.Trim();
        Status = status;
        ExecutionMode = executionMode.Trim();
        ApplyOnStartup = applyOnStartup;
        ProductionCommand = productionCommand;
        ManualCommand = manualCommand;
    }

    /// <summary>
    /// Gets the positive playbook order for this step.
    /// </summary>
    public int Order { get; }

    /// <summary>
    /// Gets the logical database-migration target identifier for this step.
    /// </summary>
    public string DatabaseMigrationId { get; }

    /// <summary>
    /// Gets the logical database role requested by migration policy.
    /// </summary>
    public string RequestedRoleId { get; }

    /// <summary>
    /// Gets the concrete database role that backs this step.
    /// </summary>
    public string ResolvedRoleId { get; }

    /// <summary>
    /// Gets the current execution status for this step.
    /// </summary>
    public DatabaseMigrationStatus Status { get; }

    /// <summary>
    /// Gets the execution mode for this step.
    /// </summary>
    public string ExecutionMode { get; }

    /// <summary>
    /// Gets a value indicating whether startup execution is enabled for this step.
    /// </summary>
    public bool ApplyOnStartup { get; }

    /// <summary>
    /// Gets a value indicating whether this step publishes a production-recommended command.
    /// </summary>
    public bool HasProductionRecommendedCommand => ProductionCommand is not null;

    /// <summary>
    /// Gets the primary production-recommended command selected for this step when available.
    /// </summary>
    public DatabaseMigrationCommandDescriptor? ProductionCommand { get; }

    /// <summary>
    /// Gets the primary direct or manual command selected for this step when available.
    /// </summary>
    public DatabaseMigrationCommandDescriptor? ManualCommand { get; }
}
