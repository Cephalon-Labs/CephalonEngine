using Cephalon.Agentics.Services;

namespace Cephalon.Agentics.Configuration;

/// <summary>
/// Configures the built-in agentic runtime pack.
/// </summary>
/// <remarks>
/// These options seed the host-owned part of the agentic runtime. Installed modules can still
/// contribute additional tools through <see cref="Services.IAgentToolContributor" />.
/// </remarks>
public sealed class AgenticRuntimeOptions
{
    /// <summary>
    /// Gets the host-defined tool descriptors that should be available to the agentic runtime.
    /// </summary>
    public IList<AgentToolDescriptor> Tools { get; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether agent memory features are enabled.
    /// </summary>
    public bool EnableMemory { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether tool execution features are enabled.
    /// </summary>
    public bool EnableExecution { get; set; } = true;

    /// <summary>
    /// Gets arbitrary metadata that can be attached to the agentic runtime configuration.
    /// </summary>
    public IDictionary<string, string> Metadata { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
