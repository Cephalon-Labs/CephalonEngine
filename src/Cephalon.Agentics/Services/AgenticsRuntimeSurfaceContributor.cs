using Cephalon.Abstractions.Technologies;

namespace Cephalon.Agentics.Services;

internal sealed class AgenticsRuntimeSurfaceContributor(IAgentToolCatalog catalog) : ITechnologyRuntimeContributor
{
    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        return new TechnologyRuntimeSurface(
            technologyId: "agentic-workloads",
            surfaceId: "agent-tools",
            displayName: "Agent Tools",
            description: "Registered agent tools available to the active agentic runtime.",
            entries: catalog.Tools
                .Select(tool => new TechnologyRuntimeEntry(
                    id: tool.Id,
                    displayName: tool.DisplayName,
                    description: tool.Description,
                    metadata: new Dictionary<string, string>
                    {
                        ["tags"] = string.Join(",", tool.Tags)
                    }))
                .ToArray());
    }
}
