namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes the engine-owned ordered operator playbook for database migration targets.
/// </summary>
public sealed class DatabaseMigrationOperationalPlaybook
{
    /// <summary>
    /// Creates a new database-migration operational playbook.
    /// </summary>
    /// <param name="generatedAtUtc">The UTC timestamp when the playbook was created.</param>
    /// <param name="steps">The ordered operator steps derived from the current migration catalog.</param>
    public DatabaseMigrationOperationalPlaybook(
        DateTimeOffset generatedAtUtc,
        IReadOnlyList<DatabaseMigrationOperationalStep>? steps = null)
    {
        GeneratedAtUtc = generatedAtUtc;
        Steps = steps?.ToArray() ?? [];
        TargetCount = Steps.Count;
        ProductionReadyTargetCount = Steps.Count(static step => step.HasProductionRecommendedCommand);
        ManualPathTargetCount = Steps.Count(static step => step.ManualCommand is not null);
        ApplyOnStartupTargetCount = Steps.Count(static step => step.ApplyOnStartup);
        CoordinationRequiredTargetCount = Steps.Count(static step => step.RequiresPhysicalTargetCoordination);
    }

    /// <summary>
    /// Gets the UTC timestamp when the playbook was created.
    /// </summary>
    public DateTimeOffset GeneratedAtUtc { get; }

    /// <summary>
    /// Gets the total number of migration targets in the playbook.
    /// </summary>
    public int TargetCount { get; }

    /// <summary>
    /// Gets the number of targets that publish a production-recommended command.
    /// </summary>
    public int ProductionReadyTargetCount { get; }

    /// <summary>
    /// Gets the number of targets that publish a direct or manual command path.
    /// </summary>
    public int ManualPathTargetCount { get; }

    /// <summary>
    /// Gets the number of targets that are configured for startup execution.
    /// </summary>
    public int ApplyOnStartupTargetCount { get; }

    /// <summary>
    /// Gets the number of targets that share one physical database target with another migration target.
    /// </summary>
    public int CoordinationRequiredTargetCount { get; }

    /// <summary>
    /// Gets the ordered operator steps derived from the current migration catalog.
    /// </summary>
    public IReadOnlyList<DatabaseMigrationOperationalStep> Steps { get; }
}
