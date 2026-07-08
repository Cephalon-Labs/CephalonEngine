namespace Cephalon.Engine.Runtime;

/// <summary>
/// Contributes one versioned, operator-facing section to the combined runtime introspection snapshot.
/// </summary>
/// <remarks>
/// Companion packages can implement this contract to extend <see cref="RuntimeIntrospectionSnapshot" />
/// without adding package-specific properties to the engine-owned snapshot contract.
/// </remarks>
public interface IRuntimeIntrospectionSectionContributor
{
    /// <summary>
    /// Creates the current operator-facing section projection.
    /// </summary>
    /// <returns>The versioned section contributed to the runtime snapshot.</returns>
    RuntimeIntrospectionSection DescribeSection();
}
