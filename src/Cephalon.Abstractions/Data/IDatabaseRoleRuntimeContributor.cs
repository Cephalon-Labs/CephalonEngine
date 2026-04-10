namespace Cephalon.Abstractions.Data;

/// <summary>
/// Contributes additive runtime state for one or more logical database roles.
/// </summary>
public interface IDatabaseRoleRuntimeContributor
{
    /// <summary>
    /// Describes the runtime state that should be merged into the active database-role catalog.
    /// </summary>
    /// <returns>The runtime descriptors that should enrich the active database-role catalog.</returns>
    IReadOnlyList<DatabaseRoleRuntimeDescriptor> DescribeDatabaseRoleRuntime();
}
