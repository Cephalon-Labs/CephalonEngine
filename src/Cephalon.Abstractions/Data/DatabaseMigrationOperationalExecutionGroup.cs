namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes one engine-owned execution group in the database-migration playbook.
/// </summary>
public sealed class DatabaseMigrationOperationalExecutionGroup
{
    /// <summary>
    /// Creates a new database-migration execution group.
    /// </summary>
    /// <param name="order">The positive execution-group order in the playbook.</param>
    /// <param name="physicalTargetId">
    /// The stable physical-target identifier that anchors this execution group. When the runtime cannot resolve
    /// a physical database identity, the engine uses a logical fallback identifier instead of leaving the group anonymous.
    /// </param>
    /// <param name="physicalTargetDisplayName">The operator-facing description of the physical target that anchors this group.</param>
    /// <param name="status">The aggregate execution status across the logical migration targets in this group.</param>
    /// <param name="databaseMigrationIds">The logical migration targets that belong to this execution group.</param>
    /// <param name="requestedRoleIds">The logical requested role ids represented in this group.</param>
    /// <param name="resolvedRoleIds">The concrete resolved role ids represented in this group.</param>
    /// <param name="productionReadyTargetCount">The number of targets in this group that publish a production-recommended command.</param>
    /// <param name="manualPathTargetCount">The number of targets in this group that publish a direct or manual command path.</param>
    /// <param name="applyOnStartupTargetCount">The number of targets in this group that are configured for startup execution.</param>
    /// <param name="productionCommands">The selected production-recommended commands grouped for this physical-target batch.</param>
    /// <param name="manualCommands">The selected direct or manual commands grouped for this physical-target batch.</param>
    /// <param name="coordinationHint">The operator-facing coordination guidance for shared physical targets, when available.</param>
    public DatabaseMigrationOperationalExecutionGroup(
        int order,
        string physicalTargetId,
        string physicalTargetDisplayName,
        DatabaseMigrationStatus status,
        IReadOnlyList<string>? databaseMigrationIds = null,
        IReadOnlyList<string>? requestedRoleIds = null,
        IReadOnlyList<string>? resolvedRoleIds = null,
        int productionReadyTargetCount = 0,
        int manualPathTargetCount = 0,
        int applyOnStartupTargetCount = 0,
        IReadOnlyList<DatabaseMigrationOperationalExecutionGroupCommand>? productionCommands = null,
        IReadOnlyList<DatabaseMigrationOperationalExecutionGroupCommand>? manualCommands = null,
        string? coordinationHint = null)
    {
        if (order <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(order),
                order,
                "Database-migration execution-group order must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(physicalTargetId))
        {
            throw new ArgumentException("Physical target id is required.", nameof(physicalTargetId));
        }

        if (string.IsNullOrWhiteSpace(physicalTargetDisplayName))
        {
            throw new ArgumentException("Physical target display name is required.", nameof(physicalTargetDisplayName));
        }

        Order = order;
        PhysicalTargetId = physicalTargetId.Trim();
        PhysicalTargetDisplayName = physicalTargetDisplayName.Trim();
        Status = status;
        DatabaseMigrationIds = Normalize(databaseMigrationIds);
        RequestedRoleIds = Normalize(requestedRoleIds);
        ResolvedRoleIds = Normalize(resolvedRoleIds);
        TargetCount = DatabaseMigrationIds.Count;
        ProductionCommands = NormalizeCommands(productionCommands);
        ManualCommands = NormalizeCommands(manualCommands);
        var effectiveProductionReadyTargetCount = ProductionCommands.Count > 0
            ? ProductionCommands.Count
            : productionReadyTargetCount;
        var effectiveManualPathTargetCount = ManualCommands.Count > 0
            ? ManualCommands.Count
            : manualPathTargetCount;

        if (effectiveProductionReadyTargetCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(productionReadyTargetCount),
                effectiveProductionReadyTargetCount,
                "Production-ready target count cannot be negative.");
        }

        if (effectiveManualPathTargetCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(manualPathTargetCount),
                effectiveManualPathTargetCount,
                "Manual-path target count cannot be negative.");
        }

