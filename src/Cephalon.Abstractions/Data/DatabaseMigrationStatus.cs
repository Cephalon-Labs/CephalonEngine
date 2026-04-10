namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes the current execution state of one logical database-migration target.
/// </summary>
public enum DatabaseMigrationStatus
{
    /// <summary>
    /// The migration target is known to the runtime but has not started executing yet.
    /// </summary>
    Planned = 0,

    /// <summary>
    /// The migration target is currently executing.
    /// </summary>
    Running = 1,

    /// <summary>
    /// The migration target completed successfully.
    /// </summary>
    Succeeded = 2,

    /// <summary>
    /// The migration target failed during execution.
    /// </summary>
    Failed = 3,

    /// <summary>
    /// The runtime cannot execute the configured migration target with the active provider-pack registrations.
    /// </summary>
    Unsupported = 4
}
