namespace Cephalon.Agentics.Services;

/// <summary>
/// Exposes the merged set of tools available to the active agentic runtime.
/// </summary>
public interface IAgentToolCatalog
{
    /// <summary>
    /// Gets the effective tool set after host options and module contributors have both been applied.
    /// </summary>
    IReadOnlyList<AgentToolDescriptor> Tools { get; }

    /// <summary>
    /// Attempts to resolve a tool descriptor by identifier.
    /// </summary>
    /// <param name="toolId">The tool identifier to resolve.</param>
    /// <param name="tool">When this method returns, contains the resolved tool if found.</param>
    /// <returns><see langword="true" /> when the tool exists; otherwise <see langword="false" />.</returns>
    bool TryGet(string toolId, out AgentToolDescriptor tool);
}
