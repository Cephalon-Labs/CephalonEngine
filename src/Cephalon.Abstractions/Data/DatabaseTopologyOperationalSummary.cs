namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes the aggregate operator-facing posture for the current database topology.
/// </summary>
public sealed class DatabaseTopologyOperationalSummary
{
    /// <summary>
    /// Creates a new database-topology operational summary.
    /// </summary>
    /// <param name="status">The aggregate topology status such as <c>Ready</c>, <c>Attention</c>, or <c>Blocked</c>.</param>
    /// <param name="headline">The operator-facing summary headline.</param>
    /// <param name="detail">The operator-facing summary detail.</param>
    /// <param name="actionLabel">The suggested operator action label.</param>
    /// <param name="actionPath">The suggested operator action path.</param>
    /// <param name="roleCount">The total number of configured logical database roles.</param>
    /// <param name="healthyRoleCount">The number of roles currently reporting healthy runtime state.</param>
    /// <param name="degradedRoleCount">The number of roles currently reporting degraded runtime state.</param>
    /// <param name="unhealthyRoleCount">The number of roles currently reporting unhealthy runtime state.</param>
    /// <param name="migrationTargetCount">The total number of visible logical migration targets.</param>
    /// <param name="succeededMigrationTargetCount">The number of migration targets currently reporting <c>Succeeded</c>.</param>
    /// <param name="failedMigrationTargetCount">The number of migration targets currently reporting <c>Failed</c>.</param>
    /// <param name="pendingMigrationTargetCount">The number of migration targets that are not yet <c>Succeeded</c>.</param>
    /// <param name="productionReadyMigrationTargetCount">The number of migration targets that publish production-recommended guidance.</param>
    public DatabaseTopologyOperationalSummary(
        string status,
        string headline,
        string detail,
        string actionLabel,
        string actionPath,
        int roleCount,
        int healthyRoleCount,
        int degradedRoleCount,
        int unhealthyRoleCount,
        int migrationTargetCount,
        int succeededMigrationTargetCount,
        int failedMigrationTargetCount,
        int pendingMigrationTargetCount,
        int productionReadyMigrationTargetCount)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            throw new ArgumentException("Database-topology status is required.", nameof(status));
        }

        if (string.IsNullOrWhiteSpace(headline))
        {
            throw new ArgumentException("Database-topology headline is required.", nameof(headline));
        }

        if (string.IsNullOrWhiteSpace(detail))
        {
            throw new ArgumentException("Database-topology detail is required.", nameof(detail));
        }

        if (string.IsNullOrWhiteSpace(actionLabel))
        {
            throw new ArgumentException("Database-topology action label is required.", nameof(actionLabel));
        }

        if (string.IsNullOrWhiteSpace(actionPath))
        {
            throw new ArgumentException("Database-topology action path is required.", nameof(actionPath));
        }

        ValidateNonNegative(roleCount, nameof(roleCount));
        ValidateNonNegative(healthyRoleCount, nameof(healthyRoleCount));
        ValidateNonNegative(degradedRoleCount, nameof(degradedRoleCount));
        ValidateNonNegative(unhealthyRoleCount, nameof(unhealthyRoleCount));
        ValidateNonNegative(migrationTargetCount, nameof(migrationTargetCount));
        ValidateNonNegative(succeededMigrationTargetCount, nameof(succeededMigrationTargetCount));
        ValidateNonNegative(failedMigrationTargetCount, nameof(failedMigrationTargetCount));
        ValidateNonNegative(pendingMigrationTargetCount, nameof(pendingMigrationTargetCount));
        ValidateNonNegative(productionReadyMigrationTargetCount, nameof(productionReadyMigrationTargetCount));

        Status = status.Trim();
        Headline = headline.Trim();
        Detail = detail.Trim();
        ActionLabel = actionLabel.Trim();
        ActionPath = actionPath.Trim();
        RoleCount = roleCount;
        HealthyRoleCount = healthyRoleCount;
        DegradedRoleCount = degradedRoleCount;
        UnhealthyRoleCount = unhealthyRoleCount;
        MigrationTargetCount = migrationTargetCount;
        SucceededMigrationTargetCount = succeededMigrationTargetCount;
        FailedMigrationTargetCount = failedMigrationTargetCount;
        PendingMigrationTargetCount = pendingMigrationTargetCount;
        ProductionReadyMigrationTargetCount = productionReadyMigrationTargetCount;
    }

    /// <summary>
    /// Gets the aggregate topology status.
    /// </summary>
    public string Status { get; }

    /// <summary>
    /// Gets the operator-facing summary headline.
    /// </summary>
    public string Headline { get; }

    /// <summary>
    /// Gets the operator-facing summary detail.
    /// </summary>
    public string Detail { get; }

    /// <summary>
    /// Gets the suggested operator action label.
    /// </summary>
    public string ActionLabel { get; }

    /// <summary>
    /// Gets the suggested operator action path.
    /// </summary>
    public string ActionPath { get; }

    /// <summary>
    /// Gets the total number of configured logical database roles.
    /// </summary>
    public int RoleCount { get; }

    /// <summary>
    /// Gets the number of roles currently reporting healthy runtime state.
    /// </summary>
    public int HealthyRoleCount { get; }

    /// <summary>
    /// Gets the number of roles currently reporting degraded runtime state.
    /// </summary>
    public int DegradedRoleCount { get; }

    /// <summary>
    /// Gets the number of roles currently reporting unhealthy runtime state.
    /// </summary>
    public int UnhealthyRoleCount { get; }

    /// <summary>
    /// Gets the total number of visible logical migration targets.
    /// </summary>
    public int MigrationTargetCount { get; }

    /// <summary>
    /// Gets the number of migration targets currently reporting <c>Succeeded</c>.
    /// </summary>
    public int SucceededMigrationTargetCount { get; }

    /// <summary>
    /// Gets the number of migration targets currently reporting <c>Failed</c>.
    /// </summary>
    public int FailedMigrationTargetCount { get; }

    /// <summary>
    /// Gets the number of migration targets that are not yet <c>Succeeded</c>.
    /// </summary>
    public int PendingMigrationTargetCount { get; }

    /// <summary>
    /// Gets the number of migration targets that publish production-recommended guidance.
    /// </summary>
    public int ProductionReadyMigrationTargetCount { get; }

    private static void ValidateNonNegative(int value, string paramName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(paramName, value, "Database-topology counts must be greater than or equal to 0.");
        }
    }
}