        if (applyOnStartupTargetCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(applyOnStartupTargetCount),
                applyOnStartupTargetCount,
                "Startup-apply target count cannot be negative.");
        }

        if (TargetCount > 0 && effectiveProductionReadyTargetCount > TargetCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(productionReadyTargetCount),
                effectiveProductionReadyTargetCount,
                "Production-ready target count cannot exceed the execution-group target count.");
        }

        if (TargetCount > 0 && effectiveManualPathTargetCount > TargetCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(manualPathTargetCount),
                effectiveManualPathTargetCount,
                "Manual-path target count cannot exceed the execution-group target count.");
        }

        if (TargetCount > 0 && applyOnStartupTargetCount > TargetCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(applyOnStartupTargetCount),
                applyOnStartupTargetCount,
                "Startup-apply target count cannot exceed the execution-group target count.");
        }

        if (ProductionCommands.Count > 0 && ProductionCommands.Count != effectiveProductionReadyTargetCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(productionReadyTargetCount),
                effectiveProductionReadyTargetCount,
                "Production-ready target count must match the selected production command count when commands are supplied.");
        }

        if (ManualCommands.Count > 0 && ManualCommands.Count != effectiveManualPathTargetCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(manualPathTargetCount),
                effectiveManualPathTargetCount,
                "Manual-path target count must match the selected manual command count when commands are supplied.");
        }

        ValidateCommandTargets(ProductionCommands, DatabaseMigrationIds, nameof(productionCommands));
        ValidateCommandTargets(ManualCommands, DatabaseMigrationIds, nameof(manualCommands));

        ProductionReadyTargetCount = effectiveProductionReadyTargetCount;
        ManualPathTargetCount = effectiveManualPathTargetCount;
        ApplyOnStartupTargetCount = applyOnStartupTargetCount;
        CoordinationHint = string.IsNullOrWhiteSpace(coordinationHint) ? null : coordinationHint.Trim();
    }

    /// <summary>
    /// Gets the positive execution-group order in the playbook.
    /// </summary>
    public int Order { get; }

    /// <summary>
    /// Gets the stable physical-target identifier that anchors this execution group.
    /// </summary>
    public string PhysicalTargetId { get; }

    /// <summary>
    /// Gets the operator-facing description of the physical target that anchors this group.
    /// </summary>
    public string PhysicalTargetDisplayName { get; }

    /// <summary>
    /// Gets the aggregate execution status across the logical migration targets in this group.
    /// </summary>
    public DatabaseMigrationStatus Status { get; }

    /// <summary>
    /// Gets the logical migration targets that belong to this execution group.
    /// </summary>
    public IReadOnlyList<string> DatabaseMigrationIds { get; }

    /// <summary>
    /// Gets the logical requested role ids represented in this group.
    /// </summary>
    public IReadOnlyList<string> RequestedRoleIds { get; }

    /// <summary>
    /// Gets the concrete resolved role ids represented in this group.
    /// </summary>
    public IReadOnlyList<string> ResolvedRoleIds { get; }

    /// <summary>
    /// Gets the number of logical migration targets represented in this group.
    /// </summary>
    public int TargetCount { get; }

    /// <summary>
    /// Gets the number of targets in this group that publish a production-recommended command.
    /// </summary>
    public int ProductionReadyTargetCount { get; }

    /// <summary>
    /// Gets the number of targets in this group that publish a direct or manual command path.
    /// </summary>
    public int ManualPathTargetCount { get; }

    /// <summary>
    /// Gets the selected production-recommended commands grouped for this physical-target batch.
    /// </summary>
    public IReadOnlyList<DatabaseMigrationOperationalExecutionGroupCommand> ProductionCommands { get; }

    /// <summary>
    /// Gets the selected direct or manual commands grouped for this physical-target batch.
    /// </summary>
    public IReadOnlyList<DatabaseMigrationOperationalExecutionGroupCommand> ManualCommands { get; }

    /// <summary>
    /// Gets the number of targets in this group that are configured for startup execution.
    /// </summary>
    public int ApplyOnStartupTargetCount { get; }

    /// <summary>
    /// Gets a value indicating whether every target in this group publishes a direct or manual command path.
    /// </summary>
    public bool HasManualCommandsForAllTargets => TargetCount > 0 && ManualPathTargetCount == TargetCount;

    /// <summary>
    /// Gets a value indicating whether this execution group spans multiple logical migration targets on one physical database target.
    /// </summary>
    public bool RequiresPhysicalTargetCoordination => TargetCount > 1;

    /// <summary>
    /// Gets the operator-facing coordination guidance for shared physical targets, when available.
    /// </summary>
    public string? CoordinationHint { get; }

    /// <summary>
    /// Gets a value indicating whether every target in this group publishes a production-recommended command.
    /// </summary>
    public bool HasProductionRecommendedCommandsForAllTargets => TargetCount > 0 && ProductionReadyTargetCount == TargetCount;

    private static string[] Normalize(IReadOnlyList<string>? values)
    {
        return values?
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }

    private static DatabaseMigrationOperationalExecutionGroupCommand[] NormalizeCommands(
        IReadOnlyList<DatabaseMigrationOperationalExecutionGroupCommand>? values)
    {
        return values?
            .Where(static value => value is not null)
            .OrderBy(static value => value.Order)
            .ThenBy(static value => value.DatabaseMigrationId, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }

    private static void ValidateCommandTargets(
        IReadOnlyList<DatabaseMigrationOperationalExecutionGroupCommand> commands,
        IReadOnlyList<string> databaseMigrationIds,
        string parameterName)
    {
        if (commands.Count == 0)
        {
            return;
        }

        var groupMigrationIds = new HashSet<string>(databaseMigrationIds, StringComparer.OrdinalIgnoreCase);
        var publishedCommandTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var command in commands)
        {
            if (!groupMigrationIds.Contains(command.DatabaseMigrationId))
            {
                throw new ArgumentException(
                    $"Execution-group command target '{command.DatabaseMigrationId}' is not part of the execution group.",
                    parameterName);
            }

            if (!publishedCommandTargets.Add(command.DatabaseMigrationId))
            {
                throw new ArgumentException(
                    $"Execution-group command target '{command.DatabaseMigrationId}' is duplicated in the same grouped command path.",
                    parameterName);
            }
        }
    }
}
