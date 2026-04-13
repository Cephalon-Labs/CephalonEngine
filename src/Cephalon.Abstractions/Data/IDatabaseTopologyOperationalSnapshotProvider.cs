namespace Cephalon.Abstractions.Data;

/// <summary>
/// Creates the engine-owned operator-facing database-topology posture snapshot for the current runtime.
/// </summary>
public interface IDatabaseTopologyOperationalSnapshotProvider
{
    /// <summary>
    /// Creates the current database-topology posture snapshot.
    /// </summary>
    /// <returns>The current operator-facing database-topology posture snapshot.</returns>
    DatabaseTopologyOperationalSnapshot CreateSnapshot();
}
