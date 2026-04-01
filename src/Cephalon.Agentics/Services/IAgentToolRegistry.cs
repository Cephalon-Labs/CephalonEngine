namespace Cephalon.Agentics.Services;

/// <summary>
/// Collects tool descriptors contributed to the active agentic runtime pack.
/// </summary>
public interface IAgentToolRegistry
{
    /// <summary>
    /// Adds a tool descriptor to the registry.
    /// </summary>
    /// <param name="tool">The tool descriptor to contribute.</param>
    void Add(AgentToolDescriptor tool);
}
