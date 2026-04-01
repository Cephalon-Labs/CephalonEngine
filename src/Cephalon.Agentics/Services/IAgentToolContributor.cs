namespace Cephalon.Agentics.Services;

/// <summary>
/// Allows a module to contribute tools into the active agentic runtime pack.
/// </summary>
public interface IAgentToolContributor
{
    /// <summary>
    /// Registers one or more tool descriptors with the supplied registry.
    /// </summary>
    /// <param name="tools">The registry that collects contributed tool descriptors.</param>
    void RegisterTools(IAgentToolRegistry tools);
}
