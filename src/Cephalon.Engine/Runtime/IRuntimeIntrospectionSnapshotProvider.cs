namespace Cephalon.Engine.Runtime;

/// <summary>
/// Creates operator-facing runtime introspection snapshots.
/// </summary>
public interface IRuntimeIntrospectionSnapshotProvider
{
    /// <summary>
    /// Creates a new runtime introspection snapshot from the current engine state.
    /// </summary>
    /// <returns>The composed runtime snapshot.</returns>
    RuntimeIntrospectionSnapshot CreateSnapshot();
}
