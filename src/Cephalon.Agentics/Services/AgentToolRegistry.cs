namespace Cephalon.Agentics.Services;

internal sealed class AgentToolRegistry : IAgentToolRegistry
{
    private readonly List<AgentToolDescriptor> tools = [];

    public void Add(AgentToolDescriptor tool)
    {
        ArgumentNullException.ThrowIfNull(tool);

        tools.Add(tool);
    }

    public IReadOnlyList<AgentToolDescriptor> Build()
    {
        return tools
            .GroupBy(static tool => tool.Id, StringComparer.OrdinalIgnoreCase)
            .Select(static group => group.First())
            .OrderBy(static tool => tool.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
