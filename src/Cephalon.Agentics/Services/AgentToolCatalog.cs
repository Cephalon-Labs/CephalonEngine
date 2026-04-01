using Cephalon.Agentics.Configuration;

namespace Cephalon.Agentics.Services;

internal sealed class AgentToolCatalog : IAgentToolCatalog
{
    private readonly Dictionary<string, AgentToolDescriptor> index;

    public AgentToolCatalog(
        AgenticRuntimeOptions options,
        IEnumerable<IAgentToolContributor> contributors)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(contributors);

        var registry = new AgentToolRegistry();
        foreach (var tool in options.Tools)
        {
            registry.Add(tool);
        }

        foreach (var contributor in contributors)
        {
            contributor.RegisterTools(registry);
        }

        Tools = registry.Build();
        index = Tools.ToDictionary(static tool => tool.Id, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<AgentToolDescriptor> Tools { get; }

    public bool TryGet(string toolId, out AgentToolDescriptor tool)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toolId);

        return index.TryGetValue(toolId.Trim(), out tool!);
    }
}
