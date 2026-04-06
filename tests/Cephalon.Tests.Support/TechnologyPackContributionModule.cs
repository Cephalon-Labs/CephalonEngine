using Cephalon.Agentics.Services;
using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Execution;
using Cephalon.Abstractions.Modules;
using Cephalon.Edge.Services;
using Cephalon.Eventing.Services;
using Cephalon.Retrieval.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Support;

internal sealed class TechnologyPackContributionModule : ModuleBase, IExecutionGraphContributor, IHostedExecutionContributor
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
        services.AddSingleton<IEventSubscriptionContributor, ContributedEventSubscriptionContributor>();
        services.AddSingleton<IEdgeNodeContributor, ContributedEdgeNodeContributor>();
    }

    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
    }

    public void RegisterExecutionGraphs(IExecutionGraphRegistry graphs)
    {
        graphs.Add(new ExecutionGraphDescriptor(
            id: "audit-subscription-flow",
            displayName: "Audit Subscription Flow",
            description: "Projects audit integration events through the declared audit subscription pipeline.",
            sourceModuleId: Descriptor.Id,
            entryNodeId: "project-audit-event",
            nodes:
            [
                new ExecutionGraphNodeDescriptor(
                    id: "project-audit-event",
                    displayName: "Project Audit Event",
                    description: "Transforms the inbound audit event into the compliance read-model shape.",
                    kind: "activity",
                    moduleId: Descriptor.Id,
                    tags: ["audit", "projection"]),
                new ExecutionGraphNodeDescriptor(
                    id: "persist-audit-event",
                    displayName: "Persist Audit Event",
                    description: "Persists the projected audit event output.",
                    kind: "activity",
                    moduleId: Descriptor.Id,
                    tags: ["audit", "persistence"])
            ],
            edges:
            [
                new ExecutionGraphEdgeDescriptor(
                    fromNodeId: "project-audit-event",
                    toNodeId: "persist-audit-event",
                    displayName: "projected")
            ],
            tags: ["audit", "subscription"],
            metadata: new Dictionary<string, string>
            {
                ["eventSubscriptionId"] = "audit-projector",
                ["surface"] = "audit-subscription-flow"
            }));
    }

    public void RegisterHostedExecutions(IHostedExecutionRegistry hostedExecutions)
    {
        hostedExecutions.Add(new HostedExecutionDescriptor(
            id: "audit-projector-pump",
            displayName: "Audit Projector Pump",
            description: "Runs the declared audit subscription as host-managed background work.",
            sourceModuleId: Descriptor.Id,
            kind: "background-service",
            executionGraphId: "audit-subscription-flow",
            startsWithHost: true,
            tags: ["audit", "module", "subscription"],
            metadata: new Dictionary<string, string>
            {
                ["eventSubscriptionId"] = "audit-projector",
                ["surface"] = "audit-projector-pump"
            }));
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

internal sealed class ContributedEventSubscriptionContributor : IEventSubscriptionContributor
{
    public void RegisterSubscriptions(IEventSubscriptionRegistry subscriptions)
    {
        subscriptions.Add(new EventSubscriptionDescriptor(
            id: "audit-projector",
            displayName: "Audit Projector",
            description: "Projects audit events into a compliance read model.",
            channelId: "audit",
            handlerId: "compliance-audit-projector",
            deliveryMode: "background-service",
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

internal sealed class InvalidEventSubscriptionHostedExecutionModule : ModuleBase, IHostedExecutionContributor
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "invalid-event-subscription-hosted-execution",
        displayName: "Invalid Event Subscription Hosted Execution",
        description: "Publishes a hosted execution that references a missing event subscription.",
        tags: ["technology", "invalid"],
        version: "1.0.0");

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
    }

    public void RegisterHostedExecutions(IHostedExecutionRegistry hostedExecutions)
    {
        hostedExecutions.Add(new HostedExecutionDescriptor(
            id: "broken-subscription-pump",
            displayName: "Broken Subscription Pump",
            description: "References an event subscription that does not exist.",
            sourceModuleId: Descriptor.Id,
            kind: "background-service",
            metadata: new Dictionary<string, string>
            {
                ["eventSubscriptionId"] = "missing-subscription"
            }));
    }
}
