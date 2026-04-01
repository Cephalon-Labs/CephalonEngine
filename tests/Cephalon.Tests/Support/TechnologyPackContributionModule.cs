using Cephalon.Agentics.Services;
using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Modules;
using Cephalon.Edge.Services;
using Cephalon.Eventing.Services;
using Cephalon.Retrieval.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Support;

internal sealed class TechnologyPackContributionModule : ModuleBase
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "technology-pack-contributions",
        displayName: "Technology Pack Contributions",
        description: "Contributes pack-specific descriptors through DI so installed packs can merge them at runtime.",
        tags: ["technology", "contributions"],
        version: "1.0.0",
        metadata: new Dictionary<string, string>
        {
            ["layer"] = "application",
            ["surface"] = "technology-pack-contributions"
        });

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IAgentToolContributor, ContributedAgentToolContributor>();
        services.AddSingleton<IKnowledgeCollectionContributor, ContributedKnowledgeCollectionContributor>();
        services.AddSingleton<IEventChannelContributor, ContributedEventChannelContributor>();
        services.AddSingleton<IEdgeNodeContributor, ContributedEdgeNodeContributor>();
    }

    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
    }
}

internal sealed class ContributedAgentToolContributor : IAgentToolContributor
{
    public void RegisterTools(IAgentToolRegistry tools)
    {
        tools.Add(new AgentToolDescriptor(
            id: "analyst",
            displayName: "Analyst",
            description: "Analyzes runtime posture using a module-contributed agent tool.",
            tags: ["analysis", "module"]));
    }
}

internal sealed class ContributedKnowledgeCollectionContributor : IKnowledgeCollectionContributor
{
    public void RegisterCollections(IKnowledgeCollectionRegistry collections)
    {
        collections.Add(new KnowledgeCollectionDescriptor(
            id: "runbooks",
            displayName: "Runbooks",
            description: "Operational runbooks contributed by a module-level retrieval extension.",
            tags: ["operations", "module"]));
    }
}

internal sealed class ContributedEventChannelContributor : IEventChannelContributor
{
    public void RegisterChannels(IEventChannelRegistry channels)
    {
        channels.Add(new EventChannelDescriptor(
            id: "audit",
            displayName: "Audit",
            description: "Compliance and audit event stream contributed by a module-level extension.",
            tags: ["audit", "module"]));
    }
}

internal sealed class ContributedEdgeNodeContributor : IEdgeNodeContributor
{
    public void RegisterNodes(IEdgeNodeRegistry nodes)
    {
        nodes.Add(new EdgeNodeDescriptor(
            id: "warehouse-edge",
            displayName: "Warehouse Edge",
            description: "Offline-first warehouse node contributed by a module-level edge extension.",
            tags: ["warehouse", "module"]));
    }
}
