using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Execution;
using Cephalon.Abstractions.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Support;

internal sealed class WorkflowCatalogTestModule : ModuleBase, IExecutionGraphContributor, IHostedExecutionContributor
{
    private readonly bool enabled;

    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "workflow-catalog",
        displayName: "Workflow Catalog",
        description: "Contributes execution-graph descriptors for orchestration testing.",
        tags: ["workflow", "orchestration"],
        version: "1.0.0",
        metadata: new Dictionary<string, string>
        {
            ["layer"] = "platform",
            ["surface"] = "execution-catalog"
        });

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public WorkflowCatalogTestModule(string scenario)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scenario);
        enabled = true;
    }

    public WorkflowCatalogTestModule()
    {
    }

    public override void ConfigureServices(IServiceCollection services)
    {
    }

    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
        if (!enabled)
        {
            return;
        }

        capabilities.Add(new Capability(
            key: "workflow.approval.request",
            displayName: "Approval request",
            description: "Creates a new approval request for downstream review."));
        capabilities.Add(new Capability(
            key: "workflow.approval.record",
            displayName: "Approval decision",
            description: "Records the reviewer decision for an approval request."));
    }

    public void RegisterExecutionGraphs(IExecutionGraphRegistry graphs)
    {
        if (!enabled)
        {
            return;
        }

        graphs.Add(new ExecutionGraphDescriptor(
            id: "approval-flow",
            displayName: "Approval Flow",
            description: "Routes a request through review, decision, and completion steps.",
            sourceModuleId: Descriptor.Id,
            entryNodeId: "request-review",
            nodes:
            [
                new ExecutionGraphNodeDescriptor(
                    id: "request-review",
                    displayName: "Request Review",
                    description: "Creates the approval request and hands it to a reviewer.",
                    kind: "activity",
                    moduleId: Descriptor.Id,
                    capabilityKey: "workflow.approval.request",
                    tags: ["entry", "request"]),
                new ExecutionGraphNodeDescriptor(
                    id: "record-decision",
                    displayName: "Record Decision",
                    description: "Captures the review outcome and routes the request.",
                    kind: "decision",
                    moduleId: Descriptor.Id,
                    capabilityKey: "workflow.approval.record",
                    tags: ["decision"]),
                new ExecutionGraphNodeDescriptor(
                    id: "complete",
                    displayName: "Complete",
                    description: "Marks the orchestration flow as complete.",
                    kind: "activity",
                    moduleId: Descriptor.Id,
                    tags: ["terminal"])
            ],
            edges:
            [
                new ExecutionGraphEdgeDescriptor(
                    fromNodeId: "request-review",
                    toNodeId: "record-decision",
                    displayName: "submitted"),
                new ExecutionGraphEdgeDescriptor(
                    fromNodeId: "record-decision",
                    toNodeId: "complete",
                    displayName: "approved",
                    condition: "decision == approved")
            ],
            tags: ["approval", "operator-visible"],
            metadata: new Dictionary<string, string>
            {
                ["surface"] = "approval-flow",
                ["owner"] = "workflow-catalog"
            }));
    }

    public void RegisterHostedExecutions(IHostedExecutionRegistry hostedExecutions)
    {
        if (!enabled)
        {
            return;
        }

        hostedExecutions.Add(new HostedExecutionDescriptor(
            id: "approval-pump",
            displayName: "Approval Pump",
            description: "Runs the approval flow as host-managed background work.",
            sourceModuleId: Descriptor.Id,
            kind: "background-service",
            executionGraphId: "approval-flow",
            startsWithHost: true,
            tags: ["background", "hosted"],
            metadata: new Dictionary<string, string>
            {
                ["owner"] = "workflow-catalog",
                ["surface"] = "approval-pump"
            }));
    }
}

internal sealed class InvalidWorkflowCapabilityModule : ModuleBase, IExecutionGraphContributor
{
    private readonly bool publishInvalidGraph;

    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "invalid-workflow",
        displayName: "Invalid Workflow",
        description: "Publishes an invalid execution graph for validation tests.",
        tags: ["workflow", "invalid"],
        version: "1.0.0");

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public InvalidWorkflowCapabilityModule(bool publishInvalidGraph)
    {
        this.publishInvalidGraph = publishInvalidGraph;
    }

    public InvalidWorkflowCapabilityModule()
    {
    }

    public override void ConfigureServices(IServiceCollection services)
    {
    }

    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
    }

    public void RegisterExecutionGraphs(IExecutionGraphRegistry graphs)
    {
        if (!publishInvalidGraph)
        {
            return;
        }

        graphs.Add(new ExecutionGraphDescriptor(
            id: "invalid-capability-flow",
            displayName: "Invalid Capability Flow",
            description: "References a capability that the runtime never published.",
            sourceModuleId: Descriptor.Id,
            entryNodeId: "missing-capability",
            nodes:
            [
                new ExecutionGraphNodeDescriptor(
                    id: "missing-capability",
                    displayName: "Missing Capability",
                    description: "References a capability that does not exist.",
                    kind: "activity",
                    moduleId: Descriptor.Id,
                    capabilityKey: "workflow.missing")
            ]));
    }
}

internal sealed class InvalidHostedExecutionModule : ModuleBase, IHostedExecutionContributor
{
    private readonly bool publishInvalidHostedExecution;

    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "invalid-hosted-execution",
        displayName: "Invalid Hosted Execution",
        description: "Publishes an invalid hosted execution for validation tests.",
        tags: ["workflow", "invalid", "hosted"],
        version: "1.0.0");

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public InvalidHostedExecutionModule(bool publishInvalidHostedExecution)
    {
        this.publishInvalidHostedExecution = publishInvalidHostedExecution;
    }

    public InvalidHostedExecutionModule()
    {
    }

    public override void ConfigureServices(IServiceCollection services)
    {
    }

    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
    }

    public void RegisterHostedExecutions(IHostedExecutionRegistry hostedExecutions)
    {
        if (!publishInvalidHostedExecution)
        {
            return;
        }

        hostedExecutions.Add(new HostedExecutionDescriptor(
            id: "invalid-pump",
            displayName: "Invalid Pump",
            description: "References an execution graph that does not exist.",
            sourceModuleId: Descriptor.Id,
            kind: "background-service",
            executionGraphId: "missing-graph"));
    }
}
